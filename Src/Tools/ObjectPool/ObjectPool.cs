using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// 采用混合命名空间策略：核心工具类放在全局命名空间，方便调用

/// <summary>
/// 对象池统计信息结构体
/// 用于追踪对象池的运行状态、复用效率和内存占用情况
/// </summary>
public struct PoolStats
{
    /// <summary> 池的唯一识别名称 </summary>
    public string? PoolName;
    /// <summary> 当前池内闲置待命的对象数量 </summary>
    public int Count;
    /// <summary> 当前正在外部被使用的活跃对象数量 </summary>
    public int ActiveCount;
    /// <summary> 自创建以来，该池总共通过工厂方法实例化的对象总数 </summary>
    public int TotalCreated;
    /// <summary> 自创建以来，外部总共从池中获取对象的次数 </summary>
    public int TotalAcquired;
    /// <summary> 自创建以来，外部成功归还对象到池中的次数 </summary>
    public int TotalReleased;
    /// <summary> 因达到 MaxSize 容量限制而被直接销毁的对象总数 </summary>
    public int TotalDiscarded;

    /// <summary>
    /// 计算池的命中率（0.0 - 1.0）
    /// 命中率越高表示对象复用效率越高
    /// </summary>
    public float HitRate => TotalAcquired > 0 ? (float)(TotalAcquired - TotalCreated) / TotalAcquired : 0;
}

/// <summary>
/// 对象池配置参数
/// 使用结构体以减少 GC 压力，建议在创建池时直接初始化
/// </summary>
public struct ObjectPoolConfig
{
    /// <summary> 初始预热大小。在构造时会预先创建此数量的对象，建议设置为场景平均使用量 </summary>
    public int InitialSize = 10;
    /// <summary> 池的最大容量上限。超过此数量的对象在归还时会被销毁，防止内存无限增长 </summary>
    public int MaxSize = 100;
    /// <summary> 池的名称。用于日志输出、统计区分以及 ObjectPoolManager 的全局检索 </summary>
    public string? Name;
    /// <summary> 池对象的父节点路径。默认为 "ECS/Node"。ObjectPool 会自动将新创建的对象挂载到此节点下。 </summary>
    public string ParentPath = "ECS/Node";
    /// <summary> 是否启用统计信息收集。关闭可微弱提升性能 </summary>
    public bool EnableStats = true;

    /// <summary>
    /// 获取默认配置
    /// </summary>
    public ObjectPoolConfig() { }
}

/// <summary>
/// 可池化对象接口
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// [初始化] 对象被取出时调用 (Spawn/Get)
    /// 相当于 _Ready() 或 Init()，用于设置初始状态（位置、数值等）
    /// </summary>
    void OnPoolAcquire() { }

    /// <summary>
    /// [清理] 对象被归还时调用 (Release)
    /// 相当于 QueueFree() 前的清理，用于停止特效、移除引用、解绑事件等
    /// </summary>
    void OnPoolRelease() { }

    /// <summary>
    /// [重置] 数据重置
    /// 在 OnPoolRelease 之后调用，专门用于将数据恢复为默认值（如 HP=Max, Score=0）
    /// </summary>
    void OnPoolReset() { }
}

// IObjectPool 接口已移除
// 原因：非泛型接口无法提供类型安全的 Get() 方法
// 新方案：ObjectPoolManager 使用 Dictionary<string, object> + 泛型方法

/// <summary>
/// 通用对象池实现
/// 专门针对 Godot 引擎优化，自动处理 Node 的生命周期和处理模式
/// </summary>
/// <typeparam name="T">池化对象类型，必须是引用类型（class）</typeparam>
public class ObjectPool<T> where T : class
{
    // 使用 Stack 而非 List，因为出池/入池操作在栈顶执行效率最高 (O(1))
    // 且具有更好的 CPU 缓存亲和性 (LIFO - 最近归还的对象最热)
    private readonly Stack<T> _stack;

    // 对象实例化工厂。对于 Node，通常是 () => scene.Instantiate<T>()
    private readonly Func<T> _createFunc;

    // 额外的重置逻辑回调。在 IPoolable.OnPoolRelease 之后执行
    private readonly Action<T>? _resetMethod;

    // 池配置引用
    private readonly ObjectPoolConfig _config;

    // 内部统计数据
    private PoolStats _stats;

    // 追踪当前活跃的对象集合，用于支持 ReleaseAll
    private readonly HashSet<T> _activeItems = new();

    /// <summary> 当对象被成功获取出池时触发 </summary>
    public event Action<T>? OnInstanceAcquire;
    /// <summary> 当对象被归还入池时触发 </summary>
    public event Action<T>? OnInstanceRelease;

    /// <summary> 获取池的名称 </summary>
    public string PoolName => _config.Name ?? "UnnamedPool";
    /// <summary> 获取池内对象的具体 Type </summary>
    public Type ItemType => typeof(T);
    /// <summary> 当前池内闲置对象的数量 </summary>
    public int Count => _stack.Count;
    /// <summary> 当前正在外部使用的对象数量 </summary>
    public int ActiveCount => _stats.ActiveCount;

