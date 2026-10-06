using System;
using System.Collections.Generic;
namespace Fogline.Sim.Chart
{

    /// <summary>
    /// 玩家知道的一切：接触目标、勘测层、标记、通告，随 mvp 第 2、3、7 步加入。
    /// 加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class ChartState
    {
        // 推进第 5 步：声纳、接触目标、观测、广播、标记褪色
        public void Tick(float dt, SimClock clock, List<ISimEvent> events) { }

        public ChartState Clone() => (ChartState)MemberwiseClone();
    }
}
