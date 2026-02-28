using Godot;

/// <summary>
/// TargetSelector 几何计算工具（纯数学层）。
/// 仅负责“点是否落在形状内”与“在形状内生成随机点”，
/// 不依赖实体系统与业务规则，可被 EntityTargetSelector / PositionTargetSelector 复用。
/// </summary>
public static class GeometryCalculator
{
    // ==================== 几何范围检测 (IsPointInShape) ====================

    /// <summary>
    /// 使用统一入口判断点是否在查询几何内。
    /// 当 <see cref="TargetSelectorQuery.Forward"/> 为空时，默认朝向使用 <see cref="Vector2.Right"/>。
    /// </summary>
    public static bool IsPointInGeometry(Vector2 point, TargetSelectorQuery query)
    {
        return query.Geometry switch
        {
            GeometryType.Circle => IsPointInCircle(point, query.Origin, query.Range),
            GeometryType.Ring => IsPointInRing(point, query.Origin, query.InnerRange, query.Range),
            GeometryType.Box => IsPointInBox(point, query.Origin, query.Forward ?? Vector2.Right, query.Width, query.Length),
            GeometryType.Line => IsPointInCapsule(point, query.Origin, query.Forward ?? Vector2.Right, query.Length, query.Width),
            GeometryType.Cone => IsPointInCone(point, query.Origin, query.Forward ?? Vector2.Right, query.Range, query.Angle),
            GeometryType.Global => true,
            _ => false
        };
    }

    /// <summary>
    /// 圆形判定：点到圆心距离小于等于半径。
    /// </summary>
    public static bool IsPointInCircle(Vector2 point, Vector2 origin, float range)
    {
        return point.DistanceTo(origin) <= range;
    }

    /// <summary>
    /// 圆环判定：点到圆心距离位于内半径与外半径之间（含边界）。
    /// </summary>
    public static bool IsPointInRing(Vector2 point, Vector2 origin, float innerRange, float outerRange)
    {
        float distance = point.DistanceTo(origin);
        return distance >= innerRange && distance <= outerRange;
    }

    /// <summary>
    /// 定向矩形判定。
    /// 通过 forward/right 构造局部坐标系，再比较点在局部空间中的投影是否落入半长半宽范围。
    /// </summary>
    public static bool IsPointInBox(Vector2 point, Vector2 origin, Vector2 forward, float width, float length)
    {
        forward = forward.Normalized();
        Vector2 right = new Vector2(-forward.Y, forward.X);
        Vector2 localPos = point - origin;

        float forwardDist = localPos.Dot(forward);
        float rightDist = localPos.Dot(right);

        // 修正：通常攻击矩形以施法者为起点，向 forward 延伸 length，而不是以自身为中心前后各延伸 length/2
        return forwardDist >= 0f && forwardDist <= length && Mathf.Abs(rightDist) <= width / 2f;
    }

    /// <summary>
    /// 线形判定（胶囊）：
    /// 计算点到线段 [origin, origin + forward * length] 的最短距离，
    /// 若该距离小于等于 width / 2，则视为命中。
    /// </summary>
    public static bool IsPointInCapsule(Vector2 point, Vector2 origin, Vector2 forward, float length, float width)
    {
        forward = forward.Normalized();
        Vector2 endPoint = origin + forward * length;
        float distToLine = PointToSegmentDistance(point, origin, endPoint);
        return distToLine <= width / 2f;
    }

