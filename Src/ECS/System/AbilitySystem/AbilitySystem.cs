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
    private static readonly Log _log = new(nameof(AbilitySystem));

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

        // 发送事件：进行目标解析
        // 注意：这里是流水线的核心环节，AbilityTargetSelectionComponent 会在此处填充 context.Targets 或 context.TargetPosition
        ability.Events.Emit(
            GameEventType.Ability.SelectTargets,
            new GameEventType.Ability.SelectTargetsEventData(context)
        );

        // 注意：如果目标选择失败且技能要求必须有目标，AbilityTargetSelectionComponent 应该负责
        // 标记失败或在 SelectTargets 之后进行验证。
        // 为保持灵活性，我们在系统层不再做强验证，而是在执行层由 Executor 判断或
        // 在此处增加一个通用的就绪性二次检查（可选）。

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
        // 注意：如果是 Periodic (周期性) 技能，由 TriggerComponent 负责循环控制频率，
        // 这里不应该再启动 CooldownComponent 的冷却计时，否则会导致 TriggerComponent 下次循环时
        // 技能还在冷却中 (Race Condition) 或 刚结束冷却但 CanUse 检查失败。
        // 因此：周期性技能跳过冷却启动。
        var triggerMode = (AbilityTriggerMode)ability.Data.Get<int>(DataKey.AbilityTriggerMode);
        if (!triggerMode.HasFlag(AbilityTriggerMode.Periodic))
        {
            ability.Events.Emit(
                GameEventType.Ability.StartCooldown,
                new GameEventType.Ability.StartCooldownEventData(ability)
            );
        }

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




    // ==================== 效果执行 ====================

    /// <summary>
    /// 执行技能效果 - 通过 AbilityExecutorRegistry 调用具体执行器
    /// </summary>
    private static void ExecuteAbilityEffects(CastContext context)
    {
        if (context.Ability == null) return;

        var ability = context.Ability;
        var abilityName = ability.Data.Get<string>(DataKey.Name) ?? string.Empty;

        _log.Debug($"[AbilitySystem] 开始执行技能效果: '{abilityName}'");
        // 调用执行器注册表
        var result = AbilityExecutorRegistry.Execute(abilityName, context);
        _log.Debug($"[AbilitySystem] 技能效果执行完成: '{abilityName}', 命中: {result.TargetsHit}");

        // 发送执行完成事件
        ability.Events.Emit(
            GameEventType.Ability.Executed,
            new GameEventType.Ability.ExecutedEventData(ability, result)
        );
    }
}

