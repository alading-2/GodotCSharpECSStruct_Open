/// <summary>
/// 目标排序方式。
/// </summary>
public enum TargetSorting
{
    /// <summary>
    /// 不排序，保持查询结果原始顺序。
    /// </summary>
    None = 0,
    /// <summary>
    /// 优先最近目标。
    /// </summary>
    Nearest = 1,
    /// <summary>
    /// 优先最远目标。
    /// </summary>
    Farthest = 2,
    /// <summary>
    /// 优先血量最低的目标。
    /// </summary>
    LowestHealth = 3,
    /// <summary>
    /// 优先血量最高的目标。
    /// </summary>
    HighestHealth = 4,
    /// <summary>
    /// 优先血量百分比最高的目标。
    /// </summary>
    HighestHealthPercent = 5,
    /// <summary>
    /// 优先血量百分比最低的目标。
    /// </summary>
    LowestHealthPercent = 6,
    /// <summary>
    /// 随机排序。
    /// </summary>
    Random = 7,
    /// <summary>
    /// 优先威胁值最高的目标。
    /// </summary>
    HighestThreat = 8,
}
