using System;
using System.Collections.Generic;
using Fogline.Core;
namespace Fogline.Sim.Sea
{

    /// <summary>
    /// 雾海的运行时状态：潮汐、潮流、声源、猎手，随 mvp 第 2–4 步加入。
    /// 加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class SeaState
    {
        // 推进第 2 步：潮汐与潮流（时间已由 SimWorld 推进）
        public void TickEnvironment(SimClock clock, SeaTuningSO tuning) { }

        // 推进第 4 步：猎手听、想、动；攻击、硬直等写进 events，由 SimWorld 应用
        public void TickHunters(float dt, SeededRandom random, List<ISimEvent> events) { }

        public SeaState Clone() => (SeaState)MemberwiseClone();
    }
}
