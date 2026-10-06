# 雾航司机 脚本架构骨架 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> 执行第一步：把本文件复制到 `docs/superpowers/plans/2026-10-06-architecture-skeleton.md` 并提交（计划模式下只能写在这里）。

**Goal:** 按架构设计落地六层 asmdef、Sim 的四个领域与根目录四个文件、固定步长驱动、快照与试玩日志、程序集约束测试，让 mvp 第 10 节的九步可以直接往里加规则。

**Architecture:** Core（平铺，通用工具）← Sim（纯模拟，`SimWorld.Tick(dt, 指令)` 按固定顺序推进 Sea/Network/Train/Chart，`Clone()` 拍快照）← Game（`SimStepper` 纯 C# 固定步长 + `SimRunner` MonoBehaviour 外壳 + `CommandLog` + `SnapshotStore`）← View。依赖方向由 asmdef 保证，Sim 的禁止项由一条扫描 IL 的 EditMode 测试保证。

**Tech Stack:** Unity 6000.3.17f1、C# 9、Unity Test Framework 1.6（NUnit, EditMode）、JsonUtility。

**Spec:** `docs/superpowers/specs/2026-10-06-architecture-design.md`（执行者两份都要读）

**范围说明：** 本计划只建骨架。各领域的具体规则（高度图、潮汐、受力、声纳、猎手……）、`ItemSO`、ServiceManager/GameManager 全局状态、输入映射、Bootstrap 与场景接线、View 的实际内容，都留给 mvp 各步各自的计划。骨架里唯一的真实规则是组合手柄级位（`ShiftController`），用来让确定性、快照、日志回放测试有东西可测。

## Global Constraints

- 六层 asmdef：`Fogline.Core`、`Fogline.Sim`、`Fogline.Game`、`Fogline.View`、`Fogline.Tests`、`Fogline.Editor`；依赖只能从上往下（spec §2 表格）。
- Sim 禁止：MonoBehaviour（任何 Component）、GameObject、`Time.*`、`SceneManager`、ServiceManager（任何 `Fogline.Game` 类型）、EventBus、`UnityEngine.Random`（spec §2、§8）。
- Sim 根目录只有四个文件：`SimWorld.cs`、`SimClock.cs`、`Commands.cs`、`ScenarioSO.cs`（spec §3）。
- 领域内部不开子目录；Core、Game、Tests、Editor 平铺；枚举和事件就近定义，不建 `Enums/`、`Events/`（spec §6.2）。
- 调参每个领域一个 SO，新参数只加字段或 `[Serializable]` 小节；SO 的 `.cs` 在所属领域，`.asset` 在 `Assets/Data/`（spec §5）。
- 运行时会变的东西复制进 `SimWorld`，绝不写回 SO（spec §5.2）。
- 固定步长初始值 20 Hz（spec §4）。
- 随机数只用 `SimWorld` 持有的带种子生成器（spec §8）。
- 命名空间：`Fogline.Core`、`Fogline.Sim`、`Fogline.Sim.Sea|Network|Train|Chart`、`Fogline.Game`、`Fogline.Tests`；Editor 程序集用 `Fogline.EditorTools`（避免 `Fogline.Editor` 命名空间遮住 `UnityEditor.Editor` 类）。
- 代码注释用中文，密度与现有 `Assets/Scripts/Core/*.cs` 一致。
- 现有插件（DOTween、Easy Save 3、Behavior Designer）在 `Assets/Plugins/` 下没有 asmdef，会编进 Assembly-CSharp-firstpass，**asmdef 程序集引用不到**。本计划不用它们；以后哪一层需要时，先给插件加 asmdef。
- **跑测试前必须关掉 Unity 编辑器**（批处理模式会因项目被占用而失败）。
- 每次批处理运行后，Unity 会为新文件/文件夹生成 `.meta`：提交时一并 `git add`，不要漏，也不要手写 `.meta`。
- 测试统一用 Task 1 建的 `tools/run-editmode-tests.ps1`；输出写在已被忽略的 `Logs/`。

## Review Focus

1. **一帧卡顿很久**（`realDt` = 10 秒）：`SimStepper` 一帧最多推进 `MaxStepsPerFrame` 步并丢弃积压，不会追帧螺旋。→ Task 5 测试 `Advance_CapsStepsAndDropsBacklog`。
2. **按键落在不足一步的帧里**：指令留在队列里，下一次推进时应用，不丢。→ Task 5 测试 `Advance_KeepsCommandsUntilAStepRuns`。
3. **同一快照恢复两次 / 恢复后继续玩**：快照本身不被改动，第二次恢复得到一样的世界。→ Task 5 测试 `Restore_TwiceGivesIndependentEqualWorlds`。
4. **极端的手柄增量**（`int.MaxValue`）：不溢出成反方向，仍夹在 [-6, 8]。→ Task 4 测试 `ShiftController_ExtremeDeltaDoesNotOverflow`。
5. **种子为 0**：xorshift 在全零状态下永远输出 0；生成器必须照常产生变化的序列。→ Task 1 测试 `ZeroSeed_StillProducesVaryingSequence`。

---

### Task 1: Core 平铺、Core/Tests 程序集、带种子的随机数

**Files:**
- Move: `Assets/Scripts/Core/EventBus/EventBus.cs(.meta)` → `Assets/Scripts/Core/`
- Move: `Assets/Scripts/Core/EventBus/EventSubscriber.cs(.meta)` → `Assets/Scripts/Core/`
- Move: `Assets/Scripts/Core/StateMachine/IState.cs(.meta)`、`StateMachine.cs(.meta)` → `Assets/Scripts/Core/`
- Delete: `Assets/Scripts/Core/EventBus.meta`、`Assets/Scripts/Core/StateMachine.meta`、`Assets/Scripts/Core/EventBus/Events.meta` 及这三个目录
- Create: `Assets/Scripts/Core/Fogline.Core.asmdef`
- Create: `Assets/Scripts/Core/SeededRandom.cs`
- Create: `Assets/Scripts/Tests/Fogline.Tests.asmdef`
- Create: `Assets/Scripts/Tests/SeededRandomTests.cs`
- Create: `tools/run-editmode-tests.ps1`

**Interfaces:**
- Produces: `Fogline.Core.SeededRandom`：`public ulong State;` `SeededRandom(ulong seed)`、`ulong NextULong()`、`float NextFloat()`（[0,1)）、`int Range(int minInclusive, int maxExclusive)`、`SeededRandom Clone()`。
- Produces: `tools/run-editmode-tests.ps1 [-Filter <name>]`，失败时退出码非 0。

- [ ] **Step 1: 平铺 Core 并删除空目录**

```powershell
git mv Assets/Scripts/Core/EventBus/EventBus.cs Assets/Scripts/Core/EventBus.cs
git mv Assets/Scripts/Core/EventBus/EventBus.cs.meta Assets/Scripts/Core/EventBus.cs.meta
git mv Assets/Scripts/Core/EventBus/EventSubscriber.cs Assets/Scripts/Core/EventSubscriber.cs
git mv Assets/Scripts/Core/EventBus/EventSubscriber.cs.meta Assets/Scripts/Core/EventSubscriber.cs.meta
git mv Assets/Scripts/Core/StateMachine/IState.cs Assets/Scripts/Core/IState.cs
git mv Assets/Scripts/Core/StateMachine/IState.cs.meta Assets/Scripts/Core/IState.cs.meta
git mv Assets/Scripts/Core/StateMachine/StateMachine.cs Assets/Scripts/Core/StateMachine.cs
git mv Assets/Scripts/Core/StateMachine/StateMachine.cs.meta Assets/Scripts/Core/StateMachine.cs.meta
git rm Assets/Scripts/Core/EventBus.meta Assets/Scripts/Core/StateMachine.meta Assets/Scripts/Core/EventBus/Events.meta
Remove-Item -Recurse -Force Assets/Scripts/Core/EventBus, Assets/Scripts/Core/StateMachine
```

保留 `.cs.meta` 是为了 GUID 不变。

- [ ] **Step 2: 建 Core 与 Tests 的 asmdef**

`Assets/Scripts/Core/Fogline.Core.asmdef`：

```json
{
    "name": "Fogline.Core",
    "rootNamespace": "Fogline.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Scripts/Tests/Fogline.Tests.asmdef`（Sim、Game 的引用在后面的任务里再加，避免引用不存在的程序集）：

