using Godot;

/// <summary>
/// 目标查询配置参数
/// 用于 TargetSelector.Query() 方法的参数传递
/// 
/// 设计原则：
/// - 使用 record struct 保证不可变性和值语义
/// - 使用 required 标记必填参数，避免遗漏
/// - 可选参数根据 Geometry 类型按需填充
/// </summary>
public readonly record struct TargetSelectorQuery
{
    // ==================== 几何参数 ====================

    /// <summary>几何形状类型（必填）</summary>
    public required AbilityTargetGeometry Geometry { get; init; }

    /// <summary>查询原点（必填）</summary>
    public required Vector2 Origin { get; init; }

    /// <summary>
    /// 方向向量（可选，归一化）
    /// Line/Cone/Box 需要此参数
    /// </summary>
    public Vector2? Forward { get; init; }

    /// <summary>
    /// 查询半径（可选）
    /// Circle/Cone 使用此参数
    /// </summary>
    public float Range { get; init; }

    /// <summary>
    /// 宽度（可选）
    /// Box/Line 使用此参数
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// 长度（可选）
    /// Box/Line 使用此参数
    /// </summary>
    public float Length { get; init; }

    /// <summary>
    /// 扇形角度（可选，单位：度）
    /// Cone 使用此参数
    /// </summary>
    public float Angle { get; init; }

    /// <summary>
    /// 链式跳跃次数（可选）
    /// Chain 使用此参数
    /// </summary>
    public int ChainCount { get; init; }

    /// <summary>
    /// 链式每跳最大距离（可选）
    /// Chain 使用此参数
    /// </summary>
    public float ChainRange { get; init; }

    // ==================== 过滤参数 ====================

    /// <summary>
    /// 阵营过滤器（可选，默认 All）
    /// 用于过滤友军/敌军/中立/自身
    /// </summary>
    public AbilityTargetTeamFilter TeamFilter { get; init; }

    /// <summary>
    /// 类型过滤器（可选，默认 AllAttackable）
    /// 用于过滤英雄/小怪/建筑/召唤物/Boss
    /// </summary>
    public AbilityTargetTypeFilter TypeFilter { get; init; }

    /// <summary>
    /// 阵营过滤基准实体（可选）
    /// 用于判断 Self/Friendly/Enemy，如果为 null 则跳过阵营过滤
    /// </summary>
    public IEntity? CenterEntity { get; init; }

    // ==================== 排序与限制 ====================

    /// <summary>
    /// 排序规则（可选，默认不排序）
    /// Nearest/Farthest/LowestHealth 等
    /// </summary>
    public AbilityTargetSorting Sorting { get; init; }

    /// <summary>
    /// 最大目标数量（可选，0 表示不限制）
    /// </summary>
    public int MaxTargets { get; init; }
}
