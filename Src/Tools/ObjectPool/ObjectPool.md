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

## 快速开始

### 1. 全局初始化 (推荐)

项目采用集中式初始化模式，通过 `ObjectPoolInit.cs` (AutoLoad) 统一管理核心对象池。

```csharp
// 在 Data/ObjectPool/ObjectPoolInit.cs 中
public override void _Ready()
{
    // 使用 PoolNames 常量避免字符串硬编码
    var scene = GD.Load<PackedScene>("res://Src/ECS/Entities/Enemies/Enemy.tscn");
    new ObjectPool<Node>(
        () => scene.Instantiate(),
        new ObjectPoolConfig { Name = PoolNames.EnemyPool, InitialSize = 100, MaxSize = 500 }
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
var pool = ObjectPoolManager.GetPool(PoolNames.EnemyPool) as ObjectPool<Node>;
var enemy = pool?.Spawn();

// 方式 B: 静态全局归还 (最整洁，推荐)
// 对象不需要持有池的引用，管理器会自动查找它属于哪个池
ObjectPoolManager.ReturnToPool(enemy);
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

### 1. 自动初始化时机 (AutoLoad Timing)

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
| `Warmup(int count)`                  | 手动预热，提前实例化指定数量的对象。                 |
| `Cleanup(int retainCount)`           | 清理闲置对象，仅保留指定数量。                       |
| `Clear()`                            | 销毁池内所有闲置对象。                               |
| `GetStats()`                         | 获取命中率、活跃数等统计信息。                       |

### ObjectPoolManager (全局管理器)

| 方法                            | 描述                                             |
| :------------------------------ | :----------------------------------------------- |
| `GetPool(string name)`          | 根据名称查找已注册的对象池接口 (`IObjectPool`)。 |
| `ReturnToPool(object instance)` | **核心方法**：自动查找对象所属的池并归还。       |
| `GetAllStats()`                 | 获取所有已注册池的统计快照。                     |
| `CleanupAll(int retainCount)`   | 批量清理所有池的闲置对象。                       |
| `DestroyAll()`                  | 一键清理所有池（建议在场景切换时调用）。         |

## 项目规范建议

1.  **名称管理**：所有全局池名称必须定义在 `PoolNames` 结构体中。
2.  **初始化**：核心系统的对象池（敌人、玩家、掉落物）应在 `ObjectPoolInit` 中完成预热。
3.  **回收**：优先在对象自身的死亡逻辑中调用 `ObjectPoolManager.ReturnToPool(this)`。
4.  **性能**：对象池会自动处理节点的挂载逻辑。对于极高频率（如每秒百发）的子弹，建议在配置中指定合理的 `ParentPath`。
