using Godot;

/// <summary>
/// 动作节点：停止移动
/// <para>
/// 将移动方向清零、速度倍率设为 0，瞬时完成。
/// 常用于攻击前停步、受击僵直等场景。
/// </para>
/// <para>
/// 返回值：
/// - Success：始终成功（瞬时动作）
/// </para>
/// </summary>
public class StopMovementAction : BehaviorNode
{
    /// <summary>
    /// 创建停止移动动作节点
    /// </summary>
    public StopMovementAction() : base("停止移动") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        ctx.Entity.Data.Set(DataKey.AIMoveDirection, Vector2.Zero);
        ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, 0f);
        return NodeState.Success;
    }
}
