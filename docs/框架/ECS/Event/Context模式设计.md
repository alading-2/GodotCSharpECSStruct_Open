# Context Object Pattern（上下文对象模式）

## 概述

**Context Object Pattern** 是事件驱动架构中实现"请求-响应"的标准模式。它解决了一个核心问题：

> **事件系统天然是单向的（发布者 → 订阅者），如何让订阅者向发布者"返回"数据？**

---

## 模式来源

此模式在多个现代框架中都有应用：

| 框架 | 实现名称 | 核心功能 |
|------|---------|---------|
| **DOM Event** | `Event.preventDefault()` | 阻止默认行为 |
| **Unreal GAS** | `FGameplayEffectContext` | 携带技能执行上下文 |
| **Unity DOTS** | `EntityCommandBuffer` | 收集命令稍后执行 |
| **ASP.NET Core** | `HttpContext` | 请求响应的上下文容器 |

---

## 核心原理

```
发布者 (AbilitySystem)                    订阅者 (Component)
        │                                         │
        │  1. 创建 Context 对象                   │
        │  2. Emit(event, Context) ──────────▶   │
        │                                         │ 3. 修改 Context
        │  4. 读取 Context.Result  ◀──────────   │
        ▼                                         ▼
```

**关键点**：
- Context 是**引用类型**（class），所有订阅者操作同一实例
- 发布者在 Emit 后立即读取 Context 的结果
- 多个订阅者可以**依次修改**同一 Context

---

## 为什么不用返回值？

传统事件系统使用 `void` 返回类型：

```csharp
// ❌ 传统事件 - 无法获取返回值
public delegate void DamageHandler(float amount);
public event DamageHandler OnDamage;

OnDamage?.Invoke(50);  // 无法知道伤害是否被抵挡
```

使用 Context 模式：

```csharp
// ✅ Context 模式 - 可获取处理结果
public class DamageContext
{
    public float OriginalDamage { get; }
    public float FinalDamage { get; set; }
    public bool IsBlocked { get; set; }
}

var context = new DamageContext(50);
Events.Emit("damage:pre_process", context);

if (!context.IsBlocked)
{
    ApplyDamage(context.FinalDamage);
}
```

---

## 三种典型场景

## 三种典型场景

## 核心设计

**不再区分 `Blockable` 和 `Operation`，统一使用 `EventContext`。**
这一点基于深度思考：无论是"检查是否可用"还是"执行是否成功"，本质都是一个布尔状态 (`Success`) 和一个原因 (`FailReason`)。

### 统一 API

```csharp
public class EventContext
{
    // 流程控制
    public bool IsHandled { get; protected set; }
    public bool IsPropagationStopped { get; private set; }
    public void StopPropagation() => IsPropagationStopped = true;

    // 结果状态
    public bool Success { get; protected set; } = true;
    public string? FailReason { get; protected set; }
    
    public void SetFailed(string reason)
    {
        Success = false;
        IsHandled = true;
        FailReason ??= reason;
    }
}
```

### 典型场景

#### 1. 检查型 (Request Check)

```csharp
// "如果不成功 (Success == false)，则不执行"
var context = new EventContext();
Emit(new CheckEventData(ability, context));

if (!context.Success) 
{
    Log($"Blocked: {context.FailReason}");
    return;
}
```

#### 2. 操作型 (Operation)

```csharp
// "如果不成功 (Success == false)，则报告错误"
var context = new EventContext();
Emit(new ConsumeEventData(ability, context));

if (!context.Success)
{
    Log($"Failed: {context.FailReason}");
}
```

不再需要记忆 `IsAllowed` 或 `SetBlocked`，统一使用 `Success` 和 `SetFailed`。

---

## 与 EventData 的区别

| 特性 | EventData | Context |
|------|-----------|---------|
| **类型** | `readonly record struct` | `class` |
| **可变性** | 不可变 | 可变 |
| **用途** | 单向通知 | 请求-响应 |
| **GC** | 零 GC（栈分配） | 有 GC（堆分配） |
| **适用场景** | 高频事件 | 低频交互 |

```csharp
// EventData: 通知型，不可变
public readonly record struct DamagedEventData(float Amount);

// Context: 请求型，可变
public class DamageCheckContext { public bool IsBlocked { get; set; } }
```

---

## 最佳实践

### ✅ 推荐

```csharp
// 1. Context 用于低频的"检查"或"请求"
var context = new AbilityCanUseCheckContext(ability);
ability.Events.Emit(GameEventType.Ability.RequestCheckCanUse, context);

// 2. 检查完后立即使用结果
if (!context.IsAllowed) return false;

// 3. 使用 record struct 做高频通知
ability.Events.Emit(GameEventType.Ability.Activated, 
    new ActivatedEventData(ability, targets));
```

### ❌ 避免

```csharp
// 1. 不要在热路径（如 _Process）中创建 Context
public override void _Process(double delta)
{
    var context = new SomeContext();  // ❌ 每帧分配
}

// 2. 不要把 Context 用于纯通知场景
Events.Emit("unit:dead", new DeadContext());  // ❌ 应该用 EventData
```

---

## 相关文档

- [EventBus 架构设计](EventBus架构设计.md)
- [EventBus 使用说明](../../../../Src/ECS/Event/README_EventBus.md)
- [AbilityContext 源码](../../../../Data/EventType/Ability/AbilityContext.cs)
