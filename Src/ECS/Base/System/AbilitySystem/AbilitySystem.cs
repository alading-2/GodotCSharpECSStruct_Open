using Godot;

/// <summary>
/// 技能系统 - 管理技能激活和执行逻辑
/// 
/// 职责：
/// - 接收 TryTrigger 请求（统一施法入口）
/// - 激活技能（就绪检查 → 消耗 → 冷却 → 执行）
/// - 目标选择（5 层目标系统）
/// - 通过 FeatureSystem / IFeatureHandler 执行具体技能逻辑并回传 AbilityExecutedResult
/// 
/// 注意：技能的增删查由 EntityManager.AddAbility/RemoveAbility/GetAbilities 负责
/// </summary>
public static class AbilitySystem
{
    private static readonly Log _log = new(nameof(AbilitySystem));

    // ==================== TryTrigger 入口 ====================

    /// <summary>
    /// 处理 TryTrigger 事件 - 统一施法入口
    /// </summary>
    public static void HandleTryTrigger(GameEventType.Ability.TryTriggerEventData eventData)
    {
        var context = eventData.Context;
        var resultContext = context.ResponseContext;

        if (context.Ability == null)
        {
            _log.Debug("TryTrigger 失败: Ability 为空");
            resultContext?.SetResult(TriggerResult.Failed);
            return;
        }

        var result = TryTriggerAbilityWithContext(context);
        resultContext?.SetResult(result);
    }

    /// <summary>
    /// 使用施法上下文触发技能（统一流水线入口）
    /// <returns>触发结果：Success / Failed / WaitingForTarget</returns>
    /// </summary>
    private static TriggerResult TryTriggerAbilityWithContext(CastContext abilityContext)
    {
        if (abilityContext.Ability == null || abilityContext.Caster == null) return TriggerResult.Failed;

        // 【新增】拦截已死亡角色的技能请求（防止周期性光环等技能死后继续触发新一轮的伤害判定）
        if (abilityContext.Caster != null && abilityContext.Caster.Data.Get<bool>(DataKey.IsDead))
        {
            _log.Debug($"技能触发失败: 施法者已阵亡");
            return TriggerResult.Failed;
        }

        var ability = abilityContext.Ability;

        // 事件驱动：就绪检查
        if (!CanUseAbility(ability))
        {
            return TriggerResult.Failed;
        }

        // 发送事件：进行目标解析
        // 注意：这里是流水线的核心环节，AbilityTargetSelectionComponent 会在此处填充 context.Targets 或 context.TargetPosition
        ability.Events.Emit(
            GameEventType.Ability.SelectTargets,
            new GameEventType.Ability.SelectTargetsEventData(abilityContext)
        );

        // ==================== 目标解析阶段（统一处理 Entity / Point / EntityOrPoint） ====================
        var targetSelection = (AbilityTargetSelection)ability.Data.Get<int>(DataKey.AbilityTargetSelection);

        // Entity 类型：必须有目标，否则中止流水线（避免空放浪费充能/冷却）
        if (targetSelection == AbilityTargetSelection.Entity
            && !abilityContext.HasPreselectedTargets)
        {
            var abilityName = ability.Data.Get<string>(DataKey.Name);
            _log.Debug($"目标验证失败: {abilityName} 需要 Entity 目标但未找到");
            return TriggerResult.Failed;
        }

        // Point 类型：需要玩家指定位置，如果还没有则进入异步瞄准
        if (targetSelection == AbilityTargetSelection.Point
            && !abilityContext.HasPreselectedPosition)
        {
            RequestPlayerTargeting(abilityContext);
            return TriggerResult.WaitingForTarget;
        }

        // EntityOrPoint 类型：先尝试 Entity 自动索敌，未命中则回退 Point 异步瞄准
        if (targetSelection == AbilityTargetSelection.EntityOrPoint
            && !abilityContext.HasPreselectedTargets && !abilityContext.HasPreselectedPosition)
        {
            RequestPlayerTargeting(abilityContext);
            return TriggerResult.WaitingForTarget;
        }

        // ==================== 资源消耗阶段 ====================
        var consumeContext = new EventContext();
        // 事件驱动：请求消耗资源（充能等）
        if (ability.Data.Get<bool>(DataKey.IsAbilityUsesCharges))
        {
            ability.Events.Emit(
                GameEventType.Ability.ConsumeCharge,
                new GameEventType.Ability.ConsumeChargeEventData(consumeContext)
            );
        }

        // 检查消耗是否成功
        if (!consumeContext.Success)
        {
            _log.Debug($"消耗资源失败: {consumeContext.FailReason}");
            return TriggerResult.Failed;
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
                new GameEventType.Ability.StartCooldownEventData()
            );
        }

        // ==================== 消耗阶段 ====================
        // 事件驱动:请求消耗成本 (魔法/能量等)
        var costContext = new EventContext();
        ability.Events.Emit(
            GameEventType.Ability.ConsumeCost,
            new GameEventType.Ability.ConsumeCostEventData(costContext)
        );

