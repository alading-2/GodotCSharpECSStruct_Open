
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 链式闪电技能执行器 - 使用 Chain 执行模式
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 执行模式：Chain（延时弹跳）
/// 目标选择：Entity（Circle 几何查询初始目标）
/// 特效：Effect_lrsc3（在每个被命中目标位置生成独立特效）
/// 伤害：魔法伤害，每次弹跳衰减（可配置）
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

        // 安全检查
        if (caster == null || ability == null)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 读取配置参数
        var baseDamage = ability.Data.Get<float>(DataKey.AbilityDamage);
        var chainCount = ability.Data.Get<int>(DataKey.AbilityChainCount);
        var chainRange = ability.Data.Get<float>(DataKey.AbilityChainRange);
        var chainDelay = ability.Data.Get<float>(DataKey.AbilityChainDelay);
        var damageDecay = ability.Data.Get<float>(DataKey.AbilityChainDamageDecay) / 100f;
        var teamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter);
        var sorting = ability.Data.Get<AbilityTargetSorting>(DataKey.AbilityTargetSorting);

        // 计算初始伤害
        var finalAttack = caster.Data.Get<float>(DataKey.FinalAttack);
        var finalSkillDamage = caster.Data.Get<float>(DataKey.FinalSkillDamage);
        var initialDamage = (baseDamage + finalAttack) * (finalSkillDamage / 100f);

        // 获取初始目标（由 AbilityTargetSelectionComponent 提供）
        var initialTargets = context.Targets;
        if (initialTargets == null || initialTargets.Count == 0)
        {
            _log.Info("链式闪电未找到初始目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 预加载特效
        var effectScene = ResourceManagement.Load<PackedScene>(ResourcePaths.Asset_Effect_003, ResourceCategory.Asset);

        // 启动链式弹跳流程
        var firstTarget = initialTargets[0];
        StartChainBounce(caster, firstTarget, initialDamage, chainCount, chainRange, chainDelay, damageDecay, teamFilter, sorting, effectScene, new HashSet<IEntity>());

        // 立即返回（异步执行）
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private void StartChainBounce(
        IEntity caster,
        IEntity currentTarget,
        float currentDamage,
        int remainingBounces,
        float chainRange,
        float chainDelay,
        float damageDecay,
        AbilityTargetTeamFilter teamFilter,
        AbilityTargetSorting sorting,
        PackedScene effectScene,
        HashSet<IEntity> hitTargets)
    {
        // 对当前目标造成伤害
        if (currentTarget is IUnit unitVictim)
        {
            var damageInfo = new DamageInfo
            {
                Attacker = caster as Node,
                Victim = unitVictim,
                Damage = currentDamage,
                Type = DamageType.Magical
            };
            DamageService.Instance.Process(damageInfo);

            // 生成特效
            if (effectScene != null && currentTarget is Node2D targetNode2D)
            {
                EffectTool.Spawn(targetNode2D.GlobalPosition, new EffectSpawnOptions(
                    VisualScene: effectScene,
                    Name: $"链式闪电特效_{hitTargets.Count}"
                ));
            }

            _log.Debug($"链式闪电 第{hitTargets.Count + 1}跳: {unitVictim.Data.Get<string>(DataKey.Name)} 受到 {damageInfo.FinalDamage:F1} 伤害");
        }

        // 记录已命中目标
        hitTargets.Add(currentTarget);

        // 检查是否继续弹跳
        if (remainingBounces <= 1)
        {
            _log.Info($"链式闪电执行完成: 总弹跳次数 {hitTargets.Count}");
            return;
        }

        // 查找下一个目标
        var nextTarget = FindNextChainTarget(caster, currentTarget, chainRange, teamFilter, sorting, hitTargets);
        if (nextTarget == null)
        {
            _log.Debug($"链式闪电中断: 未找到下一个目标，已弹跳 {hitTargets.Count} 次");
            return;
        }

        // 延时后继续弹跳
        var nextDamage = currentDamage * damageDecay;
        TimerManager.Instance.Delay(chainDelay).OnComplete(() =>
        {
            StartChainBounce(caster, nextTarget, nextDamage, remainingBounces - 1, chainRange, chainDelay, damageDecay, teamFilter, sorting, effectScene, hitTargets);
        });
    }

    private IEntity FindNextChainTarget(
        IEntity caster,
        IEntity fromTarget,
        float range,
        AbilityTargetTeamFilter teamFilter,
        AbilityTargetSorting sorting,
        HashSet<IEntity> excludeTargets)
    {
        if (fromTarget is not Node2D fromNode) return null;

        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = fromNode.GlobalPosition,
            Range = range,
            CenterEntity = caster,
            TeamFilter = teamFilter,
            Sorting = sorting,
            MaxTargets = 10
        };

        var candidates = EntityTargetSelector.Query(query);
        foreach (var candidate in candidates)
        {
            if (!excludeTargets.Contains(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
