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
/// 属性修改器 - 用于 Buff/Debuff 系统。
/// </summary>
public class AttributeModifier
{
    /// <summary>
    /// 修改器唯一标识符。
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 目标属性名称。
    /// </summary>
    public string AttributeName { get; init; }

    /// <summary>
    /// 修改器类型（加法/乘法）。
    /// </summary>
    public ModifierType Type { get; init; }

    /// <summary>
    /// 修改值。
    /// 加法类型：直接加到基础值。
    /// 乘法类型：作为乘数（1.0 = 100%，1.5 = 150%）。
    /// </summary>
    public float Value { get; init; }

    /// <summary>
    /// 计算优先级（数值越小越先计算）。
    /// </summary>
    public int Priority { get; init; }

    public AttributeModifier(string statName, ModifierType type, float value, int priority = 0, string? id = null)
    {
        Id = id ?? System.Guid.NewGuid().ToString();
        AttributeName = statName;
        Type = type;
        Value = value;
        Priority = priority;
    }
}
