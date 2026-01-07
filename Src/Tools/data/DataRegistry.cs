using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 数据注册表 - 管理所有数据的元数据和计算规则
/// 仅作为元数据存储库，由 DataInit 驱动初始化
/// </summary>
public static class DataRegistry
{
    private static readonly Log _log = new("DataRegistry");

    private static readonly Dictionary<string, DataMeta> _metaRegistry = new();

    // --- 注册接口：由 DataInit 在游戏启动时调用 ---

    public static void Register(DataMeta meta)
    {
        _metaRegistry[meta.Key] = meta;
    }

    // === 公共查询接口 ===

    /// <summary>
    /// 获取数据的元数据
    /// </summary>
    public static DataMeta? GetMeta(string key)
    {
        return _metaRegistry.TryGetValue(key, out var meta) ? meta : null;
    }

    /// <summary>
    /// 检查是否为计算数据
    /// </summary>
    public static bool IsComputed(string key)
    {
        var meta = GetMeta(key);
        return meta?.IsComputed ?? false;
    }

    /// <summary>
    /// 检查数据是否支持修改器
    /// </summary>
    public static bool SupportModifiers(string key)
    {
        var meta = GetMeta(key);
        return meta?.SupportModifiers ?? false;
    }

    /// <summary>
    /// 获取依赖指定数据的所有DataKey，主要用在MarkDirty
    /// 比如最终生命值 = 基础生命值 * (1 + 生命值加成/100)，基础生命值变了，最终生命值也要重新计算，这里返回的一般是依赖里面包含基础生命值的计算属性
    /// </summary>
    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _metaRegistry.Values
            .Where(m => m.IsComputed && m.Dependencies != null && m.Dependencies.Contains(baseKey))
            .Select(m => m.Key);
    }

    /// <summary>
    /// 获取指定分类的所有数据元数据
    /// </summary>
    public static IEnumerable<DataMeta> GetMetaByCategory(DataCategory category)
    {
        return _metaRegistry.Values.Where(m => m.Category == category);
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
