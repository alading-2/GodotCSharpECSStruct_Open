using Godot;

/// <summary>
/// 通用二维几何工具。
/// <para>
/// 只负责纯几何判定、距离计算与随机采样，不包含目标选择、实体过滤或业务语义。
/// </para>
/// </summary>
public static class Geometry2D
{
    /// <summary>
    /// 判定点是否在圆内。
    /// </summary>
    /// <param name="point">目标点。</param>
    /// <param name="origin">圆心。</param>
    /// <param name="range">半径。</param>
    /// <returns>在圆内返回 true。</returns>
    public static bool IsPointInCircle(Vector2 point, Vector2 origin, float range)
    {
        return point.DistanceTo(origin) <= range;
    }

    /// <summary>
    /// 判定点是否在圆环内。
    /// </summary>
    /// <param name="innerRange">内半径。</param>
    /// <param name="outerRange">外半径。</param>
    public static bool IsPointInRing(Vector2 point, Vector2 origin, float innerRange, float outerRange)
    {
        float distance = point.DistanceTo(origin);
        return distance >= innerRange && distance <= outerRange;
    }

    /// <summary>
    /// 判定点是否在矩形盒体内（支持旋转）。
    /// </summary>
    /// <param name="origin">矩形起始底边中心点。</param>
    /// <param name="forward">矩形延伸方向（长度轴）。</param>
    /// <param name="width">矩形宽度（侧向）。</param>
    /// <param name="length">矩形长度（前向）。</param>
    public static bool IsPointInBox(Vector2 point, Vector2 origin, Vector2 forward, float width, float length)
    {
        forward = forward.Normalized();
        // 得到垂直于前向的右向量
        Vector2 right = new Vector2(-forward.Y, forward.X);
        Vector2 localPosition = point - origin;

        // 通过投影计算点在局部坐标系下的偏移
        float forwardDistance = localPosition.Dot(forward);
        float rightDistance = localPosition.Dot(right);
        
        // 判定投影是否在范围内（长度 [0, length]，宽度 [-width/2, width/2]）
        return forwardDistance >= 0f && forwardDistance <= length && Mathf.Abs(rightDistance) <= width * 0.5f;
    }

    /// <summary>
    /// 判定点是否在胶囊体（线段扩张）内。
    /// </summary>
    /// <param name="origin">起点。</param>
    /// <param name="forward">朝向。</param>
    /// <param name="length">线段长度。</param>
    /// <param name="width">胶囊体宽度（直径）。</param>
    public static bool IsPointInCapsule(Vector2 point, Vector2 origin, Vector2 forward, float length, float width)
    {
        forward = forward.Normalized();
        Vector2 endPoint = origin + forward * length;
        // 计算点到线段的距离，并与半径（宽度的一半）比较
        return PointToSegmentDistance(point, origin, endPoint) <= width * 0.5f;
    }

    /// <summary>
    /// 计算点到线段的最短距离。
    /// </summary>
    public static float PointToSegmentDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLengthSquared = line.LengthSquared();
        // 如果线段长度极短，视为点到起点的距离
        if (lineLengthSquared < 0.000001f) return point.DistanceTo(lineStart);

