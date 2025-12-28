using Godot;
using System;
using System.Collections.Generic;

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
    public static Vector2 GetSpawnPosition(SpawnPositionStrategy strategy, SpawnPositionParams parameters, Viewport viewport = null)
    {
        return strategy switch
        {
            SpawnPositionStrategy.Random => GetRandomPosition(parameters),
            SpawnPositionStrategy.Circle => GetCirclePosition(parameters),
            SpawnPositionStrategy.Offscreen => GetOffscreenPosition(parameters, viewport),
            SpawnPositionStrategy.Grid => GetGridPosition(parameters),
            SpawnPositionStrategy.Cluster => GetClusterPosition(parameters, viewport),
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
    public static List<Vector2> GetSpawnPositions(SpawnPositionStrategy strategy, int count, SpawnPositionParams parameters, Viewport viewport = null)
    {
        var results = new List<Vector2>(count);

        // 特殊策略处理：Cluster (扎堆生成)
        // 逻辑：先在屏幕外确定一个“母点”，然后在其周围随机散布子点
        if (strategy == SpawnPositionStrategy.Cluster)
        {
            // 1. 确定中心点：通常是一个屏幕外的随机位置
            var center = GetOffscreenPosition(parameters, viewport);

            // 2. 在中心点周围随机散布
            for (int i = 0; i < count; i++)
            {
                // 使用极坐标转笛卡尔坐标实现圆内随机散布
                float angle = GD.Randf() * Mathf.Tau; // Tau = 2 * PI
                float dist = GD.Randf() * parameters.ClusterRadius;
                results.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist);
            }
        }
        else
        {
            // 其他策略（如 Random, Circle, Grid）直接循环生成
            for (int i = 0; i < count; i++)
            {
                // Grid (网格) 策略需要维护一个索引来递增坐标
                if (strategy == SpawnPositionStrategy.Grid)
                {
                    parameters.GridIndex++; // ⚠️ 注意：这会修改传入的引用对象，连续调用需注意重置
                }
                results.Add(GetSpawnPosition(strategy, parameters, viewport));
            }
        }

        return results;
    }

    /// <summary>
    /// 矩形区域内随机生成。
    /// </summary>
    private static Vector2 GetRandomPosition(SpawnPositionParams p)
    {
        return new Vector2(
            (float)GD.RandRange(p.MinX, p.MaxX),
            (float)GD.RandRange(p.MinY, p.MaxY)
        );
    }

    /// <summary>
    /// 圆形区域内随机生成。
    /// </summary>
    private static Vector2 GetCirclePosition(SpawnPositionParams p)
    {
        float angle = GD.Randf() * Mathf.Tau;
        float distance = GD.Randf() * p.Radius; // 均匀散布在圆盘内
        return p.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    /// <summary>
    /// 屏幕外边缘生成逻辑。常用于“割草”类游戏，确保敌人从屏幕视野外进入。
    /// </summary>
    private static Vector2 GetOffscreenPosition(SpawnPositionParams p, Viewport viewport)
    {
        if (viewport == null)
        {
            _log.Error("屏幕外生成策略需要提供有效的视口 (Viewport) 引用。");
            return Vector2.Zero;
        }

        // 获取当前视口的可见区域大小
        var viewportSize = viewport.GetVisibleRect().Size;
        var camera = viewport.GetCamera2D();

        Vector2 cameraPos = Vector2.Zero;
        if (camera != null)
        {
            // 如果存在活动相机，以相机位置作为屏幕中心
            cameraPos = camera.GlobalPosition;
        }
        else
        {
            // 如果没有相机，通过变换矩阵计算屏幕中心点
            // CanvasTransform.Origin 的取反通常对应了屏幕在世界空间中的平移
            cameraPos = viewport.GetCanvasTransform().Origin * -1 + viewportSize / 2;
        }

        var halfSize = viewportSize / 2;

        // 随机选择屏幕的四个方向之一进行生成 (0:上, 1:右, 2:下, 3:左)
        int side = (int)(GD.Randi() % 4);
        return side switch
        {
            // 上方：Y坐标固定在视口顶部向上偏移处，X坐标在视口宽度内随机
            0 => cameraPos + new Vector2((float)GD.RandRange(-halfSize.X, halfSize.X), -halfSize.Y - p.OffscreenDistance),
            // 右方：X坐标固定在视口右侧向右偏移处，Y坐标在视口高度内随机
            1 => cameraPos + new Vector2(halfSize.X + p.OffscreenDistance, (float)GD.RandRange(-halfSize.Y, halfSize.Y)),
            // 下方：Y坐标固定在视口底部向下偏移处，X坐标在视口宽度内随机
            2 => cameraPos + new Vector2((float)GD.RandRange(-halfSize.X, halfSize.X), halfSize.Y + p.OffscreenDistance),
            // 左方：X坐标固定在视口左侧向左偏移处，Y坐标在视口高度内随机
            _ => cameraPos + new Vector2(-halfSize.X - p.OffscreenDistance, (float)GD.RandRange(-halfSize.Y, halfSize.Y))
        };
    }

    /// <summary>
    /// 按照固定的网格规律生成。常用于测试或特定的阵型排列。
    /// </summary>
    private static Vector2 GetGridPosition(SpawnPositionParams p)
    {
        // 根据当前索引计算所在行列
        int col = p.GridIndex % p.GridColumns;
        int row = p.GridIndex / p.GridColumns;

        return p.GridOrigin + new Vector2(col * p.GridSpacing, row * p.GridSpacing);
    }

    /// <summary>
    /// 单个 Cluster 模式位置。当单独调用时，退化为屏幕外随机散布点。
    /// </summary>
    private static Vector2 GetClusterPosition(SpawnPositionParams p, Viewport viewport)
    {
        // 先找一个屏幕外基点
        var center = GetOffscreenPosition(p, viewport);
        // 在该基点周围随机偏移
        float angle = GD.Randf() * Mathf.Tau;
        float dist = GD.Randf() * p.ClusterRadius;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }
}

/// <summary>
/// 敌人生成位置的策略枚举
/// </summary>
public enum SpawnPositionStrategy
{
    /// <summary> 矩形区域随机 </summary>
    Random,
    /// <summary> 圆形区域随机 </summary>
    Circle,
    /// <summary> 屏幕视野外（根据相机动态调整） </summary>
    Offscreen,
    /// <summary> 规律网格阵型 </summary>
    Grid,
    /// <summary> 扎堆生成（先定大区域再小散步） </summary>
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

    // --- Random 策略参数 ---
    public float MinX { get; set; } = 0f;
    public float MaxX { get; set; } = 1000f;
    public float MinY { get; set; } = 0f;
    public float MaxY { get; set; } = 1000f;

    // --- Circle 策略参数 ---
    /// <summary> 圆心位置 </summary>
    public Vector2 Center { get; set; } = Vector2.Zero;
    /// <summary> 圆形半径 </summary>
    public float Radius { get; set; } = 500f;

    // --- Offscreen 策略参数 ---
    /// <summary> 生成点距离屏幕边缘的最小额外距离 </summary>
    public float OffscreenDistance { get; set; } = 100f;

    // --- Grid 策略参数 ---
    /// <summary> 网格起始原点（左上角） </summary>
    public Vector2 GridOrigin { get; set; } = Vector2.Zero;
    /// <summary> 每行显示的列数 </summary>
    public int GridColumns { get; set; } = 5;
    /// <summary> 每个网格单元的间距 </summary>
    public float GridSpacing { get; set; } = 100f;
    /// <summary> 当前生成的索引位置（用于递增） </summary>
    public int GridIndex { get; set; } = 0;

    // --- Cluster 策略参数 ---
    /// <summary> 扎堆散布的半径范围 </summary>
    public float ClusterRadius { get; set; } = 100f;
}
