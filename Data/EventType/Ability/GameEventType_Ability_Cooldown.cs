/// <summary>
/// Ability 冷却相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>技能冷却完成</summary>
        public const string Ready = "ability:ready";
        /// <summary>技能冷却完成事件数据</summary>
        public readonly record struct ReadyEventData();

        /// <summary>
        /// 请求启动冷却。
        /// 发送者：AbilitySystem (技能激活后)
        /// 接收者：CooldownComponent
        /// </summary>
        public const string StartCooldown = "ability:start_cooldown";
        /// <summary>启动冷却事件数据</summary>
        public readonly record struct StartCooldownEventData();

        /// <summary>
        /// 请求重置冷却（立即完成）。
        /// 发送者：任意逻辑 (如刷新球效果)
        /// 接收者：CooldownComponent
        /// </summary>
        public const string ResetCooldown = "ability:reset_cooldown";
        /// <summary>重置冷却事件数据</summary>
        public readonly record struct ResetCooldownEventData();
    }
}
