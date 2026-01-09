using Godot;
using System;

/// <summary>
/// 生命值组件
/// <para>
/// 遵循无状态设计：所有数据存储在 Entity.Data 中。
/// 本组件只负责逻辑处理和事件分发。
/// </para>
/// </summary>
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    // ================= IComponent 实现 =================

    private IEntity? _entity;
    private Data? _data;

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

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        _log.Debug($"就绪, MaxHp: {_data?.Get<float>(DataKey.BaseHp)}");
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// 修改生命值
    /// <param name="amount">正数回血，负数扣血</param>
    /// </summary>
    public void ModifyHealth(float amount)
    {
        if (_data == null || _entity == null) return;

        float currentHp = _data.Get<float>(DataKey.CurrentHp);
        float maxHp = _data.Get<float>(DataKey.BaseHp, 100f);
        float oldHp = currentHp;

        // 计算新值
        float newHp = Mathf.Clamp(currentHp + amount, 0, maxHp);

        // 如果数值没变，直接返回
        if (Mathf.IsEqualApprox(newHp, oldHp)) return;

        // 应用数据
        _data.Set(DataKey.CurrentHp, newHp);

        // 触发变更逻辑
        OnHpChanged(oldHp, newHp);
    }

    /// <summary>
    /// 重置生命值（对象池复用时调用）
    /// </summary>
    public void Reset()
    {
        if (_data == null) return;

        float currentHp = _data.Get<float>(DataKey.CurrentHp);
        float maxHp = _data.Get<float>(DataKey.BaseHp, 100f);

        if (!Mathf.IsEqualApprox(currentHp, maxHp))
        {
            _data.Set(DataKey.CurrentHp, maxHp);
            OnHpChanged(currentHp, maxHp);
        }
    }

    /// <summary>
    /// 处理血量变更事件分发
    /// </summary>
    private void OnHpChanged(float oldHp, float newHp)
    {
        if (_entity == null) return;

        float delta = newHp - oldHp;

        // 1. 分发具体事件
        if (delta < 0)
        {
            float damage = Mathf.Abs(delta);
            _entity.Events.Emit(GameEventType.Unit.Damaged, new GameEventType.Unit.DamagedEventData(damage));
        }
        else if (delta > 0)
        {
            _entity.Events.Emit(GameEventType.Unit.Healed, new GameEventType.Unit.HealedEventData(delta));
        }

        // 2. 分发统一变更事件 (可选，UI使用)
        // _entity.Events.Emit("HpChanged", newHp);

        // 3. 死亡判定
        if (newHp <= 0 && oldHp > 0)
        {
            _entity.Events.Emit(GameEventType.Unit.Dead, new GameEventType.Unit.DeadEventData());
        }
    }
}