    private static readonly Log _log = new Log("ObjectPool");
    /// <summary>
    /// 初始化对象池
    /// </summary>
    /// <param name="createFunc">
    /// 创建新对象的工厂函数（必须提供）。
    /// 对于 Node，通常是 () => scene.Instantiate&lt;T&gt;()。
    /// 对于纯类，通常是 () => new T()。
    /// </param>
    /// <param name="config">配置参数（包含名称、容量、预热等）</param>
    /// <param name="resetMethod">
    /// [可选] 对象归还时的自定义重置委托。
    /// 会在 IPoolable.OnPoolRelease 之后，IPoolable.OnPoolReset 之前执行。
    /// </param>
    public ObjectPool(Func<T> createFunc, ObjectPoolConfig config, Action<T>? resetMethod = null)
    {
        _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
        _config = config;

        _resetMethod = resetMethod;
        _stack = new Stack<T>(config.InitialSize);

        // 初始化统计信息
        _stats = new PoolStats
        {
            PoolName = config.Name ?? "UnnamedPool",
            Count = 0,
            ActiveCount = 0,
            TotalCreated = 0,
            TotalAcquired = 0,
            TotalReleased = 0,
            TotalDiscarded = 0
        };

        // 自动注册父节点
        if (!string.IsNullOrEmpty(_config.Name) && !string.IsNullOrEmpty(_config.ParentPath))
        {
            ParentManager.Instance.RegisterParent(_config.Name, _config.ParentPath);
        }

        // 构造时直接预热，将性能开销放在初始化阶段
        Warmup(config.InitialSize);
        // 自动注册到全局管理器
        ObjectPoolManager.Register(this);
    }

