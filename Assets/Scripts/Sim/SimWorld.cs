using System;
using System.Collections.Generic;
using Fogline.Core;
using Fogline.Sim.Chart;
using Fogline.Sim.Network;
using Fogline.Sim.Sea;
using Fogline.Sim.Train;
namespace Fogline.Sim
{

    /// <summary>模拟产生的离散事件。具体事件就近定义在产生它的领域里。</summary>
    public interface ISimEvent { }

    /// <summary>四个领域的调参资产。只读，模拟绝不写回。</summary>
    public sealed class SimTuning
    {
        public readonly SeaTuningSO     Sea;
        public readonly NetworkTuningSO Network;
        public readonly TrainTuningSO   Train;
        public readonly ChartTuningSO   Chart;

        public SimTuning(SeaTuningSO sea, NetworkTuningSO network, TrainTuningSO train, ChartTuningSO chart)
        {
            Sea     = sea     ? sea     : throw new ArgumentNullException(nameof(sea));
            Network = network ? network : throw new ArgumentNullException(nameof(network));
            Train   = train   ? train   : throw new ArgumentNullException(nameof(train));
            Chart   = chart   ? chart   : throw new ArgumentNullException(nameof(chart));
        }
    }

    /// <summary>
    /// 整个世界的状态。Tick 按固定顺序推进四个领域；Clone 用于快照。
    /// 不依赖场景，EditMode 测试可以直接跑。
    /// </summary>
    public sealed class SimWorld
    {
        public ScenarioSO Scenario { get; }
        public SimTuning  Tuning   { get; }

        public SimClock     Clock   { get; private set; }
        public SeededRandom Random  { get; private set; }
        public SeaState     Sea     { get; private set; }
        public NetworkState Network { get; private set; }
        public TrainState   Train   { get; private set; }
        public ChartState   Chart   { get; private set; }

        private readonly List<ISimEvent> _events = new();

        private SimWorld(ScenarioSO scenario, SimTuning tuning)
        {
            Scenario = scenario;
            Tuning   = tuning;
        }

        public static SimWorld Create(ScenarioSO scenario, SimTuning tuning, ulong seed)
        {
            if (!scenario) throw new ArgumentNullException(nameof(scenario));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            return new SimWorld(scenario, tuning)
            {
                Clock   = new SimClock { Seconds = scenario.StartSeconds, TimeScale = tuning.Sea.World.TimeScale },
                Random  = new SeededRandom(seed),
                Sea     = new SeaState(),
                Network = new NetworkState(),
                Train   = new TrainState(),
                Chart   = new ChartState(),
            };
        }

        /// <summary>推进一步，返回本步的事件（新数组，调用方可以保留）。</summary>
        public IReadOnlyList<ISimEvent> Tick(float dt, IReadOnlyList<ISimCommand> commands)
        {
            if (!(dt > 0f)) throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt 必须为正");
            _events.Clear();

            // 1. 应用本步的指令
            if (commands != null)
                foreach (var command in commands) Apply(command);

            // 2. Sea：推进时间、潮汐与潮流
            Clock.Advance(dt);
            Sea.TickEnvironment(Clock, Tuning.Sea);

            // 3. Train：受力、移动、施工、能源与噪音
            Train.Tick(dt, Network, Tuning.Train);

            // 4. Sea：猎手听、想、动
            Sea.TickHunters(dt, Random, _events);

            // 5. Chart：声纳、接触目标、观测、广播、标记褪色
            Chart.Tick(dt, Clock, _events);

            // 6. 返回本步的事件
            return _events.Count == 0 ? Array.Empty<ISimEvent>() : _events.ToArray();
        }

        public SimWorld Clone() => new(Scenario, Tuning)
        {
            Clock   = Clock.Clone(),
            Random  = Random.Clone(),
            Sea     = Sea.Clone(),
            Network = Network.Clone(),
            Train   = Train.Clone(),
            Chart   = Chart.Clone(),
        };

        private void Apply(ISimCommand command)
        {
            switch (command)
            {
                case null: throw new ArgumentNullException(nameof(command));
                case ShiftController c: Train.ShiftController(c.Delta, Tuning.Train); break;
                default: throw new NotSupportedException($"未处理的指令 {command.GetType().Name}");
            }
        }
    }
}
