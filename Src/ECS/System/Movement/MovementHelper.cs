using Godot;

/// <summary>
/// 运动策略共用辅助方法
/// </summary>
public static class MovementHelper
{
    /// <summary>
    /// 统一朝向更新入口
    /// <para>
    /// - CharacterBody2D：更新 VisualRoot 的 FlipH（角色表现朝向）
    /// - Node2D / Area2D：按 RotateToVelocity 规则旋转节点（几何朝向）
    /// </para>
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="velocity">用于计算朝向的速度向量</param>
    /// <param name="isPhysicsBody">是否为 CharacterBody2D 路径</param>
    /// <param name="visualRoot">角色视觉根节点（CharacterBody2D 路径可选）</param>
    public static void UpdateOrientation(
        IEntity entity,
        Data data,
        Vector2 velocity,
        bool isPhysicsBody,
        AnimatedSprite2D? visualRoot = null)
    {
        if (velocity.LengthSquared() < 0.001f) return;

        if (isPhysicsBody)
        {
            if (visualRoot == null) return;
            if (Mathf.Abs(velocity.X) < 0.1f) return;

            visualRoot.FlipH = velocity.X < 0;
            return;
        }

        ApplyRotation(entity, data, velocity);
    }

    /// <summary>
    /// 若 RotateToVelocity=true，根据速度方向更新实体旋转角
    /// <para>速度过小时跳过旋转避免抖动</para>
    /// </summary>
    public static void ApplyRotation(IEntity entity, Data data, Vector2 velocity)
    {
        if (!data.Get<bool>(DataKey.RotateToVelocity)) return;
        if (entity is not Node2D node) return;
        if (velocity.LengthSquared() < 0.001f) return;

        node.Rotation = velocity.Angle();
    }

    /// <summary>
    /// 获取到达距离阈值；小于等于 0 时返回默认值 5f
    /// </summary>
    public static float GetReachDistance(Data data)
    {
        float reach = data.Get<float>(DataKey.MoveReachDistance);
        return reach > 0f ? reach : 5f;
    }
}
