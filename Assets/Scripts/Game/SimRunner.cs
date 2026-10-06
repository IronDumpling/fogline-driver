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
    public sealed class SimRunner : MonoBehaviour
    {
        public SimWorld      World     => _stepper?.World;
        public float         Alpha     => _stepper?.Alpha ?? 0f;
        public SnapshotStore Snapshots { get; } = new();

        private readonly CommandLog _log = new();
        private SimStepper _stepper;

        public void Begin(SimWorld world)
        {
            _stepper = new SimStepper(world, SimStepper.DefaultStepSeconds, _log);
            _stepper.EventRaised += evt => EventBus.Instance.EmitBoxed(evt);
        }

        public void Enqueue(ISimCommand command)
        {
            if (_stepper == null) throw new InvalidOperationException("SimRunner 还没有 Begin");
            _stepper.Enqueue(command);
        }

        public void SaveSnapshot() => Snapshots.Save(World);

        public void RestoreSnapshot() => _stepper.Replace(Snapshots.Restore());

        private void Update() => _stepper?.Advance(Time.deltaTime);

        private void OnDestroy()
        {
            if (_log.Lines.Count == 0) return;
            var file = $"{DateTime.Now:yyyyMMdd-HHmmss}.jsonl";
            _log.WriteTo(Path.Combine(Application.persistentDataPath, "playlogs", file));
        }
    }
}
