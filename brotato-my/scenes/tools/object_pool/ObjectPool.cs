using Godot;
using System;
using System.Collections.Generic;

namespace BrotatoMy.Tools;

#region 接口定义

/// <summary>
/// 可池化对象接口 - 实现此接口的对象可以被对象池管理
/// </summary>
public interface IPoolable
{
    /// <summary>对象被从池中取出时调用</summary>
    void OnPoolAcquire();
    
    /// <summary>对象被归还到池中时调用</summary>
    void OnPoolRelease();
}

/// <summary>
/// 可池化 Node 接口 - 为 Node 类型提供的扩展接口
/// </summary>
public interface IPoolableNode : IPoolable
{
    /// <summary>获取关联的对象池</summary>
    ObjectPool<Node>? Pool { get; set; }
    
    /// <summary>是否在池中</summary>
    bool IsInPool { get; set; }
}

#endregion

#region 配置类

/// <summary>
/// 对象池配置
/// </summary>
public class ObjectPoolConfig
{
    /// <summary>池的最大容量，-1 表示无限制</summary>
    public int MaxSize { get; set; } = 50;
    
    /// <summary>初始预热数量</summary>
    public int InitialSize { get; set; } = 0;
    
    /// <summary>是否启用统计</summary>
    public bool EnableStats { get; set; } = true;
    
    /// <summary>池名称（用于调试）</summary>
    public string Name { get; set; } = "ObjectPool";
}

/// <summary>
/// 对象池统计信息
/// </summary>
public readonly struct PoolStats
{
    public string Name { get; init; }
    public int PoolSize { get; init; }
    public int ActiveCount { get; init; }
    public int TotalCount { get; init; }
    public int PeakActive { get; init; }
    public int TotalCreated { get; init; }
    public int TotalAcquired { get; init; }
    public int TotalReleased { get; init; }
    public int TotalDiscarded { get; init; }
    public float ReuseRate { get; init; }
    
    public override string ToString() =>
        $"[{Name}] 总:{TotalCount}(活:{ActiveCount}/闲:{PoolSize}) | 峰:{PeakActive} | " +
        $"创:{TotalCreated} | 获:{TotalAcquired} | 还:{TotalReleased} | 弃:{TotalDiscarded} | 复用:{ReuseRate:P1}";
}

#endregion

/// <summary>
/// 通用对象池 - 用于管理对象的复用，减少 GC 压力
/// </summary>
/// <typeparam name="T">池化对象类型</typeparam>
/// <example>
/// <code>
/// // 创建 Node 对象池
/// var bulletPool = new ObjectPool&lt;Node&gt;(bulletScene, new ObjectPoolConfig {
///     MaxSize = 50,
///     InitialSize = 20,
///     Name = "Bullets"
/// });
/// 
/// // 获取对象
/// var bullet = bulletPool.Acquire(parentNode);
/// 
/// // 归还对象
/// bulletPool.Release(bullet);
/// // 或者静态方法
/// ObjectPoolManager.ReturnToPool(bullet);
/// </code>
/// </example>
public partial class ObjectPool<T> : RefCounted where T : class
{
    #region 信号
    
    [Signal] public delegate void InstanceAcquiredEventHandler(T instance);
    [Signal] public delegate void InstanceReleasedEventHandler(T instance);
    [Signal] public delegate void PoolExhaustedEventHandler();
    [Signal] public delegate void PoolClearedEventHandler();
    
    #endregion

    #region 私有字段
    
    private readonly Stack<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly PackedScene? _scene;
    private readonly ObjectPoolConfig _config;
    
    // 统计信息
    private int _activeCount;
    private int _initialCreated;
    private int _peakActive;
    private int _totalCreated;
    private int _totalAcquired;
    private int _totalReleased;
    private int _totalDiscarded;
    
    #endregion

    #region 公开属性
    
    /// <summary>池名称</summary>
    public string PoolName => _config.Name;
    
    /// <summary>池中可用实例数量（空闲）</summary>
    public int AvailableCount => _pool.Count;
    
    /// <summary>当前活跃实例数量（已借出）</summary>
    public int ActiveCount => _activeCount;
    
    /// <summary>对象总数（活跃 + 空闲）</summary>
    public int TotalCount => _activeCount + _pool.Count;
    
    /// <summary>池是否为空</summary>
    public bool IsEmpty => _pool.Count == 0;
    
    /// <summary>池是否已满</summary>
    public bool IsFull => _config.MaxSize > 0 && _pool.Count >= _config.MaxSize;
    
    #endregion

    #region 构造函数
    
