
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 链式闪电技能执行器
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 目标选择：Entity（自动索取最近敌人），Chain 几何形状（弹跳）
/// 特效：Effect_lrsc3（在每个被命中目标位置生成独立特效）
/// 伤害：魔法伤害，每次弹跳衰减 80%
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

        // 2. 计算初始伤害（基础伤害 + 攻击力）× 技能伤害倍率
        var finalAttack = caster.Data.Get<float>(DataKey.FinalAttack);
        var finalSkillDamage = caster.Data.Get<float>(DataKey.FinalSkillDamage);
        var initialDamage = (baseDamage + finalAttack) * (finalSkillDamage / 100f);

        // 3. 获取 Chain 目标列表（由 AbilityTargetSelectionComponent 填充）
        var targets = context.Targets;
        if (targets == null || targets.Count == 0)
        {
            _log.Info("链式闪电未找到目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 4. 预加载特效场景
        var effectScene = ResourceManagement.Load<PackedScene>(ResourcePaths.Asset_Effect_lrsc3, ResourceCategory.Asset);

        // 5. 对每个目标应用伤害（带衰减）并生成特效
        int hitCount = 0;
        float currentDamage = initialDamage;
        const float damageDecay = 0.8f;

        for (int i = 0; i < targets.Count && i <= chainCount; i++)
        {
            var target = targets[i];

            if (target is IUnit unitVictim)
            {
                var damageInfo = new DamageInfo
                {
                    Attacker = caster as Node,
                    Victim = unitVictim,
                    Damage = currentDamage,
                    Type = DamageType.Magical
                };
                DamageService.Instance.Process(damageInfo);

                // 在目标位置生成闪电特效
                if (effectScene != null && target is Node2D targetNode2D)
                {
                    EffectTool.Spawn(targetNode2D.GlobalPosition, new EffectSpawnOptions(
                        VisualScene: effectScene,
                        Name: $"链式闪电特效_{i}"
                    ));
                }

                _log.Debug($"链式闪电 第{i + 1}跳: {unitVictim.Data.Get<string>(DataKey.Name)} 受到 {damageInfo.FinalDamage:F1} 伤害");

                hitCount++;
                currentDamage *= damageDecay;
            }
        }

        _log.Info($"链式闪电执行完成: 初始伤害 {initialDamage:F1}, 弹跳次数 {hitCount}");
        return new AbilityExecutedResult { TargetsHit = hitCount };
    }
}
