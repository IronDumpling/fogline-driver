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
