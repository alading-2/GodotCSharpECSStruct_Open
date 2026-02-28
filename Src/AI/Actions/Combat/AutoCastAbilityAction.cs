/// <summary>
/// 动作节点：AI 自动施放指定技能
/// <para>
/// 通过 EntityManager 查找技能实体，走标准 TryTrigger 流水线施放。
/// 施法前会自动面向目标（如有）并停止移动。
/// </para>
/// <para>
/// 返回值：
/// - Success：技能触发成功
/// - Failure：技能不存在、目标无效或触发失败
/// </para>
/// </summary>
public class AutoCastAbilityAction : BehaviorNode
{
    private readonly string _abilityName;

    /// <summary>
    /// 创建自动施法动作节点
    /// </summary>
    /// <param name="abilityName">目标技能名称（与技能配置中的 Name 对应）</param>
    public AutoCastAbilityAction(string abilityName) : base($"自动施法({abilityName})")
    {
        _abilityName = abilityName;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var ability = EntityManager.GetAbilityByName(ctx.Entity, _abilityName);
        if (ability == null) return NodeState.Failure;

        // 施法前停止移动
        ctx.Entity.Data.Set(DataKey.AIMoveDirection, Godot.Vector2.Zero);
        ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, 0f);

        var castContext = new CastContext
        {
            Ability = ability,
            Caster = ctx.Entity,
            ResponseContext = new EventContext()
        };

        ability.Events.Emit(
            GameEventType.Ability.TryTrigger,
            new GameEventType.Ability.TryTriggerEventData(castContext)
        );

        var result = castContext.ResponseContext?.HasResult == true
            ? castContext.ResponseContext.GetResult<TriggerResult>()
            : TriggerResult.Failed;

        return result == TriggerResult.Success ? NodeState.Success : NodeState.Failure;
    }
}
