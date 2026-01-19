using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 技能系统 - 管理技能激活和执行逻辑
/// 
/// 职责：
/// - 激活技能（就绪检查 → 消耗 → 冷却 → 执行）
/// - 目标选择（5 层目标系统）
/// - 效果执行协调
/// 
/// 注意：技能的增删查由 EntityManager.AddAbility/RemoveAbility/GetAbilities 负责
/// </summary>
public static class AbilitySystem
{
    private static readonly Log _log = new("AbilitySystem");

    // ==================== 技能激活 ====================

    /// <summary>
    /// 尝试激活技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityName">技能名称</param>
    /// <returns>是否成功激活</returns>
    public static bool TryActivateAbility(IEntity owner, string abilityName)
    {
        var ability = EntityManager.GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _log.Debug($"技能不存在: {abilityName}");
            return false;
        }

        return TryActivateAbilityInternal(ability);
    }

    /// <summary>
    /// 内部激活逻辑
    /// </summary>
    private static bool TryActivateAbilityInternal(AbilityEntity ability)
    {
        if (ability == null) return false;

        // 事件驱动：就绪检查
        if (!CanUseAbility(ability))
        {
            return false;
        }

        // 事件驱动：请求消耗资源（充能等）
        var consumeContext = new EventContext();
        ability.Events.Emit(
            GameEventType.Ability.ConsumeCharge,
            new GameEventType.Ability.ConsumeChargeEventData(ability, consumeContext)
        );

        // 检查消耗是否成功
        if (!consumeContext.Success)
        {
            _log.Debug($"消耗资源失败: {consumeContext.FailReason}");
            return false;
        }

        // 事件驱动：请求启动冷却
        ability.Events.Emit(
            GameEventType.Ability.RequestStartCooldown,
            new GameEventType.Ability.RequestStartCooldownEventData(ability)
        );

        // 标记为执行中
        ability.Data.Set(DataKey.AbilityIsActive, true);

        // 选择目标
        var targets = SelectTargets(ability);

        // 发送激活事件
        ability.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(ability, targets)
        );

        // 查找拥有者并发送事件
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
            abilityId, EntityRelationshipType.ENTITY_TO_ABILITY).FirstOrDefault();

        if (!string.IsNullOrEmpty(ownerId))
        {
            var owner = EntityManager.GetEntityById(ownerId) as IEntity;
            owner?.Events.Emit(
                GameEventType.Ability.Activated,
                new GameEventType.Ability.ActivatedEventData(ability, targets)
            );
        }

        // 执行效果
        ExecuteAbilityEffects(ability, targets);

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
            GameEventType.Ability.RequestCheckCanUse,
            new GameEventType.Ability.RequestCheckCanUseEventData(ability, context)
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
    private static List<IEntity> SelectTargets(AbilityEntity ability)
    {
        var targets = new List<IEntity>();

        // 1. 获取拥有者 (通过 EntityRelationshipManager)
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        ).FirstOrDefault();

        var owner = EntityManager.GetEntityById(ownerId) as IEntity;
        if (owner == null) return targets;

        // 1. 获取选取原点
        var origin = (AbilityTargetOrigin)ability.Data.Get<int>(DataKey.AbilityTargetOrigin);

        switch (origin)
        {
            case AbilityTargetOrigin.Self:
                targets.Add(owner);
                break;

            case AbilityTargetOrigin.EventSource:
                var eventData = ability.Data.Get<object>("_TriggerEventData");
                if (eventData is IEntity eventSource)
                {
                    targets.Add(eventSource);
                }
                break;

            case AbilityTargetOrigin.Unit:
            case AbilityTargetOrigin.Point:
            case AbilityTargetOrigin.Cursor:
                targets = SelectTargetsByGeometry(ability, owner);
                break;
        }

        // 排序和数量限制
        if (targets.Count > 1)
        {
            targets = SortAndLimitTargets(ability, targets, owner);
        }

        return targets;
    }

    /// <summary>
    /// 基于几何形状选择目标
    /// </summary>
    private static List<IEntity> SelectTargetsByGeometry(AbilityEntity ability, IEntity owner)
    {
        var targets = new List<IEntity>();

        var geometry = (AbilityTargetGeometry)ability.Data.Get<int>(DataKey.AbilityTargetGeometry);
        var teamFilter = (AbilityTargetTeamFilter)ability.Data.Get<int>(DataKey.AbilityTargetTeamFilter);
        var typeFilter = (AbilityTargetTypeFilter)ability.Data.Get<int>(DataKey.AbilityTargetTypeFilter);
        var range = ability.Data.Get<float>(DataKey.AbilityRange);

        // TODO: 实际实现需要物理查询或 EntityManager 查询
        switch (geometry)
        {
            case AbilityTargetGeometry.Single:
                break;
            case AbilityTargetGeometry.Circle:
                // targets = QueryEntitiesInCircle(owner.Position, range, teamFilter, typeFilter);
                break;
            case AbilityTargetGeometry.Box:
                break;
            case AbilityTargetGeometry.Line:
                break;
            case AbilityTargetGeometry.Cone:
                break;
            case AbilityTargetGeometry.Chain:
                break;
            case AbilityTargetGeometry.Global:
                break;
        }

        return targets;
    }

    /// <summary>
    /// 排序并限制目标数量
    /// </summary>
    private static List<IEntity> SortAndLimitTargets(AbilityEntity ability, List<IEntity> targets, IEntity owner)
    {
        var sorting = (AbilityTargetSorting)ability.Data.Get<int>(DataKey.AbilityTargetSorting);
        var maxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets);

        // TODO: 实现排序逻辑

        // 限制数量
        if (maxTargets > 0 && targets.Count > maxTargets)
        {
            targets = targets.GetRange(0, maxTargets);
        }

        return targets;
    }

    // ==================== 效果执行 ====================

    private static void ExecuteAbilityEffects(AbilityEntity ability, List<IEntity> targets)
    {
        // TODO: 调用 AbilityEffect 执行器

        var result = new AbilityExecuteResult
        {
            TargetsHit = targets.Count
        };

        ability.Events.Emit(
            GameEventType.Ability.Executed,
            new GameEventType.Ability.ExecutedEventData(ability, result)
        );
    }
}
