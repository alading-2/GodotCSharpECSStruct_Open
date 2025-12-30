using System;
using System.Collections.Generic;
using Godot;

// 采用混合命名空间策略：核心工具类放在全局命名空间，方便调用

/// <summary>
/// 全局对象池管理器
/// 提供静态访问接口，支持自动归还和全局池生命周期管理
/// </summary>
public static class ObjectPoolManager
{
    private static readonly Log _log = new("ObjectPoolManager");

    // 全局池字典，按 PoolName 索引（存储任意类型的池）
    // [] 是 C# 12 的集合表达式语法，等价于 new Dictionary<string, object>()
    private static readonly Dictionary<string, object> _pools = [];
    
    // 线程锁对象，用于确保在多线程环境下（如异步加载、后台线程归还对象）对 _pools 字典的操作是安全的
    // 它可以防止多个线程同时修改字典导致的崩溃或数据不一致（竞态条件）
    private static readonly object _lock = new();

    /// <summary> 注册一个对象池到管理器（泛型版本） </summary>
    public static void Register<T>(ObjectPool<T> pool) where T : class
    {
        lock (_lock)
        {
            if (_pools.ContainsKey(pool.PoolName))
            {
                _log.Warn($"池名称 '{pool.PoolName}' 已存在。将覆盖旧池。");
            }
            _pools[pool.PoolName] = pool;
        }
    }

    /// <summary> 从管理器中注销一个对象池（泛型版本） </summary>
    public static void Unregister<T>(ObjectPool<T> pool) where T : class
    {
        lock (_lock)
        {
            _pools.Remove(pool.PoolName);
        }
    }

    /// <summary>
    /// 静态归还方法（核心功能）
    /// 自动查找对象所属的对象池并执行归还操作。
    ///! 仅支持 Node 对象（通过 Data 容器存储池名称）。因为现在只有Godot Node 对象才有 GetData() 
    /// 非 Node 对象请直接调用 pool.Release(obj)。
    /// </summary>
    /// <param name="instance">要归还的对象（必须是 Node）</param>
    public static void ReturnToPool(object instance)
    {
        if (instance == null) return;

        // 只支持 Node 对象的自动归还
        if (instance is not Node node)
        {
            _log.Error($"实例 {instance} 不是 Node，无法自动归还。请直接调用 pool.Release()。");
            return;
        }

        // 从 Node.Data 中读取池名称
        string? poolName = null;
        if (node.HasData())
        {
            poolName = node.GetData().Get<string>("ObjectPoolName");
        }

        if (poolName == null)
        {
            _log.Warn($"Node {node.Name} 的 Data 中未找到 ObjectPoolName。将退回到 QueueFree。");
            node.QueueFree();
            return;
        }

        // 查找池并调用 Release
        lock (_lock)
        {
            if (_pools.TryGetValue(poolName, out var poolObj))
            {
                var releaseMethod = poolObj.GetType().GetMethod("Release");
                if (releaseMethod != null)
                {
                    releaseMethod.Invoke(poolObj, new[] { instance });
                    return;
                }
            }
        }

        // 池不存在，降级处理
        _log.Warn($"池 '{poolName}' 不存在。Node {node.Name} 将退回到 QueueFree。");
        node.QueueFree();
    }

    /// <summary> 根据名称获取对象池实例（泛型版本，提供类型安全） </summary>
    public static ObjectPool<T>? GetPool<T>(string name) where T : class
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(name, out var poolObj))
            {
                return poolObj as ObjectPool<T>;
            }
            return null;
        }
    }

    /// <summary> 获取当前所有池的详细统计信息 </summary>
    public static Dictionary<string, PoolStats> GetAllStats()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, PoolStats>();
            foreach (var kvp in _pools)
            {
                // 使用反射调用 GetStats 方法
                var getStatsMethod = kvp.Value.GetType().GetMethod("GetStats");
                if (getStatsMethod != null)
                {
                    var poolStats = getStatsMethod.Invoke(kvp.Value, null);
                    if (poolStats is PoolStats ps)
                    {
                        stats[kvp.Key] = ps;
                    }
                }
            }
            return stats;
        }
    }

    /// <summary> 清理所有池中的闲置对象，保留每个池指定的最小数量 </summary>
    public static void CleanupAll(int retainCount)
    {
        lock (_lock)
        {
            foreach (var poolObj in _pools.Values)
            {
                // 使用反射调用 Cleanup 方法
                var cleanupMethod = poolObj.GetType().GetMethod("Cleanup");
                cleanupMethod?.Invoke(poolObj, new object[] { retainCount });
            }
        }
    }

    /// <summary>
    /// 彻底销毁所有池
    /// 在场景切换或游戏结束时调用，确保内存释放
    /// </summary>
    public static void DestroyAll()
    {
        lock (_lock)
        {
            foreach (var poolObj in _pools.Values)
            {
                // 使用反射调用 Clear 方法
                var clearMethod = poolObj.GetType().GetMethod("Clear");
                clearMethod?.Invoke(poolObj, null);
            }
            _pools.Clear();
        }
        _log.Info("所有对象池已销毁并清空。");
    }
}
