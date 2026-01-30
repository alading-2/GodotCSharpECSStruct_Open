
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 复活卷轴执行器 - 示例实现
/// 
/// 演示功能：
/// 1. 限制次数技能 (Limited Charges)
/// 2. 特殊触发 (OnEvent: Death)
/// 3. 消耗充能但不会自动恢复 (ChargeTime < 0)
/// </summary>
public class ReviveScrollExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(ReviveScrollExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("ReviveScroll", new ReviveScrollExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;

        // 1. 检查触发上下文
        // 复活通常由 UnitKilled 事件触发
        // 我们需要由 TriggerComponent 传递过来的 SourceEventData 来确认死者是不是自己
        if (context.SourceEventData is not GameEventType.Unit.KilledEventData killData)
        {
            // 如果不是死亡事件触发的（比如手动点击），则允许直接使用（预防性复活？）
            _log.Info("复活卷轴手动使用（赋予复活Buff?）");
            return new AbilityExecutedResult { TargetsHit = 1 };
        }

        // 确保死的是自己
        if (killData.Victim != caster)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 2. 执行复活逻辑
        _log.Info("复活卷轴生效！英雄不朽！");

        // 实际逻辑：
        // 1. 发送复活治疗请求 (Revive source 通常不显示绿色飘字，或显示特殊样式)
        var maxHp = caster.Data.Get<float>(DataKey.FinalHp);

        // 发送治疗请求，使用 HealSource.Revive
        caster.Events.Emit(GameEventType.Unit.HealRequest,
            new GameEventType.Unit.HealRequestEventData(maxHp, HealSource.Revive));

        // 2. 移除死亡状态
        // caster.RemoveComponent<DeadComponent>(); 

        // 3. 给予无敌时间
        // caster.AddComponent(new InvincibilityComponent(3.0f));

        return new AbilityExecutedResult { TargetsHit = 1 };
    }
}
