/// <summary>
/// Unit 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Unit
    {
        public const string Created = "unit:created";
        public const string Destroyed = "unit:destroyed";

        /// <summary>单位死亡</summary>
        public const string Dead = "unit:dead";
        public readonly record struct DeadEventData();

        /// <summary>单位受到伤害</summary>
        public const string Damaged = "unit:damaged";
        public readonly record struct DamagedEventData(float Amount);

        /// <summary>单位受到治疗</summary>
        public const string Healed = "unit:healed";
        public readonly record struct HealedEventData(float Amount);

        /// <summary>单位生命值变更</summary>
        public const string HealthChanged = "unit:health_changed";
        // 保留 HealthChangedEvent 以备未来统一使用
        public readonly record struct HealthChangedEventData(float OldHp, float NewHp);
    }
}
