using System;
using Fogline.Sim.Network;
namespace Fogline.Sim.Train
{

    /// <summary>
    /// 列车的运行时状态。受力、移动、能源、噪音、格子、发射器随 mvp 各步加入。
    /// 加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class TrainState
    {
        public int ControllerNotch;     // 组合手柄级位：负数为制动，正数为牵引

        public void ShiftController(int delta, TrainTuningSO tuning)
        {
            // 用 long 相加，避免极端 delta 溢出成反方向
            var target = (long)ControllerNotch + delta;
            ControllerNotch = (int)Math.Clamp(target, -tuning.Controller.BrakeNotches, tuning.Controller.PowerNotches);
        }

        // 推进第 3 步：受力、移动、施工（改写 network）、能源与噪音
        public void Tick(float dt, NetworkState network, TrainTuningSO tuning) { }

        public TrainState Clone() => (TrainState)MemberwiseClone();
    }
}
