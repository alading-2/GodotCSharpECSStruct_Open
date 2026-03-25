using Godot;
using System;

/// <summary>
/// 通用 N 阶贝塞尔曲线工具（纯静态，无 GC，适合 _Process 高频调用）
/// <para>
/// 核心算法：De Casteljau 递归线性插值
/// - 数值稳定：不需要计算 C(n,k)·t^k·(1-t)^(n-k)，避免高阶溢出
/// - 天然支持曲线分割（Split）
/// - 任意阶：控制点数量 = 阶数 + 1（2点=线性，3点=二次，4点=三阶…）
/// </para>
/// <para>
/// 弧长参数化（Arc-Length Parameterization）：
/// 贝塞尔曲线的 t 参数与实际弧长不成正比，导致匀速运动时速度不均匀。
/// 通过 BuildLengthTable + EvaluateUniform 可实现等速运动。
/// </para>
/// </summary>
public static class BezierCurve
{
    // ======================== 核心求值 ========================

    /// <summary>
    /// De Casteljau 求值：计算任意阶贝塞尔曲线在 t 处的点
    /// <para>时间复杂度 O(n²)，n = 控制点数量 - 1（阶数）</para>
    /// </summary>
    /// <param name="points">控制点数组（至少 2 个点）</param>
    /// <param name="t">参数 [0, 1]</param>
    /// <returns>曲线上的点</returns>
    public static Vector2 Evaluate(ReadOnlySpan<Vector2> points, float t)
    {
        int n = points.Length;
        if (n == 0) return Vector2.Zero;
        if (n == 1) return points[0];

        // 特化：二次（3点）和三阶（4点）走手写公式，避免 stackalloc 开销
        if (n == 3) return EvaluateQuadratic(points[0], points[1], points[2], t);
        if (n == 4) return EvaluateCubic(points[0], points[1], points[2], points[3], t);

        // 通用 De Casteljau
        Span<Vector2> buf = n <= 16 ? stackalloc Vector2[n] : new Vector2[n];
        points.CopyTo(buf);

        for (int level = n - 1; level > 0; level--)
        {
            for (int i = 0; i < level; i++)
            {
                buf[i] = buf[i].Lerp(buf[i + 1], t);
            }
        }
        return buf[0];
    }

    /// <summary>
    /// 二次贝塞尔（3 个控制点）特化求值
    /// B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
    /// </summary>
    public static Vector2 EvaluateQuadratic(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// 三阶贝塞尔（4 个控制点）特化求值
    /// B(t) = (1-t)³·P0 + 3(1-t)²t·P1 + 3(1-t)t²·P2 + t³·P3
    /// </summary>
    public static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return u * uu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + t * tt * p3;
    }

    // ======================== 导数 / 切线 ========================

    /// <summary>
    /// 计算贝塞尔曲线在 t 处的一阶导数（切线向量，未归一化）
    /// <para>
    /// 原理：n 阶贝塞尔的导数 = n 倍的 (n-1) 阶贝塞尔，控制点为相邻差分向量
    /// B'(t) = n · Σ C(n-1,i) · (1-t)^(n-1-i) · t^i · (P_{i+1} - P_i)
    /// </para>
    /// </summary>
    /// <param name="points">控制点数组（至少 2 个点）</param>
    /// <param name="t">参数 [0, 1]</param>
    /// <returns>切线向量（长度反映曲线在该点的参数速度）</returns>
    public static Vector2 EvaluateDerivative(ReadOnlySpan<Vector2> points, float t)
    {
        int n = points.Length;
        if (n < 2) return Vector2.Zero;

        // 构建差分控制点：dP_i = P_{i+1} - P_i
        int degree = n - 1;
        Span<Vector2> diffs = degree <= 16 ? stackalloc Vector2[degree] : new Vector2[degree];
        for (int i = 0; i < degree; i++)
        {
            diffs[i] = points[i + 1] - points[i];
        }

        // 导数 = degree * Evaluate(差分控制点, t)
        return degree * Evaluate(diffs, t);
    }

