using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 敌人生成位置的策略枚举
/// </summary>
public enum SpawnPositionStrategy
{
    /// <summary> 矩形区域内随机 </summary>
    Rectangle,
    /// <summary> 圆形区域内随机 </summary>
    Circle,
    /// <summary> 圆周上随机 (边缘) </summary>
    Perimeter,
    /// <summary> 屏幕视野外（在指定矩形范围内挖去视野区域） </summary>
    Offscreen,
    /// <summary> 规律网格阵型 </summary>
    Grid,
    /// <summary> 螺旋线排列 </summary>
    Spiral,
    /// <summary> 扎堆生成（基于 BaseStrategy 确定中心点，再在周围散布） </summary>
    Cluster
}

/// <summary>
/// 用于传递给生成计算器的参数包。
/// 使用此对象避免方法签名中出现过多的可选参数。
/// </summary>
public struct SpawnPositionParams
{
    /// <summary>
    /// 显式声明无参构造函数以支持字段初始值设定项 (C# 10+ 规范)
    /// </summary>
    public SpawnPositionParams() { }

    // --- Rectangle / Offscreen 策略参数 ---
    public float MinX { get; set; } = -1500f;
    public float MaxX { get; set; } = 1500f;
    public float MinY { get; set; } = -1000f;
    public float MaxY { get; set; } = 1000f;

    // --- Circle / Perimeter 策略参数 ---
    /// <summary> 圆心位置 </summary>
    public Vector2 Center { get; set; } = Vector2.Zero;
    /// <summary> 圆形半径 </summary>
    public float Radius { get; set; } = 700f;

    // --- Offscreen 策略参数 ---
    /// <summary> 视野区域的额外缓冲距离（防止生成在刚好可见的边缘） </summary>
    public float ViewportPadding { get; set; } = 50f;

    // --- Grid 策略参数 ---
    /// <summary> 网格起始原点（如果不设置，默认会基于 Viewport 中心或 (0,0)） </summary>
    public Vector2? GridOrigin { get; set; } = null;
    /// <summary> 每行显示的列数 </summary>
    public int GridColumns { get; set; } = 5;
    /// <summary> 每个网格单元的间距 </summary>
    public float GridSpacing { get; set; } = 100f;
    /// <summary> 当前生成的索引位置（用于递增） </summary>
    public int GridIndex { get; set; } = 0;

    // --- Cluster 策略参数 ---
    /// <summary> 扎堆生成的基准策略（决定中心点在哪里） </summary>
    public SpawnPositionStrategy ClusterBaseStrategy { get; set; } = SpawnPositionStrategy.Rectangle;
    /// <summary> 扎堆散布的半径范围 </summary>
    public float ClusterRadius { get; set; } = 150f;

    // --- Spiral 策略参数 ---
    /// <summary> 螺旋线的角度步进（弧度） </summary>
    public float SpiralAngleStep { get; set; } = 0.5f;
    /// <summary> 螺旋线的距离增长步进 </summary>
    public float SpiralDistStep { get; set; } = 5f;
}

/// <summary>
/// 敌人生成位置计算工具 - 负责根据不同的生成策略计算具体的 2D 坐标。
/// 该类为静态工具类，不持有状态，适用于 ECS 系统或生成管理器调用。
/// </summary>
public static class SpawnPositionCalculator
{
    // 使用项目标准的日志系统，方便在编辑器和调试器中追踪生成逻辑
    private static readonly Log _log = new Log("SpawnPositionCalculator");

