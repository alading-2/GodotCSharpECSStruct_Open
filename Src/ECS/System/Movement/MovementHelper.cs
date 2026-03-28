using Godot;

/// <summary>
/// 运动系统共用辅助方法。
/// <para>
/// 这里放的是多个策略和调度器都会复用的纯工具逻辑，例如朝向更新、到达阈值读取、环绕计算。
/// </para>
/// </summary>
public static partial class MovementHelper
{
    /// <summary>
    /// 统一的朝向更新入口。
    /// <para>
    /// 如果实体有 `VisualRoot`，优先通过 `FlipH` 表示左右朝向，适合角色类资源。
    /// 如果没有 `VisualRoot`，则退化为根据方向向量旋转整个节点，适合子弹和特效。
    /// </para>
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="params">本次运动参数（读取 RotateToVelocity）</param>
    /// <param name="direction">用于判断朝向的意图方向，可来自速度、切线或显式面向向量</param>
    /// <param name="visualRoot">角色视觉根节点，可为空</param>
    public static void UpdateOrientation(
        IEntity entity,
        MovementParams @params,
        Vector2 direction,
        AnimatedSprite2D? visualRoot = null)
    {
        if (direction.LengthSquared() < 0.001f) return;

        if (visualRoot != null)
        {
            // 角色只关心左右朝向，接近竖直移动时不翻面，避免视觉抖动。
            if (Mathf.Abs(direction.X) < 0.1f) return;

            visualRoot.FlipH = direction.X < 0;
            return;
        }

        ApplyRotation(entity, @params, direction);
    }

    /// <summary>
    /// 当 `RotateToVelocity=true` 时，让实体朝向给定方向。
    /// <para>
    /// 该逻辑只对没有 `VisualRoot` 的普通 `Node2D` 有效，方向向量过小时会跳过旋转以避免角度抖动。
    /// </para>
    /// </summary>
    public static void ApplyRotation(IEntity entity, MovementParams @params, Vector2 direction)
    {
        if (!@params.RotateToVelocity) return;
        if (entity is not Node2D node) return;
        if (direction.LengthSquared() < 0.001f) return;

        node.RotationDegrees = Mathf.RadToDeg(direction.Angle());
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

}
