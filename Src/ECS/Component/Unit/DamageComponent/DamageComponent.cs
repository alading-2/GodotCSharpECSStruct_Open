using Godot;

/// <summary>
/// 伤害组件 - 伤害接收与致死判定的唯一入口
///
/// 核心职责：
/// - TakeDamage() - 受伤入口，扣血并判定致死
/// - 伤害统计（TotalDamageTaken 等）
/// - 发送事件：HealthChanged、Damaged、LethalDamage
///
/// 设计原则：
/// - 与 HealthComponent 分离：HealthComponent 只管治疗和 HP 访问
/// - 与 LifecycleComponent 协作：本组件判定致死后发送 LethalDamage 事件，由 LifecycleComponent 执行死亡流程
/// </summary>
public partial class DamageComponent : Node, IComponent
{
    private static readonly Log _log = new("DamageComponent");

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    // ================= 统计数据 =================

    /// <summary>累计受到的伤害</summary>
    public float TotalDamageTaken => _data?.Get<float>(DataKey.TotalDamageTaken) ?? 0f;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;
        }
    }

    public void OnComponentUnregistered()
    {
        _entity = null;
        _data = null;
    }

    // ================= 核心 API =================

    /// <summary>
    /// 接收伤害（由 DamageService 通过 HealthExecutionProcessor 调用）
    /// </summary>
    /// <param name="info">伤害上下文，包含最终伤害、攻击者等信息</param>
    public void TakeDamage(DamageInfo info)
    {
        if (_data == null || _entity == null) return;

        // 无敌检测
        if (_data.Get<bool>(DataKey.IsInvulnerable))
        {
            _log.Debug("无敌状态，伤害无效");
            return;
        }

        // 死亡检测
        if (_data.Get<bool>(DataKey.IsDead))
        {
            return;
        }

        float amount = info.FinalDamage;
        if (amount <= 0) return;

        float oldHp = _data.Get<float>(DataKey.CurrentHp);
        float newHp = Mathf.Max(0f, oldHp - amount);

        // 修改 HP
        _data.Set(DataKey.CurrentHp, newHp);

        // 统计伤害
        _data.Add(DataKey.TotalDamageTaken, amount);

        // 发送 HealthChanged 事件（供 UI 等使用）
        _entity.Events.Emit(GameEventType.Data.HealthChanged,
            new GameEventType.Data.HealthChangedEventData(oldHp, newHp));

        // 发送 Damaged 事件（供飘字等使用）
        _entity.Events.Emit(GameEventType.Unit.Damaged,
            new GameEventType.Unit.DamagedEventData(amount, info.Instigator, info.Type));

        _log.Debug($"受到伤害: {amount}, HP: {oldHp} -> {newHp}");

        // ✅ 致死判定 - 由 DamageComponent 负责判定，发送事件通知 LifecycleComponent
        if (newHp <= 0)
        {
            _log.Debug("HP 归零，发送致死伤害事件");
            _entity.Events.Emit(GameEventType.Unit.Kill,
                new GameEventType.Unit.KillEventData(info.Type, info.Instigator));
        }
    }

    /// <summary>
    /// 简化版受伤方法（用于脚本或调试）
    /// </summary>
    /// <param name="amount">伤害量</param>
    /// <param name="attacker">攻击者（可选）</param>
    /// <param name="damageType">伤害类型</param>
    public void TakeDamage(float amount, IEntity? attacker = null, DamageType damageType = DamageType.True)
    {
        var info = new DamageInfo
        {
            BaseDamage = amount,
            FinalDamage = amount,
            Type = damageType,
            Instigator = attacker as IUnit
        };
        TakeDamage(info);
    }
}
