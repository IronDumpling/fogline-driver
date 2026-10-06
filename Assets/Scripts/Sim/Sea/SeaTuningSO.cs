using System;
using UnityEngine;
namespace Fogline.Sim.Sea
{

    /// <summary>
    /// 雾海的全部调参：世界（雾面、潮汐、时间比例、雾压）、声音、猎手。
    /// 新参数加成字段或新的 [Serializable] 小节，不新建 SO 类型。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Sea", fileName = "SeaTuning")]
    public class SeaTuningSO : ScriptableObject
    {
        [Serializable]
        public class WorldSection
        {
            [Tooltip("游戏秒 / 现实秒")] [Min(0f)] public float TimeScale = 1f;
        }

        public WorldSection World = new();
    }
}
