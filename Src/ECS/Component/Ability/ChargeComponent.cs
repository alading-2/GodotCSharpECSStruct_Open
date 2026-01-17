using Godot;

/// <summary>
/// 充能组件 - 管理技能的充能次数
/// 
/// 仅用于主动技能:
/// - 被动技能没有"使用"的概念，因此不需要充能
/// - 充能可以积累多次使用 (如冲刺 2 次)
/// - 充能耗尽后需要等待恢复
/// </summary>
public partial class ChargeComponent : Node, IComponent
{
    private static readonly Log _log = new("ChargeComponent");

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

            // 初始化当前充能为最大充能
            int maxCharges = _data.Get<int>(DataKey.AbilityMaxCharges);
            _data.Set(DataKey.AbilityCurrentCharges, maxCharges);
            _data.Set(DataKey.AbilityChargeTimer, 0f);
        }
    }

    public void OnComponentReset()
    {
        if (_data == null) return;

        // 重置充能状态
        int maxCharges = _data.Get<int>(DataKey.AbilityMaxCharges);
        _data.Set(DataKey.AbilityCurrentCharges, maxCharges);
        _data.Set(DataKey.AbilityChargeTimer, 0f);
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

        // 检查是否需要恢复充能
        int currentCharges = _data.Get<int>(DataKey.AbilityCurrentCharges);
        int maxCharges = _data.Get<int>(DataKey.AbilityMaxCharges);

        if (currentCharges < maxCharges)
        {
            float chargeTimer = _data.Get<float>(DataKey.AbilityChargeTimer);
            float chargeTime = _data.Get<float>(DataKey.AbilityChargeTime);

            chargeTimer += (float)delta;

            if (chargeTimer >= chargeTime)
            {
                // 恢复一次充能
                chargeTimer -= chargeTime;
                currentCharges++;
                _data.Set(DataKey.AbilityCurrentCharges, currentCharges);

                _log.Debug($"技能充能恢复: {_data.Get<string>(DataKey.Name)}, 当前: {currentCharges}/{maxCharges}");

                // 发送充能恢复事件
                _entity?.Events.Emit(
                    GameEventType.Ability.ChargeRestored,
                    new GameEventType.Ability.ChargeRestoredEventData(currentCharges, maxCharges)
                );
            }

            _data.Set(DataKey.AbilityChargeTimer, chargeTimer);
        }
    }

    // ================= 公共接口 =================

    /// <summary>是否有可用充能</summary>
    public bool HasCharge()
    {
        if (_data == null) return false;
        return _data.Get<int>(DataKey.AbilityCurrentCharges) > 0;
    }

    /// <summary>消耗一次充能</summary>
    public bool ConsumeCharge()
    {
        if (_data == null) return false;

        int currentCharges = _data.Get<int>(DataKey.AbilityCurrentCharges);
        if (currentCharges <= 0)
        {
            _log.Debug($"技能 {_data.Get<string>(DataKey.Name)} 充能不足");
            return false;
        }

        _data.Set(DataKey.AbilityCurrentCharges, currentCharges - 1);

        int maxCharges = _data.Get<int>(DataKey.AbilityMaxCharges);
        _log.Debug($"消耗充能: {_data.Get<string>(DataKey.Name)}, 剩余: {currentCharges - 1}/{maxCharges}");

        return true;
    }

    /// <summary>获取当前充能次数</summary>
    public int GetCurrentCharges()
    {
        if (_data == null) return 0;
        return _data.Get<int>(DataKey.AbilityCurrentCharges);
    }

    /// <summary>获取最大充能次数</summary>
    public int GetMaxCharges()
    {
        if (_data == null) return 1;
        return _data.Get<int>(DataKey.AbilityMaxCharges);
    }

    /// <summary>获取下一次充能恢复进度 (0~1)</summary>
    public float GetChargeProgress()
    {
        if (_data == null) return 1f;

        int currentCharges = _data.Get<int>(DataKey.AbilityCurrentCharges);
        int maxCharges = _data.Get<int>(DataKey.AbilityMaxCharges);

        if (currentCharges >= maxCharges) return 1f;

        float chargeTimer = _data.Get<float>(DataKey.AbilityChargeTimer);
        float chargeTime = _data.Get<float>(DataKey.AbilityChargeTime);

        if (chargeTime <= 0f) return 1f;

        return chargeTimer / chargeTime;
    }

    /// <summary>立即恢复所有充能</summary>
    public void RestoreAllCharges()
    {
        if (_data == null) return;

        int maxCharges = _data.Get<int>(DataKey.AbilityMaxCharges);
        _data.Set(DataKey.AbilityCurrentCharges, maxCharges);
        _data.Set(DataKey.AbilityChargeTimer, 0f);

        _log.Debug($"技能充能完全恢复: {_data.Get<string>(DataKey.Name)}");
    }
}