    /// <summary>
    /// 使用工厂函数创建对象池（适用于非 Node 对象）
    /// </summary>
    /// <param name="factory">对象创建工厂函数</param>
    /// <param name="config">池配置</param>
    public ObjectPool(Func<T> factory, ObjectPoolConfig? config = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _config = config ?? new ObjectPoolConfig();
        
        if (_config.InitialSize > 0)
        {
            Warmup(_config.InitialSize);
        }
        
        ObjectPoolManager.RegisterPool(this);
    }
    
    /// <summary>
    /// 使用 PackedScene 创建对象池（适用于 Node 对象）
    /// </summary>
    /// <param name="scene">要池化的场景</param>
    /// <param name="config">池配置</param>
    public ObjectPool(PackedScene scene, ObjectPoolConfig? config = null)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _factory = () => (T)(object)scene.Instantiate();
        _config = config ?? new ObjectPoolConfig();
        
        if (_config.InitialSize > 0)
        {
            Warmup(_config.InitialSize);
        }
        
        ObjectPoolManager.RegisterPool(this);
    }
    
    #endregion

    #region 核心方法
    
    /// <summary>
    /// 预热池：提前创建指定数量的对象
    /// </summary>
    /// <param name="count">要创建的数量</param>
    public void Warmup(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var instance = CreateInstance();
            _initialCreated++;
            
            PrepareForPool(instance);
            _pool.Push(instance);
        }
    }
    
    /// <summary>
    /// 从池中获取一个实例（非 Node 版本）
    /// </summary>
    /// <returns>对象实例</returns>
    public T Acquire()
    {
        T instance;
        
        if (_pool.Count > 0)
        {
            instance = _pool.Pop();
        }
        else
        {
            instance = CreateInstance();
            EmitSignal(SignalName.PoolExhausted);
        }
        
        MarkAsActive(instance);
        
        // 调用生命周期回调
        if (instance is IPoolable poolable)
        {
            poolable.OnPoolAcquire();
        }
        
        if (_config.EnableStats)
        {
            _totalAcquired++;
            _activeCount++;
            if (_activeCount > _peakActive) _peakActive = _activeCount;
        }
        
        EmitSignal(SignalName.InstanceAcquired, instance);
        return instance;
    }
    
    /// <summary>
    /// 从池中获取一个 Node 实例并添加到父节点
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <returns>Node 实例</returns>
    public T Acquire(Node parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent), $"ObjectPool[{_config.Name}].Acquire: parent 不能为 null");
        
        T instance;
        
        // 尝试从池中获取有效实例
        while (_pool.Count > 0)
        {
            var pooled = _pool.Pop();
            if (pooled is Node node && GodotObject.IsInstanceValid(node))
            {
                instance = pooled;
                goto Found;
            }
            else if (pooled is not Node)
            {
                instance = pooled;
                goto Found;
            }
            // 无效实例，继续查找
            GD.PushWarning($"ObjectPool[{_config.Name}]: 发现无效实例，已跳过");
        }
        
        // 池空，创建新实例
        instance = CreateInstance();
        EmitSignal(SignalName.PoolExhausted);
        
        Found:
        MarkAsActive(instance);
        
        // Node 特殊处理
        if (instance is Node nodeInstance)
        {
            // 如果已有父节点，先移除
            nodeInstance.GetParent()?.RemoveChild(nodeInstance);
            
            // 添加到指定父节点
            parent.AddChild(nodeInstance);
            
            // 显示
            if (nodeInstance is CanvasItem canvasItem)
            {
                canvasItem.Visible = true;
            }
            
            // 启用处理
            nodeInstance.SetProcess(true);
            nodeInstance.SetPhysicsProcess(true);
        }
        
        // 调用生命周期回调
        if (instance is IPoolable poolable)
        {
            poolable.OnPoolAcquire();
        }
        else if (instance is Node n && n.HasMethod("OnPoolAcquire"))
        {
            n.Call("OnPoolAcquire");
        }
        
        if (_config.EnableStats)
        {
            _totalAcquired++;
            _activeCount++;
            if (_activeCount > _peakActive) _peakActive = _activeCount;
        }
        
        EmitSignal(SignalName.InstanceAcquired, instance);
        return instance;
    }
    
    /// <summary>
    /// 归还实例到池中
    /// </summary>
    /// <param name="instance">要归还的实例</param>
    /// <returns>是否成功归还</returns>
    public bool Release(T? instance)
    {
        if (instance == null)
        {
            GD.PushWarning($"ObjectPool[{_config.Name}].Release: 尝试归还 null 实例");
            return false;
        }
        
        // Node 有效性检查
        if (instance is Node node && !GodotObject.IsInstanceValid(node))
        {
            GD.PushWarning($"ObjectPool[{_config.Name}].Release: 尝试归还无效实例");
            return false;
        }
        
        // 防止重复归还
        if (IsInPool(instance))
        {
            GD.PushWarning($"ObjectPool[{_config.Name}].Release: 实例已在池中");
            return true;
        }
        
        // Node 特殊处理
        if (instance is Node nodeInstance)
        {
            // 禁用处理
            nodeInstance.SetProcess(false);
            nodeInstance.SetPhysicsProcess(false);
            
            // 从场景树移除
            nodeInstance.GetParent()?.RemoveChild(nodeInstance);
            
            // 隐藏
            if (nodeInstance is CanvasItem canvasItem)
            {
                canvasItem.Visible = false;
            }
        }
        
        // 调用生命周期回调
        if (instance is IPoolable poolable)
        {
            poolable.OnPoolRelease();
        }
        else if (instance is Node n && n.HasMethod("OnPoolRelease"))
        {
            n.Call("OnPoolRelease");
        }
        
        if (_config.EnableStats)
        {
            _totalReleased++;
            _activeCount = Math.Max(0, _activeCount - 1);
        }
        
        // 检查池容量
        if (_config.MaxSize > 0 && _pool.Count >= _config.MaxSize)
        {
            // 池满，销毁多余对象
            if (_config.EnableStats) _totalDiscarded++;
            
            if (instance is Node n)
            {
                n.QueueFree();
            }
        }
        else
        {
            // 归还到池
            PrepareForPool(instance);
            _pool.Push(instance);
        }
        
        EmitSignal(SignalName.InstanceReleased, instance);
        return true;
    }
    
    /// <summary>
    /// 批量获取实例
    /// </summary>
    public List<T> AcquireBatch(Node parent, int count)
    {
        var result = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(Acquire(parent));
        }
        return result;
    }
    
    /// <summary>
    /// 批量归还实例
    /// </summary>
    public void ReleaseBatch(IEnumerable<T> instances)
    {
        foreach (var instance in instances)
        {
            Release(instance);
        }
    }
    
    /// <summary>
    /// 清理池中多余的对象，保留指定数量
    /// </summary>
    /// <param name="retainCount">要保留的数量</param>
    public void Cleanup(int retainCount)
    {
        retainCount = Math.Max(0, retainCount);
        
        while (_pool.Count > retainCount)
        {
            var instance = _pool.Pop();
            if (instance is Node node && GodotObject.IsInstanceValid(node))
            {
                node.QueueFree();
            }
            if (_config.EnableStats) _totalDiscarded++;
        }
    }
    
    /// <summary>
    /// 清空池，销毁所有缓存的实例
    /// </summary>
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            var instance = _pool.Pop();
            if (instance is Node node && GodotObject.IsInstanceValid(node))
            {
                node.QueueFree();
            }
        }
        EmitSignal(SignalName.PoolCleared);
    }
    
    /// <summary>
    /// 销毁对象池
    /// </summary>
    public void Destroy()
    {
        Clear();
        ObjectPoolManager.UnregisterPool(this);
    }
    
    /// <summary>
    /// 获取统计信息
    /// </summary>
    public PoolStats GetStats()
    {
        float reuseRate = 0f;
        if (_totalAcquired > 0)
        {
            var reused = _totalAcquired - (_totalCreated - _initialCreated);
            reuseRate = Math.Clamp((float)reused / _totalAcquired, 0f, 1f);
        }
        
        return new PoolStats
        {
            Name = _config.Name,
            PoolSize = _pool.Count,
            ActiveCount = _activeCount,
            TotalCount = TotalCount,
            PeakActive = _peakActive,
            TotalCreated = _totalCreated,
            TotalAcquired = _totalAcquired,
            TotalReleased = _totalReleased,
            TotalDiscarded = _totalDiscarded,
            ReuseRate = reuseRate
        };
    }
    
    #endregion

    #region 私有方法
    
    private T CreateInstance()
    {
        var instance = _factory();
        
        // 为 Node 设置元数据
        if (instance is Node node)
        {
            node.SetMeta("_object_pool", this);
            node.SetMeta("_in_pool", false);
        }
        
        if (_config.EnableStats) _totalCreated++;
        
        return instance;
    }
    
    private void PrepareForPool(T instance)
    {
        if (instance is Node node)
        {
            node.SetMeta("_in_pool", true);
            
            if (node is CanvasItem canvasItem)
            {
                canvasItem.Visible = false;
            }
        }
    }
    
    private void MarkAsActive(T instance)
    {
        if (instance is Node node)
        {
            node.SetMeta("_in_pool", false);
        }
    }
    
    private bool IsInPool(T instance)
    {
        if (instance is Node node)
        {
            return node.GetMeta("_in_pool", false).AsBool();
        }
        return false;
    }
    
    #endregion
}