    /// <summary>
    /// 根据指定的策略计算一个合法的生成位置。
    /// </summary>
    /// <param name="strategy">生成策略枚举（随机、圆形、屏幕外、网格等）</param>
    /// <param name="parameters">包含计算所需参数的对象（如半径、边界、间距等）</param>
    /// <param name="viewport">视口引用。在使用 Offscreen（屏幕外）策略时必须提供，用于获取相机位置和屏幕尺寸</param>
    /// <returns>计算出的全局 Vector2 坐标。如果策略未知或缺少必要引用，通常返回 Vector2.Zero</returns>
    public static Vector2 GetSpawnPosition(SpawnPositionStrategy strategy, SpawnPositionParams parameters, Viewport? viewport = null)
    {
        return strategy switch
        {
            SpawnPositionStrategy.Rectangle => GetRandomRectanglePosition(parameters),
            SpawnPositionStrategy.Circle => GetRandomCirclePosition(parameters),
            SpawnPositionStrategy.Perimeter => GetPerimeterPosition(parameters),
            SpawnPositionStrategy.Offscreen => GetOffscreenHollowPosition(parameters, viewport),
            SpawnPositionStrategy.Grid => GetGridPosition(parameters, viewport),
            SpawnPositionStrategy.Spiral => GetSpiralPosition(parameters, viewport),
            SpawnPositionStrategy.Cluster => GetClusterPosition(parameters, viewport), // 单次调用 Cluster 会退化为随机散布
            _ => Vector2.Zero
        };
    }

    /// <summary>
    /// 批量计算生成位置。支持特殊逻辑处理，如“扎堆生成”。
    /// </summary>
    /// <param name="strategy">生成策略</param>
    /// <param name="count">需要生成的数量</param>
    /// <param name="parameters">生成参数</param>
    /// <param name="viewport">视口引用</param>
    /// <returns>包含 count 个坐标点的列表</returns>
    public static List<Vector2> GetSpawnPositions(SpawnPositionStrategy strategy, int count, SpawnPositionParams parameters, Viewport? viewport = null)
    {
        var results = new List<Vector2>(count);

        // 特殊策略处理：Cluster (扎堆生成)
        // 逻辑：使用 BaseStrategy 确定一个“母点”，然后在其周围随机散布子点
        if (strategy == SpawnPositionStrategy.Cluster)
        {
            // 1. 确定中心点：根据 ClusterBaseStrategy 计算
            // 注意：这里递归调用 GetSpawnPosition 获取中心点
            var center = GetSpawnPosition(parameters.ClusterBaseStrategy, parameters, viewport);

            // 2. 在中心点周围随机散布
            for (int i = 0; i < count; i++)
            {
                // 使用极坐标转笛卡尔坐标实现圆内随机散布
                float angle = GD.Randf() * Mathf.Tau; // Tau = 2 * PI
                // 使用 sqrt 保证在圆内均匀分布，否则会聚集在圆心
                float dist = Mathf.Sqrt(GD.Randf()) * parameters.ClusterRadius;
                results.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist);
            }
        }
        else
        {
            // 其他策略直接循环生成
            for (int i = 0; i < count; i++)
            {
                // Grid/Spiral 等策略可能需要递增索引
                if (strategy == SpawnPositionStrategy.Grid || strategy == SpawnPositionStrategy.Spiral)
                {
                    parameters.GridIndex++; // ⚠️ 修改传入的 struct 副本 (但这是按值传递的参数，不会影响外部，除非用 ref)
                    // 修正：由于 parameters 是 struct 且按值传递，这里的修改只在循环内有效？
                    // 不，List<Vector2> 是一次性返回，这里的 parameters 是循环外的局部变量吗？
                    // GetSpawnPositions 的 parameters 参数是局部变量副本。
                    // 但我们在循环内调用 GetSpawnPosition(..., parameters, ...)，需要把修改后的 parameters 传进去。
                    // 所以这里的 parameters.GridIndex++ 修改的是当前方法作用域内的副本，是有效的。
                }

                results.Add(GetSpawnPosition(strategy, parameters, viewport));
            }
        }

