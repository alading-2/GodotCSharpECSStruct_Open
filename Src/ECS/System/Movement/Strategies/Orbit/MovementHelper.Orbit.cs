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
    /// <item>按 <c>angularSpeed</c>（弧度/秒）推进极角 <c>currentAngle</c></item>
    /// <item>由极角 + 半径算出本帧目标轨道点 <c>newPos = center + (cos, sin) * radius</c></item>
    /// <item>将 <c>(newPos - node.GlobalPosition) / delta</c> 写入 <c>DataKey.Velocity</c></item>
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
    /// <param name="currentAngle">
    /// 当前极角（弧度），由策略实例持有，OnEnter 时从实体位置初始化（避免第一帧跳变），此后每帧累加。
    /// </param>
    /// <param name="delta">本帧时间（秒）</param>
    /// <returns>Continue(本帧位移量)</returns>
    public static MovementUpdateResult OrbitStep(
        Node2D node, Data data, MovementParams @params,
        Vector2 center, float radius, float angularSpeed,
        ref float currentAngle, float delta)
    {
        if (radius <= 0f || angularSpeed <= 0f) return MovementUpdateResult.Continue();

        float sign = @params.IsOrbitClockwise ? -1f : 1f;
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
