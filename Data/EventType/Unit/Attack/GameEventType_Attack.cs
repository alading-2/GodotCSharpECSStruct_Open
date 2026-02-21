/// <summary>
/// 攻击系统相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Attack
    {
        // === 命令事件（外部 → AttackComponent）===

        /// <summary>请求发动攻击（命令事件：AI/Player -> AttackComponent）</summary>
        public const string Requested = "attack:requested";
        /// <summary>请求发动攻击事件数据</summary>
        public readonly record struct RequestedEventData(Godot.Node2D Target);

        /// <summary>请求中断攻击（命令事件：AI/外部 -> AttackComponent）</summary>
        public const string CancelRequested = "attack:cancel_requested";
        /// <summary>请求中断攻击事件数据</summary>
        public readonly record struct CancelRequestedEventData();

        // === 通知事件（AttackComponent → AI/UI）===

        /// <summary>攻击开始（通知事件：进入 WindUp 或即时判定）</summary>
        public const string Started = "attack:started";
        /// <summary>攻击开始事件数据</summary>
        public readonly record struct StartedEventData(Godot.Node2D Target);

        /// <summary>攻击完成（通知事件：伤害已判定，走完全部阶段）</summary>
        public const string Finished = "attack:finished";
        /// <summary>攻击完成事件数据</summary>
        public readonly record struct FinishedEventData(Godot.Node2D? Target, bool DidHit);

        /// <summary>攻击被取消（通知事件：目标丢失/自身死亡等）</summary>
        public const string Cancelled = "attack:cancelled";
        /// <summary>攻击被取消事件数据</summary>
        public readonly record struct CancelledEventData(AttackCancelReason Reason);
    }
}

/// <summary>
/// 攻击取消原因
/// </summary>
public enum AttackCancelReason
{
    /// <summary>目标已死亡</summary>
    TargetDead,
    /// <summary>目标离开攻击范围</summary>
    TargetOutOfRange,
    /// <summary>目标引用失效（被回收等）</summary>
    TargetInvalid,
    /// <summary>自身死亡</summary>
    SelfDead,
    /// <summary>自身被控（眩晕等）</summary>
    SelfDisabled,
    /// <summary>外部主动取消</summary>
    ExternalCancel,
    /// <summary>组件注销清理</summary>
    ComponentCleanup
}
