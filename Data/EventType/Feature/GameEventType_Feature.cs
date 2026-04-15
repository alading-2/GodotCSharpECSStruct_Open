/// <summary>
/// Feature 生命周期事件定义
///
/// 覆盖 Feature 从授予到移除的完整语义：
/// Granted → Enabled → Activated/Executed/Ended（可重复） → Disabled → Removed
/// </summary>
public static partial class GameEventType
{
    public static class Feature
    {
        /// <summary>
        /// Feature 被授予（挂到宿主后首次生效）。
        /// 发送者：FeatureSystem.OnFeatureGranted
        /// 接收者：Owner 上感知技能变化的组件（如 UI、AI）
        /// </summary>
        public const string Granted = "feature:granted";
        public readonly record struct GrantedEventData(IEntity Feature, IEntity Owner);

        /// <summary>
        /// Feature 被启用（从禁用状态恢复）。
        /// 发送者：FeatureSystem
        /// </summary>
        public const string Enabled = "feature:enabled";
        public readonly record struct EnabledEventData(IEntity Feature, IEntity Owner);

        /// <summary>
        /// Feature 被禁用（暂停参与响应，但仍挂在宿主上）。
        /// 发送者：FeatureSystem
        /// </summary>
        public const string Disabled = "feature:disabled";
        public readonly record struct DisabledEventData(IEntity Feature, IEntity Owner);

        /// <summary>
        /// Feature 一次激活开始（对应技能执行效果前）。
        /// 发送者：FeatureSystem.OnFeatureActivated
        /// </summary>
        public const string Activated = "feature:activated";
        public readonly record struct ActivatedEventData(FeatureContext Context);

        /// <summary>
        /// Feature 一次核心效果执行完成。
        /// 发送者：FeatureSystem.OnFeatureActivated 内部的 Execute 阶段之后
        /// </summary>
        public const string Executed = "feature:executed";
        public readonly record struct ExecutedEventData(FeatureContext Context);

        /// <summary>
        /// Feature 一次激活结束（对应技能执行效果后）。
        /// 发送者：FeatureSystem.OnFeatureEnded
        /// </summary>
        public const string Ended = "feature:ended";
        public readonly record struct EndedEventData(FeatureContext Context, FeatureEndReason Reason);

        /// <summary>
        /// Feature 被彻底移除（修改器已回滚）。
        /// 发送者：FeatureSystem.OnFeatureRemoved
        /// 接收者：Owner 上感知技能变化的组件
        /// </summary>
        public const string Removed = "feature:removed";
        public readonly record struct RemovedEventData(string FeatureName, IEntity Owner);
    }
}
