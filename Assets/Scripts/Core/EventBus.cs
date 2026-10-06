using System;
using System.Collections.Generic;
namespace Fogline.Core
{

    /// <summary>
    /// 纯 C# 单例，无 MonoBehaviour 依赖。
    /// 首次访问即就绪，不存在 Awake 时序问题。
    /// </summary>
    public class EventBus
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance ??= new EventBus();

        private readonly Dictionary<Type, Delegate> _handlers = new();

        private EventBus() { }

    #if UNITY_EDITOR
        // 退出 Play 模式不会域重载，静态的 _instance 会带着本局所有没退订的处理器留到编辑模式，
        // 之后跑 EditMode 测试发出的事件会被这些残留处理器截走。
        // 回到编辑模式时直接换一条空总线；旧 Subscription 持有的是旧总线，Dispose 照常无害。
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetOnEnterEditMode()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) _instance = null;
            };
        }
    #endif

        public IDisposable On<T>(Action<T> handler)
        {
            var type = typeof(T);
            _handlers[type] = _handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
            return new Subscription<T>(this, handler);
        }

        public void Emit<T>(T evt)
        {
    #if UNITY_EDITOR
            UnityEngine.Debug.Log($"[EventBus] {typeof(T).Name} {UnityEngine.JsonUtility.ToJson(evt)}");
    #endif
            if (_handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(evt);
        }

        internal void Off<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var existing)) return;
            var updated = Delegate.Remove(existing, handler);
            if (updated == null) _handlers.Remove(type);
            else _handlers[type] = updated;
        }

        private class Subscription<T> : IDisposable
        {
            private readonly EventBus _bus;
            private readonly Action<T> _handler;
            private bool _disposed;

            public Subscription(EventBus bus, Action<T> handler)
            {
                _bus = bus;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _bus.Off(_handler);
            }
        }
    }
}
