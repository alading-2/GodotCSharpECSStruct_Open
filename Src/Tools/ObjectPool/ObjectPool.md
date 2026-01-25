# ObjectPool C# 版本

基于 TypeScript 和 GDScript 版本设计的 Godot 4.x C# 高性能对象池实现。专门针对 Godot Node 的生命周期进行了优化，支持自动处理 `ProcessMode` 和 `Visible`。

## 特性

- **泛型支持** - `ObjectPool<T>` 支持任意引用类型。
- **混合命名空间** - 核心工具类位于全局命名空间，无需 `using` 即可随时随地调用。
- **自动管理 Node** - 出池自动激活（`Inherit` + `Visible`），入池自动挂起（`Disabled` + `Invisible`）。
- **生命周期回调** - 通过 `IPoolable` 接口实现精准的对象重置逻辑。
- **全局管理器** - `ObjectPoolManager` 自动管理所有创建的池，支持**静态归还**与**按名查找**。
- **详细统计** - 实时追踪命中率、活跃数、闲置数及创建/销毁总量。
- **线程安全** - 内部使用 `lock` 确保管理器操作的安全性。

## 文件组织

- `ObjectPool.cs`: 核心对象池逻辑与 `IPoolable` 接口定义。
- `ObjectPoolManager.cs`: 全局池管理逻辑，负责池的注册、查找与静态归还。
- `ParentManager.cs`: 内部工具类，负责处理池化节点的父级挂载逻辑。
- `ObjectPoolInit.cs`: 推荐的全局初始化入口（AutoLoad）。

## 重要限制

### 静态归还仅支持 Node 对象

`ObjectPoolManager.ReturnToPool()` 方法**仅支持 Godot Node 对象**的自动归还，因为它依赖 `Node.Data` 容器存储池名称。

**支持的用法**：

```csharp
// ✅ Node 对象 - 支持静态归还
var enemy = enemyPool.Get();
ObjectPoolManager.ReturnToPool(enemy);  // 自动查找池并归还
```

**不支持的用法**：

```csharp
// ❌ 纯 C# 类 - 不支持静态归还
public class MyData { }
var pool = new ObjectPool<MyData>(() => new MyData(), config);
var data = pool.Get();
ObjectPoolManager.ReturnToPool(data);  // 会报错！
```

**纯 C# 类的正确用法**：

```csharp
// ✅ 直接调用池的 Release 方法
var pool = new ObjectPool<MyData>(() => new MyData(), config);
var data = pool.Get();
pool.Release(data);  // 必须持有池引用
```

**设计原因**：

- 移除了冗余的 `_instanceToPoolName` 字典，性能更优
- Godot 游戏中 99% 的池化对象都是 Node（Enemy、Bullet、Item）
- 纯 C# 类池化场景极少，直接持有池引用更简洁

## 快速开始

### 1. 全局初始化 (推荐)

项目采用集中式初始化模式，通过 `ObjectPoolInit.cs` (AutoLoad) 统一管理核心对象池。

```csharp
// 在 Data/ObjectPool/ObjectPoolInit.cs 中
public override void _Ready()
{
    // 使用 PoolNames 常量避免字符串硬编码
    new ObjectPool<EnemyEntity>(
        () => (EnemyEntity)ResourceManagement.LoadScene<EnemyEntity>().Instantiate(),
        new ObjectPoolConfig
        {
            Name = ObjectPoolNames.EnemyPool,
            InitialSize = 100,
            MaxSize = 500,
            ParentPath = "ECS/Entity/Enemy"
        }
    );
}
```

### 2. 局部创建 (可选)

```csharp
// 在特定脚本中手动创建，创建后会自动注册到管理器
var pool = new ObjectPool<Bullet>(
    factory: () => BulletScene.Instantiate<Bullet>(),
    config: new ObjectPoolConfig { Name = "BulletPool", InitialSize = 50 }
);
```

### 3. 获取和归还对象

```csharp
// 方式 A: 通过管理器查找 (解耦)
var pool = ObjectPoolManager.GetPool<Node>(PoolNames.EnemyPool);
var enemy = pool?.Get();

// 方式 B: 静态全局归还 (最整洁，推荐 - 仅限 Node 对象)
// 对象不需要持有池的引用，管理器会自动从 Node.Data 中查找池名称
ObjectPoolManager.ReturnToPool(enemy);

// 方式 C: 直接调用池的 Release (适用于所有类型)
pool.Release(enemy);
```

## IPoolable 接口

实现此接口可获得精准的生命周期控制：

```csharp
public partial class Enemy : CharacterBody2D, IPoolable
{
    public void OnPoolAcquire()
    {
        // 从池中取出时：重置状态、播放出生动画等
    }

    public void OnPoolReset()
    {
        // 归还到池中时：重置 HP、清理特效引用等
        Health = MaxHealth;
    }
}
```

## 常见问题与技术细节

