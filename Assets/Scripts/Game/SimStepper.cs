using System;
using System.Collections.Generic;
using Fogline.Sim;
namespace Fogline.Game
{

    /// <summary>
    /// 固定步长驱动：累积每帧的真实时间，按固定步长调用 SimWorld.Tick。
    /// 纯 C#，SimRunner 每帧调用 Advance；测试直接驱动它。
    /// </summary>
    public sealed class SimStepper
    {
        public const float DefaultStepSeconds = 1f / 20f;
        public const int   MaxStepsPerFrame   = 5;      // 卡顿时最多追这么多步，其余丢弃

        public SimWorld World       { get; private set; }
        public float    StepSeconds { get; }
        public float    Alpha       => _accumulator / StepSeconds;   // View 插值用，0–1

        public event Action<ISimEvent> EventRaised;

        private readonly CommandLog _log;
        private readonly List<ISimCommand> _pending = new();
        private float _accumulator;

        public SimStepper(SimWorld world, float stepSeconds = DefaultStepSeconds, CommandLog log = null)
        {
            if (!(stepSeconds > 0f)) throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            World       = world ?? throw new ArgumentNullException(nameof(world));
            StepSeconds = stepSeconds;
            _log        = log;
        }

        public void Enqueue(ISimCommand command) =>
            _pending.Add(command ?? throw new ArgumentNullException(nameof(command)));

        /// <summary>换成另一个世界（例如恢复快照），清空未应用的指令和累积时间。</summary>
        public void Replace(SimWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            _log?.RecordRestore(world.Clock.Step);   // 时间线回退，日志里留标记
            World = world;
            _pending.Clear();
            _accumulator = 0f;
        }

        /// <summary>推进一帧，返回本帧执行的步数。负数、NaN、无穷大的帧时间按 0 处理。</summary>
        public int Advance(float realDt)
        {
            if (realDt > 0f && !float.IsInfinity(realDt)) _accumulator += realDt;

            int steps = 0;
            while (_accumulator >= StepSeconds && steps < MaxStepsPerFrame)
            {
                _accumulator -= StepSeconds;
                RunStep();
                steps++;
            }
            if (_accumulator >= StepSeconds) _accumulator = 0f;   // 丢弃积压，避免追帧螺旋
            return steps;
        }

        private void RunStep()
        {
            var commands = _pending.Count == 0 ? Array.Empty<ISimCommand>() : _pending.ToArray();
            _pending.Clear();
            foreach (var command in commands) _log?.Record(World.Clock.Step, command);
            foreach (var evt in World.Tick(StepSeconds, commands)) EventRaised?.Invoke(evt);
        }
    }
}
