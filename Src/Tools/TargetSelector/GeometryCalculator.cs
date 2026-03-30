using Godot;

/// <summary>
/// TargetSelector 几何计算兼容门面。
/// <para>
/// 纯几何算法已下沉到 <see cref="Geometry2D"/>，本类保留旧入口，避免一次性打断
/// TargetSelector、SpawnSystem 与测试代码。
/// </para>
/// </summary>
public static class GeometryCalculator
{
    /// <summary>
    /// 判定点是否在给定的查询几何体内。
    /// </summary>
    /// <param name="point">待判定点。</param>
    /// <param name="query">包含几何类型、原点、朝向及尺寸参数的查询对象。</param>
    /// <returns>在几何体内返回 true。</returns>
    public static bool IsPointInGeometry(Vector2 point, TargetSelectorQuery query)
    {
        return query.Geometry switch
        {
            // 圆形判定
            GeometryType.Circle => Geometry2D.IsPointInCircle(point, query.Origin, query.Range),
            // 圆环判定：使用 InnerRange 作为内半径，Range 作为外半径
            GeometryType.Ring => Geometry2D.IsPointInRing(point, query.Origin, query.InnerRange, query.Range),
            // 矩形判定：通过投影实现
            GeometryType.Box => Geometry2D.IsPointInBox(point, query.Origin, query.Forward ?? Vector2.Right, query.Width, query.Length),
            // 线段（胶囊体）判定
            GeometryType.Line => Geometry2D.IsPointInCapsule(point, query.Origin, query.Forward ?? Vector2.Right, query.Length, query.Width),
            // 扇形（椎体）判定
            GeometryType.Cone => Geometry2D.IsPointInCone(point, query.Origin, query.Forward ?? Vector2.Right, query.Range, query.Angle),
            // 全局判定：始终返回 true
            GeometryType.Global => true,
            _ => false
        };
    }

    /// <summary>判定点是否在圆内。</summary>
    public static bool IsPointInCircle(Vector2 point, Vector2 origin, float range)
        => Geometry2D.IsPointInCircle(point, origin, range);

    /// <summary>判定点是否在圆环内。</summary>
    public static bool IsPointInRing(Vector2 point, Vector2 origin, float innerRange, float outerRange)
        => Geometry2D.IsPointInRing(point, origin, innerRange, outerRange);

    /// <summary>判定点是否在矩形盒体内。</summary>
    public static bool IsPointInBox(Vector2 point, Vector2 origin, Vector2 forward, float width, float length)
        => Geometry2D.IsPointInBox(point, origin, forward, width, length);

    /// <summary>判定点是否在胶囊体内。</summary>
    public static bool IsPointInCapsule(Vector2 point, Vector2 origin, Vector2 forward, float length, float width)
        => Geometry2D.IsPointInCapsule(point, origin, forward, length, width);

    /// <summary>判定点是否在扇形内。</summary>
    public static bool IsPointInCone(Vector2 point, Vector2 origin, Vector2 forward, float range, float angle)
        => Geometry2D.IsPointInCone(point, origin, forward, range, angle);

    /// <summary>
    /// 在给定的查询几何体内随机采样一个点。
    /// </summary>
    /// <param name="query">几何查询参数。</param>
    /// <param name="rng">随机数生成器（可选）。</param>
    /// <returns>采样出的世界坐标点。</returns>
    public static Vector2 GetRandomPointInGeometry(TargetSelectorQuery query, RandomNumberGenerator? rng = null)
    {
        return query.Geometry switch
        {
            GeometryType.Circle => Geometry2D.GetRandomPointInRing(query.Origin, 0f, query.Range, rng),
            GeometryType.Ring => Geometry2D.GetRandomPointInRing(query.Origin, query.InnerRange, query.Range, rng),
            GeometryType.Box => Geometry2D.GetRandomPointInBox(
                // 注意：Geometry2D.GetRandomPointInBox 需要中心点，而 query.Origin 通常是矩形底边中心
                // 因此需要偏移半个长度到中心位置
                query.Origin + (query.Forward ?? Vector2.Right) * (query.Length * 0.5f),
                query.Forward ?? Vector2.Right,
                query.Width,
                query.Length,
                rng),
            GeometryType.Line => Geometry2D.GetRandomPointInBox(
                query.Origin + (query.Forward ?? Vector2.Right) * (query.Length * 0.5f),
                query.Forward ?? Vector2.Right,
                query.Width,
                query.Length,
                rng),
            GeometryType.Cone => Geometry2D.GetRandomPointInCone(query.Origin, query.Forward ?? Vector2.Right, query.Range, query.Angle, rng),
            _ => query.Origin
        };
    }

    /// <summary>获取圆内随机均匀分布点。</summary>
    public static Vector2 GetRandomPointInCircle(Vector2 center, float radius, RandomNumberGenerator? rng = null)
        => Geometry2D.GetRandomPointInCircle(center, radius, rng);

    /// <summary>获取圆环内随机均匀分布点。</summary>
    public static Vector2 GetRandomPointInRing(Vector2 center, float innerRadius, float outerRadius, RandomNumberGenerator? rng = null)
        => Geometry2D.GetRandomPointInRing(center, innerRadius, outerRadius, rng);

    /// <summary>获取圆周上的随机点。</summary>
    public static Vector2 GetRandomPointOnPerimeter(Vector2 center, float radius, RandomNumberGenerator? rng = null)
        => Geometry2D.GetRandomPointOnPerimeter(center, radius, rng);

    /// <summary>获取矩形区域内的随机点。</summary>
    public static Vector2 GetRandomPointInBox(Vector2 center, Vector2 forward, float width, float length, RandomNumberGenerator? rng = null)
        => Geometry2D.GetRandomPointInBox(center, forward, width, length, rng);

    /// <summary>获取 AABB 矩形内的随机点。</summary>
    public static Vector2 GetRandomPointInAABB(Rect2 rect, RandomNumberGenerator? rng = null)
        => Geometry2D.GetRandomPointInAABB(rect, rng);

    /// <summary>在两个 AABB 构成的中空矩形区域内随机采样点。</summary>
    public static Vector2 GetRandomPointInHollowBox(Rect2 outerBox, Rect2 innerBox, RandomNumberGenerator? rng = null)
        => Geometry2D.GetRandomPointInHollowBox(outerBox, innerBox, rng);
}
