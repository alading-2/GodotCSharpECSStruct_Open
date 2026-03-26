using Godot;

/// <summary>
/// 动作节点：面向当前目标
/// <para>
/// 将 AIMoveDirection 设为面向目标的方向（仅设朝向，不设移动速度）。
/// 由 EntityMovementComponent 根据方向执行朝向翻转。
/// </para>
/// <para>
/// 返回值：
/// - Success：成功设置朝向（瞬时动作）
/// - Failure：目标或自身无效
/// </para>
/// </summary>
public class FaceTargetAction : BehaviorNode
{
    /// <summary>
    /// 创建面向目标动作节点
    /// </summary>
    public FaceTargetAction() : base("面向目标") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var target = ctx.Entity.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        Vector2 faceDir = (target.GlobalPosition - selfNode.GlobalPosition).Normalized();
        ctx.Entity.Data.Set(DataKey.AIMoveDirection, faceDir);

        return NodeState.Success;
    }
}
