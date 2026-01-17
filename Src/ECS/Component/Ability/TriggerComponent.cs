using Godot;

/// <summary>
/// 触发组件 - 决定技能如何触发
/// 
/// 这是区分主动/被动技能的核心组件:
/// - Manual: 主动技能，需要玩家输入
/// - OnEvent: 被动技能，监听特定事件
/// - Periodic: 被动技能，固定时间间隔触发
/// - Permanent: 被动技能，永久生效
/// - Auto: 武器技能，自动攻击
/// </summary>
public partial class TriggerComponent : Node, IComponent
{
    private static readonly Log _log = new("TriggerComponent");

    // ================= 标准字段 =================
    private Data? _data;
    private IEntity? _entity;
    private AbilityEntity? _ability;

    // ================= 周期触发计时器 =================
    private float _periodicTimer;

    // ================= 事件处理委托 =================
    private System.Action<object>? _eventHandler;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
            _ability = entity as AbilityEntity;

            // 初始化触发逻辑
            InitializeTrigger();
        }
    }

    public void OnComponentReset()
    {
        _periodicTimer = 0f;
    }

    public void OnComponentUnregistered()
    {
        // 取消事件订阅
        UnsubscribeEvent();

        _data = null;
        _entity = null;
        _ability = null;
    }

    // ================= 初始化 =================

    private void InitializeTrigger()
    {
        if (_data == null || _entity == null) return;

        var mode = (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);

        switch (mode)
        {
            case AbilityTriggerMode.OnEvent:
                SubscribeToEvent();
                break;
            case AbilityTriggerMode.Periodic:
                _periodicTimer = 0f;
                break;
            case AbilityTriggerMode.Permanent:
                // 永久生效的被动技能，直接执行一次效果 (如属性加成)
                // 通常由 AbilitySystem 在添加时处理
                break;
            default:
                // Manual 和 Auto 由外部触发
                break;
        }

        _log.Debug($"触发组件初始化: {_data.Get<string>(DataKey.Name)}, 模式: {mode}");
    }

    // ================= Godot 生命周期 =================

    public override void _Process(double delta)
    {
        if (_data == null || _ability == null) return;

        var mode = (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);

        switch (mode)
        {
            case AbilityTriggerMode.Periodic:
                ProcessPeriodicTrigger((float)delta);
                break;
            case AbilityTriggerMode.Auto:
                ProcessAutoTrigger();
                break;
        }
    }

    // ================= 周期触发 =================

    private void ProcessPeriodicTrigger(float delta)
    {
        if (_data == null) return;

        float interval = _data.Get<float>(DataKey.AbilityTriggerInterval);
        if (interval <= 0f) return;

        _periodicTimer += delta;

        if (_periodicTimer >= interval)
        {
            _periodicTimer -= interval;

            // 检查冷却 (内部冷却)
            var cooldown = EntityManager.GetComponent<CooldownComponent>(_entity as Node);
            if (cooldown != null && !cooldown.IsReady())
            {
                return;
            }

            // 触发技能
            TriggerAbility();
        }
    }

    // ================= 自动触发 (武器技能) =================

    private void ProcessAutoTrigger()
    {
        // 检查冷却
        var cooldown = EntityManager.GetComponent<CooldownComponent>(_entity as Node);
        if (cooldown != null && !cooldown.IsReady())
        {
            return;
        }

        // 检查是否有有效目标
        // 实际目标选择由 TargetingComponent 处理
        // 这里只发送尝试激活事件，由 AbilitySystem 处理
        _entity?.Events.Emit(
            GameEventType.Ability.TryActivate,
            new GameEventType.Ability.TryActivateEventData(_ability)
        );
    }

    // ================= 事件触发 =================

    private void SubscribeToEvent()
    {
        if (_data == null || _ability == null) return;

        string eventType = _data.Get<string>(DataKey.AbilityTriggerEvent);
        if (string.IsNullOrEmpty(eventType))
        {
            _log.Warn($"技能 {_data.Get<string>(DataKey.Name)} 配置为事件触发但未指定事件类型");
            return;
        }

        // 获取技能拥有者
        var owner = _ability.GetOwner();
        if (owner == null)
        {
            _log.Warn($"技能 {_data.Get<string>(DataKey.Name)} 未找到拥有者");
            return;
        }

        // 订阅拥有者的事件
        _eventHandler = (eventData) => OnEventTriggered(eventType, eventData);

        // 使用泛型事件订阅
        // 注意：这里需要根据具体事件类型进行订阅
        // 简化实现：订阅到全局事件总线
        GlobalEventBus.Global.On(eventType, _eventHandler);

        _log.Debug($"技能 {_data.Get<string>(DataKey.Name)} 订阅事件: {eventType}");
    }

    private void UnsubscribeEvent()
    {
        if (_eventHandler == null || _data == null) return;

        string eventType = _data.Get<string>(DataKey.AbilityTriggerEvent);
        if (!string.IsNullOrEmpty(eventType))
        {
            GlobalEventBus.Global.Off(eventType, _eventHandler);
        }

        _eventHandler = null;
    }

    private void OnEventTriggered(string eventType, object eventData)
    {
        if (_data == null || _ability == null) return;

        // 检查触发概率
        float chance = _data.Get<float>(DataKey.AbilityTriggerChance);
        if (chance < 1f && GD.Randf() > chance)
        {
            return;
        }

        // 检查冷却 (内部冷却)
        var cooldown = EntityManager.GetComponent<CooldownComponent>(_entity as Node);
        if (cooldown != null && !cooldown.IsReady())
        {
            return;
        }

        // 保存事件数据以供效果使用
        _data.Set("_TriggerEventData", eventData);

        // 触发技能
        TriggerAbility();
    }

    // ================= 触发技能 =================

    /// <summary>
    /// 触发技能 (内部调用)
    /// </summary>
    private void TriggerAbility()
    {
        if (_ability == null || _entity == null) return;

        // 启动冷却
        var cooldown = EntityManager.GetComponent<CooldownComponent>(_entity as Node);
        cooldown?.StartCooldown();

        // 发送技能激活事件
        _entity.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(_ability, null)
        );

        _log.Debug($"触发技能: {_data?.Get<string>(DataKey.Name)}");
    }

    // ================= 外部调用接口 =================

    /// <summary>
    /// 手动触发技能 (供 AbilitySystem 调用)
    /// 仅当触发模式为 Manual 时有效
    /// </summary>
    public bool TryManualTrigger()
    {
        if (_data == null) return false;

        var mode = (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);
        if (mode != AbilityTriggerMode.Manual)
        {
            _log.Warn($"技能 {_data.Get<string>(DataKey.Name)} 不是手动触发模式");
            return false;
        }

        // 检查冷却
        var cooldown = EntityManager.GetComponent<CooldownComponent>(_entity as Node);
        if (cooldown != null && !cooldown.IsReady())
        {
            _log.Debug($"技能 {_data.Get<string>(DataKey.Name)} 正在冷却");
            return false;
        }

        TriggerAbility();
        return true;
    }

    /// <summary>
    /// 获取当前触发模式
    /// </summary>
    public AbilityTriggerMode GetTriggerMode()
    {
        if (_data == null) return AbilityTriggerMode.Manual;
        return (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);
    }
}
