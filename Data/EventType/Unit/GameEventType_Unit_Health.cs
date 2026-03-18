/// <summary>
/// Unit 生命/伤害相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>单位受到伤害</summary>
        public const string Damaged = "unit:damaged";
        /// <summary>单位受到伤害事件数据</summary>
        public readonly record struct DamagedEventData(
            IEntity Victim,
            float Amount,
            IEntity? Attacker = null,
            DamageType Type = DamageType.True,
            bool IsCritical = false);

        /// <summary>单位闪避成功（结果事件：DodgeProcessor -> UI 飘字）</summary>
        public const string Dodged = "unit:dodged";
        /// <summary>单位闪避事件数据</summary>
        public readonly record struct DodgedEventData(IEntity Victim, IEntity? Attacker = null);

        /// <summary>请求治疗（命令事件：外部 -> HealthComponent）</summary>
        public const string HealRequest = "unit:heal_request";
        /// <summary>请求治疗事件数据</summary>
        public readonly record struct HealRequestEventData(float Amount, HealSource Source = HealSource.Unknown);

        /// <summary>治疗已应用（结果事件：HealthComponent -> UI/统计）</summary>
        public const string HealApplied = "unit:heal_applied";
        /// <summary>治疗已应用事件数据（携带原始量和实际量）</summary>
        public readonly record struct HealAppliedEventData(
            IEntity Victim,
            float RequestedAmount,
            float ActualAmount,
            HealSource Source
        );

        /// <summary>
        /// 单位被击杀（建议使用 GlobalEventBus 广播）
        /// </summary>
        /// <remarks>
        /// <para>发送者：HealthComponent（HP<=0）</para>
        /// <para>监听者：DamageStatisticsSystem（击杀统计）、LifecycleComponent（通过 Victim 筛选）</para>
        /// </remarks>
        public const string Killed = "unit:killed";
        /// <summary>单位被击杀事件数据</summary>
        public readonly record struct KilledEventData(
            IEntity? Victim,
            IEntity? Killer,
            DeathType DeathType = DeathType.Normal,
            DamageType DamageType = DamageType.True
        );
    }
}