        if (!costContext.Success)
        {
            _log.Debug($"消耗成本失败: {costContext.FailReason}");
            return TriggerResult.Failed;
        }

        // 标记为执行中
        ability.Data.Set(DataKey.FeatureIsActive, true);

        // 发送激活事件，技能UI使用
        ability.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(abilityContext)
        );

        // Feature 生命周期钩子：Activated（AbilitySystem 负责构建 FeatureContext，将 CastContext 存入 ActivationData）
        var featureCtx = new FeatureContext
        {
            Owner = abilityContext.Caster,
            Feature = ability,
            ActivationData = abilityContext,
            SourceEventData = abilityContext.SourceEventData
        };
        FeatureSystem.OnFeatureActivated(featureCtx);

        EmitAbilityExecutedEvent(abilityContext, featureCtx);

        // Feature 生命周期钩子：Ended（复用同一 FeatureContext 实例）
        FeatureSystem.OnFeatureEnded(featureCtx);

        // 标记执行完成
        ability.Data.Set(DataKey.FeatureIsActive, false);

        var name = ability.Data.Get<string>(DataKey.Name);
        _log.Debug($"激活技能: {name}");
        return TriggerResult.Success;
    }

    // ==================== 异步瞄准支持 ====================

    /// <summary>
    /// 请求玩家异步瞄准（Point 类型技能）
    /// 发送 StartTargeting 全局事件，由 TargetingManager 接管后续流程
    /// </summary>
    private static void RequestPlayerTargeting(CastContext context)
    {
        var ability = context.Ability!;
        var range = ability.Data.Get<float>(DataKey.AbilityCastRange);
        var abilityName = ability.Data.Get<string>(DataKey.Name);

        _log.Debug($"技能 {abilityName} 需要玩家瞄准，进入异步模式，射程: {range}");

        GlobalEventBus.Global.Emit(
            GameEventType.Targeting.StartTargeting,
            new GameEventType.Targeting.StartTargetingEventData(context)
        );
    }

    /// <summary>
    /// 瞄准完成后恢复流水线（由 TargetingManager 回调）
    /// context.TargetPosition 已由 TargetingManager 填充
    /// </summary>
    public static TriggerResult ResumeAfterTargeting(CastContext context)
    {
        if (context.Ability == null || context.Caster == null)
        {
            _log.Warn("ResumeAfterTargeting: 上下文无效");
            return TriggerResult.Failed;
        }

        // 重新走完整流水线（CanUse 会再次检查，因为瞄准期间时间已过）
        // SelectTargets 事件仍会发出，但 HasPreselectedPosition=true 时组件会跳过
        return TryTriggerAbilityWithContext(context);
    }

    // ==================== 就绪检查 ====================

    /// <summary>
    /// 检查技能是否可用
    /// </summary>
    public static bool CanUseAbility(AbilityEntity ability)
    {
        if (ability == null) return false;

        var abilityName = ability.Data.Get<string>(DataKey.Name);
        var isEnabled = ability.Data.Get<bool>(DataKey.FeatureEnabled);
        var isActive = ability.Data.Get<bool>(DataKey.FeatureIsActive);

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
            new GameEventType.Ability.CheckCanUseEventData(context)
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
    /// 发送技能执行完成事件 - 结果由 IFeatureHandler.OnActivated 写入 FeatureContext.ExtraData
    /// </summary>
    private static void EmitAbilityExecutedEvent(CastContext context, FeatureContext featureCtx)
    {
        if (context.Ability == null) return;

        var ability = context.Ability;
        var abilityName = ability.Data.Get<string>(DataKey.Name) ?? string.Empty;
        var handlerId = ability.Data.Get<string>(DataKey.FeatureHandlerId);
        AbilityExecutedResult result;

        if (featureCtx.ExtraData.TryGetValue(nameof(AbilityExecutedResult), out var rawResult)
            && rawResult is AbilityExecutedResult executedResult)
        {
            result = executedResult;
        }
        else
        {
            if (string.IsNullOrEmpty(handlerId))
            {
                _log.Error($"技能 {abilityName} 未配置 FeatureHandlerId，执行结果将回退为默认值");
            }
            else if (!FeatureHandlerRegistry.HasHandler(handlerId))
            {
                _log.Warn($"技能 {abilityName} 未注册 FeatureHandler: {handlerId}，执行结果将回退为默认值");
            }
            else
            {
                _log.Warn($"技能 {abilityName} 的 FeatureHandler 未写入 AbilityExecutedResult，执行结果将回退为默认值");
            }

            result = new AbilityExecutedResult
            {
                TargetsHit = context.Targets?.Count ?? 0
            };
        }

        _log.Debug($"[AbilitySystem] 技能效果执行完成: '{abilityName}', 命中: {result.TargetsHit}");

        // 发送执行完成事件
        ability.Events.Emit(
            GameEventType.Ability.Executed,
            new GameEventType.Ability.ExecutedEventData(result)
        );
    }
}
