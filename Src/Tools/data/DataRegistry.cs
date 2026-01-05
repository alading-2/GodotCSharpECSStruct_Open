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
    private static readonly Dictionary<string, ComputedData> _computedRegistry = new();

    // --- 注册接口：由 DataInit 在游戏启动时调用 ---

    public static void Register(DataMeta meta)
    {
        _metaRegistry[meta.Key] = meta;
    }

    public static void RegisterComputed(ComputedData computed)
    {
        _computedRegistry[computed.Key] = computed;
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
    /// 获取计算数据定义
    /// </summary>
    public static ComputedData? GetComputed(string key)
    {
        return _computedRegistry.TryGetValue(key, out var computed) ? computed : null;
    }

    /// <summary>
    /// 检查是否为计算数据
    /// </summary>
    public static bool IsComputed(string key)
    {
        return _computedRegistry.ContainsKey(key);
    }

    /// <summary>
    /// 检查数据是否支持修改器
    /// </summary>
    public static bool SupportModifiers(string key)
    {
        var meta = GetMeta(key);
        return meta?.ActualSupportModifiers ?? false;
    }

    /// <summary>
    /// 获取依赖指定数据的所有计算数据键
    /// </summary>
    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _computedRegistry
            .Where(kvp => kvp.Value.DependsOn(baseKey))
            .Select(kvp => kvp.Key);
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
        return _computedRegistry.Keys;
    }
}
