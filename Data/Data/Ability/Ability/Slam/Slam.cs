
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 裂地猛击技能执行器
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 目标选择：None + Circle AOE（以施法者为圆心自动选取周围敌人）
/// 特效：Effect_020（独立特效，在施法者位置播放）
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

        if (caster == null || ability == null)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 1. 获取技能参数
        var range = ability.Data.Get<float>(DataKey.AbilityRange);
        var baseDamage = ability.Data.Get<float>(DataKey.AbilityDamage);
        var casterNode = caster as Node2D;

        // 2. 目标为空时手动从施法者位置圆形范围查询
        var targets = context.Targets;
        if (targets == null || targets.Count == 0)
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Range = range,
                Origin = casterNode?.GlobalPosition ?? Vector2.Zero,
                CenterEntity = caster,
                TeamFilter = AbilityTargetTeamFilter.Enemy,
                Sorting = AbilityTargetSorting.Nearest,
                MaxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets)
            };
            targets = EntityTargetSelector.Query(query);
            context.Targets = targets;
        }

        // 3. 生成独立特效（Effect_020 在施法者位置爆炸）
        var effectScene = ResourceManagement.Load<PackedScene>(ResourcePaths.Asset_Effect_020, ResourceCategory.Asset);
        if (effectScene != null && casterNode != null)
        {
            EffectTool.Spawn(casterNode.GlobalPosition, new EffectSpawnOptions(
                VisualScene: effectScene,
                Name: "裂地猛击特效",
                MaxLifeTime: 1.2f,
                Scale: new Vector2(1.5f, 1.5f)
            ));
        }

        if (targets == null || targets.Count == 0)
        {
            _log.Info("裂地猛击未命中任何目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 4. 计算最终伤害（基础伤害 + 攻击力）× 技能伤害倍率
        var finalAttack = caster.Data.Get<float>(DataKey.FinalAttack);
        var finalSkillDamage = caster.Data.Get<float>(DataKey.FinalSkillDamage);
        var damage = (baseDamage + finalAttack) * (finalSkillDamage / 100f);

        // 5. 对每个目标应用伤害
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

        _log.Info($"裂地猛击: 范围 {range}, 最终伤害 {damage:F1}, 命中 {targetsHit}");
        return new AbilityExecutedResult { TargetsHit = targetsHit };
    }
}
