using Godot;
using System;

/// <summary>
/// 由起点、终点、半径和方向定义的二维圆弧。
/// </summary>
public readonly struct CircularArc2D
{
    /// <summary>起点。</summary>
    public Vector2 Start { get; }
    /// <summary>终点。</summary>
    public Vector2 End { get; }
    /// <summary>圆心。</summary>
    public Vector2 Center { get; }
    /// <summary>半径。</summary>
    public float Radius { get; }
    /// <summary>起始点相对于圆心的角度（弧度）。</summary>
    public float StartAngle { get; }
    /// <summary>从起点到终点的扫掠角度（弧度，[-Pi, Pi]）。</summary>
    public float SweepAngle { get; }
    /// <summary>圆弧配置是否有效。</summary>
    public bool IsValid { get; }

    private CircularArc2D(
        Vector2 start,
        Vector2 end,
        Vector2 center,
        float radius,
        float startAngle,
        float sweepAngle,
        bool isValid)
    {
        Start = start;
        End = end;
        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        SweepAngle = sweepAngle;
        IsValid = isValid;
    }

    /// <summary>
    /// 根据起点、终点和半径创建一段圆弧。
    /// </summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <param name="radius">半径（必须大于等于半弦长）。</param>
    /// <param name="clockwise">相对于弦线的前进方向，圆弧是否向顺时针侧弯曲。</param>
    public static CircularArc2D Create(Vector2 start, Vector2 end, float radius, bool clockwise)
    {
        radius = Mathf.Abs(radius);
        Vector2 chord = end - start;
        float chordLength = chord.Length();
        if (radius <= 0.001f || chordLength <= 0.001f)
        {
            return default;
        }

        float halfChord = chordLength * 0.5f;
        // 半径不能小于弦长的一半
        if (radius < halfChord)
        {
            return default;
        }

        Vector2 forward = chord / chordLength;
        // bowSide 指向圆弧鼓起的方向（根据顺逆时针决定）
        Vector2 bowSide = new Vector2(-forward.Y, forward.X) * (clockwise ? 1f : -1f);
        // 根据勾股定理计算圆心到弦中点的距离（偏移量）
        float centerOffset = Mathf.Sqrt(Mathf.Max(0f, radius * radius - halfChord * halfChord));
        // 圆心位于弦中点的反方向
        Vector2 center = (start + end) * 0.5f - bowSide * centerOffset;

        Vector2 startOffset = start - center;
        Vector2 endOffset = end - center;
        float startAngle = startOffset.Angle();
        float endAngle = endOffset.Angle();
        // 计算扫掠角度，并处理跨越 -Pi/Pi 边界的情况
        float sweepAngle = Mathf.Wrap(endAngle - startAngle, -Mathf.Pi, Mathf.Pi);

        return new CircularArc2D(start, end, center, radius, startAngle, sweepAngle, true);
    }

    /// <summary>
    /// 按参数 t 采样点，t ∈ [0, 1]。
    /// </summary>
    public Vector2 Evaluate(float t)
    {
        if (!IsValid) return Start;

        t = Mathf.Clamp(t, 0f, 1f);
        float angle = StartAngle + SweepAngle * t;
        // 标准极坐标旋转点：Center + [cos(A), sin(A)] * R
        return Center + Vector2.Right.Rotated(angle) * Radius;
    }

    /// <summary>
    /// 按参数 t 采样切线方向。
    /// </summary>
    public Vector2 EvaluateTangent(float t)
    {
        if (!IsValid) return Vector2.Right;

        t = Mathf.Clamp(t, 0f, 1f);
        float angle = StartAngle + SweepAngle * t;
        // 切线方向垂直于矢径向量 [-sin(A), cos(A)]
        Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * Mathf.Sign(SweepAngle);
        return tangent.LengthSquared() > 0.001f ? tangent.Normalized() : Vector2.Right;
    }

    /// <summary>
    /// 计算圆弧总弧长。
    /// <para>公式：L = |SweepAngle| * Radius</para>
    /// </summary>
    public float ApproximateLength()
    {
        if (!IsValid) return 0f;
        return Mathf.Abs(SweepAngle) * Radius;
    }
}
