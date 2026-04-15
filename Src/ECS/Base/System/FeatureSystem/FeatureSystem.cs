using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// Feature 系统 - 管理 Feature 完整生命周期
///
/// 生命周期：Granted → [Enabled ⇄ Disabled] → [Activated → Execute → Ended]* → Removed
///
/// 主要职责：
/// - Granted：应用数据驱动 Modifier + 调用 IFeatureHandler.OnGranted
/// - Removed：调用 IFeatureHandler.OnRemoved + 按来源批量回滚 Modifier
/// - Enabled：调用 IFeatureHandler.OnEnabled + 发出 Feature.Enabled 事件
/// - Disabled：调用 IFeatureHandler.OnDisabled + 发出 Feature.Disabled 事件
/// - Activated：调用 IFeatureHandler.OnActivated（通知）+ OnExecute（执行）+ 发出事件
/// - Ended：调用 IFeatureHandler.OnEnded + 发出 Feature.Ended 事件
/// - ExecuteActions：批量执行 IFeatureAction 列表
///
/// 设计原则：只依赖 IEntity，不引入任何子系统专有类型（无 AbilityEntity / CastContext）。
/// 调用方（如 AbilitySystem）在调用前自行构建 FeatureContext，将专有数据放入 ActivationData，
/// 从 ExecuteResult 读取执行结果。
///
/// 挂载点（调用方）：
/// - EntityManager.AddAbility → OnFeatureGranted
/// - EntityManager.RemoveAbility → OnFeatureRemoved（Destroy 之前）
/// - AbilitySystem（或其他激活方）→ 自建 FeatureContext → OnFeatureActivated / OnFeatureEnded
/// </summary>
public static class FeatureSystem
{
    private static readonly Log _log = new(nameof(FeatureSystem));

    // ==================== Granted ====================

    /// <summary>Feature 被授予时调用（由 EntityManager.AddAbility 触发）</summary>
    public static void OnFeatureGranted(IEntity feature, IEntity owner)
    {
        if (feature == null || owner == null) return;

        if (string.IsNullOrEmpty(feature.Data.Get<string>(DataKey.FeatureHandlerId)))
            _log.Error($"FeatureHandlerId is null or empty, feature: {feature.Data.Get<string>(DataKey.Name)}, owner: {owner.Data.Get<string>(DataKey.Name)}");

        feature.Data.Set(DataKey.FeatureIsActive, false);

        var instance = new FeatureInstance(owner, feature, Godot.Time.GetTicksUsec() / 1_000_000.0);
        var context = new FeatureContext { Owner = owner, Feature = feature, Instance = instance };

        // 1. 应用数据驱动修改器（Permanent Feature 最常用）
        ApplyModifiers(feature, owner);

        // 2. 调用代码处理器钩子
        var featureId = GetFeatureId(feature);
        FeatureHandlerRegistry.Get(featureId)?.OnGranted(context);

        // 3. 发出 Granted 事件（Owner 局部总线）
        owner.Events.Emit(
            GameEventType.Feature.Granted,
            new GameEventType.Feature.GrantedEventData(feature, owner)
        );

        _log.Debug($"Feature Granted: {feature.Data.Get<string>("Name")} -> {owner}");
    }

    // ==================== Removed ====================

    /// <summary>Feature 被移除时调用（由 EntityManager.RemoveAbility 触发，在 Destroy 之前）</summary>
    public static void OnFeatureRemoved(IEntity feature, IEntity owner)
    {
        if (feature == null || owner == null) return;

        var instance = new FeatureInstance(owner, feature, 0);
        var context = new FeatureContext { Owner = owner, Feature = feature, Instance = instance };
        var name = feature.Data.Get<string>("Name") ?? "";

        // 1. 先调用处理器（处理器可能依赖修改器数据）
        var featureId = GetFeatureId(feature);
        FeatureHandlerRegistry.Get(featureId)?.OnRemoved(context);

        // 2. 按来源批量回滚修改器
        RemoveModifiers(feature, owner);

        // 3. 发出 Removed 事件
        owner.Events.Emit(
            GameEventType.Feature.Removed,
            new GameEventType.Feature.RemovedEventData(name, owner)
        );

        _log.Debug($"Feature Removed: {name} <- {owner}");
    }

    // ==================== Activated ====================

    /// <summary>
    /// Feature 一次激活开始时调用。
    /// 调用方负责构建 FeatureContext（Owner / Feature / ActivationData）。
    /// 调用顺序：OnActivated（通知）→ OnExecute（执行+结果）→ 累计次数 → 发出事件
    /// </summary>
    public static void OnFeatureActivated(FeatureContext ctx)
    {
        if (ctx?.Owner == null || ctx.Feature == null) return;

        if (ctx.Instance == null)
            ctx.Instance = new FeatureInstance(ctx.Owner, ctx.Feature, 0);

        ctx.Feature.Data.Set(DataKey.FeatureIsActive, true);

        // 调用代码处理器
        var featureId = GetFeatureId(ctx.Feature);
        var handler = FeatureHandlerRegistry.Get(featureId);

        // 1. 通知阶段
        handler?.OnActivated(ctx);

        // 2. 执行阶段（结果写入 ctx.ExecuteResult）
        if (handler != null)
        {
            ctx.ExecuteResult = handler.OnExecute(ctx);
        }

        // 3. 累计激活次数
        int current = ctx.Feature.Data.Get<int>(DataKey.FeatureActivationCount);
        ctx.Feature.Data.Set(DataKey.FeatureActivationCount, current + 1);

        // 4. 发出 Activated 事件（Feature 局部总线）
        ctx.Feature.Events.Emit(
            GameEventType.Feature.Activated,
            new GameEventType.Feature.ActivatedEventData(ctx)
        );
    }

