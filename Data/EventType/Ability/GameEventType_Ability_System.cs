/// <summary>
/// Ability 系统/通用事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>技能被添加到单位</summary>
        public const string Added = "ability:added";
        /// <summary>技能被添加事件数据</summary>
        public readonly record struct AddedEventData(AbilityEntity Ability, IEntity Owner);

        /// <summary>技能被移除</summary>
        public const string Removed = "ability:removed";
        /// <summary>技能被移除事件数据</summary>
        public readonly record struct RemovedEventData(string abilityName, string abilityId, IEntity Owner);

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
        public readonly record struct CheckCanUseEventData(EventContext Context);

        /// <summary>
        /// 尝试激活技能。
        /// 发送者：TriggerComponent (当满足触发条件时，如按下按键或周期已到)
        /// 接收者：AbilitySystem (执行具体激活逻辑，如目标选择)
        /// </summary>
        public const string TryTrigger = "ability:try_trigger";
        /// <summary>
        /// 尝试激活事件数据
        /// Context: 施法上下文（包含 ResponseContext，用于回传 TriggerResult）
        /// </summary>
        public readonly record struct TryTriggerEventData(CastContext Context);
    }
}
