# 项目规则 - Godot 4.5 C# (.NET 8.0)

## 1. 核心工具类（默认使用）

### 1.1 日志系统 (Log)

```csharp
// 推荐用法：每个类声明一个静态实例
private static readonly Log _log = new Log("ClassName");

_log.Trace("细粒度追踪");  // [Conditional("DEBUG")]，Release 零开销
_log.Debug("调试信息");    // [Conditional("DEBUG")]
_log.Info("普通信息");
_log.Success("成功提示");
_log.Warn("警告");         // 自动推送到 Debugger 面板
_log.Error("错误");        // 自动推送到 Debugger 面板

// 全局配置
Log.GlobalLevel = LogLevel.Info;  // 发布版本建议 Info 或更高
Log.SetLevel("ClassName", LogLevel.Debug);  // 针对特定类调试
```

### 1.2 动态数据容器 (Data)

```csharp
// Node 扩展方法，自动管理生命周期
var data = node.GetData();
data.Set("HP", 100);
int hp = data.Get<int>("HP", 0);
data.Add("Score", 10);  // 累加
data.Multiply("Damage", 1.5f);  // 乘法

// 监听变化
data.On("HP", (oldVal, newVal) => { /* 处理 */ });
```

### 1.3 对象池 (ObjectPool)

**强制使用场景**: 子弹、伤害数字、特效、敌人（高频生成）

```csharp
// 初始化（在 _Ready 中）
private ObjectPool<Bullet> _bulletPool;

public override void _Ready()
{
    _bulletPool = new ObjectPool<Bullet>(
        () => BulletScene.Instantiate<Bullet>(),
        new ObjectPoolConfig { Name = "BulletPool", InitialSize = 50, MaxSize = 200 }
    );
}

// 获取
var bullet = _bulletPool.Spawn(this);

// 归还（推荐静态方法，对象无需持有池引用）
ObjectPoolManager.ReturnToPool(bullet);

// 实现 IPoolable 接口（可选）
public partial class Bullet : Area2D, IPoolable
{
    public void OnPoolAcquire() { /* 取出时重置状态 */ }
    public void OnPoolReset() { /* 归还时重置数据 */ }
}
```

## 2. C# 脚本规范

### 2.1 标准模板

```csharp
using Godot;
using System;

public partial class MyClass : Node
{
    private static readonly Log _log = new Log("MyClass");
    [Export] public float Speed { get; set; } = 400.0f;
}
```

### 2.2 关键规则

- **类定义**: `public partial class`，类名 = 文件名
- **命名空间**: 默认不使用（全局命名空间）
  - 测试代码必须用 `namespace BrotatoMy.Test`
  - 第三方库必须独立命名空间
- **事件**: 优先使用 C# 原生事件 `event Action<T>`，务必在 `_ExitTree` 解绑

## 3. 架构模式

### 3.1 组件化

- 优先组合而非继承
- 功能封装为独立 Node 组件（`HealthComponent`, `VelocityComponent`）

### 3.2 有限状态机 (FSM)

- 复杂逻辑必须使用状态机
- 推荐：纯 C# 类实现 `IState` 接口，不继承 Node

### 3.3 事件总线

```csharp
public static class EventBus
{
    public static event Action OnPlayerDied;
}
// 注意：静态事件必须在 _ExitTree 手动解绑
```

## 4. 性能与安全

### 4.1 Static 变量禁忌（重要）

- **严禁** `static` 变量存储 `Node`、`Resource` 或任何 `GodotObject`
- **安全**: 纯 C# 数据（string, int, struct, POCO）
- **后果**: 场景切换后 `ObjectDisposedException` 或内存泄漏

### 4.2 GC 优化（热路径）

**`_Process` / `_PhysicsProcess` 禁止**:

- `new` 引用类型（Class, List, Array, Delegate）
- 字符串拼接（`"Score: " + score`）
- LINQ（`Where`, `Select`）

**推荐**:

- 使用 `Vector2`, `Rect2`, `Color`（结构体，零 GC）
- 成员变量缓存集合，用 `Clear()` 复用

### 4.3 数据结构选择

- `List<T>`: 通用列表
- `Dictionary<K,V>`: 快速查找（O(1)）
- `HashSet<T>`: 去重/存在性判断
- `Queue<T>`: FIFO（对话系统）
- `Stack<T>`: LIFO（UI 层级）

## 5. 数据驱动

### 5.1 静态配置 (Resource)

```csharp
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public int Damage { get; set; }
}
// 编辑器创建 .tres 文件
```

### 5.2 动态存档 (JSON)

- 使用 POCO 类 + `System.Text.Json`
- 不序列化 Node 或 Resource

## 6. 文件命名规范 (统一 PascalCase)

- 场景 (.tscn): `PascalCase`
- 脚本 (.cs): `PascalCase`
- 资源: `PascalCase`
- 目录: `PascalCase`
