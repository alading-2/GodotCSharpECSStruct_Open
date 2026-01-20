using System.Linq;
using Godot;

/// <summary>
/// 触发组件 - 决定技能如何触发以及何时触发。
/// 
/// 支持 [Flags] 位运算，一个技能可同时启用多种触发模式:
/// - Manual: 手动触发。主动技能，响应玩家按键输入。
/// - OnEvent: 事件触发。被动技能，监听游戏内特定事件（如：杀敌、受伤、闪避）。
/// - Periodic: 周期触发。被动技能，固定时间间隔执行（如：每 5 秒回一次血）。
/// - Permanent: 永久生效。通常用于属性加成类被动，添加即生效。
/// - Auto: 自动触发。武器专用模式，自动搜寻目标并攻击。
/// </summary>
public partial class TriggerComponent : Node, IComponent
{
    private static readonly Log _log = new Log("TriggerComponent");

    // ================= 标准字段 =================

    /// <summary>实体数据引用</summary>
    private Data? _data;

    /// <summary>所属实体引用</summary>
    private IEntity? _entity;

    // ================= 状态变量 =================

    /// <summary>周期触发模式下的计时器实例</summary>
    private GameTimer? _periodicTimer;

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册时的初始化
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;

            // 初始化触发逻辑（如订阅事件、启动计时器等）
            InitializeTrigger();
        }
    }

    /// <summary>
    /// 组件重置逻辑
    /// </summary>
    public void OnComponentReset()
    {
        _periodicTimer?.Cancel();
        _periodicTimer = null;
    }

    /// <summary>
    /// 组件注销时的清理逻辑
    /// </summary>
    public void OnComponentUnregistered()
    {
        // 必须显式取消事件订阅，防止内存泄漏和无效回调
        UnsubscribeEvent();

        // 取消定时器
        _periodicTimer?.Cancel();
        _periodicTimer = null;

        _data = null;
        _entity = null;
    }

    // ================= 内部逻辑 =================

    /// <summary>
    /// 根据配置初始化不同的触发模式
    /// </summary>
    private void InitializeTrigger()
    {
        if (_data == null || _entity == null) return;

        // 获取触发模式位掩码
        var mode = (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);

        // 1. 事件触发初始化：订阅指定的事件
        if (mode.HasFlag(AbilityTriggerMode.OnEvent))
        {
            SubscribeToEvent();
        }

        // 2. 周期触发初始化：启动定时器
        if (mode.HasFlag(AbilityTriggerMode.Periodic))
        {
            StartPeriodicTimer();
        }

        // 3. 永久生效：通常由 AbilitySystem 在添加时直接处理一次
        if (mode.HasFlag(AbilityTriggerMode.Permanent))
        {
            // 逻辑预留
        }

        _log.Debug($"触发组件初始化: {_data.Get<string>(DataKey.Name)}, 模式: {mode}");
    }

    /// <summary>
    /// 订阅配置的触发事件
    /// </summary>
    private void SubscribeToEvent()
    {
        if (_data == null || _entity is not AbilityEntity ability) return;

        // 获取需要监听的事件类型字符串
        string eventType = _data.Get<string>(DataKey.AbilityTriggerEvent);
        if (string.IsNullOrEmpty(eventType))
        {
            _log.Warn($"技能 {_data.Get<string>(DataKey.Name)} 配置为事件触发但未指定事件类型");
            return;
        }

        // 查找技能的拥有者 (例如：玩家实体)
        // 绝大多数事件触发技能是监听其拥有者的事件（如玩家受伤）
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
            abilityId, EntityRelationshipType.ENTITY_TO_ABILITY).FirstOrDefault();

        var owner = !string.IsNullOrEmpty(ownerId)
            ? EntityManager.GetEntityById(ownerId) as IEntity
            : null;

        if (owner == null)
        {
            _log.Warn($"技能 {_data.Get<string>(DataKey.Name)} 未找到拥有者");
            return;
        }

        // 订阅全局事件总线
        // 注意：目前实现为订阅全局总线，逻辑上应通过 DataKey 配置是监听全局还是监听拥有者
        // 使用 On<object> 配合 EventBus 对 Action<object> 的支持，实现通用监听
        GlobalEventBus.Global.On<object>(eventType, OnEventTriggered);

        _log.Debug($"技能 {_data.Get<string>(DataKey.Name)} 订阅事件: {eventType}");
    }

    /// <summary>
    /// 取消事件订阅
    /// </summary>
    private void UnsubscribeEvent()
    {
        if (_data == null) return;

        string eventType = _data.Get<string>(DataKey.AbilityTriggerEvent);
        if (!string.IsNullOrEmpty(eventType))
        {
            GlobalEventBus.Global.Off<object>(eventType, OnEventTriggered);
        }
    }

    // ================= Godot 生命周期 =================

    /// <summary>
    /// 每帧更新，处理基于时间的触发逻辑（目前仅保留自动攻击）
    /// </summary>
    public override void _Process(double delta)
    {
        if (_data == null || _entity is not AbilityEntity) return;

        var mode = (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);

        // 处理自动攻击触发
        if (mode.HasFlag(AbilityTriggerMode.Auto))
        {
            ProcessAutoTrigger();
        }
    }

    // ================= 核心触发逻辑处理 =================

    /// <summary>
    /// 启动周期触发定时器
    /// </summary>
    private void StartPeriodicTimer()
    {
        if (_data == null || _entity is not AbilityEntity ability) return;

        float interval = _data.Get<float>(DataKey.AbilityTriggerInterval);
        if (interval <= 0f) return;

        // 取消旧的定时器
        _periodicTimer?.Cancel();

        // 创建新的循环定时器
        _periodicTimer = TimerManager.Instance.Loop(interval)
            .OnLoop(OnPeriodicTimerTick);

        _log.Debug($"技能 {_data.Get<string>(DataKey.Name)} 启动周期触发定时器: {interval}s");
    }

    /// <summary>
    /// 周期定时器触发时的回调
    /// </summary>
    private void OnPeriodicTimerTick()
    {
        if (_data == null || _entity is not AbilityEntity ability) return;

        // 事件驱动：检查技能是否可用（内部冷却等）
        var context = new EventContext();
        _entity?.Events.Emit(
            GameEventType.Ability.RequestCheckCanUse,
            new GameEventType.Ability.RequestCheckCanUseEventData(ability, context)
        );

        if (!context.Success) return;

        // 执行技能触发
        TriggerAbility();
    }

    /// <summary>
    /// 处理自动触发 (Auto - 武器/自动技能)
    /// </summary>
    private void ProcessAutoTrigger()
    {
        if (_entity is not AbilityEntity ability) return;

        // 事件驱动：检查技能是否可用（冷却等）
        var context = new EventContext();
        _entity?.Events.Emit(
            GameEventType.Ability.RequestCheckCanUse,
            new GameEventType.Ability.RequestCheckCanUseEventData(ability, context)
        );

        if (!context.Success) return;

        // 发送尝试激活请求
        // 自动触发模式下，由 AbilitySystem 配合 TargetingComponent 决定是否真正释放
        _entity?.Events.Emit(
            GameEventType.Ability.TryActivate,
            new GameEventType.Ability.TryActivateEventData(ability)
        );
    }

    // ================= 事件订阅与处理 =================

    /// <summary>
    /// 当监听的事件发生时的回调
    /// </summary>
    private void OnEventTriggered(object eventData)
    {
        if (_data == null || _entity is not AbilityEntity ability) return;

        // 1. 检查触发概率 (AbilityTriggerChance)
        float chance = _data.Get<float>(DataKey.AbilityTriggerChance);
        if (chance < 1f && GD.Randf() > chance)
        {
            return;
        }

        // 2. 事件驱动：检查技能是否可用（内部冷却等）
        var context = new EventContext();
        _entity?.Events.Emit(
            GameEventType.Ability.RequestCheckCanUse,
            new GameEventType.Ability.RequestCheckCanUseEventData(ability, context)
        );

        if (!context.Success) return;

        // 3. 执行触发（将事件数据传递给 AbilitySystem）
        TriggerAbility(eventData);
    }

    // ================= 触发执行 =================

    /// <summary>
    /// 核心触发逻辑：发送 TryActivate 请求，由 AbilitySystem 统一处理。
    /// 
    /// 修改说明（2026-01-20）：
    /// - 移除直接启动冷却的逻辑（由 AbilitySystem 处理）
    /// - 移除直接发送 Activated 事件（由 AbilitySystem 处理）
    /// - 改为只发送 TryActivate 请求
    /// </summary>
    /// <param name="sourceEventData">触发源事件数据（事件触发时携带）</param>
    private void TriggerAbility(object? sourceEventData = null)
    {
        if (_entity is not AbilityEntity ability) return;

        // 查找施法者
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
            abilityId, EntityRelationshipType.ENTITY_TO_ABILITY).FirstOrDefault();
        var caster = !string.IsNullOrEmpty(ownerId)
            ? EntityManager.GetEntityById(ownerId) as IEntity
            : null;

        // 发送 TryActivate 请求，由 AbilitySystem 统一处理后续流程
        // 包括：CanUse 检查、消耗资源、启动冷却、选择目标、执行效果
        _entity.Events.Emit(
            GameEventType.Ability.TryActivate,
            new GameEventType.Ability.TryActivateEventData(
                ability,
                caster,
                null,  // RequestedTargets: 自动选取
                sourceEventData
            )
        );

        _log.Debug($"触发技能请求: {_data?.Get<string>(DataKey.Name)}");
    }

    // ================= 外部公共接口 =================

    /// <summary>
    /// 尝试手动触发技能。
    /// 由玩家输入系统或 AbilitySystem 在接收到按键时调用。
    /// </summary>
    /// <returns>触发成功返回 true，否则返回 false（如冷却中、模式不匹配）</returns>
    public bool TryManualTrigger()
    {
        if (_data == null || _entity is not AbilityEntity ability) return false;

        var mode = (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);

        // 验证是否支持手动触发模式
        if (!mode.HasFlag(AbilityTriggerMode.Manual))
        {
            _log.Warn($"技能 {_data.Get<string>(DataKey.Name)} 不支持手动触发");
            return false;
        }

        // 事件驱动：检查技能是否可用（冷却、充能等）
        var context = new EventContext();
        _entity?.Events.Emit(
            GameEventType.Ability.RequestCheckCanUse,
            new GameEventType.Ability.RequestCheckCanUseEventData(ability, context)
        );

        if (!context.Success)
        {
            _log.Debug($"技能 {_data.Get<string>(DataKey.Name)} 不可用: {context.FailReason}");
            return false;
        }

        // 执行触发
        TriggerAbility();
        return true;
    }

    /// <summary>
    /// 获取当前技能的所有触发模式
    /// </summary>
    public AbilityTriggerMode GetTriggerMode()
    {
        if (_data == null) return AbilityTriggerMode.None;
        return (AbilityTriggerMode)_data.Get<int>(DataKey.AbilityTriggerMode);
    }

    /// <summary>
    /// 检查技能是否包含指定的触发模式
    /// </summary>
    public bool HasTriggerMode(AbilityTriggerMode mode)
    {
        return GetTriggerMode().HasFlag(mode);
    }
}
