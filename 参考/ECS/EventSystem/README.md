# 事件系统详细说明

## 概述

事件系统是现代游戏对象架构中实现松耦合通信的核心机制。它允许游戏对象和组件在不直接相互依赖的情况下进行交互，从而提高代码的可维护性和可扩展性。

## 核心组件

### EventBus

`EventBus`是事件系统的中心，负责事件的注册、分发和管理：

```typescript
export class EventBus {
  // 注册事件处理器
  on<T>(
    eventType: string,
    handler: EventHandler<T>,
    priority?: EventPriority,
    once?: boolean
  ): EventSubscription;

  // 注册一次性事件处理器
  once<T>(
    eventType: string,
    handler: EventHandler<T>,
    priority?: EventPriority
  ): EventSubscription;

  // 取消事件处理器
  off(subscription: EventSubscription): boolean;

  // 取消特定类型的所有处理器
  offAll(eventType: string): boolean;

  // 发射事件
  emit<T>(
    eventType: string,
    data?: T,
    source?: any,
    priority?: EventPriority
  ): boolean;

  // 清理所有事件处理器
  clear(): void;

  // 获取统计信息
  getStats(): Record<string, number>;
}
```

### 事件对象池

事件系统使用通用的`ObjectPool`来管理事件对象，减少内存分配和垃圾回收：

```typescript
// 使用通用ObjectPool管理事件对象
private eventPool: ObjectPool<PoolableGameEvent<any>>;

// 初始化事件池
this.eventPool = new ObjectPool<PoolableGameEvent<any>>(
    () => new PoolableGameEvent<any>(),
    {
        initialSize: 10,
        maxSize: 100,
        name: "EventPool",
        enableStats: true
    }
);

// 获取事件对象
const event = this.eventPool.acquire();

// 释放事件对象
this.eventPool.release(event);
```

### EventTypes

定义了事件系统使用的核心类型和接口：

```typescript
// 事件处理器函数
export interface EventHandler<T = any> {
  (data: T): void | Promise<void>;
}

// 事件优先级
export enum EventPriority {
  Highest = 100,
  High = 75,
  Normal = 50,
  Low = 25,
  Lowest = 0,
}

// 事件订阅信息
export interface EventSubscription {
  eventType: string;
  handler: EventHandler;
  priority: EventPriority;
  once: boolean;
}

// 游戏事件
export interface GameEvent<T = any> {
  type: string;
  data: T;
  source: any;
  timestamp: number;
  priority: EventPriority;
}
```

### 预定义事件

系统定义了一组标准事件类型，便于统一使用：

```typescript
export const GameEvents = {
  // 对象生命周期事件
  OBJECT_CREATED: "Entity.Created",
  OBJECT_DESTROYED: "Entity.Destroyed",
  OBJECT_INITIALIZING: "Entity.Initializing",
  OBJECT_INITIALIZED: "Entity.Initialized",
  OBJECT_DESTROYING: "Entity.Destroying",
  OBJECT_RESET: "Entity.Reset",

  // 属性变化事件
  PROPERTY_CHANGED: "Entity.PropertyChanged",
  PROPERTY_ADDED: "Entity.PropertyAdded",
  PROPERTY_REMOVED: "Entity.PropertyRemoved",

  // 组件事件
  COMPONENT_ADDED: "Component.Added",
  COMPONENT_REMOVED: "Component.Removed",
  COMPONENT_ENABLED: "Component.Enabled",
  COMPONENT_DISABLED: "Component.Disabled",

  // 特效事件
  EFFECT_CREATED: "Effect.Created",
  EFFECT_DESTROYED: "Effect.Destroyed",
  EFFECT_ATTACHED: "Effect.Attached",
  EFFECT_DETACHED: "Effect.Detached",

  // 系统事件
  ERROR: "System.Error",
  WARNING: "System.Warning",
};
```

## 工作原理

### 事件注册

1. 调用者通过`on()`或`once()`方法注册事件处理器
2. 系统创建一个`EventSubscription`对象并存储
3. 返回订阅对象，用于后续取消订阅

```typescript
// 注册事件
const subscription = eventBus.on("Unit.Damaged", (data) => {
  console.log(`受到伤害: ${data.amount}`);
});

// 注册优先级高的事件处理器
const highPrioritySubscription = eventBus.on(
  "Unit.Damaged",
  (data) => {
    console.log(`高优先级处理!`);
  },
  EventPriority.High
);

// 注册一次性事件处理器
const onceSubscription = eventBus.once("Unit.Died", (data) => {
  console.log(`单位死亡，来源: ${data.source}`);
});
```

### 事件发射

1. 调用者通过`emit()`方法发射事件
2. 系统从`EventPool`获取或创建一个`GameEvent`对象
3. 系统根据优先级排序并调用注册的处理器
4. 一次性处理器被自动移除
5. 事件处理完成后，事件对象被释放回池中

```typescript
// 发射事件
eventBus.emit("Unit.Damaged", {
  amount: 50,
  type: "physical",
  source: attacker,
});

// 带源对象的发射
eventBus.emit("Item.Used", { itemId: "healing_potion" }, player);

// 带优先级的发射
eventBus.emit(
  "System.Error",
  { message: "严重错误" },
  null,
  EventPriority.Highest
);
```

### 取消订阅

```typescript
// 取消特定订阅
eventBus.off(subscription);

// 取消特定类型的所有订阅
eventBus.offAll("Unit.Damaged");

// 清除所有订阅
eventBus.clear();
```

## 异步事件处理

事件系统支持异步事件处理器，可以使用 Promise 或 async/await：