    /// <summary>
    /// 计算贝塞尔曲线在 t 处的归一化切线方向
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="t">参数 [0, 1]</param>
    /// <returns>单位切线向量；若导数为零向量则返回 Vector2.Right</returns>
    public static Vector2 EvaluateTangent(ReadOnlySpan<Vector2> points, float t)
    {
        Vector2 d = EvaluateDerivative(points, t);
        float len = d.Length();
        return len > 1e-6f ? d / len : Vector2.Right;
    }

    /// <summary>
    /// 计算贝塞尔曲线在 t 处的二阶导数（加速度 / 曲率方向）
    /// <para>B''(t) = n(n-1) · Σ 的二次差分控制点的 (n-2) 阶贝塞尔</para>
    /// </summary>
    public static Vector2 EvaluateSecondDerivative(ReadOnlySpan<Vector2> points, float t)
    {
        int n = points.Length;
        if (n < 3) return Vector2.Zero;

        int degree = n - 1;

        // 一阶差分
        Span<Vector2> d1 = degree <= 16 ? stackalloc Vector2[degree] : new Vector2[degree];
        for (int i = 0; i < degree; i++)
            d1[i] = points[i + 1] - points[i];

        // 二阶差分
        int d2Count = degree - 1;
        Span<Vector2> d2 = d2Count <= 16 ? stackalloc Vector2[d2Count] : new Vector2[d2Count];
        for (int i = 0; i < d2Count; i++)
            d2[i] = d1[i + 1] - d1[i];

        return degree * (degree - 1) * Evaluate(d2, t);
    }

    /// <summary>
    /// 计算贝塞尔曲线在 t 处的曲率（有符号，正 = 左转，负 = 右转）
    /// <para>κ = (x'·y'' - y'·x'') / |v|³</para>
    /// </summary>
    public static float EvaluateCurvature(ReadOnlySpan<Vector2> points, float t)
    {
        Vector2 d1 = EvaluateDerivative(points, t);
        Vector2 d2 = EvaluateSecondDerivative(points, t);
        float cross = d1.X * d2.Y - d1.Y * d2.X;
        float speed = d1.Length();
        if (speed < 1e-6f) return 0f;
        return cross / (speed * speed * speed);
    }

    // ======================== 曲线分割 ========================

    /// <summary>
    /// 在参数 t 处将贝塞尔曲线分割为两条子曲线（De Casteljau 的天然副产品）
    /// <para>
    /// 左半段控制点 = De Casteljau 三角形的左边缘
    /// 右半段控制点 = De Casteljau 三角形的右边缘（逆序）
    /// </para>
    /// </summary>
    /// <param name="points">原始控制点</param>
    /// <param name="t">分割参数</param>
    /// <param name="left">输出：左半段控制点（长度同 points）</param>
    /// <param name="right">输出：右半段控制点（长度同 points）</param>
    public static void Split(ReadOnlySpan<Vector2> points, float t, Span<Vector2> left, Span<Vector2> right)
    {
        int n = points.Length;
        if (n == 0) return;

        // 工作缓冲区
        Span<Vector2> buf = n <= 16 ? stackalloc Vector2[n] : new Vector2[n];
        points.CopyTo(buf);

        // 左边缘第一个点 = P0，右边缘最后一个点 = Pn
        left[0] = buf[0];
        right[n - 1] = buf[n - 1];

        for (int level = n - 1; level > 0; level--)
        {
            for (int i = 0; i < level; i++)
            {
                buf[i] = buf[i].Lerp(buf[i + 1], t);
            }
            // 每一层的第一个值 → 左半段
            left[n - 1 - level + 1] = buf[0];
            // 每一层的最后一个值 → 右半段（逆序填充）
            right[level - 1] = buf[level - 1];
        }
    }

    // ======================== 弧长相关 ========================

