# NodeExtensions 模式

## 概述

`NodeExtensions` 是本项目中用于扩展 Godot `Node` 功能的核心工具类。它允许我们在不修改 Godot 引擎源码、不使用复杂的继承体系的情况下，为任意 Node 动态挂载数据和行为。

## 核心机制

该模式基于 C# 的两个强大特性：

### 1. 扩展方法 (Extension Methods)

扩展方法允许我们向现有类型 "添加" 方法。在 `NodeExtensions` 中，我们定义了 `GetData(this Node node)`，这使得我们可以在任何 Node 实例上直接调用 `node.GetData()`，就像它是 Node 原生方法一样。

**优势**:

- **零侵入**: 不需要修改 Godot 源码。
- **通用性**: 适用于所有 Node 及其子类 (Node2D, Control, Spatial 等)。
- **简洁性**: 调用语法自然流畅。

### 2. 条件弱表 (ConditionalWeakTable)

`ConditionalWeakTable<TKey, TValue>` 是 .NET 提供的一个特殊集合，用于将附加状态与对象关联。

**原理**:

- **弱引用键**: 它持有 Key (即 Node) 的弱引用。这意味着 `_nodeDataMap` 不会阻止 Node 被垃圾回收 (GC)。
- **生命周期绑定**: 当 Key (Node) 被 GC 回收时，对应的 Value (Data) 也会自动从表中移除并被回收。

**解决了什么问题**:

- **内存泄漏**: 如果使用普通的 `Dictionary<Node, Data>`，必须手动处理 Node 的销毁事件 (`TreeExiting`) 来移除数据，否则 Dictionary 会一直持有 Node 的引用，导致内存泄漏。`ConditionalWeakTable` 自动处理了这个问题。
- **动态属性**: 实现了类似于动态语言 (Lua/Python) 为对象动态添加属性的能力。

## 使用示例

### 1. 挂载和访问数据

```csharp
// 获取 Node 关联的数据容器
var data = myNode.GetData();

// 写入数据
data.Set("health", 100);
data.Set("team_id", 1);

// 读取数据
int hp = data.Get<int>("health");
```

### 2. 与对象池配合

在对象池中，我们利用此机制存储元数据：

```csharp
// 标记对象属于哪个池
node.GetData().Set("_object_pool", this);

// 标记对象当前状态
node.GetData().Set("_in_pool", true);
```

### 3. 类型安全的扩展

我们可以在此基础上封装更高级的扩展方法：

```csharp
public static class GameNodeExtensions
{
    public static void SetTeam(this Node node, int teamId)
    {
        node.GetData().Set("team_id", teamId);
    }

    public static int GetTeam(this Node node)
    {
        return node.GetData().Get<int>("team_id");
    }
}
```

## 最佳实践

1. **避免滥用**: 虽然可以挂载任意数据，但对于核心逻辑，仍推荐使用强类型的 Component (C# 脚本)。`GetData` 更适合存储临时状态、元数据或解耦系统间的数据交换。
2. **Key 命名规范**: 使用字符串 Key 时，建议使用 `snake_case` 或 `PascalCase` 并保持统一。系统级 Key 建议以 `_` 开头 (如 `_in_pool`)。
3. **性能注意**: `ConditionalWeakTable` 的查找性能略低于普通 Dictionary，但在非热路径 (Update/Process) 下完全可以接受。对于极高频访问的数据，请使用成员变量。
