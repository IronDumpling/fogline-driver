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