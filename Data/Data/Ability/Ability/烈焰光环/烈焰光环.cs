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

        // 1. 获取目标
        // 由于 AbilitySystem 不再自动为 None 模式选择目标，我们需要手动查询
        // 或者如果 AbilitySystem 传递了目标（预选模式），则使用传递的
        var targets = context.Targets;

        if (targets == null || targets.Count == 0)
        {
            // 手动构建查询
            var range = ability.Data.Get<float>(DataKey.AbilityRange);
            var geometry = ability.Data.Get<AbilityTargetGeometry>(DataKey.AbilityTargetGeometry);
            var teamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter);

            var query = new TargetSelectorQuery
            {
                Geometry = geometry,
                Range = range,
                Origin = (caster as Node2D)?.GlobalPosition ?? Vector2.Zero, // 以施法者为中心
                CenterEntity = caster,
                TeamFilter = teamFilter,
                Sorting = AbilityTargetSorting.Nearest, // 按距离排序
                MaxTargets = 999 // 攻击所有范围内敌人
            };

            targets = TargetSelector.Query(query);
            context.Targets = targets; // 回填到上下文
        }

        _log.Info($"[烈焰光环] 触发! 范围: {ability.Data.Get<float>(DataKey.AbilityRange)}, 找到目标: {targets?.Count ?? 0}");

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
                Damage = damage, // 基础伤害
                Type = DamageType.Magical, // 假设光环是魔法伤害
                Tags = DamageTags.Area
            };

            DamageService.Instance.Process(damageInfo);

            hitCount++;
        }

        return new AbilityExecutedResult { TargetsHit = hitCount };
    }
}
