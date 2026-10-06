using System;
namespace Fogline.Sim.Network
{

    /// <summary>
    /// 线网的运行时状态：建设状态、已铺长度、道岔指向、站点配额、委托进度，随 mvp 第 1、5、6 步加入。
    /// 自己不推进，由 Train 的施工改写。加入引用类型字段时，Clone() 必须深拷贝它们。
    /// </summary>
    [Serializable]
    public class NetworkState
    {
        public NetworkState Clone() => (NetworkState)MemberwiseClone();
    }
}
