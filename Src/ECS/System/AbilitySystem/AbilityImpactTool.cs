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
    /// 执行命中逻辑（查询 → 特效 → 伤害）。
    /// Query 是唯一的命中锚点来源；特效默认复用 Query.Origin，伤害默认作用于 Query 选中的目标。
    /// </summary>
    public static AbilityImpactResult Execute(IEntity caster, AbilityImpactOptions options)
    {
        var query = ResolveQuery(options.Query);

        // 1. 目标查询：统一从 Query 内部解析本次命中中心
        List<IEntity>? targets = query.HasValue
            ? EntityTargetSelector.Query(query.Value)
            : null;

        // 2. 特效生成
        if (options.Effect.HasValue)
        {
            EffectTool.Spawn(ResolveEffectOptions(options.Effect.Value, query));
        }

        // 3. 伤害结算：无伤害参数时跳过；无 Query 时不做隐式伤害兜底
        if (options.Damage == null)
            return new AbilityImpactResult(targets?.Count ?? 0, null);

        if (!query.HasValue)
        {
            _log.Warn("AbilityImpactTool.Execute: Damage 已配置但 Query 为空，跳过伤害结算"); // 伤害必须依赖 Query 选目标
            return new AbilityImpactResult(0, null);
        }

        if (targets == null || targets.Count == 0)
            return new AbilityImpactResult(targets?.Count ?? 0, null);

        var dmg = options.Damage;
        // 判断是否能伤害同一个目标
        var hitRegistry = dmg.AllowRepeatHitSameTarget ? null : DamageTool.CreateHitRegistry();

        bool hasDot = dmg.TickInterval > 0f && dmg.TotalDuration > 0f;
        int hitCount = 0;
        if (!hasDot || dmg.ApplyImmediateTick)
        {
            // 单次伤害总是立即结算；DoT 是否首跳立即结算由 DamageApplyOptions.ApplyImmediateTick 控制
            hitCount = DamageTool.ApplyToList(targets, dmg, hitRegistry);
        }

        // 若配置了持续伤害，委托 DamageTool 调度 DoT 定时器
        GameTimer? timer = null;
        if (hasDot)
        {
            // 每次 tick 重新解析 Query.Origin，支持跟随施法者移动的范围技能
            timer = DamageTool.ScheduleDoT(
                () =>
                {
                    var tickQuery = ResolveQuery(options.Query);
                    return tickQuery.HasValue
                        ? EntityTargetSelector.Query(tickQuery.Value)
                        : null;
                },
                dmg,
                caster as Node,     // guardian：施法者失效时自动终止 DoT
                immediate: false,
                hitRegistry
            );
        }

        return new AbilityImpactResult(hitCount, timer);
    }

    /// <summary>
    /// 解析查询参数，优先使用 Query.OriginProvider。
    /// </summary>
    private static TargetSelectorQuery? ResolveQuery(TargetSelectorQuery? query)
    {
        if (!query.HasValue) return null;
        var resolvedQuery = query.Value;
        return resolvedQuery with { Origin = resolvedQuery.ResolveOrigin() };
    }

    /// <summary>
    /// 解析特效参数；若未显式配置特效位置，则默认回落到 Query 的命中中心。
    /// </summary>
    private static EffectSpawnOptions ResolveEffectOptions(EffectSpawnOptions effect, TargetSelectorQuery? query)
    {
        if (effect.Host != null) return effect;
        if (effect.EffectPosition.HasValue) return effect;
        if (!query.HasValue) return effect;
        return effect with { EffectPosition = query.Value.Origin };
    }
}
