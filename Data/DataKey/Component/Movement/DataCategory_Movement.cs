/// <summary>
/// 运动数据分类枚举 - 用于 UI 展示和数据组织
/// </summary>
public enum DataCategory_Movement
{
    /// <summary>
    /// 运动核心参数（模式、时间、距离等）
    /// </summary>
    Basic,

    /// <summary>
    /// 目标相关（目标点、目标实体、到达距离等）
    /// </summary>
    Target,

    /// <summary>
    /// 环绕/螺旋参数（圆心、半径、角速度等）
    /// </summary>
    Orbit,

    /// <summary>
    /// 波形参数（振幅、频率、相位等）
    /// </summary>
    Wave,

    /// <summary>
    /// 贝塞尔曲线参数（控制点、起点、时长等）
    /// </summary>
    Bezier,

    /// <summary>
    /// 回旋镖参数（起点、返回标记、停顿时间等）
    /// </summary>
    Boomerang,

    /// <summary>
    /// 附着跟随参数（宿主引用、偏移等）
    /// </summary>
    Attach
}
