using Godot;

/// <summary>
/// TargetSelector 查询参数载体。
/// 统一描述几何范围、过滤、排序和数量限制，可被 EntityTargetSelector 与 PositionTargetSelector 复用。
/// </summary>
public readonly record struct TargetSelectorQuery
{
    // ==================== 几何参数 ====================

    /// <summary>
    /// 几何形状类型（必填）。
    /// 常见值：Circle / Ring / Box / Line / Cone / Chain / Global / Single。
    /// </summary>
    public required GeometryType Geometry { get; init; }

/// <summary>
/// 查询原点（必填）。
/// 所有几何计算都基于该点。
/// </summary>
public required Vector2 Origin
{ get; init; }

/// <summary>
/// 方向向量（可选）。
/// Box / Line / Cone 需要朝向，不传时通常默认使用 Vector2.Right。
/// </summary>
public Vector2? Forward { get; init; }

/// <summary>
/// 半径或最大距离（可选）。
/// Circle / Ring / Cone 等几何会使用该值。
/// </summary>
public float Range { get; init; }

/// <summary>
/// 圆环内半径（可选，默认 0）。
/// 仅 Ring 使用。
/// </summary>
public float InnerRange { get; init; }

/// <summary>
/// 宽度（可选）。
/// Box / Line 使用。
/// </summary>
public float Width { get; init; }

/// <summary>
/// 长度（可选）。
/// Box / Line 使用。
/// </summary>
public float Length { get; init; }

/// <summary>
/// 扇形角度（可选，单位：度）。
/// 仅 Cone 使用。
/// </summary>
public float Angle { get; init; }

// ==================== 过滤参数 ====================

/// <summary>
/// 阵营过滤器（可选）。
/// 支持 Friendly / Enemy / Neutral / Self 组合。
/// </summary>
public AbilityTargetTeamFilter TeamFilter { get; init; }

/// <summary>
/// 类型过滤器（可选）。
/// 通过 EntityType 位掩码筛选目标类型。
/// </summary>
public EntityType TypeFilter { get; init; }

/// <summary>
/// 阵营过滤参照实体（可选）。
/// 判断 Self / Friendly / Enemy 时需要。
/// </summary>
public IEntity? CenterEntity { get; init; }

// ==================== 排序与限制 ====================

/// <summary>
/// 排序规则（可选）。
/// 如 Nearest / Farthest / LowestHealth / Random。
/// </summary>
public AbilityTargetSorting Sorting { get; init; }

/// <summary>
/// 最大目标数量（可选）。
/// 0 表示不限。
/// </summary>
public int MaxTargets { get; init; }
}
