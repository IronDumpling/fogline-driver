using UnityEngine;
namespace Fogline.Sim
{

    /// <summary>
    /// 一张关卡的全部内容：高度图、谷地水道、线网、缺陷、旧地图差异、站点、委托、猎手出生点、开局物品。
    /// 内容随 mvp 各步加入。运行时会变的东西复制进 SimWorld，绝不写回这里。
    /// </summary>
    [CreateAssetMenu(menuName = "Fogline/Scenario", fileName = "Scenario")]
    public class ScenarioSO : ScriptableObject
    {
        [Tooltip("开局时的游戏时间（秒），决定开局潮位")] [Min(0f)] public float StartSeconds;
    }
}
