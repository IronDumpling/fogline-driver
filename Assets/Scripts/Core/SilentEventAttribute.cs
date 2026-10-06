using System;
namespace Fogline.Core
{

    /// <summary>
    /// 标记高频事件（例如模拟每步都会发的）：EventBus 在编辑器里不为它打日志，避免刷屏。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class SilentEventAttribute : Attribute { }
}
