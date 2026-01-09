using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 数据元数据 - 描述数据的所有特性
/// 使用智能默认值，减少冗余配置
/// </summary>
public class DataMeta
{
    // === 必填字段（核心三要素） ===

    /// <summary>
    /// 数据键名（必填）
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// 显示名称（选填，用于 UI）
    /// </summary>
    public string DisplayName { get; init; } = "";

    /// <summary>
    /// 数据类型（必填）
    /// </summary>
    public required Type Type { get; init; }

    // === 可选字段（智能默认值） ===

    /// <summary>
    /// 描述信息（可选，默认空字符串）
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// 数据分类（可选，默认 Basic）
    /// </summary>
    public Enum Category { get; init; }

    /// <summary>
    /// 默认值（可选，根据 Type 自动推断）
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// 最小值（可选，仅数值类型有效）
    /// </summary>
    public float? MinValue { get; init; }

    /// <summary>
    /// 最大值（可选，仅数值类型有效）
    /// </summary>
    public float? MaxValue { get; init; }

    /// <summary>
    /// 是否为百分比值（可选，默认 false）
    /// </summary>
    public bool IsPercentage { get; init; } = false;

    /// <summary>
    /// 是否支持修改器（可选，数值类型默认 true，其他默认 false）
    /// </summary>
    public bool? SupportModifiers { get; init; }

    /// <summary>
    /// 图标路径（可选，默认空字符串）
    /// </summary>
    public string IconPath { get; init; } = "";

    /// <summary>
    /// 枚举值约束（可选，仅枚举类型有效）
    /// </summary>
    public string[]? EnumValues { get; init; }

    /// <summary>
    /// 可选值列表（用于下拉选择）
    /// 索引对应实际存储的 int 值
    /// </summary>
    public List<string>? Options { get; init; }

    // === 计算属性支持（参考 TypeScript Schema ComputedPropertyDefinition） ===

    /// <summary>
    /// 依赖的数据键列表（仅计算属性使用）
    /// </summary>
    public string[]? Dependencies { get; init; }

    /// <summary>
    /// 计算函数（仅计算属性使用）
    /// </summary>
    public Func<Data, object>? Compute { get; init; }

    // === 智能属性（自动计算） ===

    /// <summary>
    /// 是否为数值类型（自动判断）
    /// </summary>
    public bool IsNumeric => Type == typeof(int) || Type == typeof(float) || Type == typeof(double);

    /// <summary>
    /// 是否为整数类型
    /// </summary>
    public bool IsInteger => Type == typeof(int) || Type == typeof(long) || Type == typeof(short);

    /// <summary>
    /// 是否为浮点类型
    /// </summary>
    public bool IsFloatingPoint => Type == typeof(float) || Type == typeof(double);

    /// <summary>
    /// 是否为布尔类型
    /// </summary>
    public bool IsBoolean => Type == typeof(bool);

    /// <summary>
    /// 是否为字符串类型
    /// </summary>
    public bool IsString => Type == typeof(string);

    /// <summary>
    /// 是否为枚举类型（自动判断）
    /// </summary>
    public bool IsEnum => Type.IsEnum;

    /// <summary>
    /// 是否为值类型
    /// </summary>
    public bool IsValueType => Type.IsValueType;

    /// <summary>
    /// 是否为引用类型
    /// </summary>
    public bool IsReferenceType => !Type.IsValueType;

    /// <summary>
    /// 是否为计算属性
    /// </summary>
    public bool IsComputed => Compute != null;

    /// <summary>
    /// 是否有选项约束
    /// </summary>
    public bool HasOptions => Options != null && Options.Count > 0;

    /// <summary>
    /// 实际默认值（智能推断）
    /// </summary>
    public object GetDefaultValue()
    {
        if (DefaultValue != null) return DefaultValue;
        return GetTypeDefaultValue(Type);
    }

    /// <summary>
    /// 根据类型推断默认值（静态方法，无需 DataMeta 实例）
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>该类型的默认值</returns>
    public static object GetTypeDefaultValue(Type type)
    {
        // 根据类型推断默认值
        if (type == typeof(int)) return 0;
        if (type == typeof(float)) return 0f;
        if (type == typeof(double)) return 0.0;
        if (type == typeof(bool)) return false;
        if (type == typeof(string)) return "";
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0)!;

        // 如果是值类型，返回其默认实例
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type)!;
        }

        return null!;
    }

    /// <summary>
    /// 验证并限制数据值
    /// </summary>
    public object Clamp(object value)
    {
        if (!IsNumeric) return value;

        float numValue = Convert.ToSingle(value);

        if (MinValue.HasValue)
            numValue = Math.Max(numValue, MinValue.Value);

        if (MaxValue.HasValue)
            numValue = Math.Min(numValue, MaxValue.Value);

        // 转换回原始类型
        if (Type == typeof(int)) return (int)numValue;
        if (Type == typeof(float)) return numValue;
        if (Type == typeof(double)) return (double)numValue;

        return value;
    }

    /// <summary>
    /// 格式化显示值
    /// </summary>
    public string FormatValue(object value)
    {
        if (IsNumeric)
        {
            float numValue = Convert.ToSingle(value);
            return IsPercentage ? $"{numValue:F1}%" : $"{numValue:F1}";
        }

        if (IsEnum)
        {
            return value.ToString() ?? "";
        }

        // 选项显示
        if (HasOptions && value is int index)
        {
            var optionName = GetOptionName(index);
            if (optionName != null) return optionName;
        }

        return value?.ToString() ?? "";
    }

    /// <summary>
    /// 获取选项的显示名称
    /// </summary>
    public string? GetOptionName(int index)
    {
        if (!HasOptions || index < 0 || index >= Options!.Count)
            return null;
        return Options[index];
    }

    /// <summary>
    /// 验证值是否在有效选项范围内
    /// </summary>
    public bool IsValidOption(object value)
    {
        if (!HasOptions) return true;
        if (value is int index)
        {
            return index >= 0 && index < Options!.Count;
        }
        return false;
    }
}
