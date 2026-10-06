using System;
namespace Fogline.Sim
{

    /// <summary>
    /// 游戏时间。每步由 SimWorld 推进一次：现实步长 × 时间比例 = 游戏秒。
    /// </summary>
    [Serializable]
    public class SimClock
    {
        public double Seconds;          // 游戏内秒
        public float  TimeScale = 1f;   // 游戏秒 / 现实秒
        public long   Step;             // 已推进的步数，试玩日志按它对齐指令

        public void Advance(float dt)
        {
            Seconds += dt * (double)TimeScale;
            Step++;
        }

        public SimClock Clone() => (SimClock)MemberwiseClone();
    }
}
