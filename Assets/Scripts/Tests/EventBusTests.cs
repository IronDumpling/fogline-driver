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
