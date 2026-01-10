/// <summary>
/// Unit 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Unit
    {
        /// <summary>单位创建</summary>
        public const string Created = "unit:created";
        /// <summary>单位销毁</summary>
        public const string Destroyed = "unit:destroyed";

        /// <summary>单位受到伤害</summary>
        public const string Damaged = "unit:damaged";
        /// <summary>单位受到伤害事件数据</summary>
        public readonly record struct DamagedEventData(
            float Amount,
            IEntity Attacker = null,
            DamageType Type = DamageType.True);

        // ================= 治疗事件（命令/结果分离）=================

        /// <summary>请求治疗（命令事件：外部 → HealthComponent）</summary>
        public const string HealRequest = "unit:heal_request";
        /// <summary>请求治疗事件数据</summary>
        public readonly record struct HealRequestEventData(float Amount, HealSource Source = HealSource.Unknown);

        /// <summary>治疗已应用（结果事件：HealthComponent → UI/统计）</summary>
        public const string HealApplied = "unit:heal_applied";
        /// <summary>治疗已应用事件数据（携带原始量和实际量）</summary>
        public readonly record struct HealAppliedEventData(
            float RequestedAmount,   // 原始请求量
            float ActualAmount,      // 实际治疗量（去溢出）
            HealSource Source
        );

        // ================= LifecycleComponent 相关事件 =================
        /// <summary>单位死亡</summary>
        public const string Dead = "unit:dead";
        /// <summary>单位死亡事件数据</summary>
        public readonly record struct DeadEventData(DeathType Type = DeathType.Normal);

        /// <summary>单位状态变化</summary>
        public const string StateChanged = "unit:state_changed";
        /// <summary>单位状态变化事件数据</summary>
        public readonly record struct StateChangedEventData(string Key, string OldValue, string NewValue);

        /// <summary>单位开始复活</summary>
        public const string Reviving = "unit:reviving";
        /// <summary>单位开始复活事件数据</summary>
        public readonly record struct RevivingEventData(float Duration);

        /// <summary>单位复活完成</summary>
        public const string Revived = "unit:revived";
        /// <summary>单位复活完成事件数据</summary>
        public readonly record struct RevivedEventData();
    }
}
