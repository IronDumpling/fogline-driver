using System;
using System.IO;
using Fogline.Core;
using Fogline.Sim;
using UnityEngine;
namespace Fogline.Game
{

    /// <summary>
    /// 把 SimStepper 接进 Unity：每帧推进，把离散事件转发到 EventBus，退出时写试玩日志。
    /// 连续量不发事件，View 每帧读 World 并用 Alpha 插值。
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 先于 View 脚本的 Update，让它们读到本帧推进后的 World/Alpha
    public sealed class SimRunner : MonoBehaviour
    {
        public SimWorld      World     => _stepper?.World;
        public float         Alpha     => _stepper?.Alpha ?? 0f;
        public SnapshotStore Snapshots { get; private set; } = new();

        private CommandLog _log = new();
        private SimStepper _stepper;

        public void Begin(SimWorld world)
        {
            WriteLog();   // 上一局的日志先落盘，避免两局混在一起
            Snapshots = new SnapshotStore();   // 新一局不沿用上一局的快照
            _log = new CommandLog();
            _stepper = new SimStepper(world, SimStepper.DefaultStepSeconds, _log);
            _stepper.EventRaised += evt => EventBus.Instance.EmitBoxed(evt);
        }

        public void Enqueue(ISimCommand command)
        {
            RequireBegun();
            _stepper.Enqueue(command);
        }

        public void SaveSnapshot()
        {
            RequireBegun();
            Snapshots.Save(World);
        }

        public void RestoreSnapshot()
        {
            RequireBegun();
            _stepper.Replace(Snapshots.Restore());
        }

        private void RequireBegun()
        {
            if (_stepper == null) throw new InvalidOperationException("SimRunner 还没有 Begin");
        }

        private void Update() => _stepper?.Advance(Time.deltaTime);

        private void OnDestroy() => WriteLog();

        private void WriteLog()
        {
            if (_log.Lines.Count == 0) return;
            var file = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}.jsonl";   // 毫秒精度，同一秒内两局不会互相覆盖
            try { _log.WriteTo(Path.Combine(Application.persistentDataPath, "playlogs", file)); }
            catch (Exception e) { Debug.LogException(e); }   // 写日志失败不能拖垮 Begin / OnDestroy
        }
    }
}
