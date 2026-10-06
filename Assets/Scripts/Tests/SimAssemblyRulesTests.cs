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
