
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 回复光环技能执行器 - 示例实现
/// 
/// 演示功能：
/// 1. 被动周期技能 (Passive Periodic)
/// 2. 治疗效果 (Heal)
/// 3. 全体/光环效果
/// </summary>
public class RegenAuraExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(RegenAuraExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("RegenAura", new RegenAuraExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        if (caster == null || ability == null) return new AbilityExecutedResult();

        // 1. 选择目标
        // 如果是单体回复，Target 通常是 Self
        // 如果是光环，Targets 列表由 AbilitySystem 根据 Range 和 FriendlyFilter 筛选
        var targets = context.Targets;

        // 兜底：如果没有目标（比如配置为 NoTarget），则默认对自己生效
        if (targets == null || targets.Count == 0)
        {
            targets = new System.Collections.Generic.List<IEntity> { caster };
        }

        // 2. 执行治疗
        int count = 0;
        foreach (var target in targets)
        {
            // 检查是否禁止生命恢复
            var isDisabled = target.Data.Get<bool>(DataKey.IsDisableHealthRecovery);
            if (isDisabled)
            {
                _log.Debug($"{target.Data.Get<string>(DataKey.Name)} 禁止生命恢复，跳过");
                continue;
            }

            // 使用 FinalHpRegen 计算属性 - 已包含基础恢复和百分比恢复
            var healPerSecond = target.Data.Get<float>(DataKey.FinalHpRegen);
            var interval = ability.Data.Get<float>(DataKey.AbilityCooldown);  // 冷却时间
            var healAmount = healPerSecond * interval;  // 实际恢复量

            // 确保不超过最大生命值
            // 使用 HealRequest 事件统一请求治疗
            // 优点：自动处理统计、飘字、Anti-Heal (减疗) 等逻辑
            // HealSource.Skill 表明这是技能带来的治疗
            target.Events.Emit(GameEventType.Unit.HealRequest,
                new GameEventType.Unit.HealRequestEventData(healAmount, HealSource.Skill));

            _log.Info($"光环治疗请求: {target.Data.Get<string>(DataKey.Name)} +{healAmount:F1}");
            count++;
        }

        return new AbilityExecutedResult { TargetsHit = count };
    }
}
