
using System.Runtime.CompilerServices;

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
    private static readonly Log _log = new("RegenAuraExecutor");

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("RegenAura", new RegenAuraExecutor());
    }

    public AbilityExecuteResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        if (caster == null || ability == null) return new AbilityExecuteResult();

        // 1. 获取回复量 (例如：每次回复 5% 最大生命值)
        var maxHp = caster.Data.Get<float>(DataKey.FinalHp);
        var healAmount = maxHp * 0.05f;

        // 2. 选择目标
        // 如果是单体回复，Target 通常是 Self
        // 如果是光环，Targets 列表由 AbilitySystem 根据 Range 和 FriendlyFilter 筛选
        var targets = context.Targets;

        // 兜底：如果没有目标（比如配置为 NoTarget），则默认对自己生效
        if (targets == null || targets.Count == 0)
        {
            targets = new System.Collections.Generic.List<IEntity> { caster };
        }

        // 3. 执行治疗
        int count = 0;
        foreach (var target in targets)
        {
            // 发送治疗事件 (假设有 Heal 事件)
            // target.Events.Emit(GameEventType.Combat.Heal, ...);

            // 或者直接修改数值 (不推荐，最好走事件)
            var currentHp = target.Data.Get<float>(DataKey.CurrentHp);
            target.Data.Set(DataKey.CurrentHp, System.Math.Min(currentHp + healAmount, maxHp));

            _log.Info($"光环治疗: {target.Data.Get<string>(DataKey.Name)} +{healAmount}");
            count++;
        }

        return new AbilityExecuteResult { TargetsHit = count };
    }
}
