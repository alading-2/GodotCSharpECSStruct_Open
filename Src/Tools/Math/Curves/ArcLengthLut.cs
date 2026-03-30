using Godot;
using System;

/// <summary>
/// 曲线弧长查找表工具。
/// <para>
/// 负责两件事：
/// 1. 将采样得到的累计弧长表归一化到 [0,1]
/// 2. 将“按路程推进的 progress”映射回曲线公式参数 t
/// </para>
/// <para>
/// 采样过程由各曲线类型自行完成，避免在高频更新里引入委托分配。
/// </para>
/// </summary>
public static class ArcLengthLut
{
    /// <summary>默认 LUT 采样段数。</summary>
    public const int DefaultSegments = 32;

    /// <summary>
    /// 将累计弧长表归一化为 [0,1]，并返回总弧长。
    /// </summary>
    /// <param name="table">采样得到的累计距离数组。执行后，table[i] 表示第 i 个采样点占总长度的比例。</param>
    /// <returns>计算出的总弧长（table[^1] 的原始值）。</returns>
    public static float NormalizeTable(Span<float> table)
    {
        if (table.Length == 0) return 0f;

        float totalLength = table[^1];
        // 如果长度几乎为 0，清空表并返回
        if (totalLength <= 0.000001f)
        {
            table.Clear();
            return 0f;
        }

        // 使用乘法代替除法进行归一化
        float inverseLength = 1f / totalLength;
        for (int i = 1; i < table.Length; i++)
        {
            table[i] *= inverseLength;
        }

        // 强制最后一个值为 1，避免浮点误差
        table[^1] = 1f;
        return totalLength;
    }

    /// <summary>
    /// 使用二分查找将按弧长推进的 progress [0, 1] 映射回曲线的原始参数 t [0, 1]。
    /// </summary>
    /// <param name="progress">弧长百分比。</param>
    /// <param name="normalizedTable">归一化后的弧长查找表。</param>
    /// <returns>映射后的曲线参数 t。</returns>
    public static float MapProgressToParameter(float progress, ReadOnlySpan<float> normalizedTable)
    {
        // 样本太少无法映射
        if (normalizedTable.Length < 2) return Mathf.Clamp(progress, 0f, 1f);

        progress = Mathf.Clamp(progress, 0f, 1f);
        int segmentCount = normalizedTable.Length - 1;

        // 二分查找目标 progress 所在的区间 [low, high]
        int low = 0;
        int high = segmentCount;
        while (low < high - 1)
        {
            int mid = (low + high) >> 1;
            if (normalizedTable[mid] < progress)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        // 在区间内进行线性插值
        float segmentSpan = normalizedTable[high] - normalizedTable[low];
        float segmentFraction = segmentSpan > 0.000001f
            ? (progress - normalizedTable[low]) / segmentSpan
            : 0f;

        // 最后将索引转回 [0, 1] 的参数 t
        return ((float)low + segmentFraction) / segmentCount;
    }
}
