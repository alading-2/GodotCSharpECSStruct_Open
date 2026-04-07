using System.Collections.Generic;
using Godot;

/// <summary>
/// 技能命中参数。全部属性均为可选，为 null 时跳过对应步骤。
/// </summary>
internal sealed record AbilityImpactOptions
{
    /// <summary>目标查询参数；null 时不执行查询（无目标则不造成伤害）。</summary>
    public TargetSelectorQuery? Query { get; init; }
    /// <summary>特效生成参数；null 时不生成特效。</summary>
    public EffectSpawnOptions? Effect { get; init; }
    /// <summary>伤害参数；null 时不造成伤害。</summary>
    public DamageApplyOptions? Damage { get; init; }
    /// <summary>特效生成位置覆盖；null 时使用 origin。</summary>
    public Vector2? EffectPosition { get; init; }
}

/// <summary>
/// 技能命中结果。
/// </summary>
internal readonly record struct AbilityImpactResult(int TargetsHit, GameTimer? Timer);

/// <summary>
/// 技能命中工具（薄层编排）。
///
/// 编排顺序：目标查询 → 特效生成 → 伤害结算。
/// 各步骤均为可选，职责分别委托给 EntityTargetSelector、EffectTool 和 DamageTool。
/// DoT 调度与重复命中控制由 DamageTool 统一管理。
/// </summary>
internal static class AbilityImpactTool
{
    private static readonly Log _log = new(nameof(AbilityImpactTool));

    /// <summary>
    /// 在固定位置执行命中逻辑（查询 → 特效 → 伤害）。
    /// 适合 Dash 落地、Slam 击打点、投射物爆炸等位置固定的技能。
    /// </summary>
    public static AbilityImpactResult Execute(Vector2 origin, IEntity caster, AbilityImpactOptions options)
    {
        return ExecuteInternal(() => origin, caster, options);
    }

    /// <summary>
    /// 以施法者当前位置执行命中逻辑（每次 tick 重新取施法者位置）。
    /// 适合光环、持续范围技能等跟随施法者移动的技能。
    /// </summary>
    public static AbilityImpactResult ExecuteAroundCaster(IEntity caster, AbilityImpactOptions options)
    {
        if (caster is not Node2D casterNode) return default;
        return ExecuteInternal(() => casterNode.GlobalPosition, caster, options);
    }

    private static AbilityImpactResult ExecuteInternal(
        System.Func<Vector2> originProvider,
        IEntity caster,
        AbilityImpactOptions options)
    {
        var origin = originProvider();

        // 1. 目标查询：始终用 originProvider() 覆盖 Query.Origin，保证 ExecuteAroundCaster 跟随施法者当前位置
        List<IEntity>? targets = options.Query.HasValue
            ? EntityTargetSelector.Query(options.Query.Value with { Origin = origin })
            : null;

        // 2. 特效生成
        if (options.Effect.HasValue)
        {
            EffectTool.Spawn(options.EffectPosition ?? origin, options.Effect.Value);
        }

        // 3. 伤害结算：无目标或无伤害参数时跳过
        if (options.Damage == null || targets == null || targets.Count == 0)
            return new AbilityImpactResult(targets?.Count ?? 0, null);

        var dmg = options.Damage;
        var hitRegistry = dmg.AllowRepeatHitSameTarget ? null : DamageTool.CreateHitRegistry();

        // 立即结算一次（单次技能就此结束，DoT 技能还会挂载定时器）
        int hitCount = DamageTool.ApplyToList(targets, dmg, hitRegistry);

        // 若配置了持续伤害，委托 DamageTool 调度 DoT 定时器
        GameTimer? timer = null;
        if (dmg.TickInterval > 0f && dmg.TotalDuration > 0f)
        {
            // 每次 tick 用 originProvider() 重新取位置，支持 ExecuteAroundCaster 跟随施法者移动
            timer = DamageTool.ScheduleDoT(
                () => options.Query.HasValue
                    ? EntityTargetSelector.Query(options.Query.Value with { Origin = originProvider() })
                    : null,
                dmg,
                caster as Node,     // guardian：施法者失效时自动终止 DoT
                hitRegistry
            );
        }

        return new AbilityImpactResult(hitCount, timer);
    }
}
