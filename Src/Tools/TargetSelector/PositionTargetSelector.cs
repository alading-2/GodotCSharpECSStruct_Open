using Godot;
using System.Collections.Generic;

/// <summary>
/// 空间位置生成工具
/// 利用 GeometryCalculator 进行形状检测，生成指定数量的有效随机坐标点
/// </summary>
public static class PositionTargetSelector
{
    private static readonly Log _log = new(nameof(PositionTargetSelector));

    /// <summary>
    /// 根据查询配置在指定的几何形状内生成随机目标位置点
    /// </summary>
    /// <param name="query">查询参数（几何类型、原点、范围、数量等）。</param>
    /// <returns>随机生成的位置列表，长度至少为 1。</returns>
    public static List<Vector2> Query(TargetSelectorQuery query)
    {
        var results = new List<Vector2>();
        int count = query.MaxTargets > 0 ? query.MaxTargets : 1;

        // 使用当前毫秒时间作为种子，确保同一帧外的调用具备足够随机性。
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Time.GetTicksMsec();

        for (int i = 0; i < count; i++)
        {
            Vector2 point = GeometryCalculator.GetRandomPointInGeometry(query, rng);
            results.Add(point);
        }

        return results;
    }
}
