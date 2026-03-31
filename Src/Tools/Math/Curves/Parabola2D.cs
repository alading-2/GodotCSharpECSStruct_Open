using Godot;
using System;

/// <summary>
/// 由起点、终点和顶高定义的二维抛物线。
/// <para>
/// 使用沿弦线线性推进 + 横向抬高的形式，局部方程为：
/// <c>y = 4h * t * (1 - t)</c>
/// </para>
/// </summary>
public readonly struct Parabola2D
{
    /// <summary>起点。</summary>
    public Vector2 Start { get; }
    /// <summary>终点。</summary>
    public Vector2 End { get; }
    /// <summary>弦线中心点。</summary>
    public Vector2 Center { get; }
    /// <summary>沿弦线的单位方向向量。</summary>
    public Vector2 Forward { get; }
    /// <summary>垂直于弦线的单位方向向量（指向顶点方向）。</summary>
    public Vector2 Side { get; }
    /// <summary>半弦长。</summary>
    public float HalfChord { get; }
    /// <summary>顶点相对于弦线的高度。</summary>
    public float ApexHeight { get; }
    /// <summary>抛物线配置是否有效。</summary>
    public bool IsValid { get; }

    private Parabola2D(
        Vector2 start,
        Vector2 end,
        Vector2 center,
        Vector2 forward,
        Vector2 side,
        float halfChord,
        float apexHeight,
        bool isValid)
    {
        Start = start;
        End = end;
        Center = center;
        Forward = forward;
        Side = side;
        HalfChord = halfChord;
        ApexHeight = apexHeight;
        IsValid = isValid;
    }

    /// <summary>
    /// 创建一条抛物线。
    /// </summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <param name="apexHeight">顶点相对于弦线的高度（可正可负，决定弯曲方向）。</param>
    public static Parabola2D Create(Vector2 start, Vector2 end, float apexHeight)
    {
        Vector2 chord = end - start;
        float chordLength = chord.Length();
        if (chordLength <= 0.001f)
        {
            return default;
        }

        Vector2 forward = chord / chordLength;
        // 默认向上（左手法则）
        Vector2 side = new Vector2(-forward.Y, forward.X);
        if (apexHeight < 0f)
        {
            side = -side;
        }

        return new Parabola2D(
            start,
            end,
            (start + end) * 0.5f,
            forward,
            side,
            chordLength * 0.5f,
            Mathf.Abs(apexHeight),
            true);
    }

    /// <summary>
    /// 按参数 t 采样点，t ∈ [0, 1]。
    /// <para>公式：P(t) = Center + Forward * lerp(-HalfChord, HalfChord, t) + Side * (4 * h * t * (1-t))</para>
    /// </summary>
    public Vector2 Evaluate(float t)
    {
        if (!IsValid) return Start;

        t = Mathf.Clamp(t, 0f, 1f);
        // localX: 沿弦线从 -HalfChord 到 HalfChord
        float localX = Mathf.Lerp(-HalfChord, HalfChord, t);
        // localY: 满足抛物线方程，t=0.5 时达到 ApexHeight
        float localY = 4f * ApexHeight * t * (1f - t);
        return Center + Forward * localX + Side * localY;
    }

    /// <summary>
    /// 按参数 t 采样切线方向。
    /// </summary>
    public Vector2 EvaluateTangent(float t)
    {
        if (!IsValid) return Vector2.Right;

        t = Mathf.Clamp(t, 0f, 1f);
        // 对 Evaluate 公式求导：
        // dx/dt = 2 * HalfChord (即弦长)
        // dy/dt = 4 * ApexHeight * (1 - 2t)
        float dx = HalfChord * 2f;
        float dy = 4f * ApexHeight * (1f - 2f * t);
        Vector2 tangent = Forward * dx + Side * dy;
        return tangent.LengthSquared() > 0.001f ? tangent.Normalized() : Forward;
    }

    /// <summary>
    /// 近似计算曲线总弧长。
    /// </summary>
    public float ApproximateLength()
    {
        if (!IsValid) return 0f;

        float chordLength = HalfChord * 2f;
        if (ApexHeight <= 0.001f) return chordLength;

        float dx = chordLength;
        float dy = ApexHeight * 4f;
        float sqrt = Mathf.Sqrt(dx * dx + dy * dy);
        float ratio = dy / dx;
        return 0.5f * sqrt + (dx * dx / (2f * dy)) * MathF.Asinh(ratio);
    }
}
