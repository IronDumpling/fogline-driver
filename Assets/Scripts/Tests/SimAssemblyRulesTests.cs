using System;
using System.Linq;
using Fogline.Sim;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class SimAssemblyRulesTests
    {
        private static readonly string[] ForbiddenTypeNames =
        {
            "UnityEngine.Component",            // 含 MonoBehaviour
            "UnityEngine.GameObject",
            "UnityEngine.Time",
            "UnityEngine.Random",
            "UnityEngine.SceneManagement.SceneManager",
            "Fogline.Core.EventBus",
            "System.Random",                    // 非确定性来源，Sim 只能用 SeededRandom
            "System.DateTime",
            "System.Diagnostics.Stopwatch",
        };

        private static bool IsForbidden(Type t) =>
            ForbiddenTypeNames.Contains(t.FullName) ||
            (t.Namespace != null && (t.Namespace == "Fogline.Game" || t.Namespace.StartsWith("Fogline.Game."))); // ServiceManager 等

        private class UsesTime { public float Now() => UnityEngine.Time.time; }
        private class HoldsGameObject { public UnityEngine.GameObject Go; }
        private class IsMonoBehaviour : UnityEngine.MonoBehaviour { }
        private class UsesGenericMethodArg { public int Count() => System.Array.Empty<UnityEngine.GameObject>().Length; }
        // 用数组保证编译器保留这个仅被读取长度的局部变量
        private class UsesLocalOnly { public int Count() { var arr = new UnityEngine.GameObject[0]; return arr.Length; } }
        private class UsesTypeof { public Type Get() => typeof(UnityEngine.Time); }
        private class UsesRandom { public float Roll() => UnityEngine.Random.value; }
        private class UsesSystemRandom { public int Roll() => new System.Random().Next(); }
        private class UsesDateTimeNow { public long Now() => System.DateTime.Now.Ticks; }
        private class UsesEventBus { public object Get() => Fogline.Core.EventBus.Instance; }
        private class CleanMath { public float Length(UnityEngine.Vector2 v) => v.magnitude; }

        [TestCase(typeof(UsesTime))]
        [TestCase(typeof(HoldsGameObject))]
        [TestCase(typeof(IsMonoBehaviour))]
        [TestCase(typeof(UsesGenericMethodArg))]
        [TestCase(typeof(UsesLocalOnly))]
        [TestCase(typeof(UsesTypeof))]
        [TestCase(typeof(UsesRandom))]
        [TestCase(typeof(UsesSystemRandom))]
        [TestCase(typeof(UsesDateTimeNow))]
        [TestCase(typeof(UsesEventBus))]
        public void Scanner_FlagsForbiddenUsages(Type fixture)
        {
            Assert.IsNotEmpty(ForbiddenApiScanner.Scan(new[] { fixture }, IsForbidden), fixture.Name);
        }

        [Test]
        public void Scanner_AllowsUnityMath()
        {
            Assert.IsEmpty(ForbiddenApiScanner.Scan(new[] { typeof(CleanMath) }, IsForbidden));
        }

        [Test]
        public void SimAssembly_UsesNoForbiddenApis()
        {
            var hits = ForbiddenApiScanner.Scan(typeof(SimClock).Assembly.GetTypes(), IsForbidden);
            Assert.IsEmpty(hits, string.Join("\n", hits));
        }
    }
}
