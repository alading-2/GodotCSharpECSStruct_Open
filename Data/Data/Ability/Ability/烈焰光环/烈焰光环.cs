using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 圆形范围持续伤害技能执行器 (Circle Damage Over Time)
/// 
/// 演示功能：
/// 1. 周期性触发 (Periodic)
/// 2. 圆形范围目标选择 (Circle Geometry)
/// 3. 敌对目标过滤 (Team Filter)
/// </summary>
public class CircleDamageExecutor : IAbilityExecutor
{
    private static readonly Log _log = new("CircleDamageExecutor");

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

        // 1. 获取目标
        // AbilitySystem 已经根据 AbilityTargetGeometry=Circle, AbilityRange=..., AbilityTargetTeamFilter=...
        // 筛选出了范围内的所有有效目标，并放入 context.Targets
        var targets = context.Targets;

        if (targets == null || targets.Count == 0)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 2. 获取配置参数
        var damage = ability.Data.Get<float>(DataKey.AbilityDamage);

        // 3. 对每个目标造成伤害
        int hitCount = 0;
        foreach (var target in targets)
        {
            // --- 伤害逻辑开始 ---
            // 使用 DamageService 统一处理伤害，自动处理闪避、暴击、护甲、护盾、伤害统计等
            var damageInfo = new DamageInfo
            {
                Attacker = caster as Node, // 攻击者
                Victim = target as IUnit, // 受害者
                BaseDamage = damage, // 基础伤害
                Type = DamageType.Magical, // 假设光环是魔法伤害
                Tags = DamageTags.Area
            };

            DamageService.Instance.Process(damageInfo);

            hitCount++;
        }

        return new AbilityExecutedResult { TargetsHit = hitCount };
    }
}