        return results;
    }

    /// <summary>
    /// 矩形区域内随机生成。
    /// </summary>
    private static Vector2 GetRandomRectanglePosition(SpawnPositionParams p)
    {
        var rect = new Rect2(p.MinX, p.MinY, p.MaxX - p.MinX, p.MaxY - p.MinY);
        return Geometry2D.GetRandomPointInAABB(rect);
    }

    /// <summary>
    /// 圆形区域内随机生成 (实心圆)。
    /// </summary>
    private static Vector2 GetRandomCirclePosition(SpawnPositionParams p)
    {
        return Geometry2D.GetRandomPointInCircle(p.Center, p.Radius);
    }

    /// <summary>
    /// 圆周上随机生成 (空心圆环边缘)。
    /// </summary>
    private static Vector2 GetPerimeterPosition(SpawnPositionParams p)
    {
        return Geometry2D.GetRandomPointOnPerimeter(p.Center, p.Radius);
    }

    /// <summary>
    /// 屏幕外生成逻辑 (Hollow Rectangle)。
    /// 在 [Min, Max] 定义的大矩形内，挖去 Viewport 定义的小矩形。
    /// </summary>
    private static Vector2 GetOffscreenHollowPosition(SpawnPositionParams p, Viewport? viewport)
    {
        if (viewport == null) return Vector2.Zero;

        // 1. 获取视口区域 (World Space)
        var visibleRect = viewport.GetVisibleRect();
        var camera = viewport.GetCamera2D();
        Vector2 viewCenter = camera != null ? camera.GlobalPosition : (visibleRect.Position + visibleRect.Size / 2);
        Vector2 viewSize = visibleRect.Size;

        // 视口边界矩形 (含 Padding)
        var innerRect = new Rect2(
            viewCenter.X - viewSize.X / 2 - p.ViewportPadding,
            viewCenter.Y - viewSize.Y / 2 - p.ViewportPadding,
            viewSize.X + p.ViewportPadding * 2,
            viewSize.Y + p.ViewportPadding * 2
        );

        // 外部大矩形
        var outerRect = new Rect2(p.MinX, p.MinY, p.MaxX - p.MinX, p.MaxY - p.MinY);

        return Geometry2D.GetRandomPointInHollowBox(outerRect, innerRect);
    }

    /// <summary>
    /// 网格生成位置。
    /// </summary>
    private static Vector2 GetGridPosition(SpawnPositionParams p, Viewport? viewport)
    {
        // 确定原点：如果有指定则用指定，否则用视口中心，再否则用 (0,0)
        Vector2 origin = p.GridOrigin ?? (viewport?.GetCamera2D()?.GlobalPosition ?? Vector2.Zero);

        // 为了让网格居中，可以做个偏移
        // 这里简化实现：直接从 origin 开始向右下排布
        int col = p.GridIndex % p.GridColumns;
        int row = p.GridIndex / p.GridColumns;

        // 可以做个中心化偏移，让 origin 是网格的中心
        float offsetX = (p.GridColumns - 1) * p.GridSpacing / 2f;
        // float offsetY... 暂不处理行数未知的情况

        return origin + new Vector2(col * p.GridSpacing - offsetX, row * p.GridSpacing);
    }

    /// <summary>
    /// 螺旋线生成位置。
    /// </summary>
    private static Vector2 GetSpiralPosition(SpawnPositionParams p, Viewport? viewport)
    {
        Vector2 center = p.Center;
        if (viewport != null)
        {
            center = viewport.GetCamera2D()?.GlobalPosition ?? p.Center;
        }

        float angle = p.GridIndex * p.SpiralAngleStep;
        float dist = p.GridIndex * p.SpiralDistStep; // 距离随索引增加

        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    /// <summary>
    /// 单个 Cluster 模式位置 (退化为基于 BaseStrategy 的单点偏移)。
    /// </summary>
    private static Vector2 GetClusterPosition(SpawnPositionParams p, Viewport? viewport)
    {
        // 如果直接调用 GetSpawnPosition(Cluster)，我们只能随机选一个中心点，然后偏离一点
        var center = GetSpawnPosition(p.ClusterBaseStrategy, p, viewport);
        float angle = GD.Randf() * Mathf.Tau;
        float dist = Mathf.Sqrt(GD.Randf()) * p.ClusterRadius;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }
}

