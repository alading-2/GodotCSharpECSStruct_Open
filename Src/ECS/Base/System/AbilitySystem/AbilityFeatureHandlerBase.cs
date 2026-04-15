using System;

/// <summary>
/// 技能 Feature 处理器基类 - 桥接 FeatureSystem 通用生命周期与技能专有执行逻辑
///
/// 职责：
/// - 实现 IFeatureHandler 的完整生命周期（OnGranted/OnRemoved/OnEnabled/OnDisabled/OnActivated/OnEnded）
/// - 覆写 OnExecute：从 FeatureContext.ActivationData 提取 CastContext，调用子类的 ExecuteAbility
/// - 将 AbilityExecutedResult 作为通用 object? 返回值，由 FeatureSystem 写入 FeatureContext.ExecuteResult
///
/// 具体 Handler（如 Slam、Dash、ArcShot）只需覆写 ExecuteAbility(CastContext) 即可。
/// </summary>
internal abstract class AbilityFeatureHandlerBase : IFeatureHandler
{
    private static readonly Log _log = new(nameof(AbilityFeatureHandlerBase));

    public abstract string FeatureId { get; }

    public virtual string FeatureGroup => global::FeatureId.Ability.Groups.Root;

    // ===== 通用生命周期（空 virtual，子类按需覆写）=====

    public virtual void OnGranted(FeatureContext context) { }
    public virtual void OnRemoved(FeatureContext context) { }
    public virtual void OnEnabled(FeatureContext context) { }
    public virtual void OnDisabled(FeatureContext context) { }
    public virtual void OnActivated(FeatureContext context) { }
    public virtual void OnEnded(FeatureContext context) { }

    // ===== 执行阶段：桥接通用 FeatureContext → 技能专有 CastContext =====

    /// <summary>
    /// 覆写 OnExecute：从 ActivationData 提取 CastContext，调用 ExecuteAbility，返回结果。
    /// 结果由 FeatureSystem 写入 FeatureContext.ExecuteResult，AbilitySystem 从中读取。
    /// </summary>
    public object? OnExecute(FeatureContext context)
    {
        if (context.ActivationData is not CastContext castContext)
        {
            _log.Warn($"FeatureHandler {FeatureId} 缺少 CastContext");
            return new AbilityExecutedResult();
        }

        try
        {
            return ExecuteAbility(castContext) ?? new AbilityExecutedResult();
        }
        catch (Exception ex)
        {
            _log.Error($"FeatureHandler {FeatureId} 执行异常: {ex.Message}");
            return new AbilityExecutedResult();
        }
    }

    /// <summary>
    /// 具体 Handler 实现此方法，执行技能效果并返回结果。
    /// 签名不变，所有具体 Handler 无需修改。
    /// </summary>
    protected abstract AbilityExecutedResult ExecuteAbility(CastContext context);
}