    /// <summary>
    /// 用分段线性近似估算曲线总弧长
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="segments">分段数（越大越精确，默认 64）</param>
    /// <returns>近似弧长（像素）</returns>
    public static float ApproximateLength(ReadOnlySpan<Vector2> points, int segments = 64)
    {
        if (points.Length < 2) return 0f;

        float totalLength = 0f;
        Vector2 prev = Evaluate(points, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector2 curr = Evaluate(points, t);
            totalLength += prev.DistanceTo(curr);
            prev = curr;
        }
        return totalLength;
    }

    /// <summary>
    /// 构建弧长参数化查找表（Arc-Length LUT）
    /// <para>
    /// 返回 float[] 长度为 segments+1，lut[i] = 从 t=0 到 t=i/segments 的累积弧长占比 [0,1]
    /// 用于 EvaluateUniform() 将均匀的 u∈[0,1] 映射到非均匀的 t∈[0,1]
    /// </para>
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="segments">采样段数（默认 64，精度与性能平衡）</param>
    /// <returns>弧长比例查找表</returns>
    public static float[] BuildLengthTable(ReadOnlySpan<Vector2> points, int segments = 64)
    {
        float[] lut = new float[segments + 1];
        if (points.Length < 2)
        {
            // 退化：全部填 0
            return lut;
        }

        lut[0] = 0f;
        Vector2 prev = Evaluate(points, 0f);
        float totalLength = 0f;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector2 curr = Evaluate(points, t);
            totalLength += prev.DistanceTo(curr);
            lut[i] = totalLength;
            prev = curr;
        }

        // 归一化为 [0, 1]
        if (totalLength > 1e-6f)
        {
            float inv = 1f / totalLength;
            for (int i = 1; i <= segments; i++)
            {
                lut[i] *= inv;
            }
        }
        lut[segments] = 1f; // 确保末尾精确为 1

