/// <summary>
/// 目标几何形状 - 决定影响范围形状（纯空间概念）
/// </summary>
public enum GeometryType
{
    /// <summary>单体</summary>
    Single = 0,
    /// <summary>圆形 (需要 Range)</summary>
    Circle = 1,
    /// <summary>圆环 (需要 InnerRange, Range)：排除内圆、保留外圆范围内的目标</summary>
    Ring = 2,
    /// <summary>矩形 (需要 Width, Length)</summary>
    Box = 3,
    /// <summary>线性 (需要 Width, Length)</summary>
    Line = 4,
    /// <summary>扇形 (需要 Range, Angle)</summary>
    Cone = 5,
    /// <summary>全屏</summary>
    Global = 6,
}
