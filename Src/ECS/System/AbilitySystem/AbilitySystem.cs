using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 技能系统 - 管理技能激活和执行逻辑
/// 
/// 职责：
/// - 接收 TryTrigger 请求（统一施法入口）
/// - 激活技能（就绪检查 → 消耗 → 冷却 → 执行）
/// - 目标选择（5 层目标系统）
/// - 调用 AbilityExecutorRegistry 执行效果
/// 
/// 注意：技能的增删查由 EntityManager.AddAbility/RemoveAbility/GetAbilities 负责
/// </summary>
public static class AbilitySystem
{
    private static readonly Log _log = new("AbilitySystem");

    // ==================== TryTrigger 入口 ====================

    /// <summary>
    /// 处理 TryTrigger 事件 - 统一施法入口
    /// 
    /// 调用者：
    /// - TriggerComponent 发送的 TryTrigger 事件
    /// - 可直接调用（如输入系统、AI）
    /// </summary>
    public static void HandleTryTrigger(GameEventType.Ability.TryTriggerEventData eventData)
    {
        // 直接使用传入的上下文
        var context = eventData.Context;

        if (context.Ability == null)
        {
            _log.Debug("TryTrigger 失败: Ability 为空");
            return;
        }

        TryTriggerAbilityWithContext(context);
    }

    /// <summary>
    /// 尝试直接触发技能，不通过TriggerComponent触发
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityName">技能名称</param>
    /// <returns>是否成功触发</returns>
    public static bool TryTriggerAbility(IEntity owner, string abilityName)
    {
        var ability = EntityManager.GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _log.Debug($"技能不存在: {abilityName}");
            return false;
        }

        var context = new CastContext
        {
            Ability = ability,
            Caster = owner
        };