    /// <summary>
    /// 计算点到线段最短距离。
    /// 使用投影参数 t（限制在 [0,1]）取得最近点，再计算欧氏距离。
    /// </summary>
    private static float PointToSegmentDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLengthSquared = line.LengthSquared();
        if (lineLengthSquared < 0.000001f) return point.DistanceTo(lineStart);
        float t = Mathf.Clamp((point - lineStart).Dot(line) / lineLengthSquared, 0f, 1f);
        Vector2 projection = lineStart + line * t;
        return point.DistanceTo(projection);
    }

    /// <summary>
    /// 扇形判定：
    /// 1) 先做半径裁剪；
    /// 2) 再比较 forward 与 toTarget 的夹角是否落在半角内。
    /// </summary>
    public static bool IsPointInCone(Vector2 point, Vector2 origin, Vector2 forward, float range, float angle)
    {
        Vector2 toTarget = point - origin;
        if (toTarget.Length() > range) return false;

        forward = forward.Normalized();
        float halfAngleRad = Mathf.DegToRad(angle / 2f);
        float angleToTarget = forward.AngleTo(toTarget);
        return Mathf.Abs(angleToTarget) <= halfAngleRad;
    }

    // ==================== 几何位置生成 (GetRandomPointInShape) ====================

    /// <summary>
    /// 在指定几何内生成一个随机点。
    /// 该方法仅负责形状分发与参数转义，不做障碍、导航可达性或业务合法性检查。
    /// </summary>
    public static Vector2 GetRandomPointInGeometry(TargetSelectorQuery query, RandomNumberGenerator? rng = null)
    {
        return query.Geometry switch
        {
            GeometryType.Circle => GetRandomPointInRing(query.Origin, 0, query.Range, rng),
            GeometryType.Ring => GetRandomPointInRing(query.Origin, query.InnerRange, query.Range, rng),
            GeometryType.Box => GetRandomPointInBox(query.Origin + (query.Forward ?? Vector2.Right) * (query.Length / 2f), query.Forward ?? Vector2.Right, query.Width, query.Length, rng), // 修正为向前偏移中心
            GeometryType.Line => GetRandomPointInBox(query.Origin + (query.Forward ?? Vector2.Right) * (query.Length / 2f), query.Forward ?? Vector2.Right, query.Width, query.Length, rng), // 线形(胶囊) 简化为矩形中心偏移长度一半
            GeometryType.Cone => InternalRandomPointInCone(query.Origin, query.Forward ?? Vector2.Right, query.Range, query.Angle, rng),
            _ => query.Origin
        };
    }

    /// <summary>
    /// 在实心圆内生成面积均匀分布的随机点。
    /// </summary>
    public static Vector2 GetRandomPointInCircle(Vector2 center, float radius, RandomNumberGenerator? rng = null)
    {
        return GetRandomPointInRing(center, 0f, radius, rng);
    }

    /// <summary>
    /// 在圆环内生成面积均匀分布的随机点。
    /// 通过对随机半径使用 sqrt 变换避免圆心附近过密。
    /// </summary>
    public static Vector2 GetRandomPointInRing(Vector2 center, float innerRadius, float outerRadius, RandomNumberGenerator? rng = null)
    {
        float rand1 = rng?.Randf() ?? GD.Randf();
        float rand2 = rng?.Randf() ?? GD.Randf();

        float angle = rand1 * Mathf.Tau;
        // 使用 Sqrt 保证面积分布均匀（修复了简单的线性 rand 导致的圆心密集问题）
        // 半径的分布: r = sqrt(rand * (R^2 - r^2) + r^2)
        float sqrInner = innerRadius * innerRadius;
        float sqrOuter = outerRadius * outerRadius;
        float dist = Mathf.Sqrt(rand2 * (sqrOuter - sqrInner) + sqrInner);

        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    /// <summary>
    /// 在圆周边缘生成随机点（固定半径，随机角度）。
    /// </summary>
    public static Vector2 GetRandomPointOnPerimeter(Vector2 center, float radius, RandomNumberGenerator? rng = null)
    {
        float angle = (rng?.Randf() ?? GD.Randf()) * Mathf.Tau;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    /// <summary>
    /// 在带朝向矩形内生成均匀分布的随机点。
    /// 本质是先在局部轴向随机，再映射回世界坐标。
    /// </summary>
    public static Vector2 GetRandomPointInBox(Vector2 center, Vector2 forward, float width, float length, RandomNumberGenerator? rng = null)
    {
        forward = forward.Normalized();
        Vector2 right = new Vector2(-forward.Y, forward.X);

        float rand1 = rng?.Randf() ?? GD.Randf();
        float rand2 = rng?.Randf() ?? GD.Randf();

        float randL = Mathf.Lerp(-length / 2f, length / 2f, rand1);
        float randW = Mathf.Lerp(-width / 2f, width / 2f, rand2);

        return center + forward * randL + right * randW;
    }

    /// <summary>
    /// 扇形内随机点生成的内部实现。
    /// 使用 sqrt(rand) 让半径分布满足面积均匀。
    /// </summary>
    private static Vector2 InternalRandomPointInCone(Vector2 origin, Vector2 forward, float range, float angle, RandomNumberGenerator? rng)
    {
        float rand1 = rng?.Randf() ?? GD.Randf();
        float rand2 = rng?.Randf() ?? GD.Randf();

        float startAngle = forward.Angle() - Mathf.DegToRad(angle / 2f);
        float randomAngle = startAngle + (rand1 * Mathf.DegToRad(angle));
        float dist = Mathf.Sqrt(rand2) * range; // 扇形同样使用 Sqrt 维持面积平均

        return origin + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * dist;
    }

    // =========================== EnemySpawn方法 ===========================

    /// <summary>
    /// 在 AABB（轴对齐矩形）内生成均匀分布的随机点。
    /// </summary>
    public static Vector2 GetRandomPointInAABB(Rect2 rect, RandomNumberGenerator? rng = null)
    {
        float randX = rng?.Randf() ?? GD.Randf();
        float randY = rng?.Randf() ?? GD.Randf();
        return new Vector2(
            Mathf.Lerp(rect.Position.X, rect.End.X, randX),
            Mathf.Lerp(rect.Position.Y, rect.End.Y, randY)
        );
    }

    /// <summary>
    /// 在大矩形挖去子矩形后的“空心回字形”区域内生成均匀分布的随机点。
    /// 常用于“屏幕外刷怪”：外层是生成边界，内层是相机可见区域。
    /// 实现方式：将可用区域拆分为上/下/左/右 4 个不重叠矩形，按面积加权随机。
    /// </summary>
    public static Vector2 GetRandomPointInHollowBox(Rect2 outerBox, Rect2 innerBox, RandomNumberGenerator? rng = null)
    {
        // 确保外边框完全包含内边框，否则强制限制
        float outerLeft = Mathf.Min(outerBox.Position.X, innerBox.Position.X);
        float outerRight = Mathf.Max(outerBox.End.X, innerBox.End.X);
        float outerTop = Mathf.Min(outerBox.Position.Y, innerBox.Position.Y);
        float outerBottom = Mathf.Max(outerBox.End.Y, innerBox.End.Y);

        float innerLeft = innerBox.Position.X;
        float innerRight = innerBox.End.X;
        float innerTop = innerBox.Position.Y;
        float innerBottom = innerBox.End.Y;

        // 将剩余区域划分为 4 个不重叠的矩形块 (上、下、左、右)
        float areaTop = (outerRight - outerLeft) * (innerTop - outerTop);
        float areaBottom = (outerRight - outerLeft) * (outerBottom - innerBottom);
        float areaLeft = (innerLeft - outerLeft) * (innerBottom - innerTop);
        float areaRight = (outerRight - innerRight) * (innerBottom - innerTop);

        // 防止负面积
        areaTop = Mathf.Max(0, areaTop);
        areaBottom = Mathf.Max(0, areaBottom);
        areaLeft = Mathf.Max(0, areaLeft);
        areaRight = Mathf.Max(0, areaRight);

        float totalArea = areaTop + areaBottom + areaLeft + areaRight;

        // 如果没有可用区域，退化为环绕内环边缘的一个随机圈
        if (totalArea <= 0.001f)
        {
            return GetRandomPointOnPerimeter(innerBox.GetCenter(), innerBox.Size.X / 2f + 50f, rng);
        }

        // 按面积权重随机选择一个区域
        float r = (rng?.Randf() ?? GD.Randf()) * totalArea;

        float rand1 = rng?.Randf() ?? GD.Randf();
        float rand2 = rng?.Randf() ?? GD.Randf();

        if (r < areaTop) // Top 区域
        {
            return new Vector2(Mathf.Lerp(outerLeft, outerRight, rand1), Mathf.Lerp(outerTop, innerTop, rand2));
        }
        else if (r < areaTop + areaBottom) // Bottom 区域
        {
            return new Vector2(Mathf.Lerp(outerLeft, outerRight, rand1), Mathf.Lerp(innerBottom, outerBottom, rand2));
        }
        else if (r < areaTop + areaBottom + areaLeft) // Left 区域
        {
            return new Vector2(Mathf.Lerp(outerLeft, innerLeft, rand1), Mathf.Lerp(innerTop, innerBottom, rand2));
        }
        else // Right 区域
        {
            return new Vector2(Mathf.Lerp(innerRight, outerRight, rand1), Mathf.Lerp(innerTop, innerBottom, rand2));
        }
    }
}
