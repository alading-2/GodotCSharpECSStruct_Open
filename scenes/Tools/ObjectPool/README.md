# ObjectPool C# 版本

基于 TypeScript 和 GDScript 版本设计的 Godot 4.x C# 高性能对象池实现。专门针对 Godot Node 的生命周期进行了优化，支持自动处理 `ProcessMode` 和 `Visible`。

## 特性

- **泛型支持** - `ObjectPool<T>` 支持任意引用类型。
- **混合命名空间** - 核心工具类位于全局命名空间，无需 `using` 即可随时随地调用。
- **自动管理 Node** - 出池自动激活（`Inherit` + `Visible`），入池自动挂起（`Disabled` + `Invisible`）。
- **生命周期回调** - 通过 `IPoolable` 接口实现精准的对象重置逻辑。
- **全局管理器** - `ObjectPoolManager` 支持**静态归还**，对象无需持有池引用即可一键回池。
- **详细统计** - 实时追踪命中率、活跃数、闲置数及创建/销毁总量。
- **线程安全** - 内部使用 `lock` 确保管理器操作的安全性。

## 快速开始

### 1. 创建对象池

```csharp
// 推荐方式：在场景管理器的 _Ready 中创建
public partial class BulletManager : Node
{
    [Export] public PackedScene BulletScene;
    private ObjectPool<Bullet> _pool;

    public override void _Ready()
    {
        _pool = new ObjectPool<Bullet>(
            factory: () => BulletScene.Instantiate<Bullet>(),
            config: new ObjectPoolConfig
            {
                Name = "BulletPool",
                InitialSize = 50, // 初始预热，避免战斗开始卡顿
                MaxSize = 200    // 硬上限，防止内存溢出
            }
        );
    }
}
```

### 2. 获取和归还对象

```csharp
// 获取（自动添加到指定父节点，并处理 ProcessMode 和 Visible）
var bullet = _pool.Spawn(this);

// 批量获取
var bullets = _pool.SpawnBatch(this, 10);

// 归还方式 A：通过池实例（最高效）
_pool.Release(bullet);

// 归还方式 B：静态全局归还（最整洁，推荐在对象内部调用）
// 对象不需要持有池的引用
ObjectPoolManager.ReturnToPool(bullet);
```

### 3. 实现 IPoolable 接口（可选）

利用 C# 8.0 默认接口方法，你只需实现需要的钩子：

```csharp
public partial class Bullet : Area2D, IPoolable
{
    public void OnPoolAcquire()
    {
        // 从池中取出时重置物理状态
        GlobalPosition = Vector2.Zero;
    }

    public void OnPoolReset()
    {
        // 专门用于重置数值数据，在 Release 流程中自动调用
        _currentHp = MaxHp;
    }
}
```

## API 参考

### 核心 API (`ObjectPool<T>`)

| 方法                                 | 描述                                             |
| :----------------------------------- | :----------------------------------------------- |
| `Get()`                              | 获取一个纯对象（不处理 Godot 节点属性）。        |
| `Spawn(Node parent)`                 | 获取对象，自动处理显隐、处理模式并挂载到父节点。 |
| `SpawnBatch(Node parent, int count)` | 批量执行 `Spawn`。                               |
| `Release(T item)`                    | 归还对象到池中。                                 |
| `ReleaseBatch(IEnumerable<T> items)` | 批量归还对象。                                   |
| `Warmup(int count)`                  | 手动预热，提前实例化指定数量的对象。             |
| `Cleanup(int retainCount)`           | 清理闲置对象，仅保留指定数量。                   |
| `Clear()`                            | 销毁池内所有闲置对象。                           |
| `Destroy()`                          | 彻底销毁池并从管理器注销。                       |
| `GetStats()`                         | 获取命中率、活跃数等统计信息。                   |

### 事件 (Events)

| 事件                | 描述                        |
| :------------------ | :-------------------------- |
| `OnInstanceAcquire` | 当新对象被取出/创建时触发。 |
| `OnInstanceRelease` | 当对象成功归还入池时触发。  |

### ObjectPoolManager (全局管理器)

| 方法                            | 描述                                       |
| :------------------------------ | :----------------------------------------- |
| `ReturnToPool(object instance)` | **核心方法**：自动查找对象所属的池并归还。 |
| `GetAllStats()`                 | 获取所有已注册池的统计快照。               |
| `CleanupAll(int retainCount)`   | 批量清理所有池的闲置对象。                 |
| `DestroyAll()`                  | 一键清理所有池（建议在场景切换时调用）。   |

## 最佳实践

1.  **初始化时机**：务必在 `_Ready()` 中初始化 `ObjectPool`。因为 `[Export]` 的 `PackedScene` 在构造函数阶段尚未注入。
2.  **Reparent 注意事项**：`Spawn(parent)` 内部会执行 `Reparent`。在 Godot 中频繁跨节点挂载有一定开销。对于高频子弹，建议将 `BulletManager` 作为所有子弹的唯一父节点，仅切换显隐。
3.  **LIFO 优势**：本实现使用 `Stack` (后进先出)。这保证了刚归还的“热”对象被优先复用，对 CPU 缓存更友好。
4.  **接口默认实现**：`IPoolable` 提供了默认空实现。如果你只需要重置 HP，只需写 `OnPoolReset`，无需写 `OnPoolAcquire`。
