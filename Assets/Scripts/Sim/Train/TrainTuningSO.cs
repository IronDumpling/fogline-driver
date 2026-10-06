using System;
using UnityEngine;
namespace Fogline.Sim.Train
{

    /// <summary>
    /// 列车的全部调参：质量、牵引、制动、能源、耐压、车厢格子、发射器。
    /// 新参数加成字段或新的 [Serializable] 小节，不新建 SO 类型。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Train", fileName = "TrainTuning")]
    public class TrainTuningSO : ScriptableObject
    {
        [Serializable]
        public class ControllerSection
        {
            [Tooltip("组合手柄制动级数（v6.1：6）")] [Min(0)] public int BrakeNotches = 6;
            [Tooltip("组合手柄牵引级数（v6.1：8）")] [Min(0)] public int PowerNotches = 8;
        }

        public ControllerSection Controller = new();
    }
}
