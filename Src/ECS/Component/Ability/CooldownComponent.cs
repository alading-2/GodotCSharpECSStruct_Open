using Godot;

/// <summary>
/// 冷却组件 - 管理技能的冷却时间
/// 
/// 适用于:
/// - 主动技能: 使用后冷却
/// - 被动技能: 内部冷却 (防止触发过于频繁)
/// - 武器技能: 攻击间隔
/// 
/// 遵循 Component 规范:
/// - 无状态设计，所有数据存储在 Data 中
/// - 冷却时间支持修改器 (CooldownReduction)
/// </summary>
public partial class CooldownComponent : Node, IComponent
{
    private static readonly Log _log = new("CooldownComponent");

    // ================= 标准字段 =================
    private Data? _data;
    private IEntity? _entity;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
        }
    }

    public void OnComponentReset()
    {
        // 重置冷却状态
        _data?.Set(DataKey.AbilityCooldownRemaining, 0f);
        _data?.Set(DataKey.AbilityIsCoolingDown, false);
    }

    public void OnComponentUnregistered()
    {
        _data = null;
        _entity = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Process(double delta)
    {
        if (_data == null) return;

        // 仅在冷却中时更新
        if (!_data.Get<bool>(DataKey.AbilityIsCoolingDown)) return;

        float remaining = _data.Get<float>(DataKey.AbilityCooldownRemaining);
        remaining -= (float)delta;

        if (remaining <= 0f)
        {
            // 冷却完成
            _data.Set(DataKey.AbilityCooldownRemaining, 0f);
            _data.Set(DataKey.AbilityIsCoolingDown, false);

            // 发送冷却完成事件
            _entity?.Events.Emit(
                GameEventType.Ability.Ready,
                new GameEventType.Ability.ReadyEventData(_entity as AbilityEntity)
            );

            _log.Debug($"技能冷却完成: {_data.Get<string>(DataKey.Name)}");
        }
        else
        {
            _data.Set(DataKey.AbilityCooldownRemaining, remaining);
        }
    }

    // ================= 公共接口 =================

    /// <summary>检查冷却是否完成</summary>
    public bool IsReady()
    {
        if (_data == null) return false;
        return !_data.Get<bool>(DataKey.AbilityIsCoolingDown);
    }

    /// <summary>启动冷却计时</summary>
    public void StartCooldown()
    {
        if (_data == null) return;

        float totalCooldown = GetTotalCooldown();
        if (totalCooldown <= 0f) return; // 无冷却时间则跳过

        _data.Set(DataKey.AbilityCooldownRemaining, totalCooldown);
        _data.Set(DataKey.AbilityIsCoolingDown, true);

        _log.Debug($"技能开始冷却: {_data.Get<string>(DataKey.Name)}, 时长: {totalCooldown:F2}s");
    }

    /// <summary>重置冷却（立即完成冷却）</summary>
    public void ResetCooldown()
    {
        if (_data == null) return;

        _data.Set(DataKey.AbilityCooldownRemaining, 0f);
        _data.Set(DataKey.AbilityIsCoolingDown, false);

        _log.Debug($"技能冷却重置: {_data.Get<string>(DataKey.Name)}");
    }

    /// <summary>获取冷却进度 (0=刚开始, 1=完成)</summary>
    public float GetCooldownProgress()
    {
        if (_data == null) return 1f;

        float total = GetTotalCooldown();
        if (total <= 0f) return 1f;

        float remaining = _data.Get<float>(DataKey.AbilityCooldownRemaining);
        return 1f - (remaining / total);
    }

    /// <summary>获取剩余冷却时间 (秒)</summary>
    public float GetRemainingCooldown()
    {
        if (_data == null) return 0f;
        return _data.Get<float>(DataKey.AbilityCooldownRemaining);
    }

    /// <summary>获取总冷却时间 (应用冷却缩减后)</summary>
    public float GetTotalCooldown()
    {
        if (_data == null) return 0f;

        // 获取基础冷却时间 (支持修改器)
        float baseCooldown = _data.Get<float>(DataKey.AbilityCooldown);

        // 获取冷却缩减 (支持修改器)
        float cdReduction = _data.Get<float>(DataKey.CooldownReduction);

        // 应用冷却缩减 (最多减少 80%)
        cdReduction = Mathf.Clamp(cdReduction, 0f, 0.8f);

        return baseCooldown * (1f - cdReduction);
    }
}