        // 计算点在无限长直线上的投影参数 t
        float t = Mathf.Clamp((point - lineStart).Dot(line) / lineLengthSquared, 0f, 1f);
        // 限制在 [0, 1] 范围内即为线段上的投影点
        Vector2 projection = lineStart + line * t;
        return point.DistanceTo(projection);
    }

    /// <summary>
    /// 判定点是否在扇形（椎体）内。
    /// </summary>
    /// <param name="origin">顶点。</param>
    /// <param name="forward">中心朝向。</param>
    /// <param name="range">半径。</param>
    /// <param name="angle">扇形总夹角（度）。</param>
    public static bool IsPointInCone(Vector2 point, Vector2 origin, Vector2 forward, float range, float angle)
    {
        Vector2 toTarget = point - origin;
        // 距离检查
        if (toTarget.Length() > range) return false;

        forward = forward.Normalized();
        float halfAngleRad = Mathf.DegToRad(angle * 0.5f);
        // 计算目标方向与中心方向的夹角
        float angleToTarget = forward.AngleTo(toTarget);
        return Mathf.Abs(angleToTarget) <= halfAngleRad;
    }

    /// <summary>
    /// 获取圆内随机点（均匀分布）。
    /// </summary>
    public static Vector2 GetRandomPointInCircle(Vector2 center, float radius, RandomNumberGenerator? rng = null)
    {
        return GetRandomPointInRing(center, 0f, radius, rng);
    }

    /// <summary>
    /// 获取圆环内随机点（均匀分布）。
    /// </summary>
    public static Vector2 GetRandomPointInRing(Vector2 center, float innerRadius, float outerRadius, RandomNumberGenerator? rng = null)
    {
        float randomAngle = (rng?.Randf() ?? GD.Randf()) * Mathf.Tau;
        float radiusRandom = rng?.Randf() ?? GD.Randf();

        float innerSquared = innerRadius * innerRadius;
        float outerSquared = outerRadius * outerRadius;
        // 为了保证在极坐标下均匀分布，需要对随机值开方进行半径采样
        // 面积 A ∝ r^2，所以样本 r ∝ sqrt(u)
        float distance = Mathf.Sqrt(radiusRandom * (outerSquared - innerSquared) + innerSquared);

        return center + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * distance;
    }

    /// <summary>
    /// 获取圆周上的随机点。
    /// </summary>
    public static Vector2 GetRandomPointOnPerimeter(Vector2 center, float radius, RandomNumberGenerator? rng = null)
    {
        float angle = (rng?.Randf() ?? GD.Randf()) * Mathf.Tau;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    /// <summary>
    /// 获取矩形盒体内的随机点（均匀分布）。
    /// </summary>
    /// <param name="center">矩形中心点。</param>
    public static Vector2 GetRandomPointInBox(Vector2 center, Vector2 forward, float width, float length, RandomNumberGenerator? rng = null)
    {
        forward = forward.Normalized();
        Vector2 right = new Vector2(-forward.Y, forward.X);

        float randomLength = Mathf.Lerp(-length * 0.5f, length * 0.5f, rng?.Randf() ?? GD.Randf());
        float randomWidth = Mathf.Lerp(-width * 0.5f, width * 0.5f, rng?.Randf() ?? GD.Randf());

        return center + forward * randomLength + right * randomWidth;
    }

    /// <summary>
    /// 获取扇形（椎体）内的随机点（均匀分布）。
    /// </summary>
    public static Vector2 GetRandomPointInCone(Vector2 origin, Vector2 forward, float range, float angle, RandomNumberGenerator? rng = null)
    {
        float randomAngle = rng?.Randf() ?? GD.Randf();
        float radiusRandom = rng?.Randf() ?? GD.Randf();

        float startAngle = forward.Angle() - Mathf.DegToRad(angle * 0.5f);
        float sampleAngle = startAngle + randomAngle * Mathf.DegToRad(angle);
        // 使用开方确保均匀分布
        float distance = Mathf.Sqrt(radiusRandom) * range;

        return origin + new Vector2(Mathf.Cos(sampleAngle), Mathf.Sin(sampleAngle)) * distance;
    }

    /// <summary>
    /// 获取轴向对齐包围盒 (AABB) 内的随机点。
    /// </summary>
    public static Vector2 GetRandomPointInAABB(Rect2 rect, RandomNumberGenerator? rng = null)
    {
        return new Vector2(
            Mathf.Lerp(rect.Position.X, rect.End.X, rng?.Randf() ?? GD.Randf()),
            Mathf.Lerp(rect.Position.Y, rect.End.Y, rng?.Randf() ?? GD.Randf()));
    }

    /// <summary>
    /// 在“挖空矩形”区域（外盒减去内盒）内获取随机点。
    /// <para>常用于在视野边缘外（但仍在波次限制内）生成敌人。</para>
    /// </summary>
    public static Vector2 GetRandomPointInHollowBox(Rect2 outerBox, Rect2 innerBox, RandomNumberGenerator? rng = null)
    {
        // 确保边界正确
        float outerLeft = Mathf.Min(outerBox.Position.X, innerBox.Position.X);
        float outerRight = Mathf.Max(outerBox.End.X, innerBox.End.X);
        float outerTop = Mathf.Min(outerBox.Position.Y, innerBox.Position.Y);
        float outerBottom = Mathf.Max(outerBox.End.Y, innerBox.End.Y);

        float innerLeft = innerBox.Position.X;
        float innerRight = innerBox.End.X;
        float innerTop = innerBox.Position.Y;
        float innerBottom = innerBox.End.Y;

        // 将中空区域划分为四个矩形：上、下、左、右
        float areaTop = Mathf.Max(0f, (outerRight - outerLeft) * (innerTop - outerTop));
        float areaBottom = Mathf.Max(0f, (outerRight - outerLeft) * (outerBottom - innerBottom));
        float areaLeft = Mathf.Max(0f, (innerLeft - outerLeft) * (innerBottom - innerTop));
        float areaRight = Mathf.Max(0f, (outerRight - innerRight) * (innerBottom - innerTop));
        float totalArea = areaTop + areaBottom + areaLeft + areaRight;

        // 如果总面积太小，回退到圆周采样
        if (totalArea <= 0.001f)
        {
            return GetRandomPointOnPerimeter(innerBox.GetCenter(), innerBox.Size.X * 0.5f + 50f, rng);
        }

        // 按面积权重随机选择一个子区域
        float regionPick = (rng?.Randf() ?? GD.Randf()) * totalArea;
        float rand1 = rng?.Randf() ?? GD.Randf();
        float rand2 = rng?.Randf() ?? GD.Randf();

        if (regionPick < areaTop)
        {
            return new Vector2(Mathf.Lerp(outerLeft, outerRight, rand1), Mathf.Lerp(outerTop, innerTop, rand2));
        }

        if (regionPick < areaTop + areaBottom)
        {
            return new Vector2(Mathf.Lerp(outerLeft, outerRight, rand1), Mathf.Lerp(innerBottom, outerBottom, rand2));
        }

        if (regionPick < areaTop + areaBottom + areaLeft)
        {
            return new Vector2(Mathf.Lerp(outerLeft, innerLeft, rand1), Mathf.Lerp(innerTop, innerBottom, rand2));
        }

        return new Vector2(Mathf.Lerp(innerRight, outerRight, rand1), Mathf.Lerp(innerTop, innerBottom, rand2));
    }
}