```json
{
    "name": "Fogline.Tests",
    "rootNamespace": "Fogline.Tests",
    "references": [
        "Fogline.Core",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: 建测试脚本 `tools/run-editmode-tests.ps1`**

```powershell
param([string]$Filter = "")
$ErrorActionPreference = "Stop"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe"
$results = "Logs/editmode-results.xml"
$log = "Logs/editmode.log"
New-Item -ItemType Directory -Force Logs | Out-Null
Remove-Item $results -ErrorAction SilentlyContinue
$unityArgs = @("-batchmode", "-nographics", "-projectPath", (Get-Location).Path,
    "-runTests", "-testPlatform", "EditMode", "-testResults", $results, "-logFile", $log)
if ($Filter) { $unityArgs += @("-testFilter", $Filter) }
& $unity @unityArgs | Out-Null
if (-not (Test-Path $results)) {
    Write-Host "没有测试结果。编译错误："
    Select-String -Path $log -Pattern "error CS\d+" | ForEach-Object { $_.Line }
    exit 1
}
[xml]$xml = Get-Content $results
$run = $xml.'test-run'
Write-Host "result=$($run.result) total=$($run.total) passed=$($run.passed) failed=$($run.failed)"
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
    Write-Host "FAILED $($case.fullname)"
    Write-Host $case.SelectSingleNode("failure/message").InnerText
}
if ($run.failed -ne "0") { exit 1 }
```

- [ ] **Step 4: 写失败的测试 `Assets/Scripts/Tests/SeededRandomTests.cs`**

```csharp
using System;
using System.Linq;
using Fogline.Core;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_GivesSameSequence()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void DifferentSeeds_GiveDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            Assert.AreNotEqual(a.NextULong(), b.NextULong());
        }

        [Test]
        public void ZeroSeed_StillProducesVaryingSequence()
        {
            var r = new SeededRandom(0);
            var values = Enumerable.Range(0, 10).Select(_ => r.NextULong()).ToArray();
            Assert.That(values, Has.None.EqualTo(0UL));
            Assert.That(values.Distinct().Count(), Is.EqualTo(10));
        }

        [Test]
        public void Clone_ContinuesSameSequenceIndependently()
        {
            var original = new SeededRandom(7);
            original.NextULong();
            var clone = original.Clone();
            var fromClone = clone.NextULong();
            Assert.AreEqual(fromClone, original.NextULong());
            clone.NextULong();
            Assert.AreNotEqual(clone.State, original.State);
        }

        [Test]
        public void NextFloat_IsInZeroToOne()
        {
            var r = new SeededRandom(3);
            for (int i = 0; i < 10000; i++)
            {
                var f = r.NextFloat();
                Assert.That(f, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void Range_StaysWithinBoundsAndHitsBothEnds()
        {
            var r = new SeededRandom(5);
            var seen = Enumerable.Range(0, 2000).Select(_ => r.Range(-2, 3)).Distinct().OrderBy(v => v).ToArray();
            Assert.That(seen, Is.EqualTo(new[] { -2, -1, 0, 1, 2 }));
        }

        [Test]
        public void Range_EmptyRangeThrows()
        {
            var r = new SeededRandom(5);
            Assert.Throws<ArgumentException>(() => r.Range(5, 5));
        }
    }
}
```

- [ ] **Step 5: 运行，确认失败**

Run: `powershell -File tools/run-editmode-tests.ps1 -Filter Fogline.Tests.SeededRandomTests`
Expected: 退出码 1，输出 `error CS0246: The type or namespace name 'SeededRandom' could not be found`。

- [ ] **Step 6: 实现 `Assets/Scripts/Core/SeededRandom.cs`**

```csharp
using System;
namespace Fogline.Core
{

    /// <summary>
    /// 带种子的确定性随机数（xorshift64*）。状态只有一个 ulong，复制即快照。
    /// 模拟里一律用它，不用 UnityEngine.Random。
    /// </summary>
    [Serializable]
    public class SeededRandom
    {
        // xorshift 在全零状态下只会输出 0，种子为 0 时换成固定的非零值
        private const ulong ZeroSeedReplacement = 0x9E3779B97F4A7C15UL;

        public ulong State;

        public SeededRandom(ulong seed) => State = seed == 0 ? ZeroSeedReplacement : seed;

        public ulong NextULong()
        {
            State ^= State >> 12;
            State ^= State << 25;
            State ^= State >> 27;
            return State * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>[0, 1)，取高 24 位。</summary>
        public float NextFloat() => (NextULong() >> 40) * (1f / (1 << 24));

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentException($"空区间 [{minInclusive}, {maxExclusive})");
            var span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % span));
        }

        public SeededRandom Clone() => (SeededRandom)MemberwiseClone();
    }
}
```

- [ ] **Step 7: 运行，确认通过**

Run: `powershell -File tools/run-editmode-tests.ps1 -Filter Fogline.Tests.SeededRandomTests`
Expected: `result=Passed total=7 passed=7 failed=0`

- [ ] **Step 8: 提交**

```powershell
git add -A Assets/Scripts tools
git commit -m "refactor: flatten Core, add Fogline.Core/Tests asmdefs and SeededRandom"
```

---

### Task 2: EventBus 转发未知静态类型的事件、过滤高频日志

**Files:**
- Modify: `Assets/Scripts/Core/EventBus.cs`（`Emit<T>` 与新方法）
- Create: `Assets/Scripts/Core/SilentEventAttribute.cs`
- Create: `Assets/Scripts/Tests/EventBusTests.cs`

**Interfaces:**
- Consumes: `EventBus.Instance`、`On<T>(Action<T>) : IDisposable`（已有）。
- Produces: `EventBus.EmitBoxed(object evt)`——按运行时类型分发给 `On<具体类型>` 的订阅者；`[SilentEvent]`——标在事件类型上，编辑器里不打 `[EventBus]` 日志。

- [ ] **Step 1: 写失败的测试 `Assets/Scripts/Tests/EventBusTests.cs`**

```csharp
using System;
using System.Collections.Generic;
using Fogline.Core;
using NUnit.Framework;
using UnityEngine;

namespace Fogline.Tests
{
    public class EventBusTests
    {
        [Serializable] private class LoudEvent { public int N; }
        [SilentEvent, Serializable] private class QuietEvent { public int N; }
        private class OtherEvent { }

        [Test]
        public void EmitBoxed_DispatchesByRuntimeType()
        {
            object boxed = new LoudEvent { N = 3 };
            int received = 0;
            using (EventBus.Instance.On<LoudEvent>(e => received = e.N))
                EventBus.Instance.EmitBoxed(boxed);
            Assert.AreEqual(3, received);
        }

        [Test]
        public void EmitBoxed_DoesNotReachOtherTypes()
        {
            bool called = false;
            using (EventBus.Instance.On<OtherEvent>(_ => called = true))
                EventBus.Instance.EmitBoxed(new LoudEvent());
            Assert.IsFalse(called);
        }

        [Test]
        public void EmitBoxed_NullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => EventBus.Instance.EmitBoxed(null));
        }

        [Test]
        public void EmitBoxed_HandlerExceptionSurfacesUnwrapped()
        {
            using (EventBus.Instance.On<LoudEvent>(_ => throw new InvalidOperationException("boom")))
                Assert.Throws<InvalidOperationException>(() => EventBus.Instance.EmitBoxed(new LoudEvent()));
        }

        [Test]
        public void SilentEvents_AreNotLogged_OthersAre()
        {
            var logged = Capture(() =>
            {
                EventBus.Instance.EmitBoxed(new QuietEvent());
                EventBus.Instance.Emit(new QuietEvent());
                EventBus.Instance.EmitBoxed(new LoudEvent());
            });
            Assert.That(logged, Has.None.Contains(nameof(QuietEvent)));
            Assert.That(logged, Has.Some.Contains(nameof(LoudEvent)));
        }

        private static List<string> Capture(Action action)
        {
            var logged = new List<string>();
            Application.LogCallback callback = (message, _, _) => logged.Add(message);
            Application.logMessageReceived += callback;
            try { action(); }
            finally { Application.logMessageReceived -= callback; }
            return logged;
        }
    }
}
```

- [ ] **Step 2: 运行，确认失败**

Run: `powershell -File tools/run-editmode-tests.ps1 -Filter Fogline.Tests.EventBusTests`
Expected: 编译错误，`'EventBus' does not contain a definition for 'EmitBoxed'` 与 `SilentEvent` 找不到。

- [ ] **Step 3: 新建 `Assets/Scripts/Core/SilentEventAttribute.cs`**

```csharp
using System;
namespace Fogline.Core
{

