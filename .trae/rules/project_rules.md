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
- **唯一标识符**: 动态创建 Node 或对象且需要唯一名称时，优先使用 `System.Guid.NewGuid()` 以避免命名冲突

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

## 7. 文档规范

### 7.1 文档分层原则

项目文档分为两个层次，职责明确：

**Docs/ 目录（概念层）**：

- **定位**：架构设计、概念说明、决策记录、AI 提示词
- **受众**：架构师、新成员、AI 助手
- **内容**：
  - 为什么这样设计（设计动机）
  - 架构理念和哲学
  - 技术选型的权衡
  - 常见误区和最佳实践
  - 系统间的协作关系
- **特点**：偏理论、重思想、讲原理
- **示例**：`Docs/框架/ECS/Entity/Entity架构设计理念.md`

**Src/ 目录（实现层）**：

- **定位**：代码说明、API 文档、使用指南
- **受众**：开发者、维护者
- **内容**：
  - 如何使用（API 接口）
  - 参数说明和返回值
  - 代码示例和模板
  - 注意事项和陷阱
  - 快速开始指南
- **特点**：偏实践、重操作、讲用法
- **示例**：`Src/ECS/Entity/Core/README.md`

### 7.2 文档命名规范

**Docs/ 目录**：

- 设计文档：`{模块名}架构设计.md`、`{模块名}设计理念.md`
- 决策记录：`{模块名}技术选型.md`、`{模块名}修改说明.md`
- 概念说明：`{概念名}深度解析.md`

**Src/ 目录**：

- 统一使用 `README.md`（每个模块一个）
- 内容包含：概述、快速开始、API 文档、示例代码、注意事项

### 7.3 文档更新原则

1. **架构变更**：先更新 Docs/ 的设计文档，再更新 Src/ 的 README
2. **API 变更**：直接更新 Src/ 的 README，必要时同步 Docs/ 的示例
3. **概念澄清**：更新 Docs/ 的设计文档
4. **使用问题**：更新 Src/ README 的注意事项部分

## 8. 索引维护规则

### 8.1 索引同步 (Mandatory)

凡是涉及以下目录的文件变更，必须同步更新对应的索引类：

- **ECS 索引 (`ECSIndex.cs`)**:
  - 变更范围：`Src/ECS/Entity/` 或 `Src/ECS/Component/` 下的 `.tscn` 场景文件。
  - 操作：更新 `ECSIndex` 类中的常量定义及 `_nameToPathMap` 映射。
- **数据资源索引 (`DataResourceIndex.cs`)**:
  - 变更范围：`Data/Resources/` 下的 `.tres` 资源文件。
  - 操作：更新 `DataResourceIndex` 类中的 `_pathRegistry` 映射，确保简写名称与路径一致。

### 8.2 维护目的

- **解耦**: 业务代码通过简写或常量访问资源，无需关心物理路径。
- **安全**: 统一管理路径，方便在重构目录结构时进行全局替换和验证。
- **性能**: 通过索引进行预加载或缓存管理。
