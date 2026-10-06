using System;
using System.Collections.Generic;
namespace Fogline.Core
{

    public class StateMachine
    {
        public IState Current     { get; private set; }
        public Type   CurrentType => Current?.GetType();
        private readonly Dictionary<Type, IState> _states = new();

        public void Add<T>(T state) where T : IState => _states[typeof(T)] = state;

        public void ChangeState<T>() where T : IState
        {
            Current?.Exit();
            Current = _states[typeof(T)];
            Current?.Enter();
        }

        public void Tick(float dt) => Current?.Tick(dt);
    }
}
