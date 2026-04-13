using Godot;

/// <summary>
/// MovementHelper 的 Orbit 专用辅助方法（含螺旋参数化）。
/// </summary>
public static partial class MovementHelper
{
    /// <summary>
    /// 角速度三选二推导：<c>OrbitAngularSpeed &gt; 0</c> 直接用；否则从 <c>OrbitTotalAngle / MaxDuration</c> 推算；两者均无效返回 0。
    /// </summary>
    public static float ResolveAngularSpeed(MovementParams @params)
    {
        if (@params.OrbitAngularSpeed > 0f) return @params.OrbitAngularSpeed;
        if (@params.OrbitTotalAngle >= 0f && @params.MaxDuration >= 0f)
            return @params.OrbitTotalAngle / @params.MaxDuration;
        return 0f;
    }

    /// <summary>
    /// 解析本帧环绕圆心：<c>TargetNode</c> 有效时实时跟随，否则使用 <c>OrbitCenter</c> 固定点。
    /// 返回 <c>null</c> 表示 <c>TargetNode</c> 已设置但已失效（调用方应停止移动）。
    /// </summary>
    public static Vector2? ResolveOrbitCenter(MovementParams @params)
    {
        if (@params.TargetNode != null)
        {
            if (!GodotObject.IsInstanceValid(@params.TargetNode)) return null;
            return @params.TargetNode.GlobalPosition;
        }

        return @params.OrbitCenter;
    }

    /// <summary>
    /// 环绕运动单帧核心计算，供 <c>OrbitStrategy</c> 共用（可通过径向参数表达螺旋）。
    /// <para>
    /// 【算法流程】
    /// <list type="number">
    /// <item>按 <c>angularSpeed</c>（度/秒）推进极角 <c>currentAngle</c>（度）</item>
    /// <item>由极角 + 半径算出本帧目标轨道点 <c>newPos = center + (cos, sin) * radius</c></item>
    /// <item>将 <c>(newPos - node.GlobalPosition) / delta</c> 写入 <c>DataKey.Velocity</c></item>
    /// <item>使用切向速度 + 径向速度合成轨迹切线，作为显式朝向意图返回</item>
    /// </list>
    /// 速度驱动（而非直接赋值 GlobalPosition），碰撞体走 MoveAndSlide 后若有偏移，下一帧速度会自动拉回轨道。
    /// </para>
    /// <para>
    /// 【为什么不从位置反推极角】
    /// 极角由调用方以 <c>ref float currentAngle</c> 显式存储，而非每帧 <c>Atan2(entity - center)</c> 重算。原因：
    /// <list type="bullet">
    /// <item>数值累加比 <c>Atan2</c> 更快，且无 ±π 不连续跳变问题</item>
    /// <item>实体被碰撞偏离轨道时，存储角度仍按预期速度推进，下帧速度校正；反推角度则轨道随偏移飘移</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="node">当前运动节点</param>
    /// <param name="data">实体数据容器（写入 DataKey.Velocity）</param>
    /// <param name="params">运动参数（读取 <c>IsOrbitClockwise</c>）</param>
    /// <param name="center">本帧圆心坐标（调用方已处理 null 检测）</param>
    /// <param name="radius">本帧环绕半径（调用方已处理径向变化，如螺旋 <c>_currentRadius</c>）</param>
    /// <param name="angularSpeed">本帧角速度（调用方已处理加速度，恒 >= 0）</param>
    /// <param name="radialSpeed">本帧实际径向速度（像素/秒），用于计算螺旋轨迹的视觉朝向</param>
    /// <param name="currentAngle">
    /// 当前极角（度），由策略实例持有，OnEnter 时从实体位置初始化（避免第一帧跳变），此后每帧累加。
    /// </param>
    /// <param name="delta">本帧时间（秒）</param>
    /// <returns>Continue(本帧位移量)</returns>
    public static MovementUpdateResult OrbitStep(
        Node2D node, Data data, MovementParams @params,
        Vector2 center, float radius, float angularSpeed, float radialSpeed,
        ref float currentAngle, float delta)
    {
        // 任一关键量为 0 时，本帧不产生有效轨道推进：
        // - radius <= 0：退化到圆心点
        // - angularSpeed <= 0：角度不再推进
        // 统一返回 Continue，让上层终止条件（如总角度/时长）决定是否结束。
        if (radius <= 0f || angularSpeed <= 0f) return MovementUpdateResult.Continue();

        // 0 = 向右、90 = 向下、180 = 向左
        float sign = @params.IsOrbitClockwise ? 1f : -1f;
        currentAngle += sign * angularSpeed * delta;

        float currentAngleRad = Mathf.DegToRad(currentAngle);
        float cos = Mathf.Cos(currentAngleRad);
        float sin = Mathf.Sin(currentAngleRad);
        Vector2 newPos = center + new Vector2(cos * radius, sin * radius);

        // 期望轨道点与当前位置的差，表示本帧需要“拉回轨道”的位移。
        // 这里不直接写 GlobalPosition，而是转换成速度交给统一运动管线（如 MoveAndSlide）处理。
        Vector2 toTarget = newPos - node.GlobalPosition;
        float displacement = toTarget.Length();
        Vector2 velocity = displacement > 0.001f ? toTarget / Mathf.Max(delta, 0.001f) : Vector2.Zero;
        data.Set(DataKey.Velocity, velocity);

        // 视觉朝向取解析轨迹切线，而不是当前位置到轨道点的纠偏向量。
        // 这样做的好处：朝向基于预期轨迹而非实际偏差，避免碰撞偏移导致的朝向抖动。

        // 1. 计算径向单位向量：从圆心指向当前位置
        Vector2 radialDirection = new Vector2(cos, sin);

        // 2. 计算切向单位向量：垂直于径向，顺时针/逆时针由 sign 决定
        //    数学推导：对 (cosθ, sinθ) 求导得到 (-sinθ, cosθ)，即切线方向
        Vector2 tangentialDirection = new Vector2(-sin, cos) * sign;

        // 3. 将角速度转换为弧度制（用于速度计算）
        float angularSpeedRad = Mathf.DegToRad(angularSpeed);

        // 4. 速度合成：切向速度 + 径向速度 = 瞬时速度方向
        //    - 切向速度 = 半径 × 角速度（圆周运动的线速度）
        //    - 径向速度 = 螺旋运动的径向变化率
        //    - 合成方向即为实体在轨道上的瞬时运动方向
        // 这里的 radialSpeed 由 OrbitStrategy 用 ValueDelta / delta 传入，
        // 因此可正确表达“本帧向内收缩/向外扩散”的方向信息。
        Vector2 facingDirection = tangentialDirection * (radius * angularSpeedRad) + radialDirection * radialSpeed;

        // 5. 处理零速度特殊情况：当角速度和径向速度都接近 0 时
        if (facingDirection.LengthSquared() < 0.001f)
        {
            // 优先使用切向方向（纯圆周运动的预期方向）
            facingDirection = tangentialDirection;
        }

        return MovementUpdateResult.Continue(displacement, facingDirection);
    }
}