    /// <summary>
    /// 预先填充池
    /// 用于在游戏加载阶段提前分配内存和实例化对象，避免战斗中卡顿
    /// </summary>
    /// <param name="count">预热对象的数量</param>
    public void Warmup(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Count >= _config.MaxSize) break;
            var obj = CreateNew();
            PushToStack(obj);
        }
    }

    /// <summary> 内部：创建新对象并记录统计 </summary>
    private T CreateNew()
    {
        var result = _createFunc();

        if (result is Node node)
        {
            // 为新生成的节点分配唯一名称，避免 Godot 默认命名导致在场景树中难以定位或冲突
            node.Name = $"{_config.Name}_{Guid.NewGuid().ToString()[..8]}";

            // 存储池名称而非池引用，避免循环引用和类型转换问题
            // 使用 Godot 原生 Meta 数据存储，解耦 Data 系统
            node.SetMeta("ObjectPoolName", _config.Name);
            node.SetMeta("InPool", false);

            // 自动挂载到父节点
            if (node.GetParent() == null && !string.IsNullOrEmpty(_config.Name))
            {
                var parent = ParentManager.Instance.GetParent(_config.Name);
                if (parent != null)
                {
                    parent.AddChild(node);
                }
            }
        }

        _stats.TotalCreated++;
        return result;
    }

    /// <summary> 内部：将对象推入闲置栈，并执行 Godot 挂起逻辑 </summary>
    private void PushToStack(T obj)
    {
        if (obj is Node node)
        {
            node.SetMeta("InPool", true);
            // 关键：停止节点的所有处理逻辑（PhysicsProcess, Process, Input 等）
            node.ProcessMode = Node.ProcessModeEnum.Disabled;
            // 隐藏节点，避免渲染开销
            if (node is CanvasItem item) item.Visible = false;
        }
        _stack.Push(obj);
        _stats.Count = _stack.Count;
    }

    /// <summary>
    /// [获取] 从池中获取一个可用对象 (纯数据获取)
    /// 如果池为空，则自动创建一个新对象
    /// </summary>
    /// <returns>获取到的对象实例</returns>
    public T Get()
    {
        T obj;
        if (_stack.Count > 0)
        {
            obj = _stack.Pop();
            _stats.Count = _stack.Count;
        }
        else
        {
            obj = CreateNew();
        }

        _stats.ActiveCount++;
        _stats.TotalAcquired++;

        // 追踪活跃对象
        _activeItems.Add(obj);
        // 注册对象归属映射 (支持非 Node 对象的静态归还)
        ObjectPoolManager.MapObject(obj, PoolName);

        // 执行 Godot 激活逻辑
        if (obj is Node node)
        {
            // 标记对象已出池
            node.SetMeta("InPool", false);

            // 恢复节点的处理能力
            node.ProcessMode = Node.ProcessModeEnum.Inherit;
            // 显示节点
            if (node is CanvasItem item) item.Visible = true;
        }

        // 生命周期回调
        if (obj is IPoolable poolable) poolable.OnPoolAcquire();
        OnInstanceAcquire?.Invoke(obj);

        return obj;
    }

    /// <summary>
    /// [生成] 从池中获取一个对象并激活
    /// 语义化方法，内部直接调用 Get()
    /// </summary>
    public T Spawn() => Get();

    /// <summary>
    /// [批量生成] 批量获取对象 (纯数据)
    /// </summary>
    /// <param name="count">获取数量</param>
    /// <returns>包含所有获取对象的列表</returns>
    public List<T> SpawnBatch(int count)
    {
        var list = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(Get());
        }
        return list;
    }



    /// <summary>
    /// [归还] 将对象归还到池中
    /// 如果池已满（超过 MaxSize），则对象会被直接销毁
    /// 归还流程：
    /// 1. 解除 Manager 注册
    /// 2. 调用 IPoolable.OnPoolRelease (清理)
    /// 3. 调用 resetMethod (外部自定义重置)
    /// 4. 调用 IPoolable.OnPoolReset (数据重置)
    /// 5. 触发 OnInstanceRelease 事件
    /// 6. 推入栈或销毁
    /// </summary>
    /// <param name="obj">要归还的对象（如果为 null 则忽略）</param>
    public void Release(T obj)
    {
        if (obj == null) return;

        // 1. 检查是否已经在池中 (对标 GDScript 的 meta 检查)
        if (obj is Node node && node.HasMeta("InPool") && node.GetMeta("InPool").AsBool())
        {
            _log.Warn($"{PoolName}: 实例 {obj} 已在池中。已忽略。");
            return;
        }

        // 2. 更新统计
        _stats.ActiveCount--;
        _stats.TotalReleased++;

        // 移除活跃追踪
        _activeItems.Remove(obj);
        // 解除对象归属映射
        ObjectPoolManager.UnmapObject(obj);

        // 3. 生命周期回调: 清理
        if (obj is IPoolable poolable) poolable.OnPoolRelease();

        // 4. 外部重置回调
        _resetMethod?.Invoke(obj);

        // 5. 生命周期回调: 重置数据
        if (obj is IPoolable poolableReset) poolableReset.OnPoolReset();

        OnInstanceRelease?.Invoke(obj);

        // 容量检查
        if (_stack.Count >= _config.MaxSize)
        {
            _stats.TotalDiscarded++;
            Discard(obj);
            return;
        }

        PushToStack(obj);
    }

    // 接口实现已移除（IObjectPool 接口已废弃）

    /// <summary>
    /// 批量归还对象
    /// </summary>
    public void ReleaseBatch(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Release(item);
        }
    }

    private void Discard(T obj)
    {
        if (obj is Node node) node.QueueFree();
        else if (obj is IDisposable disposable) disposable.Dispose();
    }

    /// <summary>
    /// 清理池中多余对象，保留指定数量
    /// </summary>
    public void Cleanup(int retainCount)
    {
        int targetCount = Math.Max(0, retainCount);
        while (_stack.Count > targetCount)
        {
            var obj = _stack.Pop();
            Discard(obj);
        }
        _stats.Count = _stack.Count;
    }

    /// <summary>
    /// 清空池（销毁池内所有对象）
    /// </summary>
    public void Clear()
    {
        foreach (var obj in _stack)
        {
            Discard(obj);
        }
        _stack.Clear();
        _stats.Count = 0;
        // 注意：无法销毁外部活跃的对象
    }

    /// <summary>
    /// 获取所有活跃对象的只读快照
    /// 注意：返回的是快照列表，不会因为后续的 Get/Release 操作而改变
    /// </summary>
    /// <returns>活跃对象的列表副本</returns>
    public List<T> GetActiveSnapshot()
    {
        return _activeItems.ToList();
    }

    /// <summary>
    /// 对所有活跃对象执行指定操作（安全版本）
    /// 内部会先创建快照，避免在遍历时修改集合导致异常
    /// </summary>
    /// <param name="action">要执行的操作</param>
    public void ForEachActive(Action<T> action)
    {
        if (action == null) return;

        // 复制一份列表，防止在遍历时修改集合
        var snapshot = GetActiveSnapshot();
        foreach (var item in snapshot)
        {
            action(item);
        }
    }

    /// <summary>
    /// 回收所有当前活跃的对象
    /// 适用于：波次结束清场、切换场景、游戏结束
    /// </summary>
    public void ReleaseAll()
    {
        // 必须先复制一份列表，因为 Release 会修改 _activeItems 集合
        var itemsToRelease = _activeItems.ToList();
        foreach (var item in itemsToRelease)
        {
            Release(item);
        }
        _activeItems.Clear();
    }

    /// <summary>
    /// 销毁池
    /// </summary>
    public void Destroy()
    {
        Clear();
        ObjectPoolManager.Unregister(this);
    }

    /// <summary>
    /// 获取当前池的统计信息
    /// </summary>
    public PoolStats GetStats()
    {
        return _stats;
    }
}
