---
name: ecs-event
description: 使用 EventBus 进行组件间通信、发布/订阅事件、定义新事件类型时使用。适用于：跨组件通信，替代 Godot Signal，使用 Entity.Events 局部事件或 GlobalEventBus 全局事件。触发关键词：EventBus、发布事件、订阅事件、GlobalEventBus、Entity.Events、定义事件类型、GameEventType。
---

# ECS EventBus 事件系统规范

## 核心原则
- **禁止 Godot Signal** 处理核心逻辑，统一用 EventBus
- **两层架构**：`Entity.Events`（实体内部）+ `GlobalEventBus.Global`（跨实体全局）
- **零 GC 设计**：事件数据用 `readonly record struct`

## 发布与订阅

```csharp
// ✅ 发布局部事件（实体内部组件间）
_entity.Events.Emit(GameEventType.Unit.HealRequest,
    new GameEventType.Unit.HealRequestEventData(amount));

// ✅ 订阅局部事件
_entity.Events.On<GameEventType.Unit.HealRequestEventData>(
    GameEventType.Unit.HealRequest, OnHealRequest);

// ✅ 发布全局事件（跨实体）
GlobalEventBus.Global.Emit(GameEventType.Unit.Killed,
    new GameEventType.Unit.KilledEventData(victim, deathType));

// ✅ 订阅全局事件（在 OnPoolAcquire 中订阅）
GlobalEventBus.Global.On<GameEventType.Unit.KilledEventData>(
    GameEventType.Unit.Killed, OnKilled);

// ✅ 取消订阅（在 OnPoolRelease 中取消）
GlobalEventBus.Global.Off<GameEventType.Unit.KilledEventData>(
    GameEventType.Unit.Killed, OnKilled);
```

## 何时用局部 vs 全局事件

| 场景 | 使用 |
|------|------|
| 同一 Entity 内组件通信（HP变化、攻击请求） | `Entity.Events` |
| 跨 Entity 通知（击杀、生成、全局状态） | `GlobalEventBus.Global` |
| UI 监听特定 Entity 状态 | `Entity.Events`（绑定后监听） |

## 带返回值的事件（EventContext 模式）

```csharp
// 发送方：创建 EventContext 接收返回值
var context = new EventContext();
_entity.Events.Emit(GameEventType.Ability.CheckCanUse,
    new GameEventType.Ability.CheckCanUseEventData(context));

// 检查结果
if (!context.Success)
{
    var reason = context.FailReason;
}

// 接收方：写入结果
private void OnCheckCanUse(GameEventType.Ability.CheckCanUseEventData evt)
{
    if (IsOnCooldown)
        evt.Context.Fail("冷却中");
    // 不调用 Fail 则默认 Success = true
}
```

## 定义新事件类型

在 `Data/EventType/` 对应目录添加：

```csharp
public static partial class GameEventType
{
    public static class MyModule
    {
        // 事件标识符（字符串常量）
        public static readonly string MyEvent = nameof(MyEvent);

        // 事件数据（readonly record struct，零 GC）
        public readonly record struct MyEventData(float Value, IEntity Source);
    }
}
```

## 生命周期管理

```csharp
// Component 中：无需手动解绑，EntityManager.Destroy 自动 Events.Clear()
public void OnComponentUnregistered() { /* 无需 Off */ }

// Entity 对象池中：全局事件必须手动管理
public void OnPoolAcquire()
{
    GlobalEventBus.Global.On<...>(..., Handler);  // 订阅
}
public void OnPoolRelease()
{
    GlobalEventBus.Global.Off<...>(..., Handler);  // 必须取消！
}
```

## 禁止事项
- ❌ 使用 Godot `[Signal]` 处理核心逻辑
- ❌ 直接调用其他 Component 的方法（用事件解耦）
- ❌ 在 Component 中订阅全局事件后忘记取消订阅（内存泄漏）
- ❌ 事件数据用 class（用 `readonly record struct`）

## 关键文件路径
- **核心引擎** → `Src/ECS/Event/EventBus.cs`
- **全局总线** → `Src/ECS/Event/GlobalEventBus.cs`
- **事件上下文** → `Src/ECS/Event/EventContext.cs`
- **最佳实践** → `Src/ECS/Event/README_EventBus.md`
- **架构设计** → `Docs/框架/ECS/Event/EventBus架构设计.md`
- **事件类型定义目录** → `Data/EventType/`
- **技能事件** → `Data/EventType/Ability/GameEventType_Ability.cs`
- **攻击事件** → `Data/EventType/Unit/Attack/GameEventType_Attack.cs`
- **瞄准事件** → `Data/EventType/Unit/Targeting/GameEventType_Targeting.cs`