    /// <summary>
    /// 标记高频事件（例如模拟每步都会发的）：EventBus 在编辑器里不为它打日志，避免刷屏。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class SilentEventAttribute : Attribute { }
}
```

- [ ] **Step 4: 修改 `Assets/Scripts/Core/EventBus.cs`**

在文件顶部加 `using System.Reflection;` 与 `using System.Runtime.ExceptionServices;`。把现有 `Emit<T>` 整个替换为下面三个方法：

```csharp
        public void Emit<T>(T evt)
        {
            Log(typeof(T), evt);
            if (_handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(evt);
        }

        // 静态类型未知时发布（例如 SimRunner 转发的模拟事件）：按运行时类型分发，订阅方照常用 On<具体类型>
        public void EmitBoxed(object evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            var type = evt.GetType();
            Log(type, evt);
            if (!_handlers.TryGetValue(type, out var handler)) return;
            try { handler.DynamicInvoke(evt); }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void Log(Type type, object evt)
        {
            if (Attribute.IsDefined(type, typeof(SilentEventAttribute), false)) return;
            UnityEngine.Debug.Log($"[EventBus] {type.Name} {UnityEngine.JsonUtility.ToJson(evt)}");
        }
```

- [ ] **Step 5: 运行，确认通过**

Run: `powershell -File tools/run-editmode-tests.ps1 -Filter Fogline.Tests.EventBusTests`
Expected: `result=Passed total=5 passed=5 failed=0`

- [ ] **Step 6: 提交**

```powershell
git add -A Assets/Scripts
git commit -m "feat(core): EventBus.EmitBoxed and SilentEvent log filter"
```

---

### Task 3: Sim 程序集、SimClock、程序集禁止项扫描

**Files:**
- Create: `Assets/Scripts/Sim/Fogline.Sim.asmdef`
- Create: `Assets/Scripts/Sim/SimClock.cs`
- Modify: `Assets/Scripts/Tests/Fogline.Tests.asmdef`（references 加 `"Fogline.Sim"`）
- Create: `Assets/Scripts/Tests/ForbiddenApiScanner.cs`
- Create: `Assets/Scripts/Tests/SimAssemblyRulesTests.cs`
- Create: `Assets/Scripts/Tests/SimClockTests.cs`

**Interfaces:**
- Produces: `Fogline.Sim.SimClock`（`[Serializable]`）：`public double Seconds;`（游戏秒）、`public float TimeScale = 1f;`（游戏秒/现实秒）、`public long Step;`（已推进的步数）、`void Advance(float dt)`、`SimClock Clone()`。
- Produces: `Fogline.Tests.ForbiddenApiScanner.Scan(IEnumerable<Type> types, Func<Type, bool> isForbidden) : List<string>`。

- [ ] **Step 1: 建 `Assets/Scripts/Sim/Fogline.Sim.asmdef`**

```json
{
    "name": "Fogline.Sim",
    "rootNamespace": "Fogline.Sim",
    "references": ["Fogline.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

并在 `Fogline.Tests.asmdef` 的 `references` 里 `"Fogline.Core"` 之后加 `"Fogline.Sim"`。

- [ ] **Step 2: 写扫描器 `Assets/Scripts/Tests/ForbiddenApiScanner.cs`**

```csharp
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
```

- [ ] **Step 3: 写失败的测试 `Assets/Scripts/Tests/SimAssemblyRulesTests.cs` 与 `SimClockTests.cs`**

`SimAssemblyRulesTests.cs`：

```csharp
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
        };

        private static bool IsForbidden(Type t) =>
            ForbiddenTypeNames.Contains(t.FullName) ||
            (t.Namespace != null && t.Namespace.StartsWith("Fogline.Game")); // ServiceManager 等

        private class UsesTime { public float Now() => UnityEngine.Time.time; }
        private class HoldsGameObject { public UnityEngine.GameObject Go; }
        private class IsMonoBehaviour : UnityEngine.MonoBehaviour { }
        private class CleanMath { public float Length(UnityEngine.Vector2 v) => v.magnitude; }

        [Test]
        public void Scanner_FlagsForbiddenUsages()
        {
            Assert.IsNotEmpty(ForbiddenApiScanner.Scan(new[] { typeof(UsesTime) }, IsForbidden));
            Assert.IsNotEmpty(ForbiddenApiScanner.Scan(new[] { typeof(HoldsGameObject) }, IsForbidden));
            Assert.IsNotEmpty(ForbiddenApiScanner.Scan(new[] { typeof(IsMonoBehaviour) }, IsForbidden));
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
```

`SimClockTests.cs`：

```csharp
using Fogline.Sim;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class SimClockTests
    {
        [Test]
        public void Advance_ScalesRealSecondsAndCountsSteps()
        {
            var clock = new SimClock { Seconds = 100, TimeScale = 60f };
            clock.Advance(0.05f);
            Assert.AreEqual(103.0, clock.Seconds, 1e-6);
            Assert.AreEqual(1, clock.Step);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var clock = new SimClock { TimeScale = 1f };
            var clone = clock.Clone();
            clock.Advance(1f);
            Assert.AreEqual(0.0, clone.Seconds);
            Assert.AreEqual(0, clone.Step);
        }
    }
}
```

- [ ] **Step 4: 运行，确认失败**

Run: `powershell -File tools/run-editmode-tests.ps1`
Expected: 编译错误 `The type or namespace name 'SimClock' could not be found`。

- [ ] **Step 5: 实现 `Assets/Scripts/Sim/SimClock.cs`**

```csharp
using System;
namespace Fogline.Sim
{

    /// <summary>
    /// 游戏时间。每步由 SimWorld 推进一次：现实步长 × 时间比例 = 游戏秒。
    /// </summary>
    [Serializable]
    public class SimClock
    {
        public double Seconds;          // 游戏内秒
        public float  TimeScale = 1f;   // 游戏秒 / 现实秒
        public long   Step;             // 已推进的步数，试玩日志按它对齐指令

        public void Advance(float dt)
        {
            Seconds += dt * (double)TimeScale;
            Step++;
        }

        public SimClock Clone() => (SimClock)MemberwiseClone();
    }
}
```

- [ ] **Step 6: 运行，确认通过**

Run: `powershell -File tools/run-editmode-tests.ps1`
Expected: `result=Passed total=17 passed=17 failed=0`（Task 1–3 的全部测试）

- [ ] **Step 7: 提交**

```powershell
git add -A Assets/Scripts
git commit -m "feat(sim): Fogline.Sim assembly, SimClock and forbidden-API scan test"
```

---

### Task 4: Sim 四个领域、调参与关卡资产类型、指令、SimWorld

**Files:**
- Create: `Assets/Scripts/Sim/ScenarioSO.cs`、`Assets/Scripts/Sim/Commands.cs`、`Assets/Scripts/Sim/SimWorld.cs`
- Create: `Assets/Scripts/Sim/Sea/SeaState.cs`、`Assets/Scripts/Sim/Sea/SeaTuningSO.cs`
- Create: `Assets/Scripts/Sim/Network/NetworkState.cs`、`Assets/Scripts/Sim/Network/NetworkTuningSO.cs`
- Create: `Assets/Scripts/Sim/Train/TrainState.cs`、`Assets/Scripts/Sim/Train/TrainTuningSO.cs`
- Create: `Assets/Scripts/Sim/Chart/ChartState.cs`、`Assets/Scripts/Sim/Chart/ChartTuningSO.cs`
- Create: `Assets/Scripts/Tests/TestWorlds.cs`、`Assets/Scripts/Tests/SimWorldTests.cs`

**Interfaces:**
- Consumes: `SimClock`（Task 3）、`SeededRandom`（Task 1）。
- Produces:
  - `Fogline.Sim.ISimEvent`（标记接口）、`Fogline.Sim.ISimCommand`（标记接口）。
  - `Fogline.Sim.ShiftController : ISimCommand`（`[Serializable]`，`public int Delta;`，无参构造 + `ShiftController(int delta)`）。
  - `Fogline.Sim.SimTuning(SeaTuningSO sea, NetworkTuningSO network, TrainTuningSO train, ChartTuningSO chart)`，只读字段 `Sea/Network/Train/Chart`。
  - `Fogline.Sim.SimWorld`：`static SimWorld Create(ScenarioSO scenario, SimTuning tuning, ulong seed)`；属性 `Scenario`、`Tuning`、`Clock`、`Random`、`Sea`、`Network`、`Train`、`Chart`；`IReadOnlyList<ISimEvent> Tick(float dt, IReadOnlyList<ISimCommand> commands)`；`SimWorld Clone()`。
  - `ScenarioSO.StartSeconds`（float）、`SeaTuningSO.World.TimeScale`（float）、`TrainTuningSO.Controller.BrakeNotches/PowerNotches`（int，6/8）。
  - `TrainState.ControllerNotch`（int，负数制动、正数牵引）、`TrainState.ShiftController(int delta, TrainTuningSO tuning)`。
  - `Fogline.Tests.TestWorlds.Create(ulong seed = 1)`、`TestWorlds.Digest(SimWorld)`（Task 5 也用）。

- [ ] **Step 1: 写测试辅助 `Assets/Scripts/Tests/TestWorlds.cs`**

```csharp
using System.Globalization;
using Fogline.Sim;
using Fogline.Sim.Chart;
using Fogline.Sim.Network;
using Fogline.Sim.Sea;
using Fogline.Sim.Train;
using UnityEngine;

namespace Fogline.Tests
{
    /// <summary>不依赖 Data 资产的测试世界，以及用来比较两个世界是否完全相同的摘要。</summary>
    public static class TestWorlds
    {
        public static SimTuning Tuning() => new SimTuning(
            ScriptableObject.CreateInstance<SeaTuningSO>(),
            ScriptableObject.CreateInstance<NetworkTuningSO>(),
            ScriptableObject.CreateInstance<TrainTuningSO>(),
            ScriptableObject.CreateInstance<ChartTuningSO>());

        public static SimWorld Create(ulong seed = 1) =>
            SimWorld.Create(ScriptableObject.CreateInstance<ScenarioSO>(), Tuning(), seed);

        public static string Digest(SimWorld w) => string.Join("|",
            w.Clock.Seconds.ToString("R", CultureInfo.InvariantCulture),
            w.Clock.Step.ToString(CultureInfo.InvariantCulture),
            w.Random.State.ToString(CultureInfo.InvariantCulture),
            JsonUtility.ToJson(w.Sea),
            JsonUtility.ToJson(w.Network),
            JsonUtility.ToJson(w.Train),
            JsonUtility.ToJson(w.Chart));
    }
}
```

- [ ] **Step 2: 写失败的测试 `Assets/Scripts/Tests/SimWorldTests.cs`**

```csharp
using System;
using System.Collections.Generic;
using Fogline.Core;
using Fogline.Sim;
using NUnit.Framework;
using UnityEngine;

namespace Fogline.Tests
{
    public class SimWorldTests
    {
        private const float Dt = 0.05f;
        private static readonly ISimCommand[] None = Array.Empty<ISimCommand>();

        private class UnknownCommand : ISimCommand { }

        [Test]
        public void Create_TakesStartTimeFromScenarioAndTimeScaleFromTuning()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioSO>();
            scenario.StartSeconds = 3600f;
            var tuning = TestWorlds.Tuning();
            tuning.Sea.World.TimeScale = 60f;

            var world = SimWorld.Create(scenario, tuning, 1);

            Assert.AreEqual(3600.0, world.Clock.Seconds);
            Assert.AreEqual(60f, world.Clock.TimeScale);
        }

        [Test]
        public void Create_NullArgumentsThrow()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioSO>();
            Assert.Throws<ArgumentNullException>(() => SimWorld.Create(null, TestWorlds.Tuning(), 1));
            Assert.Throws<ArgumentNullException>(() => SimWorld.Create(scenario, null, 1));
            Assert.Throws<ArgumentNullException>(() => new SimTuning(null, null, null, null));
        }

        [Test]
        public void Tick_AdvancesClockOnce()
        {
            var world = TestWorlds.Create();
            world.Clock.TimeScale = 60f;
            world.Tick(Dt, None);
            Assert.AreEqual(3.0, world.Clock.Seconds, 1e-6);
            Assert.AreEqual(1, world.Clock.Step);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void Tick_RejectsNonPositiveDt(float dt)
        {
            var world = TestWorlds.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => world.Tick(dt, None));
        }

        [Test]
        public void Tick_NullCommandListMeansNoCommands()
        {
            var world = TestWorlds.Create();
            Assert.DoesNotThrow(() => world.Tick(Dt, null));
        }

        [Test]
        public void Tick_UnknownOrNullCommandThrows()
        {
            var world = TestWorlds.Create();
            Assert.Throws<NotSupportedException>(() => world.Tick(Dt, new ISimCommand[] { new UnknownCommand() }));
            Assert.Throws<ArgumentNullException>(() => world.Tick(Dt, new ISimCommand[] { null }));
        }

        [Test]
        public void ShiftController_MovesOneNotchAndClampsToTuning()
        {
            var world = TestWorlds.Create();
            world.Tick(Dt, new ISimCommand[] { new ShiftController(+1), new ShiftController(+1) });
            Assert.AreEqual(2, world.Train.ControllerNotch);

            world.Tick(Dt, new ISimCommand[] { new ShiftController(+20) });
            Assert.AreEqual(8, world.Train.ControllerNotch);

            world.Tick(Dt, new ISimCommand[] { new ShiftController(-30) });
            Assert.AreEqual(-6, world.Train.ControllerNotch);
        }

        [Test]
        public void ShiftController_ExtremeDeltaDoesNotOverflow()
        {
            var world = TestWorlds.Create();
            world.Tick(Dt, new ISimCommand[] { new ShiftController(+3), new ShiftController(int.MaxValue) });
            Assert.AreEqual(8, world.Train.ControllerNotch);
            world.Tick(Dt, new ISimCommand[] { new ShiftController(-3), new ShiftController(int.MinValue) });
            Assert.AreEqual(-6, world.Train.ControllerNotch);
        }

        [Test]
        public void Clone_IsIndependentAndReplaysIdentically()
        {
            var world = TestWorlds.Create(9);
            world.Tick(Dt, new ISimCommand[] { new ShiftController(+2) });
            var clone = world.Clone();
            var cloneDigest = TestWorlds.Digest(clone);

            var commands = new ISimCommand[] { new ShiftController(+3) };
            world.Tick(Dt, commands);
            Assert.AreEqual(cloneDigest, TestWorlds.Digest(clone), "推进原世界不应改动快照");

            clone.Tick(Dt, commands);
            Assert.AreEqual(TestWorlds.Digest(world), TestWorlds.Digest(clone));
        }

        [Test]
        public void SameSeedAndCommands_GiveIdenticalWorlds()
        {
            var a = TestWorlds.Create(123);
            var b = TestWorlds.Create(123);
            var script = new SeededRandom(77);
            for (int step = 0; step < 500; step++)
            {
                var commands = new List<ISimCommand>();
                if (script.NextFloat() < 0.3f) commands.Add(new ShiftController(script.Range(-2, 3)));
                a.Tick(Dt, commands);
                b.Tick(Dt, commands);
            }
            Assert.AreEqual(TestWorlds.Digest(a), TestWorlds.Digest(b));
        }

        [Test]
        public void Tick_NeverWritesBackToAssets()
        {
            var world = TestWorlds.Create();
            world.Scenario.StartSeconds = 10f;
            world.Tuning.Sea.World.TimeScale = 2f;
            for (int i = 0; i < 50; i++) world.Tick(Dt, new ISimCommand[] { new ShiftController(+1) });
            Assert.AreEqual(10f, world.Scenario.StartSeconds);
            Assert.AreEqual(2f, world.Tuning.Sea.World.TimeScale);
            Assert.AreEqual(6, world.Tuning.Train.Controller.BrakeNotches);
            Assert.AreEqual(8, world.Tuning.Train.Controller.PowerNotches);
        }
    }
}
```

- [ ] **Step 3: 运行，确认失败**

Run: `powershell -File tools/run-editmode-tests.ps1 -Filter Fogline.Tests.SimWorldTests`
Expected: 编译错误，`SimWorld`、`ScenarioSO`、`SeaTuningSO` 等找不到。

- [ ] **Step 4: 写四个调参 SO 与 ScenarioSO**

`Assets/Scripts/Sim/Sea/SeaTuningSO.cs`：

```csharp
using System;
using UnityEngine;
namespace Fogline.Sim.Sea
{

    /// <summary>
    /// 雾海的全部调参：世界（雾面、潮汐、时间比例、雾压）、声音、猎手。
    /// 新参数加成字段或新的 [Serializable] 小节，不新建 SO 类型。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Sea", fileName = "SeaTuning")]
    public class SeaTuningSO : ScriptableObject
    {
        [Serializable]
        public class WorldSection
        {
            [Tooltip("游戏秒 / 现实秒")] [Min(0f)] public float TimeScale = 1f;
        }

        public WorldSection World = new();
    }
}
```

`Assets/Scripts/Sim/Network/NetworkTuningSO.cs`：

```csharp
using UnityEngine;
namespace Fogline.Sim.Network
{

    /// <summary>
    /// 线网的全部调参：段长、结构判定、造价、限速与噪音、缺陷。字段随 mvp 第 1、6 步加入。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Network", fileName = "NetworkTuning")]
    public class NetworkTuningSO : ScriptableObject { }
}
```

`Assets/Scripts/Sim/Train/TrainTuningSO.cs`：

```csharp
using System;
using UnityEngine;
namespace Fogline.Sim.Train
{

    /// <summary>
    /// 列车的全部调参：质量、牵引、制动、能源、耐压、车厢格子、发射器。
    /// 新参数加成字段或新的 [Serializable] 小节，不新建 SO 类型。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Train", fileName = "TrainTuning")]
    public class TrainTuningSO : ScriptableObject
    {
        [Serializable]
        public class ControllerSection
        {
            [Tooltip("组合手柄制动级数（v6.1：6）")] [Min(0)] public int BrakeNotches = 6;
            [Tooltip("组合手柄牵引级数（v6.1：8）")] [Min(0)] public int PowerNotches = 8;
        }

        public ControllerSection Controller = new();
    }
}
```

`Assets/Scripts/Sim/Chart/ChartTuningSO.cs`：

```csharp
using UnityEngine;
namespace Fogline.Sim.Chart
{

    /// <summary>
    /// 海图的全部调参：声纳、测向、标记褪色、广播覆盖、预测模糊度。字段随 mvp 第 2、3、7 步加入。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Chart", fileName = "ChartTuning")]
    public class ChartTuningSO : ScriptableObject { }
}
```

`Assets/Scripts/Sim/ScenarioSO.cs`：

```csharp
using UnityEngine;
namespace Fogline.Sim
{

    /// <summary>
    /// 一张关卡的全部内容：高度图、谷地水道、线网、缺陷、旧地图差异、站点、委托、猎手出生点、开局物品。
    /// 内容随 mvp 各步加入。运行时会变的东西复制进 SimWorld，绝不写回这里。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Scenario", fileName = "Scenario")]
    public class ScenarioSO : ScriptableObject
    {
        [Tooltip("开局时的游戏时间（秒），决定开局潮位")] [Min(0f)] public float StartSeconds;
    }
}
```

- [ ] **Step 5: 写四个领域的运行时状态**

`Assets/Scripts/Sim/Sea/SeaState.cs`：

```csharp
using System;
using System.Collections.Generic;
using Fogline.Core;
namespace Fogline.Sim.Sea
{

    /// <summary>
    /// 雾海的运行时状态：潮汐、潮流、声源、猎手，随 mvp 第 2–4 步加入。
    /// 加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class SeaState
    {
        // 推进第 2 步：潮汐与潮流（时间已由 SimWorld 推进）
        public void TickEnvironment(SimClock clock, SeaTuningSO tuning) { }

        // 推进第 4 步：猎手听、想、动；攻击、硬直等写进 events，由 SimWorld 应用
        public void TickHunters(float dt, SeededRandom random, List<ISimEvent> events) { }

        public SeaState Clone() => (SeaState)MemberwiseClone();
    }
}
```

`Assets/Scripts/Sim/Network/NetworkState.cs`：

```csharp
using System;
namespace Fogline.Sim.Network
{

    /// <summary>
    /// 线网的运行时状态：建设状态、已铺长度、道岔指向、站点配额、委托进度，随 mvp 第 1、5、6 步加入。
    /// 自己不推进，由 Train 的施工改写。加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class NetworkState
    {
        public NetworkState Clone() => (NetworkState)MemberwiseClone();
    }
}
```

`Assets/Scripts/Sim/Train/TrainState.cs`：

```csharp
using System;
using Fogline.Sim.Network;
namespace Fogline.Sim.Train
{

    /// <summary>
    /// 列车的运行时状态。受力、移动、能源、噪音、格子、发射器随 mvp 各步加入。
    /// 加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class TrainState
    {
        public int ControllerNotch;     // 组合手柄级位：负数为制动，正数为牵引

        public void ShiftController(int delta, TrainTuningSO tuning)
        {
            // 用 long 相加，避免极端 delta 溢出成反方向
            var target = (long)ControllerNotch + delta;
            ControllerNotch = (int)Math.Clamp(target, -tuning.Controller.BrakeNotches, tuning.Controller.PowerNotches);
        }

        // 推进第 3 步：受力、移动、施工（改写 network）、能源与噪音
        public void Tick(float dt, NetworkState network, TrainTuningSO tuning) { }

        public TrainState Clone() => (TrainState)MemberwiseClone();
    }
}
```

`Assets/Scripts/Sim/Chart/ChartState.cs`：

```csharp
using System;
using System.Collections.Generic;
namespace Fogline.Sim.Chart
{

    /// <summary>
    /// 玩家知道的一切：接触目标、勘测层、标记、通告，随 mvp 第 2、3、7 步加入。
    /// 加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class ChartState
    {
        // 推进第 5 步：声纳、接触目标、观测、广播、标记褪色
        public void Tick(float dt, SimClock clock, List<ISimEvent> events) { }

        public ChartState Clone() => (ChartState)MemberwiseClone();
    }
}
```

- [ ] **Step 6: 写 `Assets/Scripts/Sim/Commands.cs`**

```csharp
using System;
namespace Fogline.Sim
{

    /// <summary>
    /// 玩家指令。必须能被 JsonUtility 序列化（公有字段 + 无参构造），以便写进试玩日志并回放。
    /// 新指令写在这个文件里，并在 SimWorld.Apply 里处理。
    /// </summary>
    public interface ISimCommand { }

    /// <summary>组合手柄：W 往牵引推一级（+1），S 往制动拉一级（−1）。</summary>
    [Serializable]
    public sealed class ShiftController : ISimCommand
    {
        public int Delta;

        public ShiftController() { }
        public ShiftController(int delta) => Delta = delta;
    }
}
```

- [ ] **Step 7: 写 `Assets/Scripts/Sim/SimWorld.cs`**

```csharp
using System;
using System.Collections.Generic;
using Fogline.Core;
using Fogline.Sim.Chart;
using Fogline.Sim.Network;
using Fogline.Sim.Sea;
using Fogline.Sim.Train;
namespace Fogline.Sim
{

    /// <summary>模拟产生的离散事件。具体事件就近定义在产生它的领域里。</summary>
    public interface ISimEvent { }

    /// <summary>四个领域的调参资产。只读，模拟绝不写回。</summary>
    public sealed class SimTuning
    {
        public readonly SeaTuningSO     Sea;
        public readonly NetworkTuningSO Network;
        public readonly TrainTuningSO   Train;
        public readonly ChartTuningSO   Chart;

        public SimTuning(SeaTuningSO sea, NetworkTuningSO network, TrainTuningSO train, ChartTuningSO chart)
        {
            Sea     = sea     ? sea     : throw new ArgumentNullException(nameof(sea));
            Network = network ? network : throw new ArgumentNullException(nameof(network));
            Train   = train   ? train   : throw new ArgumentNullException(nameof(train));
            Chart   = chart   ? chart   : throw new ArgumentNullException(nameof(chart));
        }
    }

    /// <summary>
    /// 整个世界的状态。Tick 按固定顺序推进四个领域；Clone 用于快照。
    /// 不依赖场景，EditMode 测试可以直接跑。
    /// </summary>
    public sealed class SimWorld
    {
        public ScenarioSO Scenario { get; }
        public SimTuning  Tuning   { get; }

        public SimClock     Clock   { get; private set; }
        public SeededRandom Random  { get; private set; }
        public SeaState     Sea     { get; private set; }
        public NetworkState Network { get; private set; }
        public TrainState   Train   { get; private set; }
        public ChartState   Chart   { get; private set; }

        private readonly List<ISimEvent> _events = new();

        private SimWorld(ScenarioSO scenario, SimTuning tuning)
        {
            Scenario = scenario;
            Tuning   = tuning;
        }

        public static SimWorld Create(ScenarioSO scenario, SimTuning tuning, ulong seed)
        {
            if (!scenario) throw new ArgumentNullException(nameof(scenario));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            return new SimWorld(scenario, tuning)
            {
                Clock   = new SimClock { Seconds = scenario.StartSeconds, TimeScale = tuning.Sea.World.TimeScale },
                Random  = new SeededRandom(seed),
                Sea     = new SeaState(),
                Network = new NetworkState(),
                Train   = new TrainState(),
                Chart   = new ChartState(),
            };
        }

        /// <summary>推进一步，返回本步的事件（新数组，调用方可以保留）。</summary>
        public IReadOnlyList<ISimEvent> Tick(float dt, IReadOnlyList<ISimCommand> commands)
        {
            if (!(dt > 0f)) throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt 必须为正");
            _events.Clear();

            // 1. 应用本步的指令
            if (commands != null)
                foreach (var command in commands) Apply(command);

            // 2. Sea：推进时间、潮汐与潮流
            Clock.Advance(dt);
            Sea.TickEnvironment(Clock, Tuning.Sea);

            // 3. Train：受力、移动、施工、能源与噪音
            Train.Tick(dt, Network, Tuning.Train);

            // 4. Sea：猎手听、想、动
            Sea.TickHunters(dt, Random, _events);

            // 5. Chart：声纳、接触目标、观测、广播、标记褪色
            Chart.Tick(dt, Clock, _events);

            // 6. 返回本步的事件
            return _events.Count == 0 ? Array.Empty<ISimEvent>() : _events.ToArray();
        }

        public SimWorld Clone() => new(Scenario, Tuning)
        {
            Clock   = Clock.Clone(),
            Random  = Random.Clone(),
            Sea     = Sea.Clone(),
            Network = Network.Clone(),
            Train   = Train.Clone(),
            Chart   = Chart.Clone(),
        };

        private void Apply(ISimCommand command)
        {
            switch (command)
            {
                case null: throw new ArgumentNullException(nameof(command));
                case ShiftController c: Train.ShiftController(c.Delta, Tuning.Train); break;
                default: throw new NotSupportedException($"未处理的指令 {command.GetType().Name}");
            }
        }
    }
}
```

（`SimTuning` 构造里用 `sea ? sea : throw` 而不是 `??`，因为 Unity 对象的 `??` 不认已销毁的对象。）

- [ ] **Step 8: 运行全部测试，确认通过**

Run: `powershell -File tools/run-editmode-tests.ps1`
Expected: `result=Passed`，`failed=0`；`SimAssembly_UsesNoForbiddenApis` 仍然通过（新文件没引入禁止项）。

- [ ] **Step 9: 确认 Sim 根目录只有四个 `.cs`**

Run: `(Get-ChildItem Assets/Scripts/Sim -Filter *.cs -File).Name`
Expected: 恰好 `Commands.cs`、`ScenarioSO.cs`、`SimClock.cs`、`SimWorld.cs`。

- [ ] **Step 10: 提交**

```powershell
git add -A Assets/Scripts
git commit -m "feat(sim): SimWorld with four domains, tuning/scenario SOs and commands"
```

---

### Task 5: Game 程序集：固定步长驱动、试玩日志、快照、SimRunner

**Files:**
- Create: `Assets/Scripts/Game/Fogline.Game.asmdef`
- Create: `Assets/Scripts/Game/SimStepper.cs`、`CommandLog.cs`、`SnapshotStore.cs`、`SimRunner.cs`
- Modify: `Assets/Scripts/Tests/Fogline.Tests.asmdef`（references 加 `"Fogline.Game"`）
- Create: `Assets/Scripts/Tests/SimStepperTests.cs`、`CommandLogTests.cs`、`SnapshotStoreTests.cs`

**Interfaces:**
- Consumes: `SimWorld.Tick/Clone`、`SimWorld.Clock.Step`、`ISimCommand`、`ISimEvent`、`ShiftController`（Task 4）；`EventBus.EmitBoxed`（Task 2）；`TestWorlds`（Task 4）。
- Produces:
  - `SimStepper(SimWorld world, float stepSeconds = DefaultStepSeconds, CommandLog log = null)`；`const float DefaultStepSeconds = 1f / 20f`；`const int MaxStepsPerFrame = 5`；`World`、`StepSeconds`、`Alpha`（0–1，供 View 插值）；`void Enqueue(ISimCommand)`；`int Advance(float realDt)`；`void Replace(SimWorld world)`；`event Action<ISimEvent> EventRaised`。
  - `CommandLog`：`void Record(long step, ISimCommand command)`、`IReadOnlyList<string> Lines`、`void WriteTo(string path)`、`static List<(long Step, ISimCommand Command)> Parse(IEnumerable<string> lines)`。
  - `SnapshotStore`：`bool HasSnapshot`、`void Save(SimWorld)`、`SimWorld Restore()`。
  - `SimRunner : MonoBehaviour`：`Begin(SimWorld world)`、`Enqueue(ISimCommand)`、`World`、`Alpha`、`Snapshots`、`SaveSnapshot()`、`RestoreSnapshot()`。

- [ ] **Step 1: 建 `Assets/Scripts/Game/Fogline.Game.asmdef`，并在 Tests asmdef 里加 `"Fogline.Game"`**

```json
{
    "name": "Fogline.Game",
    "rootNamespace": "Fogline.Game",
    "references": ["Fogline.Core", "Fogline.Sim"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 写失败的测试**

`Assets/Scripts/Tests/SimStepperTests.cs`：

```csharp
using System.Collections.Generic;
using Fogline.Core;
using Fogline.Game;
using Fogline.Sim;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class SimStepperTests
    {
        private const float Step = SimStepper.DefaultStepSeconds;

        [Test]
        public void Advance_RunsWholeStepsAndKeepsRemainderForInterpolation()
        {
            var stepper = new SimStepper(TestWorlds.Create());
            Assert.AreEqual(2, stepper.Advance(0.12f));
            Assert.AreEqual(2, stepper.World.Clock.Step);
            Assert.AreEqual(0.4f, stepper.Alpha, 1e-3f);
        }

        [Test]
        public void Advance_KeepsCommandsUntilAStepRuns()
        {
            var stepper = new SimStepper(TestWorlds.Create());
            stepper.Enqueue(new ShiftController(+1));
            Assert.AreEqual(0, stepper.Advance(0.03f));
            Assert.AreEqual(0, stepper.World.Train.ControllerNotch);
            Assert.AreEqual(1, stepper.Advance(0.03f));
            Assert.AreEqual(1, stepper.World.Train.ControllerNotch);
        }

        [Test]
        public void Advance_AppliesEachCommandOnlyOnce()
        {
            var stepper = new SimStepper(TestWorlds.Create());
            stepper.Enqueue(new ShiftController(+1));
            stepper.Advance(Step * 3.5f);
            Assert.AreEqual(1, stepper.World.Train.ControllerNotch);
        }

        [Test]
        public void Advance_CapsStepsAndDropsBacklog()
        {
            var stepper = new SimStepper(TestWorlds.Create());
            Assert.AreEqual(SimStepper.MaxStepsPerFrame, stepper.Advance(10f));
            Assert.AreEqual(0, stepper.Advance(0f));
            Assert.That(stepper.Alpha, Is.InRange(0f, 1f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Advance_IgnoresInvalidFrameTime(float realDt)
        {
            var stepper = new SimStepper(TestWorlds.Create());
            Assert.AreEqual(0, stepper.Advance(realDt));
            Assert.AreEqual(1, stepper.Advance(Step));
        }

        [Test]
        public void Replace_SwapsWorldAndDropsPendingCommands()
        {
            var stepper = new SimStepper(TestWorlds.Create());
            var other = TestWorlds.Create(2);
            stepper.Enqueue(new ShiftController(+1));
            stepper.Replace(other);
            stepper.Advance(Step);
            Assert.AreSame(other, stepper.World);
            Assert.AreEqual(0, other.Train.ControllerNotch);
        }

        [Test]
        public void LoggedCommands_ReplayToIdenticalWorld()
        {
            var log = new CommandLog();
            var stepper = new SimStepper(TestWorlds.Create(42), Step, log);
            var script = new SeededRandom(5);
            for (int frame = 0; frame < 300; frame++)
            {
                if (script.NextFloat() < 0.2f) stepper.Enqueue(new ShiftController(script.Range(-1, 2)));
                stepper.Advance(script.NextFloat() * 0.1f);
            }

            var replayed = Replay(CommandLog.Parse(log.Lines), stepper.World.Clock.Step, 42);

            Assert.AreEqual(TestWorlds.Digest(stepper.World), TestWorlds.Digest(replayed));
        }

        private static SimWorld Replay(List<(long Step, ISimCommand Command)> log, long steps, ulong seed)
        {
            var world = TestWorlds.Create(seed);
            int next = 0;
            for (long s = 0; s < steps; s++)
            {
                var commands = new List<ISimCommand>();
                while (next < log.Count && log[next].Step == s) commands.Add(log[next++].Command);
                world.Tick(Step, commands);
            }
            Assert.AreEqual(log.Count, next, "日志里有指令没被回放");
            return world;
        }
    }
}
```

`Assets/Scripts/Tests/CommandLogTests.cs`：

```csharp
using System;
using System.IO;
using Fogline.Game;
using Fogline.Sim;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class CommandLogTests
    {
        [Test]
        public void Parse_RoundTripsStepTypeAndFields()
        {
            var log = new CommandLog();
            log.Record(7, new ShiftController(-1));
            var parsed = CommandLog.Parse(log.Lines);
            Assert.AreEqual(1, parsed.Count);
            Assert.AreEqual(7, parsed[0].Step);
            Assert.AreEqual(-1, ((ShiftController)parsed[0].Command).Delta);
        }

        [Test]
        public void Parse_SkipsBlankLines()
        {
            var log = new CommandLog();
            log.Record(1, new ShiftController(1));
            var lines = new[] { "", log.Lines[0], "   " };
            Assert.AreEqual(1, CommandLog.Parse(lines).Count);
        }

        [Test]
        public void Parse_UnknownTypeThrowsFormatException()
        {
            var line = "{\"Step\":0,\"Type\":\"Fogline.Sim.NoSuchCommand\",\"Json\":\"{}\"}";
            Assert.Throws<FormatException>(() => CommandLog.Parse(new[] { line }));
        }

        [Test]
        public void Parse_NonCommandTypeThrowsFormatException()
        {
            var line = "{\"Step\":0,\"Type\":\"Fogline.Sim.SimClock\",\"Json\":\"{}\"}";
            Assert.Throws<FormatException>(() => CommandLog.Parse(new[] { line }));
        }

        [Test]
        public void WriteTo_CreatesDirectoryAndFileReadsBack()
        {
            var dir = Path.Combine(Path.GetTempPath(), "fogline-log-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(dir, "nested", "run.jsonl");
            try
            {
                var log = new CommandLog();
                log.Record(3, new ShiftController(1));
                log.WriteTo(path);
                Assert.AreEqual(3, CommandLog.Parse(File.ReadAllLines(path))[0].Step);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
```

`Assets/Scripts/Tests/SnapshotStoreTests.cs`：

```csharp
using System;
using Fogline.Game;
using Fogline.Sim;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class SnapshotStoreTests
    {
        private const float Dt = 0.05f;

        [Test]
        public void Restore_WithoutSnapshotThrows()
        {
            var store = new SnapshotStore();
            Assert.IsFalse(store.HasSnapshot);
            Assert.Throws<InvalidOperationException>(() => store.Restore());
        }

        [Test]
        public void Save_IsNotAffectedByLaterPlay()
        {
            var world = TestWorlds.Create();
            var store = new SnapshotStore();
            store.Save(world);
            var saved = TestWorlds.Digest(world);

            world.Tick(Dt, new ISimCommand[] { new ShiftController(+4) });

            Assert.AreEqual(saved, TestWorlds.Digest(store.Restore()));
        }

        [Test]
        public void Restore_TwiceGivesIndependentEqualWorlds()
        {
            var store = new SnapshotStore();
            store.Save(TestWorlds.Create());

            var first = store.Restore();
            first.Tick(Dt, new ISimCommand[] { new ShiftController(+4) });
            var second = store.Restore();

            Assert.AreNotSame(first, second);
            Assert.AreEqual(0, second.Train.ControllerNotch);
            Assert.AreEqual(0, second.Clock.Step);
        }

        [Test]
        public void Save_NullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new SnapshotStore().Save(null));
        }
    }
}
```

- [ ] **Step 3: 运行，确认失败**

Run: `powershell -File tools/run-editmode-tests.ps1`
Expected: 编译错误，`SimStepper`、`CommandLog`、`SnapshotStore` 找不到。

- [ ] **Step 4: 实现 `Assets/Scripts/Game/CommandLog.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Fogline.Sim;
using UnityEngine;
namespace Fogline.Game
{

    /// <summary>
    /// 试玩日志：按步记录玩家指令，每行一条 JSON（步号、指令类型、指令字段），可原样回放。
    /// </summary>
    public sealed class CommandLog
    {
        [Serializable]
        private struct Line
        {
            public long   Step;
            public string Type;
            public string Json;
        }

        private readonly List<string> _lines = new();
        public IReadOnlyList<string> Lines => _lines;

        public void Record(long step, ISimCommand command) =>
            _lines.Add(JsonUtility.ToJson(new Line
            {
                Step = step,
                Type = command.GetType().FullName,
                Json = JsonUtility.ToJson(command),
            }));

        public void WriteTo(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(path, _lines);
        }

        public static List<(long Step, ISimCommand Command)> Parse(IEnumerable<string> lines)
        {
            var result = new List<(long, ISimCommand)>();
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = JsonUtility.FromJson<Line>(raw);
                var type = typeof(ISimCommand).Assembly.GetType(line.Type);
                if (type == null || !typeof(ISimCommand).IsAssignableFrom(type))
                    throw new FormatException($"不是已知的指令类型：{line.Type}");
                result.Add((line.Step, (ISimCommand)JsonUtility.FromJson(line.Json, type)));
            }
            return result;
        }
    }
}
```

- [ ] **Step 5: 实现 `Assets/Scripts/Game/SnapshotStore.cs`**

```csharp
using System;
using Fogline.Sim;
namespace Fogline.Game
{

    /// <summary>
    /// 停靠时存一份世界快照，能源归零时恢复。demo 只存内存。
    /// 存和取都复制一份，快照本身永远不会被游玩改动。
    /// </summary>
    public sealed class SnapshotStore
    {
        private SimWorld _saved;

        public bool HasSnapshot => _saved != null;

        public void Save(SimWorld world) =>
            _saved = (world ?? throw new ArgumentNullException(nameof(world))).Clone();

        public SimWorld Restore() =>
            _saved?.Clone() ?? throw new InvalidOperationException("还没有快照");
    }
}
```

- [ ] **Step 6: 实现 `Assets/Scripts/Game/SimStepper.cs`**

```csharp
using System;
using System.Collections.Generic;
using Fogline.Sim;
namespace Fogline.Game
{

    /// <summary>
    /// 固定步长驱动：累积每帧的真实时间，按固定步长调用 SimWorld.Tick。
    /// 纯 C#，SimRunner 每帧调用 Advance；测试直接驱动它。
    /// </summary>
    public sealed class SimStepper
    {
        public const float DefaultStepSeconds = 1f / 20f;
        public const int   MaxStepsPerFrame   = 5;      // 卡顿时最多追这么多步，其余丢弃

        public SimWorld World       { get; private set; }
        public float    StepSeconds { get; }
        public float    Alpha       => _accumulator / StepSeconds;   // View 插值用，0–1

        public event Action<ISimEvent> EventRaised;

        private readonly CommandLog _log;
        private readonly List<ISimCommand> _pending = new();
        private float _accumulator;

        public SimStepper(SimWorld world, float stepSeconds = DefaultStepSeconds, CommandLog log = null)
        {
            if (!(stepSeconds > 0f)) throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            World       = world ?? throw new ArgumentNullException(nameof(world));
            StepSeconds = stepSeconds;
            _log        = log;
        }

        public void Enqueue(ISimCommand command) =>
            _pending.Add(command ?? throw new ArgumentNullException(nameof(command)));

        /// <summary>换成另一个世界（例如恢复快照），清空未应用的指令和累积时间。</summary>
        public void Replace(SimWorld world)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            _pending.Clear();
            _accumulator = 0f;
        }

        /// <summary>推进一帧，返回本帧执行的步数。负数、NaN、无穷大的帧时间按 0 处理。</summary>
        public int Advance(float realDt)
        {
            if (realDt > 0f && !float.IsInfinity(realDt)) _accumulator += realDt;

            int steps = 0;
            while (_accumulator >= StepSeconds && steps < MaxStepsPerFrame)
            {
                _accumulator -= StepSeconds;
                RunStep();
                steps++;
            }
            if (_accumulator >= StepSeconds) _accumulator = 0f;   // 丢弃积压，避免追帧螺旋
            return steps;
        }

        private void RunStep()
        {
            var commands = _pending.Count == 0 ? Array.Empty<ISimCommand>() : _pending.ToArray();
            _pending.Clear();
            foreach (var command in commands) _log?.Record(World.Clock.Step, command);
            foreach (var evt in World.Tick(StepSeconds, commands)) EventRaised?.Invoke(evt);
        }
    }
}
```

- [ ] **Step 7: 实现 `Assets/Scripts/Game/SimRunner.cs`（MonoBehaviour 外壳，不在 EditMode 里测）**

```csharp
using System;
using System.IO;
using Fogline.Core;
using Fogline.Sim;
using UnityEngine;
namespace Fogline.Game
{

    /// <summary>
    /// 把 SimStepper 接进 Unity：每帧推进，把离散事件转发到 EventBus，退出时写试玩日志。
    /// 连续量不发事件，View 每帧读 World 并用 Alpha 插值。
    /// </summary>
    public sealed class SimRunner : MonoBehaviour
    {
        public SimWorld      World     => _stepper?.World;
        public float         Alpha     => _stepper?.Alpha ?? 0f;
        public SnapshotStore Snapshots { get; } = new();

        private readonly CommandLog _log = new();
        private SimStepper _stepper;

        public void Begin(SimWorld world)
        {
            _stepper = new SimStepper(world, SimStepper.DefaultStepSeconds, _log);
            _stepper.EventRaised += evt => EventBus.Instance.EmitBoxed(evt);
        }

        public void Enqueue(ISimCommand command)
        {
            if (_stepper == null) throw new InvalidOperationException("SimRunner 还没有 Begin");
            _stepper.Enqueue(command);
        }

        public void SaveSnapshot() => Snapshots.Save(World);

        public void RestoreSnapshot() => _stepper.Replace(Snapshots.Restore());

        private void Update() => _stepper?.Advance(Time.deltaTime);

        private void OnDestroy()
        {
            if (_log.Lines.Count == 0) return;
            var file = $"{DateTime.Now:yyyyMMdd-HHmmss}.jsonl";
            _log.WriteTo(Path.Combine(Application.persistentDataPath, "playlogs", file));
        }
    }
}
```

- [ ] **Step 8: 运行全部测试，确认通过**

Run: `powershell -File tools/run-editmode-tests.ps1`
Expected: `result=Passed`，`failed=0`。

- [ ] **Step 9: 提交**

```powershell
git add -A Assets/Scripts
git commit -m "feat(game): fixed-step SimStepper, command log, snapshots and SimRunner"
```

---

### Task 6: View 与 Editor 程序集、Data 资产

**Files:**
- Create: `Assets/Scripts/View/Fogline.View.asmdef`
- Create: `Assets/Scripts/Editor/Fogline.Editor.asmdef`
- Create: `Assets/Scripts/Editor/DataAssetsCreator.cs`
- Create（由 Unity 生成）: `Assets/Data/Tuning/{Sea,Network,Train,Chart}Tuning.asset`、`Assets/Data/Scenarios/Linzhi/LinzhiScenario.asset`
- Create: `Assets/Scripts/Tests/DataAssetsTests.cs`

**Interfaces:**
- Consumes: 四个调参 SO 与 `ScenarioSO`（Task 4）。
- Produces: 菜单 `Fogline/Create Missing Data Assets` 与静态方法 `Fogline.EditorTools.DataAssetsCreator.CreateMissing()`（只补缺失的，不覆盖已有资产）；固定的资产路径（平衡检查测试以后从这些路径读）。

- [ ] **Step 1: 建两个 asmdef**

`Assets/Scripts/View/Fogline.View.asmdef`（暂无脚本；`Chart/`、`Cockpit/`、`Station/` 子目录在各自内容出现时再建）：

```json
{
    "name": "Fogline.View",
    "rootNamespace": "Fogline.View",
    "references": ["Fogline.Core", "Fogline.Sim", "Fogline.Game"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/Scripts/Editor/Fogline.Editor.asmdef`：

```json
{
    "name": "Fogline.Editor",
    "rootNamespace": "Fogline.EditorTools",
    "references": ["Fogline.Core", "Fogline.Sim", "Fogline.Game", "Fogline.View"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: 写失败的测试 `Assets/Scripts/Tests/DataAssetsTests.cs`**

```csharp
using Fogline.Sim;
using Fogline.Sim.Chart;
using Fogline.Sim.Network;
using Fogline.Sim.Sea;
using Fogline.Sim.Train;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fogline.Tests
{
    public class DataAssetsTests
    {
        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"缺少资产 {path}，运行菜单 Fogline/Create Missing Data Assets");
            return asset;
        }

        [Test]
        public void LinzhiWorld_BuildsFromDataAssets()
        {
            var tuning = new SimTuning(
                Load<SeaTuningSO>("Assets/Data/Tuning/SeaTuning.asset"),
                Load<NetworkTuningSO>("Assets/Data/Tuning/NetworkTuning.asset"),
                Load<TrainTuningSO>("Assets/Data/Tuning/TrainTuning.asset"),
                Load<ChartTuningSO>("Assets/Data/Tuning/ChartTuning.asset"));
            var scenario = Load<ScenarioSO>("Assets/Data/Scenarios/Linzhi/LinzhiScenario.asset");

            var world = SimWorld.Create(scenario, tuning, 1);
            world.Tick(0.05f, null);

            Assert.AreEqual(1, world.Clock.Step);
        }
    }
}
```

- [ ] **Step 3: 运行，确认失败**

Run: `powershell -File tools/run-editmode-tests.ps1 -Filter Fogline.Tests.DataAssetsTests`
Expected: `failed=1`，消息 `缺少资产 Assets/Data/Tuning/SeaTuning.asset`。

- [ ] **Step 4: 实现 `Assets/Scripts/Editor/DataAssetsCreator.cs`**

```csharp
using System.IO;
using Fogline.Sim;
using Fogline.Sim.Chart;
using Fogline.Sim.Network;
using Fogline.Sim.Sea;
using Fogline.Sim.Train;
using UnityEditor;
using UnityEngine;
namespace Fogline.EditorTools
{

    /// <summary>
    /// 补齐 Assets/Data 下的四个调参资产和林芝关卡资产。只建缺失的，不覆盖已有的。
    /// </summary>
    public static class DataAssetsCreator
    {
        private const string TuningDir = "Assets/Data/Tuning";
        private const string LinzhiDir = "Assets/Data/Scenarios/Linzhi";

        [MenuItem("Fogline/Create Missing Data Assets")]
        public static void CreateMissing()
        {
            CreateIfMissing<SeaTuningSO>(TuningDir + "/SeaTuning.asset");
            CreateIfMissing<NetworkTuningSO>(TuningDir + "/NetworkTuning.asset");
            CreateIfMissing<TrainTuningSO>(TuningDir + "/TrainTuning.asset");
            CreateIfMissing<ChartTuningSO>(TuningDir + "/ChartTuning.asset");
            CreateIfMissing<ScenarioSO>(LinzhiDir + "/LinzhiScenario.asset");
            AssetDatabase.SaveAssets();
        }

        private static void CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
```

- [ ] **Step 5: 用批处理生成资产，并确认重复运行不改动**

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe"
& $unity -batchmode -nographics -quit -projectPath (Get-Location).Path -executeMethod Fogline.EditorTools.DataAssetsCreator.CreateMissing -logFile Logs/create-data.log | Out-Null
git status --short Assets/Data
```

Expected: 列出 `Assets/Data/Tuning/` 下四个 `.asset` 和 `Assets/Data/Scenarios/Linzhi/LinzhiScenario.asset`（及对应 `.meta`）。`git add Assets/Data` 后再跑一次同样的命令，`git status --short Assets/Data` 应为空。

- [ ] **Step 6: 运行全部测试**

Run: `powershell -File tools/run-editmode-tests.ps1`
Expected: `result=Passed`，`failed=0`。

- [ ] **Step 7: 确认程序集与目录符合架构**

```powershell
(Get-ChildItem Assets/Scripts -Recurse -Filter *.asmdef).Name
Get-ChildItem Assets/Scripts -Recurse -Directory | ForEach-Object { $_.FullName.Substring((Get-Location).Path.Length + 1) }
Select-String -Path Logs/editmode.log -Pattern "warning CS|error CS"
```

Expected: 六个 asmdef；目录只有 `Core`、`Sim`、`Sim/Sea`、`Sim/Network`、`Sim/Train`、`Sim/Chart`、`Game`、`View`、`Tests`、`Editor`；日志里没有我们代码的编译警告或错误（`Assets/Plugins/` 里的第三方警告可忽略）。

- [ ] **Step 8: 打开 Unity 编辑器做一次人工确认**

打开项目，确认 Console 无编译错误；菜单栏有 `Fogline/Create Missing Data Assets`；`Assets/Create/Fogline/` 下有 Scenario 与 Tuning 四项；选中 `TrainTuning.asset`，Inspector 里 `Controller` 小节可折叠，显示 6 和 8。关闭编辑器。

- [ ] **Step 9: 提交**

```powershell
git add -A Assets/Scripts Assets/Data
git commit -m "feat: View/Editor assemblies and default data assets"
```

---

## 验证（全部完成后）

1. `powershell -File tools/run-editmode-tests.ps1` 全部通过，含 `SimAssembly_UsesNoForbiddenApis`、`SameSeedAndCommands_GiveIdenticalWorlds`、`LoggedCommands_ReplayToIdenticalWorld`。
2. 反向验证依赖方向：临时在 `Assets/Scripts/Sim/SimClock.cs` 里加一行 `private object _x = typeof(Fogline.Game.SimStepper);`，跑测试应得到编译错误 `The type or namespace name 'Game' does not exist`；改成 `UnityEngine.Time.time` 的用法，`SimAssembly_UsesNoForbiddenApis` 应失败。验证后撤销。
3. `git status` 干净，所有新 `.meta` 已提交。

## 执行方式

推荐 **Native**（本会话内逐个任务实现，最后一次整体审查）：六个任务严格串行，每个都依赖前一个的类型名和 asmdef，并且每次跑测试都要独占 Unity 批处理（约 1–2 分钟），逐任务派新子代理收益不大。
