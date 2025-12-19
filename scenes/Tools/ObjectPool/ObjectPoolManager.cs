using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrotatoMy.Tools;

/// <summary>
/// 全局对象池管理器 - 管理所有对象池的生命周期和统计
/// </summary>
/// <example>
/// <code>
/// // 注册池（通常由 ObjectPool 自动完成）
/// ObjectPoolManager.RegisterPool(myPool);
/// 
/// // 获取池
/// var pool = ObjectPoolManager.GetPool&lt;Node&gt;("Bullets");
/// 
/// // 从实例归还到池（推荐方式）
/// ObjectPoolManager.ReturnToPool(bulletNode);
/// 
/// // 获取所有统计
/// var allStats = ObjectPoolManager.GetAllStats();
/// 
/// // 清理所有池
/// ObjectPoolManager.CleanupAll(10);
/// </code>
/// </example>
public static class ObjectPoolManager
{
    #region 私有字段

    /// <summary>按名称索引的对象池字典</summary>
    private static readonly Dictionary<string, object> _pools = new();

    /// <summary>按类型索引的对象池字典</summary>
    private static readonly Dictionary<Type, object> _poolsByType = new();

    #endregion

    #region 公开属性

    /// <summary>已注册的池数量</summary>
    public static int PoolCount => _pools.Count;

    /// <summary>所有池名称</summary>
    public static IEnumerable<string> PoolNames => _pools.Keys;

    #endregion

    #region 注册/注销

    /// <summary>
    /// 注册对象池（通常由 ObjectPool 构造函数自动调用）
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    /// <param name="pool">要注册的对象池</param>
    public static void RegisterPool<T>(ObjectPool<T> pool) where T : class
    {
        if (pool == null) return;

        var name = pool.PoolName;
        if (_pools.ContainsKey(name))
            GD.PushWarning($"ObjectPoolManager: 池 [{name}] 已存在，将被覆盖");

        _pools[name] = pool;
        _poolsByType[typeof(T)] = pool;
        GD.Print($"ObjectPoolManager: 池 [{name}] 已注册");
    }

    /// <summary>
    /// 注销对象池（通常由 ObjectPool.Destroy() 自动调用）
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    /// <param name="pool">要注销的对象池</param>
    public static void UnregisterPool<T>(ObjectPool<T> pool) where T : class
    {
        if (pool == null) return;

        if (_pools.Remove(pool.PoolName))
        {
            _poolsByType.Remove(typeof(T));
            GD.Print($"ObjectPoolManager: 池 [{pool.PoolName}] 已注销");
        }
    }

    #endregion

    #region 获取池

    /// <summary>
    /// 按名称获取对象池
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    /// <param name="name">池名称</param>
    /// <returns>对象池，不存在则返回 null</returns>
    public static ObjectPool<T> GetPool<T>(string name) where T : class
    {
        return _pools.TryGetValue(name, out var pool) ? pool as ObjectPool<T> : null;
    }

    /// <summary>
    /// 按类型获取对象池
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    /// <returns>对象池，不存在则返回 null</returns>
    public static ObjectPool<T> GetPool<T>() where T : class
    {
        return _poolsByType.TryGetValue(typeof(T), out var pool) ? pool as ObjectPool<T> : null;
    }

    /// <summary>
    /// 检查池是否存在
    /// </summary>
    /// <param name="name">池名称</param>
    /// <returns>是否存在</returns>
    public static bool HasPool(string name) => _pools.ContainsKey(name);

    #endregion

    #region 静态归还方法

    /// <summary>
    /// 从 Node 实例归还到其所属的池（推荐方式，对象无需持有池引用）
    /// </summary>
    /// <param name="instance">要归还的 Node 实例</param>
    /// <returns>是否成功归还</returns>
    /// <remarks>
    /// 此方法通过 Node 的元数据 "_object_pool" 获取所属池，
    /// 对象无需持有池引用，使用更简洁
    /// </remarks>
    public static bool ReturnToPool(Node instance)
    {
        if (instance == null || !GodotObject.IsInstanceValid(instance))
        {
            GD.PushWarning("ObjectPoolManager.ReturnToPool: 实例无效");
            return false;
        }

        // 从元数据获取所属池
        var poolVariant = instance.GetMeta("_object_pool", default);
        if (poolVariant.VariantType == Variant.Type.Nil)
        {
            GD.PushWarning("ObjectPoolManager.ReturnToPool: 实例没有关联的对象池");
            return false;
        }

        if (poolVariant.Obj is ObjectPool<Node> nodePool)
            return nodePool.Release(instance);

        GD.PushWarning("ObjectPoolManager.ReturnToPool: 无法获取对象池引用");
        return false;
    }

