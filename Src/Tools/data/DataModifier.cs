// 注意：ModifierType 枚举已在 AttributeModifier.cs 中定义，此处复用

/// <summary>
/// 修改器类型枚举。
/// </summary>
public enum ModifierType
{
    /// <summary>
    /// 加法修改器：直接加到基础值上。
    /// </summary>
    Additive,

    /// <summary>
    /// 乘法修改器：乘以基础值（加法修改后）。
    /// </summary>
    Multiplicative
}

/// <summary>
/// 数据修改器 - 用于 Buff/Debuff 系统
/// 公式：最终值 = (基础值 + Σ加法) × Π乘法
/// </summary>
public class DataModifier
{
    /// <summary>
    /// 修改器唯一标识符
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 修改器类型（加法/乘法）
    /// </summary>
    public ModifierType Type { get; init; }

    /// <summary>
    /// 修改值
    /// 加法类型：直接加到基础值
    /// 乘法类型：作为乘数（1.0 = 100%，1.5 = 150%）
    /// </summary>
    public float Value { get; init; }

    /// <summary>
    /// 计算优先级（数值越小越先计算）
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// 修改器来源对象（例如：装备 Entity、Buff 实例）
    /// 用于按来源批量移除修改器
    /// </summary>
    public object? Source { get; init; }

    /// <summary>
    /// 创建数据修改器
    /// </summary>
    /// <param name="type">修改器类型</param>
    /// <param name="value">修改值</param>
    /// <param name="priority">优先级（默认 0）</param>
    /// <param name="id">唯一标识符（默认自动生成）</param>
    /// <param name="source">来源对象（可选）</param>
    public DataModifier(ModifierType type, float value, int priority = 0, string? id = null, object? source = null)
    {
        Id = id ?? System.Guid.NewGuid().ToString();
        Type = type;
        Value = value;
        Priority = priority;
        Source = source;
    }
}
