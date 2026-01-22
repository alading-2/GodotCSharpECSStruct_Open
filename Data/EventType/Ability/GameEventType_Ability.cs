/// <summary>
/// Ability 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Ability
    {
        // ================= Common / System (通用/系统事件) =================

        /// <summary>技能被添加到单位</summary>
        public const string Added = "ability:added";
        /// <summary>技能被添加事件数据</summary>
        public readonly record struct AddedEventData(AbilityEntity Ability, IEntity Owner);

        /// <summary>技能被移除</summary>
        public const string Removed = "ability:removed";
        /// <summary>技能被移除事件数据</summary>
        public readonly record struct RemovedEventData(string abilityName, IEntity Owner);

        /// <summary>技能升级</summary>
        public const string LevelUp = "ability:level_up";
        /// <summary>技能升级事件数据</summary>
        public readonly record struct LevelUpEventData(AbilityEntity? Ability, int OldLevel, int NewLevel);

        /// <summary>
        /// 请求检查技能是否可用。
        /// 响应者：所有资源/限制类组件 (CooldownComponent是否冷却完成, ChargeComponent是否充能足够, CostComponent是否消耗得起 等)
        /// </summary>
        public const string CheckCanUse = "ability:check_can_use";
        /// <summary>检查可用性事件数据</summary>
        public readonly record struct CheckCanUseEventData(AbilityEntity Ability, EventContext Context);

        // ================= AbilitySystem (技能系统核心流程) =================

        /// <summary>技能激活成功 (开始执行效果)</summary>
        public const string Activated = "ability:activated";
        /// <summary>技能激活事件数据</summary>
        public readonly record struct ActivatedEventData(AbilityEntity? Ability, System.Collections.Generic.List<IEntity>? Targets);

        /// <summary>技能效果执行完成</summary>
        public const string Executed = "ability:executed";
        /// <summary>技能执行完成事件数据</summary>
        public readonly record struct ExecutedEventData(AbilityEntity? Ability, AbilityExecutedResult? Result);

        /// <summary>技能被取消 (如蓄力被打断)</summary>
        public const string Cancelled = "ability:cancelled";
        /// <summary>技能取消事件数据</summary>
        public readonly record struct CancelledEventData(AbilityEntity? Ability, string Reason);


        // ================= TriggerComponent (触发组件) =================

        /// <summary>
        /// 尝试激活技能。
        /// 发送者：TriggerComponent (当满足触发条件时，如按下按键或周期已到)
        /// 接收者：AbilitySystem (执行具体激活逻辑，如目标选择)
        /// </summary>
        /// <summary>
        /// 尝试激活技能。
        /// 发送者：TriggerComponent (当满足触发条件时，如按下按键或周期已到)
        /// 接收者：AbilitySystem (执行具体激活逻辑，如目标选择)
        /// </summary>
        public const string TryTrigger = "ability:try_trigger";
        /// <summary>
        /// 尝试激活事件数据
        /// 使用 CastContext 传递所有上下文信息，避免参数重复
        /// </summary>
        public readonly record struct TryTriggerEventData(CastContext Context);


        // ================= CooldownComponent (冷却组件) =================

        /// <summary>技能冷却完成</summary>
        public const string Ready = "ability:ready";
        /// <summary>技能冷却完成事件数据</summary>
        public readonly record struct ReadyEventData(AbilityEntity? Ability);

        /// <summary>
        /// 请求启动冷却。
        /// 发送者：AbilitySystem (技能激活后)
        /// 接收者：CooldownComponent
        /// </summary>
        /// <summary>
        /// 请求启动冷却。
        /// 发送者：AbilitySystem (技能激活后)
        /// 接收者：CooldownComponent
        /// </summary>
        public const string StartCooldown = "ability:start_cooldown";
        /// <summary>启动冷却事件数据</summary>
        public readonly record struct StartCooldownEventData(AbilityEntity Ability);

        /// <summary>
        /// 请求重置冷却（立即完成）。
        /// 发送者：任意逻辑 (如刷新球效果)
        /// 接收者：CooldownComponent
        /// </summary>
        /// <summary>
        /// 请求重置冷却（立即完成）。
        /// 发送者：任意逻辑 (如刷新球效果)
        /// 接收者：CooldownComponent
        /// </summary>
        public const string ResetCooldown = "ability:reset_cooldown";
        /// <summary>重置冷却事件数据</summary>
        public readonly record struct ResetCooldownEventData(AbilityEntity Ability);

        // ================= ChargeComponent (充能组件) =================

        /// <summary>充能恢复</summary>
        public const string ChargeRestored = "ability:charge_restored";
        /// <summary>充能恢复事件数据</summary>
        public readonly record struct ChargeRestoredEventData(int CurrentCharges, int MaxCharges);

        /// <summary>
        /// 使用技能消耗充能事件。
        /// 发送者：AbilitySystem (技能激活时)
        /// 接收者：ChargeComponent
        /// </summary>
        public const string ConsumeCharge = "ability:consume_charge";
        /// <summary>消耗充能事件数据</summary>
        public readonly record struct ConsumeChargeEventData(AbilityEntity Ability, EventContext Context);

        /// <summary>
        /// 请求增加充能事件。
        /// 发送者：道具系统、Buff 等外部逻辑
        /// 接收者：ChargeComponent
        /// </summary>
        public const string AddCharge = "ability:add_charge";
        /// <summary>增加充能事件数据</summary>
        public readonly record struct AddChargeEventData(int Amount);

        // ================= CostComponent (消耗组件) =================

        /// <summary>
        /// 请求消耗成本 (魔法/能量/生命值等)。
        /// 发送者：AbilitySystem (技能激活时)
        /// 接收者：CostComponent
        /// </summary>
        public const string ConsumeCost = "ability:consume_cost";
        /// <summary>消耗成本请求事件数据</summary>
        public readonly record struct ConsumeCostEventData(AbilityEntity Ability, EventContext Context);

        /// <summary>
        /// 成本消耗完成事件 (供 UI 监听)。
        /// 发送者：CostComponent
        /// 接收者：UI、统计系统等
        /// </summary>
        public const string CostConsumed = "ability:cost_consumed";
        /// <summary>成本消耗完成事件数据</summary>
        public readonly record struct CostConsumedEventData(AbilityEntity Ability, AbilityCostType CostType, float Amount);
    }
}

// Context 类定义已移至 AbilityContext.cs
// 包括：AbilityCanUseCheckContext, AbilityConsumeChargeContext, AbilityExecuteResult
