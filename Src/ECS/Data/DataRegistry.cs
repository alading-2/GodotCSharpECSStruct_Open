using System.Collections.Generic;
using System.Linq;
using System;
/// <summary>
/// 数据注册表 - 管理所有 DataMeta（含运行时约束 + 展示字段）
/// </summary>
public static class DataRegistry
{
    private static readonly Log _log = new(nameof(DataRegistry));

    private static readonly Dictionary<string, DataMeta> _metaRegistry = new();

    // --- 注册接口 ---

    /// <summary>
    /// 注册元数据，返回同一实例（便于静态字段直接赋值）
    /// </summary>
    public static DataMeta Register(DataMeta meta)
    {
        _metaRegistry[meta.Key] = meta;
        return meta;
    }

    // === 公共查询接口 ===

    /// <summary>
    /// 获取元数据（未注册返回 null，走快速路径）
    /// </summary>
    public static DataMeta? GetMeta(string key)
    {
        return _metaRegistry.GetValueOrDefault(key);
    }

    /// <summary>
    /// 检查是否为计算数据
    /// </summary>
    public static bool IsComputed(string key)
    {
        return GetMeta(key)?.IsComputed ?? false;
    }

    /// <summary>
    /// 检查数据是否支持修改器
    /// </summary>
    public static bool SupportModifiers(string key)
    {
        return GetMeta(key)?.SupportModifiers ?? false;
    }

    /// <summary>
    /// 获取依赖指定 baseKey 的所有计算键（用于 MarkDirty 级联失效）
    /// </summary>
    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _metaRegistry.Values
            .Where(m => m.IsComputed && m.Dependencies != null && m.Dependencies.Contains(baseKey))
            .Select(m => m.Key);
    }

    /// <summary>
    /// 获取指定分类的所有元数据（通过 DataMeta.Category 筛选）
    /// </summary>
    public static IEnumerable<DataMeta> GetMetaByCategory(Enum category)
    {
        return _metaRegistry.Values.Where(m => Equals(m.Category, category));
    }

    /// <summary>
    /// 获取所有已注册的数据键
    /// </summary>
    public static IEnumerable<string> GetAllKeys()
    {
        return _metaRegistry.Keys;
    }

    /// <summary>
    /// 获取所有计算数据键
    /// </summary>
    public static IEnumerable<string> GetAllComputedKeys()
    {
        return _metaRegistry.Values.Where(m => m.IsComputed).Select(m => m.Key);
    }
}
