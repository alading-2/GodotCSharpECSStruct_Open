using System;

internal abstract class AbilityFeatureHandlerBase : IFeatureHandler
{
    private static readonly Log _log = new(nameof(AbilityFeatureHandlerBase));

    public abstract string FeatureId { get; }

    public virtual string FeatureGroup => global::FeatureId.Ability.Groups.Root;

    public virtual void OnGranted(FeatureContext context)
    {
    }

    public virtual void OnRemoved(FeatureContext context)
    {
    }

    public virtual void OnEnded(FeatureContext context)
    {
    }

    public void OnActivated(FeatureContext context)
    {
        if (context.ActivationData is not CastContext castContext)
        {
            _log.Warn($"FeatureHandler {FeatureId} 激活时缺少 CastContext");
            context.ExtraData[nameof(AbilityExecutedResult)] = new AbilityExecutedResult();
            return;
        }

        try
        {
            var result = ExecuteAbility(castContext) ?? new AbilityExecutedResult();
            context.ExtraData[nameof(AbilityExecutedResult)] = result;
        }
        catch (Exception ex)
        {
            _log.Error($"FeatureHandler {FeatureId} 执行异常: {ex.Message}");
            context.ExtraData[nameof(AbilityExecutedResult)] = new AbilityExecutedResult();
        }
    }

    protected abstract AbilityExecutedResult ExecuteAbility(CastContext context);
}