    // ==================== Ended ====================

    /// <summary>
    /// Feature 一次激活结束时调用。
    /// 传入与 OnFeatureActivated 同一个 FeatureContext 实例。
    /// </summary>
    public static void OnFeatureEnded(FeatureContext ctx)
    {
        if (ctx?.Owner == null || ctx.Feature == null) return;

        ctx.Feature.Data.Set(DataKey.FeatureIsActive, false);

        // 调用代码处理器
        var featureId = GetFeatureId(ctx.Feature);
        FeatureHandlerRegistry.Get(featureId)?.OnEnded(ctx);

        // 发出 Ended 事件（Feature 局部总线）
        ctx.Feature.Events.Emit(
            GameEventType.Feature.Ended,
            new GameEventType.Feature.EndedEventData(ctx)
        );
    }

    // ==================== Enable / Disable ====================

    /// <summary>启用 Feature（从禁用状态恢复）</summary>
    public static void EnableFeature(IEntity feature, IEntity owner)
    {
        if (feature == null || owner == null) return;

        feature.Data.Set(DataKey.FeatureEnabled, true);

        // 调用代码处理器
        var featureId = GetFeatureId(feature);
        var ctx = new FeatureContext { Owner = owner, Feature = feature };
        FeatureHandlerRegistry.Get(featureId)?.OnEnabled(ctx);

        owner.Events.Emit(
            GameEventType.Feature.Enabled,
            new GameEventType.Feature.EnabledEventData(feature, owner)
        );

        _log.Debug($"Feature Enabled: {feature.Data.Get<string>("Name")}");
    }

    /// <summary>禁用 Feature（保留在宿主上，但暂停响应）</summary>
    public static void DisableFeature(IEntity feature, IEntity owner)
    {
        if (feature == null || owner == null) return;

        feature.Data.Set(DataKey.FeatureEnabled, false);

        // 调用代码处理器
        var featureId = GetFeatureId(feature);
        var ctx = new FeatureContext { Owner = owner, Feature = feature };
        FeatureHandlerRegistry.Get(featureId)?.OnDisabled(ctx);

        owner.Events.Emit(
            GameEventType.Feature.Disabled,
            new GameEventType.Feature.DisabledEventData(feature, owner)
        );

        _log.Debug($"Feature Disabled: {feature.Data.Get<string>("Name")}");
    }

    // ==================== Action 执行 ====================

    /// <summary>
    /// 批量执行 IFeatureAction 列表
    ///
    /// 在 IFeatureHandler.OnGranted/OnActivated 内使用：
    /// <code>FeatureSystem.ExecuteActions(myActions, ctx);</code>
    /// </summary>
    public static void ExecuteActions(IEnumerable<IFeatureAction> actions, FeatureContext ctx)
    {
        if (actions == null || ctx == null) return;
        foreach (var action in actions)
        {
            action?.Execute(ctx);
        }
    }

    // ==================== 内部工具 ====================

    /// <summary>获取 Feature 的处理器 ID（优先 FeatureHandlerId，退化到 Name）</summary>
    private static string GetFeatureId(IEntity feature)
    {
        var handlerId = feature.Data.Get<string>(DataKey.FeatureHandlerId);
        if (!string.IsNullOrEmpty(handlerId)) return handlerId;
        var featureName = feature.Data.Get<string>(DataKey.Name) ?? "(unknown)";
        _log.Error($"Feature '{featureName}' 未配置 FeatureHandlerId，无法查找对应的 IFeatureHandler");
        return string.Empty;
    }

    /// <summary>将 FeatureDefinition.Modifiers 施加到 Owner.Data（Granted 阶段调用）</summary>
    private static void ApplyModifiers(IEntity feature, IEntity owner)
    {
        var raw = feature.Data.Get<object>(DataKey.FeatureModifiers);
        if (raw is not Array<FeatureModifierEntry> modifiers || modifiers.Count == 0) return;

        int applied = 0;
        foreach (var entry in modifiers)
        {
            if (entry == null || string.IsNullOrEmpty(entry.DataKeyName)) continue;

            var modifier = new DataModifier(
                type: entry.ModifierType,
                value: entry.Value,
                priority: entry.Priority,
                source: feature
            );
            owner.Data.AddModifier(entry.DataKeyName, modifier);
            applied++;
        }

        if (applied > 0)
            _log.Debug($"Feature {feature.Data.Get<string>("Name")} 施加 {applied} 个修改器");
    }

    /// <summary>按来源批量回滚修改器（Removed 阶段调用）</summary>
    private static void RemoveModifiers(IEntity feature, IEntity owner)
    {
        owner.Data.RemoveModifiersBySource(feature);
        _log.Debug($"Feature {feature.Data.Get<string>("Name")} 修改器已回滚");
    }
}
