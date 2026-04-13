# NodeExtensions 节点扩展

## 概述

**路径**: `Src/Tools/Extensions/NodeExtensions.cs`

**核心价值**:
- 为 Node 提供 Data 挂载能力
- 实现 Entity-Component 关联
- 自动生命周期管理（ConditionalWeakTable）

## 快速开始

```csharp
// 扩展方法，调用简洁
enemy.GetData().Set("Reward", 100);

// 获取泛型数据
int hp = enemy.GetData().Get<int>("HP");

// 弱引用，不阻止 GC
// Node 被回收时，Data 自动清理
```

## 架构意义

这是伪 ECS 的核心粘合剂，让 Godot Node 具备了 ECS Entity 的数据挂载能力。通过 `ConditionalWeakTable` 实现了无侵入式的数据附加，无需修改 Godot 原生类或强制继承特定基类。