### 1. 为什么 ObjectPoolInit 使用 \_EnterTree 而非 \_Ready？

**关键时序问题**：

```
AutoLoad 加载顺序（按 Priority）：
1. ObjectPoolInit._EnterTree()  ← 必须在这里初始化对象池
2. TimerManager._EnterTree()    ← 在这里获取对象池
3. ObjectPoolInit._Ready()      ← 如果在这里初始化就太晚了！
4. TimerManager._Ready()
```

**原因**：

- Godot 的生命周期是：所有节点的 `_EnterTree()` 执行完后，才开始执行 `_Ready()`
- 如果 ObjectPoolInit 在 `_Ready()` 中初始化，其他系统在 `_EnterTree()` 中就无法获取对象池
- 使用 `_EnterTree()` 确保对象池在所有系统需要时已经就绪

**Priority 的作用**：

- `Priority.System - 10` 确保 ObjectPoolInit 比其他系统先进入场景树
- 但只有在 `_EnterTree()` 中初始化，才能保证时序正确

### 2. 自动初始化时机 (AutoLoad Timing)

**现象**：在 `ObjectPoolInit` (AutoLoad) 的 `_Ready` 中创建 `ParentPath` 时，场景树中不显示节点。
**原因**：Godot 的 AutoLoad 节点在主场景加载前初始化。此时 `SceneTree.Root` 虽然存在，但立即同步调用 `AddChild` 可能会因为引擎内部的状态机尚未切换到“就绪”而导致节点挂载失败或在远程调试器中不可见。
**解决方案**：

- 推荐在 `_Ready` 中使用 `CallDeferred(nameof(MyInitMethod))`。
- 确保初始化逻辑在引擎完成首帧设置后执行。

### 2. 节点层级管理 (ParentManager)

为了保持场景树整洁并防止场景切换时对象池被销毁，`ParentManager` 默认将对象池挂载在 `/root` 下：

- **全局持久化**：挂载在 `root` 下的节点不会随 `CurrentScene` 的切换而销毁。
- **唯一性**：通过 `ObjectPoolConfig.Name` 和 `Guid` 确保每个对象在场景中都有唯一标识，避免 Godot 默认命名导致的挂载冲突。

## API 参考

### 核心 API (`ObjectPool<T>`)

| 方法                                 | 描述                                                 |
| :----------------------------------- | :--------------------------------------------------- |
| `Get()`                              | 获取一个纯对象（不处理 Godot 节点属性）。            |
| `Spawn()`                            | 获取对象，自动处理显隐、处理模式并挂载到预设父节点。 |
| `SpawnBatch(int count)`              | 批量执行 `Spawn`。                                   |
| `Release(T item)`                    | 归还对象到池中。                                     |
| `ReleaseBatch(IEnumerable<T> items)` | 批量归还对象。                                       |
| `ReleaseAll()`                       | 回收当前池中所有正在外部使用的对象。                 |
| `GetActiveSnapshot()`                | **新增**: 获取所有活跃对象的只读快照列表。           |
| `ForEachActive(Action<T>)`           | **新增**: 对所有活跃对象执行操作（内部使用快照）。   |
| `Warmup(int count)`                  | 手动预热，提前实例化指定数量的对象。                 |
| `Cleanup(int retainCount)`           | 清理闲置对象，仅保留指定数量。                       |
| `Clear()`                            | 销毁池内所有闲置对象。                               |
| `GetStats()`                         | 获取命中率、活跃数等统计信息。                       |

### ObjectPoolManager (全局管理器)

| 方法                            | 描述                                                               |
| :------------------------------ | :----------------------------------------------------------------- |
| `GetPool<T>(string name)`       | 根据名称查找已注册的对象池（泛型版本，类型安全）。                 |
| `ReturnToPool(object instance)` | **核心方法**：自动查找对象所属的池并归还（**仅支持 Node 对象**）。 |
| `GetAllStats()`                 | 获取所有已注册池的统计快照。                                       |
| `CleanupAll(int retainCount)`   | 批量清理所有池的闲置对象。                                         |
| `DestroyAll()`                  | 一键清理所有池（建议在场景切换时调用）。                           |

## 项目规范建议

1.  **名称管理**：所有全局池名称必须定义在 `PoolNames` 结构体中。
2.  **初始化**：核心系统的对象池（敌人、玩家、掉落物）应在 `ObjectPoolInit` 中完成预热。
3.  **回收**：
    - **Node 对象**：优先在对象自身的死亡逻辑中调用 `ObjectPoolManager.ReturnToPool(this)`。
    - **纯 C# 类**：必须持有池引用并调用 `pool.Release(obj)`。
4.  **性能**：对象池会自动处理节点的挂载逻辑。对于极高频率（如每秒百发）的子弹，建议在配置中指定合理的 `ParentPath`。
5.  **类型选择**：本项目主要池化 Node 对象（Enemy、Bullet、Item），极少需要池化纯 C# 类。
