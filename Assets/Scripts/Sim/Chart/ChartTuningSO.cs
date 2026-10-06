using UnityEngine;
namespace Fogline.Sim.Chart
{

    /// <summary>
    /// 海图的全部调参：声纳、测向、标记褪色、广播覆盖、预测模糊度。字段随 mvp 第 2、3、7 步加入。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Tuning/Chart", fileName = "ChartTuning")]
    public class ChartTuningSO : ScriptableObject { }
}
