using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Fogline.Tests
{
    /// <summary>
    /// 扫描类型的基类、字段、方法签名和方法体 IL，找出对禁用类型的引用。
    /// 返回每处命中的描述；为空表示干净。
    /// </summary>
    public static class ForbiddenApiScanner
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<short, OpCode> OpCodesByValue =
            typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => (OpCode)f.GetValue(null))
                .GroupBy(o => o.Value)
                .ToDictionary(g => g.Key, g => g.First());

        public static List<string> Scan(IEnumerable<Type> types, Func<Type, bool> isForbidden)
        {
            var hits = new List<string>();
            foreach (var type in types)
            {
                void Check(Type referenced, string where)
                {
                    if (referenced != null && Refers(referenced, isForbidden))
                        hits.Add($"{type.FullName}: {where} -> {referenced.FullName}");
                }

                Check(type.BaseType, "base type");
                foreach (var field in type.GetFields(All)) Check(field.FieldType, $"field {field.Name}");
                var methods = type.GetMethods(All).Cast<MethodBase>().Concat(type.GetConstructors(All));
                foreach (var method in methods)
                {
                    if (method is MethodInfo info) Check(info.ReturnType, $"{method.Name} return");
                    foreach (var p in method.GetParameters()) Check(p.ParameterType, $"{method.Name} param {p.Name}");
                    foreach (var t in BodyReferences(method)) Check(t, $"{method.Name} body");
                }
            }
            return hits;
        }

        // 类型本身、元素类型、泛型参数、基类链，任何一个被禁止就算命中
        private static bool Refers(Type t, Func<Type, bool> isForbidden)
        {
            if (t.IsGenericParameter) return false;
            if (t.HasElementType) return Refers(t.GetElementType(), isForbidden);
            if (t.IsGenericType && t.GetGenericArguments().Any(a => Refers(a, isForbidden))) return true;
            for (var b = t; b != null; b = b.BaseType)
                if (isForbidden(b)) return true;
            return false;
        }

        private static IEnumerable<Type> BodyReferences(MethodBase method)
        {
            var il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null) yield break;
            var typeArgs = method.DeclaringType != null && method.DeclaringType.IsGenericType
                ? method.DeclaringType.GetGenericArguments() : null;
            var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;

            int i = 0;
            while (i < il.Length)
            {
                short value = il[i++];
                if (value == 0xFE) value = unchecked((short)(0xFE00 | il[i++]));
                var op = OpCodesByValue[value];
                switch (op.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: i += 1; break;
                    case OperandType.InlineVar: i += 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: i += 8; break;
                    case OperandType.InlineSwitch: i += 4 + 4 * BitConverter.ToInt32(il, i); break;
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        var member = Resolve(method.Module, BitConverter.ToInt32(il, i), typeArgs, methodArgs);
                        i += 4;
                        if (member is Type t) yield return t;
                        else if (member != null) yield return member.DeclaringType;
                        break;
                    default: i += 4; break; // InlineBrTarget、InlineI、InlineSig、InlineString、ShortInlineR
                }
            }
        }

        private static MemberInfo Resolve(Module module, int token, Type[] typeArgs, Type[] methodArgs)
        {
            try { return module.ResolveMember(token, typeArgs, methodArgs); }
            catch (Exception) { return null; }
        }
    }
}
