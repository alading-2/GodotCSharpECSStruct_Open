
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 裂地猛击技能执行器
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 目标选择：None（自动在角色周围随机选点）
/// 逻辑：
///   1. 在角色周围圆环内随机选一个点（AbilityCastRange 为选点半径）
///   2. 在该点造成圆形范围伤害（AbilityEffectRadius 为伤害半径）
/// 特效：Effect_020（在随机选点位置播放）
/// 伤害：物理伤害，带 Area + Melee 标签
/// </summary>
public class SlamExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(SlamExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("Slam", new SlamExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        // 安全检查
        if (caster == null || ability == null)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        var casterNode = caster as Node2D;
        if (casterNode == null)
        {
            _log.Warn("施法者不是 Node2D");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 1. 获取技能参数
        var abilityRange = ability.Data.Get<float>(DataKey.AbilityCastRange);      // 选点范围（角色周围圆环半径）
        var damageRadius = ability.Data.Get<float>(DataKey.AbilityEffectRadius);   // 伤害范围（圆形半径）
        var baseDamage = ability.Data.Get<float>(DataKey.AbilityDamage);
        var maxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets);

        // 2. 在角色周围随机选点
        // 使用 Ring 几何（内径=0，外径=selectRadius）获取随机位置
        var randomPoint = GeometryCalculator.GetRandomPointInRing(
            casterNode.GlobalPosition,
            innerRadius: 0f,
            outerRadius: abilityRange
        );

        // 3. 在随机点位置查询敌人目标
        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = randomPoint,
            Range = damageRadius,
            CenterEntity = caster,
            TeamFilter = AbilityTargetTeamFilter.Enemy,
            Sorting = AbilityTargetSorting.Nearest,
            MaxTargets = maxTargets
        };
        var targets = EntityTargetSelector.Query(query);

        // 4. 生成特效（在随机选点位置）
        var effectScene = ResourceManagement.Load<PackedScene>(ResourcePaths.Asset_Effect_020, ResourceCategory.Asset);
        if (effectScene != null)
        {
            EffectTool.Spawn(randomPoint, new EffectSpawnOptions(
                VisualScene: effectScene,
                Name: "裂地猛击特效",
                Scale: Vector2.One * 0.6f
            ));
        }

        // 5. 如果范围内没有目标，直接返回
        if (targets == null || targets.Count == 0)
        {
            _log.Info($"裂地猛击在位置 {randomPoint} 未命中任何目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 6. 计算最终伤害
        var finalAttack = caster.Data.Get<float>(DataKey.FinalAttack);
        var finalSkillDamage = caster.Data.Get<float>(DataKey.FinalSkillDamage);
        var damage = (baseDamage + finalAttack) * (finalSkillDamage / 100f);

        // 7. 对每个目标应用伤害
        int targetsHit = 0;
        foreach (var target in targets)
        {
            if (target is IUnit unitVictim)
            {
                DamageService.Instance.Process(new DamageInfo
                {
                    Attacker = casterNode,
                    Victim = unitVictim,
                    Damage = damage,
                    Type = DamageType.Physical,
                    Tags = DamageTags.Area | DamageTags.Melee
                });
                targetsHit++;
            }
        }

        _log.Info($"裂地猛击: 选点范围 {abilityRange}, 伤害半径 {damageRadius}, 最终伤害 {damage:F1}, 命中 {targetsHit}");
        return new AbilityExecutedResult { TargetsHit = targetsHit };
    }
}