```typescript
// 异步事件处理器
eventBus.on("Item.Used", async (data) => {
  // 执行异步操作
  await someLongRunningOperation();
  console.log("异步处理完成");
});

// 带Promise的处理器
eventBus.on("Ability.Cast", (data) => {
  return new Promise((resolve) => {
    setTimeout(() => {
      console.log("延迟处理完成");
      resolve();
    }, 1000);
  });
});
```

## 事件优先级

事件系统支持处理器优先级，高优先级处理器会先执行：

```typescript
// 定义多个不同优先级的处理器
eventBus.on(
  "Unit.Damaged",
  (data) => {
    console.log("正常优先级处理");
  },
  EventPriority.Normal
);

eventBus.on(
  "Unit.Damaged",
  (data) => {
    console.log("最高优先级处理");
    data.amount *= 0.5; // 减少50%的伤害
  },
  EventPriority.Highest
);

eventBus.on(
  "Unit.Damaged",
  (data) => {
    console.log("最低优先级处理");
    // 这里总是能看到被修改后的伤害值
  },
  EventPriority.Lowest
);
```

## 常用事件数据格式

### 属性变更事件

```typescript
// 监听属性变更
Entity.on(GameEvents.PROPERTY_CHANGED, (data) => {
  console.log(`属性 ${data.key} 从 ${data.oldValue} 变为 ${data.newValue}`);
});

// 事件数据结构
interface PropertyChangeData {
  key: string;
  oldValue: any;
  newValue: any;
  source: Entity;
}
```

### 组件事件

```typescript
// 监听组件添加
Entity.on(EventTypes.COMPONENT_ADDED, (data) => {
  console.log(`添加了组件: ${data.componentType}`);
});

// 事件数据结构
interface ComponentEventData {
  componentType: string;
  component: Component;
  source: Entity;
}
```

### 生命周期事件

```typescript
// 监听对象初始化
Entity.on(GameEvents.OBJECT_INITIALIZED, (data) => {
  console.log(`对象已初始化: ${data.source.getId()}`);
});

// 事件数据结构
interface LifecycleEventData {
  source: Entity;
}
```

## 与 Entity 和组件集成

### Entity 集成

Entity 类自动集成了事件系统，提供了与 EventBus 相同的事件方法：

```typescript
// Entity中的事件方法
class Entity {
  on<T>(
    eventType: string,
    handler: EventHandler<T>,
    priority?: EventPriority,
    once?: boolean
  ): EventSubscription {
    // 确保EventComponent存在
    this.ensureEventComponent();
    return this.eventComponent.on(eventType, handler, priority, once);
  }

  once<T>(
    eventType: string,
    handler: EventHandler<T>,
    priority?: EventPriority
  ): EventSubscription {
    this.ensureEventComponent();
    return this.eventComponent.once(eventType, handler, priority);
  }

  off(subscription: EventSubscription): boolean {
    if (!this.eventComponent) return false;
    return this.eventComponent.off(subscription);
  }

  emit<T>(
    eventType: string,
    data?: T,
    source?: any,
    priority?: EventPriority
  ): boolean {
    this.ensureEventComponent();
    return this.eventComponent.emit(eventType, data, source || this, priority);
  }
}
```

### EventComponent

EventComponent 是一个特殊组件，为 Entity 提供事件功能：

```typescript
// 在Entity中获取或创建EventComponent
private ensureEventComponent(): void {
    if (!this.eventComponent) {
        const existingComponent = this.getComponent(EventComponent);
        if (existingComponent) {
            this.eventComponent = existingComponent;
        } else {
            this.eventComponent = this.addComponent(EventComponent);
        }
    }
}
```

## 性能考虑

### 事件池化

系统使用通用的`ObjectPool`来重用事件对象，减少内存分配和垃圾回收：

```typescript
// EventBus中使用ObjectPool
private eventPool: ObjectPool<PoolableGameEvent<any>>;

emit<T>(eventType: string, data?: T, source?: any, priority?: EventPriority): boolean {
    // 从池中获取事件对象
    const event = this.eventPool.acquire();
    event.type = eventType;
    event.data = data;
    event.source = source;
    event.timestamp = Date.now();
    event.priority = priority || EventPriority.Normal;

    // 处理事件...

    // 完成后释放回池中
    this.eventPool.release(event);
}
```

### 事件队列

EventBus 实现了事件队列，确保在处理一个事件时发射的新事件不会导致递归调用：

```typescript
// EventBus中的事件队列处理
private eventQueue: GameEvent<any>[] = [];
private isProcessing: boolean = false;

private processEventQueue(): void {
    if (this.isProcessing || this.eventQueue.length === 0) {
        return;
    }

    this.isProcessing = true;
    while (this.eventQueue.length > 0) {
        const event = this.eventQueue.shift();
        this.processEvent(event);
    }
    this.isProcessing = false;
}
```

## 最佳实践

1. **使用标准事件类型**：尽量使用`GameEvents`中定义的标准事件类型
2. **命名约定**：自定义事件类型使用"类别.动作"格式，如"Unit.Damaged"
3. **数据传递**：通过事件数据传递所需信息，避免全局状态
4. **清理订阅**：对象销毁时取消所有事件订阅
5. **适当的优先级**：根据处理器的重要性设置合适的优先级
6. **事件粒度**：避免过于频繁或粒度过小的事件，以减少性能开销
7. **类型安全**：使用 TypeScript 泛型来确保事件数据类型安全

```typescript
// 定义事件数据接口
interface DamageEventData {
  amount: number;
  type: string;
  source: Entity;
}

// 类型安全的事件处理
Entity.on<DamageEventData>("Unit.Damaged", (data) => {
  // 这里data有正确的类型提示
  console.log(
    `受到${data.amount}点${data.type}伤害，来源: ${data.source.getId()}`
  );
});
```