    /// <summary>
    /// 按池名称归还实例
    /// </summary>
    /// <typeparam name="T">池化对象类型</typeparam>
    /// <param name="instance">要归还的实例</param>
    /// <param name="poolName">池名称</param>
    /// <returns>是否成功归还</returns>
    public static bool ReturnToPool<T>(T instance, string poolName) where T : class
    {
        var pool = GetPool<T>(poolName);
        if (pool == null)
        {
            GD.PushWarning($"ObjectPoolManager.ReturnToPool: 找不到池 [{poolName}]");
            return false;
        }
        return pool.Release(instance);
    }

    #endregion

    #region 批量操作

    /// <summary>
    /// 清理所有池中多余的对象
    /// </summary>
    /// <param name="retainCount">每个池保留的对象数量</param>
    public static void CleanupAll(int retainCount = 10)
    {
        foreach (var pool in _pools.Values)
            pool.GetType().GetMethod("Cleanup")?.Invoke(pool, new object[] { retainCount });
        GD.Print($"ObjectPoolManager: 所有池清理完成");
    }

    /// <summary>
    /// 清空所有池
    /// </summary>
    public static void ClearAll()
    {
        foreach (var pool in _pools.Values)
            pool.GetType().GetMethod("Clear")?.Invoke(pool, null);
        GD.Print("ObjectPoolManager: 所有池已清空");
    }

    /// <summary>
    /// 销毁所有池
    /// </summary>
    public static void DestroyAll()
    {
        var poolNames = _pools.Keys.ToList();
        foreach (var name in poolNames)
            if (_pools.TryGetValue(name, out var pool))
                pool.GetType().GetMethod("Destroy")?.Invoke(pool, null);

        _pools.Clear();
        _poolsByType.Clear();
        GD.Print("ObjectPoolManager: 所有池已销毁");
    }

    #endregion

    #region 统计信息

    /// <summary>
    /// 获取所有池的统计信息
    /// </summary>
    /// <returns>池名称到统计信息的字典</returns>
    public static Dictionary<string, PoolStats> GetAllStats()
    {
        var result = new Dictionary<string, PoolStats>();
        foreach (var (name, pool) in _pools)
            if (pool.GetType().GetMethod("GetStats")?.Invoke(pool, null) is PoolStats stats)
                result[name] = stats;
        return result;
    }

    /// <summary>
    /// 获取全局统计摘要
    /// </summary>
    /// <returns>全局统计信息</returns>
    public static GlobalPoolStats GetGlobalStats()
    {
        var allStats = GetAllStats();
        return new GlobalPoolStats
        {
            TotalPools = _pools.Count,
            TotalPooledObjects = allStats.Values.Sum(s => s.PoolSize),
            TotalActiveObjects = allStats.Values.Sum(s => s.ActiveCount),
            TotalCreated = allStats.Values.Sum(s => s.TotalCreated),
            TotalAcquired = allStats.Values.Sum(s => s.TotalAcquired),
            TotalReleased = allStats.Values.Sum(s => s.TotalReleased),
            TotalDiscarded = allStats.Values.Sum(s => s.TotalDiscarded),
            AverageReuseRate = allStats.Count > 0 ? allStats.Values.Average(s => s.ReuseRate) : 0f
        };
    }

    /// <summary>
    /// 打印所有池的统计信息到控制台
    /// </summary>
    public static void PrintAllStats()
    {
        GD.Print("=== 对象池统计 ===");
        foreach (var stats in GetAllStats().Values)
            GD.Print(stats.ToString());
        GD.Print($"=== 全局: {GetGlobalStats()} ===");
    }

    #endregion
}

/// <summary>
/// 全局池统计信息
/// </summary>
public readonly struct GlobalPoolStats
{
    /// <summary>总对象池数量</summary>
    public int TotalPools { get; init; }

    /// <summary>总空闲对象数量</summary>
    public int TotalPooledObjects { get; init; }

    /// <summary>总活跃对象数量</summary>
    public int TotalActiveObjects { get; init; }

    /// <summary>累计创建对象总数</summary>
    public int TotalCreated { get; init; }

    /// <summary>累计获取次数</summary>
    public int TotalAcquired { get; init; }

    /// <summary>累计归还次数</summary>
    public int TotalReleased { get; init; }

    /// <summary>累计丢弃次数</summary>
    public int TotalDiscarded { get; init; }

    /// <summary>平均复用率</summary>
    public float AverageReuseRate { get; init; }

    public override string ToString() =>
        $"池:{TotalPools} | 闲:{TotalPooledObjects} | 活:{TotalActiveObjects} | " +
        $"创:{TotalCreated} | 获:{TotalAcquired} | 还:{TotalReleased} | 弃:{TotalDiscarded} | 平均复用:{AverageReuseRate:P1}";
}
