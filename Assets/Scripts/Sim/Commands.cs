using System;
namespace Fogline.Sim
{

    /// <summary>
    /// 玩家指令。必须能被 JsonUtility 序列化（公有字段 + 无参构造），以便写进试玩日志并回放。
    /// 新指令写在这个文件里，并在 SimWorld.Apply 里处理。
    /// </summary>
    public interface ISimCommand { }

    /// <summary>组合手柄：W 往牵引推一级（+1），S 往制动拉一级（−1）。</summary>
    [Serializable]
    public sealed class ShiftController : ISimCommand
    {
        public int Delta;

        public ShiftController() { }
        public ShiftController(int delta) => Delta = delta;
    }
}
