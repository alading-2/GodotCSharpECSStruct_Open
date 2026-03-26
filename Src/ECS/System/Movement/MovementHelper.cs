using Godot;

/// <summary>
/// 运动系统共用辅助方法。
/// <para>
/// 这里放的是多个策略和调度器都会复用的纯工具逻辑，例如朝向更新、到达阈值读取、环绕计算。
/// </para>
/// </summary>
public static class MovementHelper
{
    /// <summary>
    /// 统一的朝向更新入口。
    /// <para>
    /// 如果实体有 `VisualRoot`，优先通过 `FlipH` 表示左右朝向，适合角色类资源。
    /// 如果没有 `VisualRoot`，则退化为根据 `RotateToVelocity` 旋转整个节点，适合子弹和特效。
    /// </para>
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="params">本次运动参数（读取 RotateToVelocity）</param>
    /// <param name="velocity">用于判断朝向的意图速度</param>
    /// <param name="visualRoot">角色视觉根节点，可为空</param>
    public static void UpdateOrientation(
        IEntity entity,
        MovementParams @params,
        Vector2 velocity,
        AnimatedSprite2D? visualRoot = null)
    {
        if (velocity.LengthSquared() < 0.001f) return;

        if (visualRoot != null)
        {
            // 角色只关心左右朝向，接近竖直移动时不翻面，避免视觉抖动。
            if (Mathf.Abs(velocity.X) < 0.1f) return;

            visualRoot.FlipH = velocity.X < 0;
            return;
        }

        ApplyRotation(entity, @params, velocity);
    }

    /// <summary>
    /// 当 `RotateToVelocity=true` 时，让实体朝向速度方向。
    /// <para>
    /// 该逻辑只对没有 `VisualRoot` 的普通 `Node2D` 有效，速度过小时会跳过旋转以避免角度抖动。
    /// </para>
    /// </summary>
    public static void ApplyRotation(IEntity entity, MovementParams @params, Vector2 velocity)
    {
        if (!@params.RotateToVelocity) return;
        if (entity is not Node2D node) return;
        if (velocity.LengthSquared() < 0.001f) return;

        node.Rotation = velocity.Angle();
    }

    /// <summary>
    /// 获取当前运动进度 [0, 1]，供策略做帧级插值或阶段判断使用。
    /// <para>优先按时间（MaxDuration），其次按距离（MaxDistance），两者均不限制时返回 0。</para>
    /// </summary>
    public static float GetProgress(MovementParams @params)
    {
        if (@params.MaxDuration >= 0f)
            return Mathf.Clamp(@params.ElapsedTime / @params.MaxDuration, 0f, 1f);

        if (@params.MaxDistance >= 0f)
            return Mathf.Clamp(@params.TraveledDistance / @params.MaxDistance, 0f, 1f);

        return 0f;
    }

    /// <summary>
    /// 三选二速度推导：从 <c>ActionSpeed</c> / <c>MaxDistance</c> / <c>MaxDuration</c> 中任意提供两个，推算出实际移动速度。
    /// <list type="bullet">
    /// <item><c>ActionSpeed &gt; 0</c> → 直接使用</item>
    /// <item><c>MaxDistance &gt; 0 &amp;&amp; MaxDuration &gt; 0</c> → speed = MaxDistance / MaxDuration</item>
    /// <item>其余情况返回 0f（策略应做保护处理）</item>
    /// </list>
    /// </summary>
    public static float ResolveActionSpeed(MovementParams @params)
    {
        if (@params.ActionSpeed > 0f) return @params.ActionSpeed;
        if (@params.MaxDistance > 0f && @params.MaxDuration > 0f)
            return @params.MaxDistance / @params.MaxDuration;
        return 0f;
    }

    /// <summary>
    /// 环绕运动单帧计算，供 OrbitPoint / OrbitEntity / Spiral 三个策略共用。
    /// 根据圆心、当前半径、角速度推进极角并计算本帧轨道点，将结果写入 DataKey.Velocity。
    /// </summary>
    /// <param name="node">当前运动节点</param>
    /// <param name="data">实体数据容器（写入 Velocity）</param>
    /// <param name="center">本帧圆心坐标</param>
    /// <param name="radius">本帧环绕半径</param>
    /// <param name="angularSpeed">角速度（弧度/秒）</param>
    /// <param name="clockwise">是否顺时针</param>
    /// <param name="currentAngle">当前极角（弧度），由策略实例持有，每帧更新</param>
    /// <param name="delta">本帧时间（秒）</param>
    /// <returns>Continue(displacement) 结果</returns>
    public static MovementUpdateResult OrbitStep(
        Node2D node, Data data,
        Vector2 center, float radius, float angularSpeed, bool clockwise,
        ref float currentAngle, float delta)
    {
        if (radius <= 0f || angularSpeed <= 0f) return MovementUpdateResult.Continue();

        float sign = clockwise ? -1f : 1f;
        currentAngle += sign * angularSpeed * delta;

        float cos = Mathf.Cos(currentAngle);
        float sin = Mathf.Sin(currentAngle);
        Vector2 newPos = center + new Vector2(cos * radius, sin * radius);

        Vector2 toTarget = newPos - node.GlobalPosition;
        float displacement = toTarget.Length();
        Vector2 velocity = displacement > 0.001f ? toTarget / Mathf.Max(delta, 0.001f) : Vector2.Zero;
        data.Set(DataKey.Velocity, velocity);

        return MovementUpdateResult.Continue(displacement);
    }
}
