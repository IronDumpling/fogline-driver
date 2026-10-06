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
