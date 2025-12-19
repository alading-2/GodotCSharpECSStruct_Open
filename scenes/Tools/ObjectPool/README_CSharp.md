# ObjectPool C# 版本

基于 TypeScript 和 GDScript 版本设计的 Godot C# 对象池实现。

## 特性

- **泛型支持** - `ObjectPool<T>` 支持任意类型
- **双重创建方式** - 工厂函数或 PackedScene
- **生命周期回调** - `IPoolable` 接口或约定方法
- **全局管理器** - `ObjectPoolManager` 统一管理所有池
- **详细统计** - 复用率、峰值、创建/获取/归还/丢弃计数
- **信号系统** - 支持 Godot 信号机制

## 快速开始

### 1. 创建对象池

```csharp
// 方式一：使用 PackedScene（推荐用于 Node）
var bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
var bulletPool = new ObjectPool<Node>(bulletScene, new ObjectPoolConfig
{
    MaxSize = 100,
    InitialSize = 20,
    Name = "Bullets"
});

// 方式二：使用工厂函数（适用于非 Node 对象）
var vectorPool = new ObjectPool<Vector2>(() => new Vector2(), new ObjectPoolConfig
{
    MaxSize = 500,
    Name = "Vectors"
});
```

### 2. 获取和归还对象

```csharp
// 获取（Node 版本需要指定父节点）
var bullet = bulletPool.Acquire(parentNode);

// 归还方式一：通过池实例
bulletPool.Release(bullet);

// 归还方式二：静态方法（推荐，对象无需持有池引用）
ObjectPoolManager.ReturnToPool(bullet);
```

### 3. 实现 IPoolable 接口（可选）

```csharp
public partial class Bullet : Area2D, IPoolable
{
    private Vector2 _velocity;

    public void OnPoolAcquire()
    {
        // 对象被取出时调用，初始化状态
        _velocity = Vector2.Zero;
    }

    public void OnPoolRelease()
    {
        // 对象被归还时调用，清理状态
        _velocity = Vector2.Zero;
    }
}
```

或者使用约定方法（无需实现接口）：

```csharp
public partial class Bullet : Area2D
{
    // 对象池会自动调用这些方法（如果存在）
    public void OnPoolAcquire() { /* ... */ }
    public void OnPoolRelease() { /* ... */ }
}
```

## API 参考

### ObjectPool<T>

| 方法                                     | 说明                     |
| ---------------------------------------- | ------------------------ |
| `Acquire()`                              | 获取对象（非 Node）      |
| `Acquire(Node parent)`                   | 获取 Node 并添加到父节点 |
| `Release(T instance)`                    | 归还对象到池             |
| `AcquireBatch(Node parent, int count)`   | 批量获取                 |
| `ReleaseBatch(IEnumerable<T> instances)` | 批量归还                 |
| `Warmup(int count)`                      | 预热池                   |
| `Cleanup(int retainCount)`               | 清理多余对象             |
| `Clear()`                                | 清空池                   |
| `Destroy()`                              | 销毁池                   |
| `GetStats()`                             | 获取统计信息             |

| 属性             | 说明     |
| ---------------- | -------- |
| `PoolName`       | 池名称   |
| `AvailableCount` | 可用数量 |
| `ActiveCount`    | 活跃数量 |
| `TotalCount`     | 总数量   |
| `IsEmpty`        | 是否为空 |
| `IsFull`         | 是否已满 |

| 信号               | 说明             |
| ------------------ | ---------------- |
| `InstanceAcquired` | 实例被获取       |
| `InstanceReleased` | 实例被归还       |
| `PoolExhausted`    | 池空需创建新实例 |
| `PoolCleared`      | 池被清空         |

### ObjectPoolManager

| 方法                          | 说明             |
| ----------------------------- | ---------------- |
| `GetPool<T>(string name)`     | 按名称获取池     |
| `GetPool<T>()`                | 按类型获取池     |
| `ReturnToPool(Node instance)` | 静态归还方法     |
| `CleanupAll(int retainCount)` | 清理所有池       |
| `ClearAll()`                  | 清空所有池       |
| `DestroyAll()`                | 销毁所有池       |
| `GetAllStats()`               | 获取所有统计     |
| `PrintAllStats()`             | 打印统计到控制台 |

## 与 GDScript 版本对比

| 特性       | GDScript          | C#                        |
| ---------- | ----------------- | ------------------------- |
| 泛型       | ❌                | ✅                        |
| 接口约束   | 约定方法          | IPoolable 接口 + 约定方法 |
| 全局管理器 | ❌                | ✅ ObjectPoolManager      |
| 信号       | ✅                | ✅                        |
| 链式配置   | ✅                | ❌（使用配置对象）        |
| 静态归还   | ✅ return_to_pool | ✅ ReturnToPool           |

## 最佳实践

1. **预热池** - 在游戏开始时预热，避免运行时创建开销
2. **合理设置 MaxSize** - 根据实际需求设置，避免内存浪费
3. **使用静态归还** - `ObjectPoolManager.ReturnToPool()` 更简洁
4. **监控统计** - 定期检查复用率，优化池配置
5. **及时销毁** - 场景切换时调用 `DestroyAll()` 清理
