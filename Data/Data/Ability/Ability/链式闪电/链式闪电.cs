
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 链式闪电技能执行器 - 复杂技能示例
/// 
/// 演示功能：
/// 1. Chain几何形状 - 链式弹跳目标选择
/// 2. 伤害衰减机制 - 每次弹跳伤害递减
/// 3. 属性计算 - 结合FinalAttack、FinalSkillDamage
/// 4. 目标排序 - 使用AbilityTargetSorting
/// </summary>
public class ChainLightningExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(ChainLightningExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("ChainLightning", new ChainLightningExecutor());
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
        var baseDamage = ability.Data.Get<float>(DataKey.AbilityDamage);
        var chainCount = ability.Data.Get<int>(DataKey.AbilityChainCount);

        // 2. 计算初始伤害 - 结合施法者属性
        var finalAttack = caster.Data.Get<float>(DataKey.FinalAttack);
        var finalSkillDamage = caster.Data.Get<float>(DataKey.FinalSkillDamage);

        // 最终伤害 = (基础伤害 + 攻击力) × 技能伤害倍率
        var initialDamage = (baseDamage + finalAttack) * (finalSkillDamage / 100f);

        // 3. 获取Chain目标列表 (由AbilitySystem通过TargetSelector提供)
        var targets = context.Targets;
        if (targets == null || targets.Count == 0)
        {
            _log.Info("链式闪电未找到目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 4. 对每个目标应用伤害 - 展示伤害衰减
        int hitCount = 0;
        float currentDamage = initialDamage;
        const float damageDecay = 0.8f;  // 每次弹跳伤害衰减至80%

        for (int i = 0; i < targets.Count && i <= chainCount; i++)
        {
            var target = targets[i];

            if (target is IUnit unitVictim)
            {
                // 移除手动暴击计算，交由 DamageService 管道处理
                var damageInfo = new DamageInfo
                {
                    Attacker = caster as Node,
                    Victim = unitVictim,
                    Damage = currentDamage,
                    Type = DamageType.Magical,
                    // IsCritical = false // 默认由 DamageService 计算
                };

                DamageService.Instance.Process(damageInfo);

                _log.Debug($"链式闪电 第{i + 1}跳: {unitVictim.Data.Get<string>(DataKey.Name)} 受到 {damageInfo.FinalDamage:F1} 伤害");

                hitCount++;

                // 下次弹跳伤害衰减
                currentDamage *= damageDecay;
            }
        }

        _log.Info($"链式闪电执行完成: 初始伤害 {initialDamage:F1}, 弹跳次数 {hitCount}");

        return new AbilityExecutedResult { TargetsHit = hitCount };
    }
}
