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
