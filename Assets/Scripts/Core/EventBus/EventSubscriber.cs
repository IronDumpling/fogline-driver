using System;
using System.Collections.Generic;
using UnityEngine;
namespace Fogline.Core
{

    public abstract class EventSubscriber : MonoBehaviour
    {
        private readonly List<IDisposable> _subs = new();
        private bool _registered;

        protected void Subscribe<T>(Action<T> handler) =>
            _subs.Add(EventBus.Instance.On(handler));

        // 供自己管理订阅生命周期的辅助类挂靠——避免每个调用方各自声明字段、
        // 各自在 OnDestroy 里手写 Dispose()，统一并入 Subscribe<T> 已有的批量释放。
        protected void Track(IDisposable disposable) => _subs.Add(disposable);

        // Initialize() 在 RegisterSubscriptions() 之前调用，保证 handler 所需字段已初始化。
        // 子类在此查询 VisualElement 引用等，禁止在 RegisterSubscriptions() 的 handler 里
        // 访问未经 Initialize() 初始化的字段。
        protected virtual void Initialize() { }

        protected virtual void OnEnable()
        {
            if (_registered) return;
            Initialize();
            RegisterSubscriptions();
            _registered = true;
        }

        // 对象销毁时统一清理，而非每次 OnDisable
        protected virtual void OnDestroy() => ReleaseSubscriptions();

        // 立即释放全部订阅。Play 模式下 Destroy 延迟到帧末才触发 OnDestroy，需要当场断开的先调这个
        protected void ReleaseSubscriptions()
        {
            foreach (var s in _subs) s.Dispose();
            _subs.Clear();
        }

        protected abstract void RegisterSubscriptions();
    }
}
