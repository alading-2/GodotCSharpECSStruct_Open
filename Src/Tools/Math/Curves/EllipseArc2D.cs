using Godot;
using System;

/// <summary>
/// 由起点、终点、弧高和侧偏方向定义的二维弧线。
/// 用途：专门为回旋镖技能设计，轨迹更自然
/// <para>
/// 该实现保持当前回旋镖轨迹语义：
/// - X 轴沿弦线线性推进
/// - Y 轴按半周期正弦鼓起
/// </para>
/// <para>
/// 虽然命名为 EllipseArc2D，但这里更强调“椭圆感弧线”的稳定游戏语义，
/// 而不是严格的解析几何椭圆参数方程。
/// </para>
/// </summary>
public readonly struct EllipseArc2D
{
    /// <summary>起点。</summary>
    public Vector2 Start { get; }
    /// <summary>终点。</summary>
    public Vector2 End { get; }
    /// <summary>弦线中心点。</summary>
    public Vector2 Center { get; }
    /// <summary>沿弦线的单位方向向量。</summary>
    public Vector2 Forward { get; }
    /// <summary>垂直于弦线的单位方向向量（弧线鼓起方向）。</summary>
    public Vector2 Side { get; }
    /// <summary>半弦长。</summary>
    public float HalfChord { get; }
    /// <summary>弧线最高点相对于弦线的高度。</summary>
    public float ArcHeight { get; }
    /// <summary>配置是否有效。</summary>
    public bool IsValid { get; }

    /// <summary>
    /// 初始化椭圆感弧线。
    /// </summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <param name="center">弦线中心点。</param>
    /// <param name="forward">沿弦线的单位方向向量。</param>
    /// <param name="side">垂直于弦线的单位方向向量（弧线鼓起方向）。</param>
    /// <param name="halfChord">半弦长。</param>
    /// <param name="arcHeight">弧线最高点相对于弦线的高度。</param>
    /// <param name="isValid">配置是否有效。</param>
    private EllipseArc2D(
        Vector2 start,
        Vector2 end,
        Vector2 center,
        Vector2 forward,
        Vector2 side,
        float halfChord,
        float arcHeight,
        bool isValid)
    {
        Start = start;
        End = end;
        Center = center;
        Forward = forward;
        Side = side;
        HalfChord = halfChord;
        ArcHeight = arcHeight;
        IsValid = isValid;
    }

    /// <summary>
    /// 创建一条椭圆感弧线。
    /// </summary>
    /// <param name="start">起点。</param>
    /// <param name="end">终点。</param>
    /// <param name="height">垂直高度。</param>
    /// <param name="clockwise">相对于弦线的前进方向，是否顺时针弯曲。</param>
    public static EllipseArc2D Create(Vector2 start, Vector2 end, float height, bool clockwise)
    {
        Vector2 chord = end - start;    // 弦向量
        float chordLength = chord.Length();
        if (chordLength <= 0.001f)
        {
            return default;
        }

        Vector2 forward = chord / chordLength;
        // clockwise 决定 side 向量相对于 forward 是向左还是向右
        float sideSign = clockwise ? 1f : -1f;
        Vector2 side = new Vector2(-forward.Y, forward.X) * sideSign;

        return new EllipseArc2D(
            start,                  // 起点
            end,                    // 终点
            (start + end) * 0.5f,   // 弦线中心点
            forward,                // 沿弦线的单位方向向量
            side,                   // 垂直于弦线的单位方向向量（弧线鼓起方向）
            chordLength * 0.5f,     // 半弦长
            Mathf.Abs(height),      // 弧线最高点相对于弦线的高度
            true);                  // 配置是否有效
    }

    /// <summary>
    /// 按参数 t 采样点，t ∈ [0,1]。
    /// <para>公式：弦向线性插值，侧向 Sin(t * Pi) 采样。</para>
    /// </summary>
    public Vector2 Evaluate(float t)
    {
        if (!IsValid) return Start;

        t = Mathf.Clamp(t, 0f, 1f);
        // 沿弦线线性推进
        float localX = Mathf.Lerp(-HalfChord, HalfChord, t);
        // 侧向量高度按正弦波鼓起 (t=0.5 时 Sin(0.5Pi)=1 为顶点)
        float localY = Mathf.Sin(t * Mathf.Pi) * ArcHeight;
        return Center + Forward * localX + Side * localY;
    }

    /// <summary>
    /// 按参数 t 采样切线方向。
    /// </summary>
    public Vector2 EvaluateTangent(float t)
    {
        if (!IsValid) return Vector2.Right;

        t = Mathf.Clamp(t, 0f, 1f);
        // 求导：
        // dx/dt = 弦长
        // dy/dt = Height * Pi * Cos(t * Pi)
        float dx = HalfChord * 2f;
        float dy = Mathf.Pi * ArcHeight * Mathf.Cos(t * Mathf.Pi);
        Vector2 tangent = Forward * dx + Side * dy;
        return tangent.LengthSquared() > 0.001f ? tangent.Normalized() : Forward;
    }

    /// <summary>
    /// 近似计算椭圆弧总弧长。
    /// <para>内部使用椭圆周长的 Ramanujan 级数近似算法。</para>
    /// </summary>
    public float ApproximateLength()
    {
        if (!IsValid) return 0f;
        if (ArcHeight <= 0.001f) return HalfChord * 2f;

        // 计算一个完整椭圆的周长 (Ramanujan 近似公式 1)
        // a = HalfChord, b = ArcHeight
        float fullPerimeter = Mathf.Pi *
            (3f * (HalfChord + ArcHeight) - Mathf.Sqrt((3f * HalfChord + ArcHeight) * (HalfChord + 3f * ArcHeight)));
        // 由于我们的 y 轴只用了 Sin 半周，近似弧长为半周长
        return fullPerimeter * 0.5f;
    }
}
