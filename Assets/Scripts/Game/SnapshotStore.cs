using System;
using Fogline.Sim;
namespace Fogline.Game
{

    /// <summary>
    /// 停靠时存一份世界快照，能源归零时恢复。demo 只存内存。
    /// 存和取都复制一份，快照本身永远不会被游玩改动。
    /// </summary>
    public sealed class SnapshotStore
    {
        private SimWorld _saved;

        public bool HasSnapshot => _saved != null;

        public void Save(SimWorld world) =>
            _saved = (world ?? throw new ArgumentNullException(nameof(world))).Clone();

        /// <summary>清掉已存的快照（新一局开始时用）。</summary>
        public void Clear() => _saved = null;

        public SimWorld Restore() =>
            _saved?.Clone() ?? throw new InvalidOperationException("还没有快照");
    }
}
