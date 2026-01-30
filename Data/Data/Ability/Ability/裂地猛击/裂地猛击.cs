
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

        // 2. 计算最终伤害 - 展示如何结合多个DataKey
        // 伤害 = 技能基础伤害 × 技能伤害加成 + 攻击力
        var finalAttack = caster.Data.Get<float>(DataKey.FinalAttack);  // 使用计算属性
        var finalSkillDamage = caster.Data.Get<float>(DataKey.FinalSkillDamage);  // 技能伤害百分比

        // 最终伤害 = (基础伤害 + 攻击力) × 技能伤害倍率
        var damage = (baseDamage + finalAttack) * (finalSkillDamage / 100f);

        // 3. 获取目标列表
        var targets = context.Targets;
        if (targets == null || targets.Count == 0)
        {
            _log.Info("猛击未命中任何目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 4. 应用效果 - 展示暴击判定
        int targetsHit = 0;
        foreach (var target in targets)
        {
            if (target is IUnit unitVictim)
            {
                // 移除手动暴击计算，交由 DamageService 处理
                var damageInfo = new DamageInfo
                {
                    Attacker = caster as Node,
                    Victim = unitVictim,
                    BaseDamage = damage, // 传入基础伤害(含Attributes加成)
                    Type = DamageType.Physical, // 猛击通常是物理伤害
                    Tags = DamageTags.Area | DamageTags.Melee // 范围+近战
                };

                // 调用伤害服务处理
                DamageService.Instance.Process(damageInfo);
                targetsHit++;
            }
        }

        // 5. 播放特效 (此处仅打日志)
        _log.Info($"执行裂地猛击: 范围 {range}, 基础伤害 {baseDamage}, 最终伤害 {damage:F1}, 命中 {targetsHit}");

        return new AbilityExecutedResult { TargetsHit = targetsHit };
    }
}
