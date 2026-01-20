
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 裂地猛击技能执行器 - 示例实现
/// 
/// 演示功能：
/// 1. 主动技能 (Active)
/// 2. 范围伤害 (AOE - Circle)
/// 3. 目标筛选 (TargetTeamFilter)
/// 4. 伤害应用 (SourceEventData)
/// </summary>
public class SlamExecutor : IAbilityExecutor
{
    private static readonly Log _log = new("SlamExecutor");

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("Slam", new SlamExecutor());
    }

    public AbilityExecuteResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        if (caster == null || ability == null)
        {
            return new AbilityExecuteResult { TargetsHit = 0 };
        }

        // 1. 获取技能参数
        var range = ability.Data.Get<float>(DataKey.AbilityRange);
        var damage = ability.Data.Get<float>(DataKey.AbilityDamage);

        // 示例：可以结合人物属性进行伤害计算
        // var strength = caster.Data.Get<float>(DataKey.AttackBonus);
        // damage *= (1 + strength);

        // 2. 目标检测 (这里演示逻辑，实际应调用物理查询)
        // var targets = PhysicsQuery.OverlapCircle(caster.Position, range, LayerMask.Enemy);
        // 目前简单取 context.Targets (如果在 AbilitySystem 中已经选好了)
        var targets = context.Targets;

        if (targets == null || targets.Count == 0)
        {
            _log.Info("猛击未命中任何目标");
            return new AbilityExecuteResult { TargetsHit = 0 };
        }

        // 3. 应用效果
        foreach (var target in targets)
        {
            if (target is IUnit unitVictim)
            {
                var damageInfo = new DamageInfo
                {
                    Attacker = caster as Node,
                    Victim = unitVictim,
                    BaseDamage = damage,
                    Type = DamageType.Physical
                };

                // 直接调用伤害服务处理
                DamageService.Instance.Process(damageInfo);
            }
        }

        // 4. 播放特效 (此处仅打日志)
        _log.Info($"执行裂地猛击: 范围 {range}, 伤害 {damage}, 命中 {targets.Count}");

        return new AbilityExecuteResult { TargetsHit = targets.Count };
    }
}
