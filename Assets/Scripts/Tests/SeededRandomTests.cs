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
