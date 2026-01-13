using Godot;
using System;

/// <summary>
/// 生命值管理组件 - HP 读写的唯一入口
///
/// 核心职责：
/// - 提供 CurrentHp/MaxHp 属性访问
/// - ApplyDamage() - 受伤入口，触发 Damaged 事件
/// - ApplyHeal() - 治疗入口，触发 Healed 事件
/// - SetHp() - 直接设置（用于复活等特殊场景）
///
/// 设计原则：
/// - 谁修改数据，谁触发事件
/// - 所有 HP 变更事件集中在此组件触发
/// </summary>
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;


    // ================= 属性访问 =================

    /// <summary>当前生命值</summary>
    public float CurrentHp => _data.Get<float>(DataKey.CurrentHp);

    /// <summary>最大生命值</summary>
    public float MaxHp => _data.Get<float>(DataKey.FinalHp);

    /// <summary>生命值百分比 (0-1)</summary>
    public float HpPercent => MaxHp > 0 ? CurrentHp / MaxHp : 0f;

    /// <summary>是否满血，RecoverySystem使用</summary>
    public bool IsFullHp => CurrentHp >= MaxHp;


    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;

            // ✅ 监听治疗请求事件（命令事件）
            _entity.Events.On<GameEventType.Unit.HealRequestEventData>(
                GameEventType.Unit.HealRequest, ApplyHeal);
        }

    }

    public void OnComponentUnregistered()
    {
        _entity = null;
        _data = null;
    }

    // ================= 核心 API =================

    /// <summary>
    /// 应用治疗生命/魔法
    /// </summary>
    /// <param name="evt">治疗请求事件数据</param>
    public void ApplyHeal(GameEventType.Unit.HealRequestEventData evt)
    {
        float amount = evt.Amount;
        HealSource source = evt.Source;
        if (amount <= 0) return;
        if (_data == null || _entity == null) return;

        // 死亡检测
        if (_data.Get<bool>(DataKey.IsDead))
        {
            return;
        }

        // 禁疗检测（复活来源可以绕过禁疗）
        if (source != HealSource.Revive)
        {
            bool isHealingDisabled = _data.Get<bool>(DataKey.IsDisableHealthRecovery);
            if (isHealingDisabled)
            {
                _log.Debug("禁疗状态，治疗无效");
                return;
            }
        }

        float oldHp = CurrentHp;
        float maxHp = MaxHp;
        float newHp = Mathf.Min(oldHp + amount, maxHp);
        float actualHeal = newHp - oldHp;

        if (actualHeal <= 0) return;

        // 修改 HP
        _data.Set(DataKey.CurrentHp, newHp);

        // 触发 HealthChanged 事件
        _entity.Events.Emit(GameEventType.Data.HealthChanged,
            new GameEventType.Data.HealthChangedEventData(oldHp, newHp));

        // ✅ 触发治疗完成事件（结果事件：通知 UI 飘字等）
        // 复活来源不触发飘字
        if (source != HealSource.Revive)
        {
            _entity.Events.Emit(GameEventType.Unit.HealApplied,
                new GameEventType.Unit.HealAppliedEventData(
                    amount,        // 原始请求量
                    actualHeal,    // 实际治疗量（去溢出）
                    source
                ));
            _log.Debug($"治疗: {actualHeal}, 来源: {source}, HP: {oldHp} -> {newHp}");
        }
    }

}
