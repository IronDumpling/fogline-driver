using UnityEngine;
namespace Fogline.Sim.Network
{

    /// <summary>
    /// 线网的全部调参：段长、结构判定、造价、限速与噪音、缺陷。字段随 mvp 第 1、6 步加入。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Network", fileName = "NetworkTuning")]
    public class NetworkTuningSO : ScriptableObject { }
}
