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
    private GameTimer? _timer;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;

            // 订阅事件驱动请求
            SubscribeEvents();
        }
    }

    public void OnComponentReset()
    {
    }

    public void OnComponentUnregistered()
    {
        CancelTimer();
        _data = null;
        _entity = null;
    }

    // ================= 事件驱动 =================

    /// <summary>订阅请求事件</summary>
    private void SubscribeEvents()
    {
        if (_entity == null) return;
        // 监听请求检查可用性事件
        _entity.Events.On<GameEventType.Ability.RequestCheckCanUseEventData>(
            GameEventType.Ability.RequestCheckCanUse,
            OnRequestCheckCanUse
        );
        // 监听请求启动冷却事件
        _entity.Events.On<GameEventType.Ability.RequestStartCooldownEventData>(
            GameEventType.Ability.RequestStartCooldown,
            StartCooldown
        );
        // 监听请求重置冷却事件
        _entity.Events.On<GameEventType.Ability.RequestResetCooldownEventData>(
            GameEventType.Ability.RequestResetCooldown,
            ResetCooldown
        );
    }

    /// <summary>响应可用性检查请求</summary>
    private void OnRequestCheckCanUse(GameEventType.Ability.RequestCheckCanUseEventData eventData)
    {
        if (!IsReady())
        {
            eventData.Context.SetFailed("技能冷却中");
        }
    }

    public override void _ExitTree()
    {
        CancelTimer();
    }

    // ================= 公共接口 =================

    /// <summary>检查冷却是否完成</summary>
    public bool IsReady()
    {
        // 如果 Timer 存在，说明正在冷却中
        return _timer == null;
    }

    /// <summary>启动冷却计时</summary>
    public void StartCooldown(GameEventType.Ability.RequestStartCooldownEventData eventData)
    {
        if (_data == null) return;

        // 计算总冷却时间
        float totalCooldown = GetTotalCooldown();
        if (totalCooldown <= 0f) return;

        CancelTimer();

        string abilityName = _data.Get<string>(DataKey.Name);

        // 创建 Timer
        _timer = TimerManager.Instance.Delay(totalCooldown)
            .WithTag("AbilityCooldown")
            .OnComplete(() =>
            {
                // 冷却完成
                _timer = null;

                _entity?.Events.Emit(
                    GameEventType.Ability.Ready,
                    new GameEventType.Ability.ReadyEventData(_entity as AbilityEntity)
                );

                _log.Debug($"技能冷却完成: {abilityName}");
            });

        _log.Debug($"技能开始冷却: {abilityName}, 时长: {totalCooldown:F2}s");
    }

    /// <summary>重置冷却（立即完成冷却）</summary>
    public void ResetCooldown(GameEventType.Ability.RequestResetCooldownEventData eventData)
    {
        CancelTimer();
        _log.Debug($"技能冷却重置: {_data?.Get<string>(DataKey.Name)}");
    }

    /// <summary>获取冷却进度 (0.0=刚开始, 1.0=完成)</summary>
    public float GetCooldownProgress()
    {
        if (_timer == null) return 1f;
        return _timer.Progress;
    }

    /// <summary>获取剩余冷却时间 (秒)</summary>
    public float GetRemainingCooldown()
    {
        if (_timer == null) return 0f;
        return _timer.Remaining;
    }

    /// <summary>获取最终冷却时间 (应用冷却缩减后)</summary>
    public float GetTotalCooldown()
    {
        if (_data == null) return 0f;

        // 获取基础冷却时间 (支持修改器)
        float baseCooldown = _data.Get<float>(DataKey.AbilityCooldown);

        // 获取冷却缩减 (支持修改器)
        float cdReduction = _data.Get<float>(DataKey.CooldownReduction);

        // 使用 MyMath 统一公式
        return MyMath.CalculateFinalCooldownTime(baseCooldown, cdReduction);
    }

    // ================= 私有方法 =================

    private void CancelTimer()
    {
        if (_timer != null)
        {
            _timer.Cancel();
            _timer = null;
        }
    }
}
