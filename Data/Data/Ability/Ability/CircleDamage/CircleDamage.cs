using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 烈焰光环技能执行器
/// 
/// 触发方式：Periodic（周期触发，由 TriggerComponent 每隔 AbilityCooldown 秒自动执行）
/// 目标选择：None（自动对施法者周围圆形范围内所有敌人）
/// 特效：Effect_003（每次触发在施法者位置播放一次独立特效）
/// 伤害：魔法伤害，带 Area 标签
/// </summary>
internal class CircleDamageExecutor : AbilityFeatureHandlerBase
{
    private static readonly Log _log = new(nameof(CircleDamageExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new CircleDamageExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Active.CircleDamage;
    public override string FeatureGroup => global::FeatureId.Ability.Groups.Active;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        // 安全检查：确保施法者和技能实体存在
        if (caster == null || ability == null) return new AbilityExecutedResult();

        var casterNode2D = caster as Node2D;

        // 1. 目标查询逻辑
        // 由于烈焰光环通常由 TriggerComponent 周期性（Periodic）触发，
        // 这种触发方式下，CastContext 可能不会由 AbilityTargetSelectionComponent 预填充目标，
        // 因此需要在此处利用 TargetSelector 进行实时范围扫描。
        var targets = context.Targets;
        if (targets == null || targets.Count == 0)
        {
            // 获取技能配置中的伤害半径和阵营过滤条件
            var range = ability.Data.Get<float>(DataKey.AbilityEffectRadius);
            var teamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter);

            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle, // 以施法者为圆心的圆形区域
                Range = range,                  // 查询半径
                Origin = casterNode2D?.GlobalPosition ?? Vector2.Zero,
                CenterEntity = caster,
                TeamFilter = teamFilter,        // 根据配置过滤目标阵营（通常为敌人）
                Sorting = AbilityTargetSorting.None,
                MaxTargets = -1               // 烈焰光环通常不限命中数量
            };

            // 执行核心空间查询并将结果存回上下文暂存
            targets = EntityTargetSelector.Query(query);
            context.Targets = targets;
        }

        // 2. 每次触发时生成视觉反馈
        // 获取配置中的通用特效资源
        var effectScene = ability.Data.Get<PackedScene>(DataKey.EffectScene);
        if (effectScene != null && casterNode2D != null)
        {
            // 在施法者脚下生成特效，设置较大的缩放以符合光环意图
            EffectTool.Spawn(casterNode2D.GlobalPosition, new EffectSpawnOptions(
                VisualScene: effectScene,
                Name: "烈焰光环特效",
                Scale: new Vector2(2.0f, 2.0f)
            ));
        }

        _log.Info($"[烈焰光环] 触发! 范围: {ability.Data.Get<float>(DataKey.AbilityEffectRadius)}, 找到目标: {targets?.Count ?? 0}");

        // 如果没有探测到任何合法目标，则直接结束执行
        if (targets == null || targets.Count == 0)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 3. 对扫描到的每个目标执行伤害结算
        // 技能基础伤害 × 施法者技能伤害倍率
        var damage = ability.Data.Get<float>(DataKey.AbilityDamage)
                   * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f;
        int hitCount = 0;
        foreach (var target in targets)
        {
            if (target is IUnit victim)
            {
                // 调用伤害服务，标记为魔法伤害类型并附加 Area（范围）标签
                DamageService.Instance.Process(new DamageInfo
                {
                    Attacker = casterNode2D,
                    Victim = victim,
                    Damage = damage,
                    Type = DamageType.Magical,
                    Tags = DamageTags.Area | DamageTags.Ability
                });
                hitCount++;
            }
        }

        // 返回本次触发总计命中的目标数量
        return new AbilityExecutedResult { TargetsHit = hitCount };
    }
}
