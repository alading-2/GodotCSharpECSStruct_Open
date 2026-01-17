/// <summary>
/// Ability 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Ability
    {
        // ================= 生命周期事件 =================

        /// <summary>技能被添加到单位</summary>
        public const string Added = "ability:added";
        /// <summary>技能被添加事件数据</summary>
        public readonly record struct AddedEventData(AbilityEntity Ability, IEntity Owner);

        /// <summary>技能被移除</summary>
        public const string Removed = "ability:removed";
        /// <summary>技能被移除事件数据</summary>
        public readonly record struct RemovedEventData(string AbilityId, IEntity Owner);

        // ================= 激活事件 =================

        /// <summary>尝试激活技能 (由 TriggerComponent 发送)</summary>
        public const string TryActivate = "ability:try_activate";
        /// <summary>尝试激活事件数据</summary>
        public readonly record struct TryActivateEventData(AbilityEntity? Ability);

        /// <summary>技能激活成功</summary>
        public const string Activated = "ability:activated";
        /// <summary>技能激活事件数据</summary>
        public readonly record struct ActivatedEventData(AbilityEntity? Ability, System.Collections.Generic.List<IEntity>? Targets);

        /// <summary>技能效果执行完成</summary>
        public const string Executed = "ability:executed";
        /// <summary>技能执行完成事件数据</summary>
        public readonly record struct ExecutedEventData(AbilityEntity? Ability, AbilityExecuteResult? Result);

        /// <summary>技能被取消</summary>
        public const string Cancelled = "ability:cancelled";
        /// <summary>技能取消事件数据</summary>
        public readonly record struct CancelledEventData(AbilityEntity? Ability, string Reason);

        // ================= 冷却/充能事件 =================

        /// <summary>技能冷却完成</summary>
        public const string Ready = "ability:ready";
        /// <summary>技能冷却完成事件数据</summary>
        public readonly record struct ReadyEventData(AbilityEntity? Ability);

        /// <summary>充能恢复</summary>
        public const string ChargeRestored = "ability:charge_restored";
        /// <summary>充能恢复事件数据</summary>
        public readonly record struct ChargeRestoredEventData(int CurrentCharges, int MaxCharges);

        // ================= 等级事件 =================

        /// <summary>技能升级</summary>
        public const string LevelUp = "ability:level_up";
        /// <summary>技能升级事件数据</summary>
        public readonly record struct LevelUpEventData(AbilityEntity? Ability, int OldLevel, int NewLevel);
    }
}

/// <summary>
/// 技能执行结果
/// </summary>
public class AbilityExecuteResult
{
    /// <summary>是否执行成功</summary>
    public bool Success { get; set; }

    /// <summary>造成的总伤害</summary>
    public float TotalDamage { get; set; }

    /// <summary>治疗的总量</summary>
    public float TotalHeal { get; set; }

    /// <summary>命中的目标数</summary>
    public int TargetsHit { get; set; }

    /// <summary>错误信息 (如果失败)</summary>
    public string? ErrorMessage { get; set; }
}
