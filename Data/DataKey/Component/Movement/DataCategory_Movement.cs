/// <summary>
/// 运动数据分类枚举，用于整理运动系统相关的 `DataKey`。
/// <para>
/// 这些分类既服务于编辑器展示，也服务于 `ResetByCategories()` 这类批量重置逻辑。
/// </para>
/// </summary>
public enum DataCategory_Movement
{
    /// <summary>
    /// 通用运动核心数据。
    /// <para>
    /// 包括当前模式、默认模式、速度、时间限制、距离限制、累计统计等跨策略通用参数。
    /// </para>
    /// </summary>
    Basic,

    /// <summary>
    /// 目标相关数据。
    /// <para>
    /// 包括目标点、目标节点、到达阈值等由追踪或冲刺类策略读取的数据。
    /// </para>
    /// </summary>
    Target,

    /// <summary>
    /// 环绕与螺旋相关数据。
    /// <para>
    /// 包括圆心、半径、角速度、方向、当前角度和径向变化速度。
    /// </para>
    /// </summary>
    Orbit,

    /// <summary>
    /// 波形相关数据。
    /// <para>
    /// 包括振幅、频率、相位，以及正弦波策略内部缓存的基础前进方向。
    /// </para>
    /// </summary>
    Wave,

    /// <summary>
    /// 贝塞尔曲线相关数据。
    /// <para>
    /// 包括控制点、起点、时长、匀速开关与弧长查找表。
    /// </para>
    /// </summary>
    Bezier,

    /// <summary>
    /// 回旋镖相关数据。
    /// <para>
    /// 包括起点、是否回程、停顿时长与停顿计时器。
    /// </para>
    /// </summary>
    Boomerang,

    /// <summary>
    /// 附着跟随相关数据。
    /// <para>
    /// 主要用于宿主引用和附着偏移这类只对附着策略有意义的数据。
    /// </para>
    /// </summary>
    Attach
}