        return lut;
    }

    /// <summary>
    /// 弧长参数化求值：将均匀参数 u∈[0,1] 映射为曲线上等距点
    /// <para>
    /// 解决贝塞尔曲线 t 参数速度不均匀的问题。
    /// 先用 BuildLengthTable 预计算 LUT，再每帧调用本方法。
    /// </para>
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="u">均匀参数 [0, 1]（与弧长成正比）</param>
    /// <param name="lut">由 BuildLengthTable 生成的查找表</param>
    /// <returns>曲线上的点（等速分布）</returns>
    public static Vector2 EvaluateUniform(ReadOnlySpan<Vector2> points, float u, float[] lut)
    {
        float t = MapUniformToT(u, lut);
        return Evaluate(points, t);
    }

    /// <summary>
    /// 弧长参数化切线：与 EvaluateUniform 配套，获取等速运动时的切线方向
    /// </summary>
    public static Vector2 EvaluateUniformTangent(ReadOnlySpan<Vector2> points, float u, float[] lut)
    {
        float t = MapUniformToT(u, lut);
        return EvaluateTangent(points, t);
    }

    /// <summary>
    /// 将均匀参数 u 映射到贝塞尔参数 t（通过 LUT 二分查找 + 线性插值）
    /// </summary>
    /// <param name="u">均匀参数 [0, 1]</param>
    /// <param name="lut">弧长比例查找表</param>
    /// <returns>对应的贝塞尔参数 t</returns>
    public static float MapUniformToT(float u, float[] lut)
    {
        if (lut == null || lut.Length < 2) return u;

        u = Mathf.Clamp(u, 0f, 1f);
        int segments = lut.Length - 1;

        // 二分查找：找到 lut[lo] <= u < lut[lo+1]
        int lo = 0, hi = segments;
        while (lo < hi - 1)
        {
            int mid = (lo + hi) >> 1;
            if (lut[mid] < u) lo = mid;
            else hi = mid;
        }

        // 在 [lo, lo+1] 区间内线性插值
        float segLen = lut[hi] - lut[lo];
        float frac = segLen > 1e-8f ? (u - lut[lo]) / segLen : 0f;
        return ((float)lo + frac) / segments;
    }

    // ======================== 实用工具 ========================

    /// <summary>
    /// 计算曲线的近似包围盒
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="segments">采样段数</param>
    /// <returns>Rect2 包围盒</returns>
    public static Rect2 ApproximateBounds(ReadOnlySpan<Vector2> points, int segments = 32)
    {
        if (points.Length == 0) return new Rect2();

        Vector2 min = Evaluate(points, 0f);
        Vector2 max = min;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector2 p = Evaluate(points, t);
            min = new Vector2(Mathf.Min(min.X, p.X), Mathf.Min(min.Y, p.Y));
            max = new Vector2(Mathf.Max(max.X, p.X), Mathf.Max(max.Y, p.Y));
        }

        return new Rect2(min, max - min);
    }

    /// <summary>
    /// 在曲线上均匀采样多个点（基于弧长参数化）
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="sampleCount">采样点数量（至少 2）</param>
    /// <param name="lutSegments">LUT 精度</param>
    /// <returns>均匀分布的采样点数组</returns>
    public static Vector2[] SampleUniform(ReadOnlySpan<Vector2> points, int sampleCount, int lutSegments = 64)
    {
        sampleCount = Math.Max(2, sampleCount);
        var lut = BuildLengthTable(points, lutSegments);
        var samples = new Vector2[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float u = (float)i / (sampleCount - 1);
            samples[i] = EvaluateUniform(points, u, lut);
        }
        return samples;
    }

    /// <summary>
    /// 找到曲线上距目标点最近的参数 t（暴力采样 + 局部细化）
    /// </summary>
    /// <param name="points">控制点数组</param>
    /// <param name="target">目标点</param>
    /// <param name="coarseSamples">粗采样数（默认 32）</param>
    /// <param name="refinements">细化迭代次数（默认 4）</param>
    /// <returns>最近点对应的参数 t</returns>
    public static float FindClosestT(ReadOnlySpan<Vector2> points, Vector2 target, int coarseSamples = 32, int refinements = 4)
    {
        if (points.Length < 2) return 0f;

        // 粗采样
        float bestT = 0f;
        float bestDist = float.MaxValue;

        for (int i = 0; i <= coarseSamples; i++)
        {
            float t = (float)i / coarseSamples;
            float dist = Evaluate(points, t).DistanceSquaredTo(target);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestT = t;
            }
        }

        // 局部细化（逐步缩小搜索范围）
        float range = 1f / coarseSamples;
        for (int r = 0; r < refinements; r++)
        {
            float lo = Mathf.Max(0f, bestT - range);
            float hi = Mathf.Min(1f, bestT + range);
            int steps = 8;

            for (int i = 0; i <= steps; i++)
            {
                float t = lo + (hi - lo) * i / steps;
                float dist = Evaluate(points, t).DistanceSquaredTo(target);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestT = t;
                }
            }
            range /= steps;
        }

        return bestT;
    }

    /// <summary>
    /// 提升曲线阶数（Degree Elevation）：n 阶 → n+1 阶，形状不变
    /// <para>新控制点 Q_i = (i/(n+1))·P_{i-1} + (1 - i/(n+1))·P_i</para>
    /// </summary>
    /// <param name="points">原始控制点（n+1 个 → 阶数 n）</param>
    /// <returns>提升后的控制点（n+2 个 → 阶数 n+1）</returns>
    public static Vector2[] ElevateDegree(ReadOnlySpan<Vector2> points)
    {
        int n = points.Length; // n 个点 = (n-1) 阶
        if (n < 2) return points.ToArray();

        var elevated = new Vector2[n + 1];
        elevated[0] = points[0];
        elevated[n] = points[n - 1];

        for (int i = 1; i < n; i++)
        {
            float ratio = (float)i / n;
            elevated[i] = ratio * points[i - 1] + (1f - ratio) * points[i];
        }
        return elevated;
    }
}