        return TryTriggerAbilityWithContext(context);
    }

    /// <summary>
    /// 使用施法上下文触发技能
    /// <returns>是否成功触发</returns>
    /// </summary>
    private static bool TryTriggerAbilityWithContext(CastContext context)
    {
        if (context.Ability == null) return false;

        var ability = context.Ability;

        // 事件驱动：就绪检查
        if (!CanUseAbility(ability))
        {
            return false;
        }

        var consumeContext = new EventContext();
        // 事件驱动：请求消耗资源（充能等）
        if (ability.Data.Get<bool>(DataKey.IsAbilityUsesCharges))
        {
            ability.Events.Emit(
                GameEventType.Ability.ConsumeCharge,
                new GameEventType.Ability.ConsumeChargeEventData(ability, consumeContext)
            );
        }

        // 检查消耗是否成功
        if (!consumeContext.Success)
        {
            _log.Debug($"消耗资源失败: {consumeContext.FailReason}");
            return false;
        }

        // 事件驱动：请求启动冷却
        ability.Events.Emit(
            GameEventType.Ability.StartCooldown,
            new GameEventType.Ability.StartCooldownEventData(ability)
        );

        // 标记为执行中
        ability.Data.Set(DataKey.AbilityIsActive, true);

        // 目标选择：优先使用预选目标，否则自动选取
        List<IEntity> targets;
        if (context.HasPreselectedTargets)
        {
            targets = context.Targets!;
        }
        else
        {
            targets = SelectTargets(ability, context);
        }

        // 更新上下文的目标列表
        context.Targets = targets;

        // 发送激活事件
        ability.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(ability, targets)
        );

        // 执行效果（通过 AbilityExecutorRegistry）
        ExecuteAbilityEffects(context);

        // 标记执行完成
        ability.Data.Set(DataKey.AbilityIsActive, false);

        var name = ability.Data.Get<string>(DataKey.Name);
        _log.Debug($"激活技能: {name}");
        return true;
    }

    // ==================== 就绪检查 ====================

    /// <summary>
    /// 检查技能是否可用
    /// </summary>
    public static bool CanUseAbility(AbilityEntity ability)
    {
        if (ability == null) return false;

        var abilityName = ability.Data.Get<string>(DataKey.Name);
        var isEnabled = ability.Data.Get<bool>(DataKey.AbilityEnabled);
        var isActive = ability.Data.Get<bool>(DataKey.AbilityIsActive);

        // 检查启用状态
        if (!isEnabled)
        {
            _log.Debug($"技能 {abilityName} 未启用");
            return false;
        }

        // 检查是否正在执行
        if (isActive)
        {
            _log.Debug($"技能 {abilityName} 正在执行中");
            return false;
        }

        // 事件驱动：请求检查可用性（冷却、充能等组件响应）
        var context = new EventContext();
        ability.Events.Emit(
            GameEventType.Ability.CheckCanUse,
            new GameEventType.Ability.CheckCanUseEventData(ability, context)
        );

        if (!context.Success)
        {
            _log.Debug($"技能 {abilityName} 不可用: {context.FailReason}");
            return false;
        }

        // TODO: 检查资源消耗 (CostComponent)

        return true;
    }

    // ==================== 目标选择 ====================

    /// <summary>
    /// 基于 5 层目标系统选择目标
    /// </summary>
    /// <param name="ability">技能实体</param>
    /// <param name="context">施法上下文</param>
    /// <returns>选中的目标列表</returns>
    private static List<IEntity> SelectTargets(AbilityEntity ability, CastContext context)
    {
        var owner = context.Caster;
        if (owner == null) return new List<IEntity>();

        // 1. 获取选取原点 (Origin)
        var origin = (AbilityTargetOrigin)ability.Data.Get<int>(DataKey.AbilityTargetOrigin);

        switch (origin)
        {
            case AbilityTargetOrigin.Self:
                // 以施法者自身作为目标
                return new List<IEntity> { owner };

            case AbilityTargetOrigin.EventSource:
                // 从施法上下文中获取触发此技能的事件源实体（例如反击技能的目标是攻击者）
                if (context.SourceEventData is IEntity eventSource)
                {
                    return new List<IEntity> { eventSource };
                }
                return new List<IEntity>();

            case AbilityTargetOrigin.Unit:
            case AbilityTargetOrigin.Point:
            case AbilityTargetOrigin.Cursor:
                // 委托给 TargetSelector 进行几何查询
                return SelectTargetsUsingSelector(ability, owner);

            default:
                return new List<IEntity>();
        }
    }

    /// <summary>
    /// 使用 TargetSelector 进行几何查询
    /// </summary>
    private static List<IEntity> SelectTargetsUsingSelector(AbilityEntity ability, IEntity owner)
    {
        // 获取施法者位置和朝向
        Vector2 origin = Vector2.Zero;
        Vector2? forward = null;

        if (owner is Node2D ownerNode2D)
        {
            origin = ownerNode2D.GlobalPosition;
            // 从旋转角度计算前向向量
            forward = Vector2.Right.Rotated(ownerNode2D.GlobalRotation);
        }

        // 构造查询配置
        var query = new TargetSelectorQuery
        {
            // 几何参数
            Geometry = (AbilityTargetGeometry)ability.Data.Get<int>(DataKey.AbilityTargetGeometry),
            Origin = origin,
            Forward = forward,
            Range = ability.Data.Get<float>(DataKey.AbilityRange),
            Width = ability.Data.Get<float>(DataKey.AbilityWidth),
            Length = ability.Data.Get<float>(DataKey.AbilityLength),
            Angle = ability.Data.Get<float>(DataKey.AbilityAngle),
            ChainCount = ability.Data.Get<int>(DataKey.AbilityChainCount),
            ChainRange = ability.Data.Get<float>(DataKey.AbilityChainRange),

            // 过滤参数
            CenterEntity = owner,
            TeamFilter = (AbilityTargetTeamFilter)ability.Data.Get<int>(DataKey.AbilityTargetTeamFilter),
            TypeFilter = (AbilityTargetTypeFilter)ability.Data.Get<int>(DataKey.AbilityTargetTypeFilter),

            // 排序与限制
            Sorting = (AbilityTargetSorting)ability.Data.Get<int>(DataKey.AbilityTargetSorting),
            MaxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets)
        };

        // 调用 TargetSelector
        return TargetSelector.Query(query);
    }


    // ==================== 效果执行 ====================

    /// <summary>
    /// 执行技能效果 - 通过 AbilityExecutorRegistry 调用具体执行器
    /// </summary>
    private static void ExecuteAbilityEffects(CastContext context)
    {
        if (context.Ability == null) return;

        var ability = context.Ability;
        var abilityName = ability.Data.Get<string>(DataKey.Name) ?? string.Empty;

        // 调用执行器注册表
        var result = AbilityExecutorRegistry.Execute(abilityName, context);

        // 发送执行完成事件
        ability.Events.Emit(
            GameEventType.Ability.Executed,
            new GameEventType.Ability.ExecutedEventData(ability, result)
        );
    }
}

