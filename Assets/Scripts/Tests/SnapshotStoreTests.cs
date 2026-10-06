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
        public void Clear_DropsSnapshot()
        {
            var store = new SnapshotStore();
            store.Save(TestWorlds.Create());
            Assert.IsTrue(store.HasSnapshot);

            store.Clear();

            Assert.IsFalse(store.HasSnapshot);
            Assert.Throws<InvalidOperationException>(() => store.Restore());
        }

        [Test]
        public void Save_NullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new SnapshotStore().Save(null));
        }
    }
}
