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
public class CircleDamageExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(CircleDamageExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("CircleDamage", new CircleDamageExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        if (caster == null || ability == null) return new AbilityExecutedResult();

        var casterNode2D = caster as Node2D;

        // 1. 目标查询（Periodic 技能不走 AbilityTargetSelectionComponent，需手动查询）
        var targets = context.Targets;
        if (targets == null || targets.Count == 0)
        {
            var range = ability.Data.Get<float>(DataKey.AbilityRange);
            var teamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter);

            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Range = range,
                Origin = casterNode2D?.GlobalPosition ?? Vector2.Zero,
                CenterEntity = caster,
                TeamFilter = teamFilter,
                Sorting = AbilityTargetSorting.Nearest,
                MaxTargets = 999
            };

            targets = EntityTargetSelector.Query(query);
            context.Targets = targets;
        }

        // 2. 每次触发在施法者位置生成光环特效
        var effectScene = ResourceManagement.Load<PackedScene>(ResourcePaths.Asset_Effect_003, ResourceCategory.Asset);
        if (effectScene != null && casterNode2D != null)
        {
            EffectTool.Spawn(casterNode2D.GlobalPosition, new EffectSpawnOptions(
                VisualScene: effectScene,
                Name: "烈焰光环特效",
                Scale: new Vector2(2.0f, 2.0f)
            ));
        }

        _log.Info($"[烈焰光环] 触发! 范围: {ability.Data.Get<float>(DataKey.AbilityRange)}, 找到目标: {targets?.Count ?? 0}");

        if (targets == null || targets.Count == 0)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 3. 对每个目标造成魔法 AOE 伤害
        var damage = ability.Data.Get<float>(DataKey.AbilityDamage);
        int hitCount = 0;
        foreach (var target in targets)
        {
            if (target is IUnit victim)
            {
                DamageService.Instance.Process(new DamageInfo
                {
                    Attacker = casterNode2D,
                    Victim = victim,
                    Damage = damage,
                    Type = DamageType.Magical,
                    Tags = DamageTags.Area
                });
                hitCount++;
            }
        }

        return new AbilityExecutedResult { TargetsHit = hitCount };
    }
}
