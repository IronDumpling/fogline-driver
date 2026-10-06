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