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
