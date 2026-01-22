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
    /// 尝试触发技能（带预填充上下文）
    /// 用于主动技能输入系统，外部预先填充目标/位置
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityName">技能名称</param>
    /// <param name="context">预填充的施法上下文</param>
    /// <returns>是否成功触发</returns>
    public static bool TryTriggerAbility(IEntity owner, string abilityName, CastContext context)
    {
        var ability = EntityManager.GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _log.Debug($"技能不存在: {abilityName}");
            return false;
        }

        context.Ability = ability;
        context.Caster = owner;

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

        // 验证输入是否符合配置 (移除自动 SelectTargets 逻辑)
        if (!ValidateTargetInput(ability, context))
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

        // 事件驱动:请求启动冷却
        ability.Events.Emit(
            GameEventType.Ability.StartCooldown,
            new GameEventType.Ability.StartCooldownEventData(ability)
        );

        // 事件驱动:请求消耗成本 (魔法/能量等)
        var costContext = new EventContext();
        ability.Events.Emit(
            GameEventType.Ability.ConsumeCost,
            new GameEventType.Ability.ConsumeCostEventData(ability, costContext)
        );

        if (!costContext.Success)
        {
            _log.Debug($"消耗成本失败: {costContext.FailReason}");
            return false;
        }

        // 标记为执行中
        ability.Data.Set(DataKey.AbilityIsActive, true);

        // 发送激活事件
        // 注意：这里直接传递上下文中的 targets，如果没有则传空列表
        ability.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(ability, context.Targets ?? new List<IEntity>())
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

        return true;
    }

    // ==================== 目标/输入验证 ====================

    /// <summary>
    /// 验证施法上下文中的输入数据是否符合技能配置
    /// 原则：既然配置了需要 Unit/Point，调用者(Input/Trigger)就必须设置好 CastContext。
    /// </summary>
    private static bool ValidateTargetInput(AbilityEntity ability, CastContext context)
    {
        var selection = ability.Data.Get<AbilityTargetSelection>(DataKey.AbilityTargetSelection);

        switch (selection)
        {
            case AbilityTargetSelection.Unit:
                if (!context.HasPreselectedTargets)
                {
                    _log.Error($"技能 '{ability.Data.Get<string>(DataKey.Name)}' 配置为 [Unit] 但上下文无目标单位！");
                    return false;
                }
                break;

            case AbilityTargetSelection.Point:
                if (!context.HasPreselectedPosition)
                {
                    _log.Error($"技能 '{ability.Data.Get<string>(DataKey.Name)}' 配置为 [Point] 但上下文无目标位置！");
                    return false;
                }
                break;

            case AbilityTargetSelection.UnitOrPoint:
                if (!context.HasPreselectedTargets && !context.HasPreselectedPosition)
                {
                    _log.Error($"技能 '{ability.Data.Get<string>(DataKey.Name)}' 配置为 [UnitOrPoint] 但上下文无任何输入！");
                    return false;
                }
                break;

            // None 模式不强制要求输入
            case AbilityTargetSelection.None:
                break;
        }

        return true;
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

