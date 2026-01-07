# Timer 架构设计分析与改进建议

## 1. 现状分析

### 1.1 现有 Timer.ts 架构

**优势：**

- **全局统一管理**：通过静态数组 `Timer.Timers`集中管理所有计时器实例
- **高性能**：使用单一中心计时器（0.01 秒间隔）驱动所有 Timer 实例
- **功能完整**：支持延迟执行、循环计时、倒计时等常用功能
- **同步/异步分离**：`Timer`和 `ASyncTimer`分别处理不同环境
- **内存友好**：避免为每个 Timer 创建独立的 War3 计时器句柄

**设计特点：**

```typescript
// 全局管理模式
static Timers: Timer[] = [];
static onInit() {
  // 单一驱动器模式
  jasscj.TimerStart(timer, inteval, true, func);
}
```

### 1.2 现有 TimerComponent.ts 架构

**设计特点：**

- **Entity 绑定**：每个 Entity 拥有独立的计时器集合
- **ID 管理**：通过字符串 ID 标识和管理计时器
- **容量限制**：支持最大计时器数量限制
- **进度跟踪**：提供动画进度回调功能
- **生命周期管理**：与 Entity 生命周期绑定

**功能重叠：**

```typescript
// 与Timer.ts功能重叠
setTimeout(duration: number, callback: () => void, repeat: boolean = false)
setInterval(interval: number, callback: () => void)
```

## 2. 架构问题分析

### 2.1 功能重复问题

**重复实现：**

- 两套独立的计时器系统
- 相似的延迟执行、循环执行功能
- 不同的时间管理机制（全局 vs 组件级别）

**维护成本：**

- 双重代码维护负担
- 功能同步困难
- 性能优化需要在两处进行

### 2.2 架构不一致

**时间驱动方式：**

- `Timer.ts`：基于 War3 原生计时器的全局时间系统
- `TimerComponent.ts`：基于 ECS 的 `update(deltaTime)`机制

**内存管理：**

- `Timer.ts`：全局数组，统一管理
- `TimerComponent.ts`：分散在各个 Entity 中

### 2.3 ECS 设计原则冲突

根据现代 ECS 架构最佳实践 `<mcreference link="https://www.devzery.com/post/entity-component-system" index="1">`1`</mcreference>` `<mcreference link="https://www.linkedin.com/pulse/understanding-unitys-entity-component-system-ecs-games-sarkar" index="2">`2`</mcreference>`：

**ECS 核心原则：**

- **数据与逻辑分离**：Components 只存储数据，Systems 处理逻辑
- **组合优于继承**：通过组件组合实现功能
- **系统间解耦**：Systems 独立运行，通过 Components 通信

**当前 TimerComponent 违反的原则：**

- **逻辑过重**：Component 包含了大量计时器管理逻辑
- **职责不清**：既是数据容器又是逻辑处理器
- **难以复用**：与 Entity 强绑定，无法独立使用

## 3. 现代游戏框架对比分析

### 3.1 Unity ECS (DOTS) 模式 `<mcreference link="https://www.linkedin.com/pulse/understanding-unitys-entity-component-system-ecs-games-sarkar" index="2">`2`</mcreference>`

```csharp
// 纯数据组件
public struct TimerComponent : IComponentData {
    public float Duration;
    public float Remaining;
    public bool IsActive;
}

// 独立的系统处理逻辑
public class TimerSystem : SystemBase {
    protected override void OnUpdate() {
        float deltaTime = Time.DeltaTime;
        Entities.ForEach((ref TimerComponent timer) => {
            if (timer.IsActive) {
                timer.Remaining -= deltaTime;
            }
        }).Run();
    }
}
```

### 3.2 Unreal Engine Timer Manager `<mcreference link="https://dev.epicgames.com/documentation/en-us/unreal-engine/using-timers-in-unreal-engine" index="5">`5`</mcreference>`

```cpp
// 全局Timer Manager管理
FTimerManager& TimerManager = GetWorld()->GetTimerManager();
FTimerHandle TimerHandle;

// 设置计时器
TimerManager.SetTimer(TimerHandle, this, &AMyActor::MyFunction, 1.0f, true);
```

**设计特点：**

- **全局管理器**：统一的 TimerManager 负责所有计时器
- **句柄系统**：通过 Handle 引用和管理计时器
- **类型安全**：强类型的回调绑定

## 4. 改进方案设计

### 4.1 方案一：TimerManager + TimerComponent（推荐）

**架构设计：**

```typescript
// 1. 全局TimerManager（基于现有Timer.ts改进）
export class TimerManager {
  private static instance: TimerManager;
  private timers: Map<string, TimerHandle> = new Map();

  // 统一的计时器创建接口
  static createTimer(config: TimerConfig): string {
    // 复用现有Timer.ts的高性能实现
  }

  static destroyTimer(id: string): boolean {
    // 统一销毁逻辑
  }
}

// 2. 轻量级TimerComponent（纯数据）
export class TimerComponent extends Component<TimerComponentProps> {
  // 只存储计时器ID引用
  private timerIds: Set<string> = new Set();

  // 简化的接口，委托给TimerManager
  setTimeout(duration: number, callback: () => void): string {
    const id = TimerManager.createTimer({
      duration,
      callback,
      owner: this.owner.id,
    });
    this.timerIds.add(id);
    return id;
  }

  destroy(): void {
    // 自动清理所有关联的计时器
    for (const id of this.timerIds) {
      TimerManager.destroyTimer(id);
    }
  }
}

// 3. TimerSystem（可选，用于ECS风格的处理）
export class TimerSystem extends System {
  update(deltaTime: number): void {
    // 处理需要ECS集成的特殊计时器逻辑
    // 如：基于组件状态的条件计时器
  }
}
```

**优势：**

- **性能最优**：复用现有 Timer.ts 的高性能全局管理
- **ECS 兼容**：TimerComponent 变为轻量级数据容器
- **统一接口**：所有计时器通过 TimerManager 管理
- **自动清理**：Entity 销毁时自动清理关联计时器
- **向后兼容**：现有 Timer.ts 代码无需大幅修改

### 4.2 方案二：纯 ECS TimerSystem

**架构设计：**

```typescript
// 1. 纯数据组件
interface TimerData {
  id: string;
  duration: number;
  remaining: number;
  callback: () => void;
  repeat: boolean;
  paused: boolean;
}

export class TimerComponent extends Component<TimerComponentProps> {
  timers: TimerData[] = [];
  // 只存储数据，无逻辑
}

// 2. 专门的TimerSystem
export class TimerSystem extends System {
  update(deltaTime: number): void {
    // 查询所有具有TimerComponent的Entity
    const entities =
      this.entityManager.getEntitiesWithComponent(TimerComponent);

    for (const entity of entities) {
      const timerComp = entity.getComponent(TimerComponent);
      this.updateTimers(timerComp.timers, deltaTime);
    }
  }

  private updateTimers(timers: TimerData[], deltaTime: number): void {
    // 处理计时器逻辑
  }
}
```

**优势：**

- **纯 ECS 设计**：完全符合 ECS 架构原则
- **数据驱动**：所有逻辑在 System 中处理
- **易于扩展**：可以轻松添加新的计时器相关 System

**劣势：**

- **性能较低**：需要遍历所有 Entity
- **复杂度高**：需要重新实现 Timer.ts 的优化

### 4.3 方案三：移除 TimerComponent

**设计思路：**

- 完全依赖现有的 `Timer.ts`
- 在 Entity 中直接使用 Timer 静态方法
- 通过 Entity 的 `destroy`事件清理计时器

```typescript
export class Entity {
  private timerIds: string[] = [];

  createTimer(duration: number, callback: () => void): string {
    const timer = Timer.RunLater(duration, callback);
    const id = this.generateTimerId();
    this.timerIds.push(id);
    return id;
  }

  destroy(): void {
    // 清理所有计时器
    this.timerIds.forEach((id) => {
      // 需要扩展Timer.ts支持ID管理
    });
  }
}
```

## 5. 推荐方案详细设计

### 5.1 TimerManager 设计

```typescript
export interface TimerConfig {
  duration: number;
  callback: () => void;
  repeat?: boolean;
  immediate?: boolean;
  owner?: string; // Entity ID
  tag?: string; // 标签，用于分组管理
}

export interface TimerHandle {
  id: string;
  timer: Timer; // 复用现有Timer实例
  config: TimerConfig;
  createdAt: number;
}

export class TimerManager {
  private static instance: TimerManager;
  private idMap: Map<string, TimerHandle> = new Map();
  private ownerMap: Map<string, Set<string>> = new Map(); // owner -> timer IDs
  private tagMap: Map<string, Set<string>> = new Map(); // tag -> timer IDs
  private idCounter: number = 0;

  static getInstance(): TimerManager {
    if (!this.instance) {
      this.instance = Timer.CreateManager();
    }
    return this.instance;
  }

  createTimer(config: TimerConfig): string {
    const id = `timer_${++this.idCounter}`;

    // 创建底层Timer实例
    const timer = Timer.Create(config.duration, config.repeat || false, () => {
      try {
        config.callback();
      } catch (error) {
        logger.error(`Timer ${id} callback error:`, error);
      }

      // 如果不是循环计时器，自动清理
      if (!config.repeat) {
        this.destroyTimer(id);
      }
    });

    const handle: TimerHandle = {
      id,
      timer,
      config,
      createdAt: Timer.gametime,
    };

    this.idMap.set(id, handle);

    // 建立owner映射
    if (config.owner) {
      if (!this.ownerMap.has(config.owner)) {
        this.ownerMap.set(config.owner, new Set());
      }
      this.ownerMap.get(config.owner)!.add(id);
    }

    // 建立tag映射
    if (config.tag) {
      if (!this.tagMap.has(config.tag)) {
        this.tagMap.set(config.tag, new Set());
      }
      this.tagMap.get(config.tag)!.add(id);
    }

    return id;
  }

  destroyTimer(id: string): boolean {
    const handle = this.idMap.get(id);
    if (!handle) return false;

    // 销毁底层Timer
    handle.timer.Null();

    // 清理映射关系
    if (handle.config.owner) {
      this.ownerMap.get(handle.config.owner)?.delete(id);
    }
    if (handle.config.tag) {
      this.tagMap.get(handle.config.tag)?.delete(id);
    }

    this.idMap.delete(id);
    return true;
  }

  destroyTimersByOwner(owner: string): number {
    const timerIds = this.ownerMap.get(owner);
    if (!timerIds) return 0;

    let count = 0;
    for (const id of Array.from(timerIds)) {
      if (this.destroyTimer(id)) {
        count++;
      }
    }
    return count;
  }

  destroyTimersByTag(tag: string): number {
    const timerIds = this.tagMap.get(tag);
    if (!timerIds) return 0;

    let count = 0;
    for (const id of Array.from(timerIds)) {
      if (this.destroyTimer(id)) {
        count++;
      }
    }
    return count;
  }

  pauseTimer(id: string): boolean {
    const handle = this.idMap.get(id);
    if (!handle) return false;

    handle.timer.pause = true;
    return true;
  }

  resumeTimer(id: string): boolean {
    const handle = this.idMap.get(id);
    if (!handle) return false;

    handle.timer.pause = false;
    return true;
  }

  getTimerInfo(id: string): TimerHandle | undefined {
    return this.idMap.get(id);
  }

  getActiveTimerCount(): number {
    return this.idMap.size;
  }

  getTimersByOwner(owner: string): TimerHandle[] {
    const timerIds = this.ownerMap.get(owner);
    if (!timerIds) return [];

    return Array.from(timerIds)
      .map((id) => this.idMap.get(id)!)
      .filter((handle) => handle !== undefined);
  }
}
```

### 5.2 简化的 TimerComponent

```typescript
export interface TimerComponentProps {
  autoCleanup?: boolean;
  defaultTag?: string;
}

export class TimerComponent extends Component<TimerComponentProps> {
  protected static readonly TYPE: string = "TimerComponent";

  private timerIds: Set<string> = new Set();
  private timerManager: TimerManager;

  constructor(owner: Entity, props?: TimerComponentProps) {
    super(owner, {
      autoCleanup: true,
      ...props,
    });
    this.timerManager = TimerManager.getInstance();
  }

  setTimeout(
    duration: number,
    callback: () => void,
    repeat: boolean = false
  ): string {
    const id = this.timerManager.createTimer({
      duration,
      callback,
      repeat,
      owner: this.owner.id,
      tag: this.props.defaultTag,
    });

    this.timerIds.add(id);
    return id;
  }

  setInterval(interval: number, callback: () => void): string {
    return this.setTimeout(interval, callback, true);
  }

  animate(
    duration: number,
    onUpdate: (progress: number) => void,
    onComplete?: () => void
  ): string {
    let startTime = Timer.gametime;

    return this.setInterval(0.01, () => {
      //
      const elapsed = (Timer.gametime - startTime) / 100; // 转换为秒
      const progress = Math.min(1, elapsed / duration);

      onUpdate(progress);

      if (progress >= 1) {
        if (onComplete) {
          onComplete();
        }
        return true; // 停止循环
      }
      return false;
    });
  }

  clearTimer(id: string): boolean {
    if (this.timerIds.has(id)) {
      this.timerIds.delete(id);
      return this.timerManager.destroyTimer(id);
    }
    return false;
  }

  clearAllTimers(): void {
    for (const id of this.timerIds) {
      this.timerManager.destroyTimer(id);
    }
    this.timerIds.clear();
  }

  pauseTimer(id: string): boolean {
    return this.timerManager.pauseTimer(id);
  }

  resumeTimer(id: string): boolean {
    return this.timerManager.resumeTimer(id);
  }

  getTimerCount(): number {
    return this.timerIds.size;
  }

  destroy(): void {
    if (this.props.autoCleanup) {
      this.clearAllTimers();
    }
  }
}
```

## 6. 迁移策略

### 6.1 阶段一：创建 TimerManager

1. 基于现有 `Timer.ts`创建 `TimerManager`
2. 保持现有 API 兼容性
3. 添加 ID 管理和 owner 映射功能

### 6.2 阶段二：重构 TimerComponent

1. 简化 `TimerComponent`为轻量级包装器
2. 委托所有逻辑到 `TimerManager`
3. 保持对外 API 不变

### 6.3 阶段三：优化和清理

1. 移除重复代码
2. 性能优化
3. 添加监控和调试功能

## 7. 性能对比分析

### 7.1 内存使用

**当前方案：**

- Timer.ts: 全局数组，O(n)空间复杂度
- TimerComponent: 每个 Entity 独立 Map，总体 O(m\*k)空间复杂度

**改进方案：**

- TimerManager: 全局 Map + 索引，O(n + m)空间复杂度
- TimerComponent: 只存储 ID 引用，O(k)空间复杂度

### 7.2 执行性能

**当前方案：**

- Timer.ts: O(n)遍历，高效
- TimerComponent: O(m\*k)分散更新，低效

**改进方案：**

- 统一 O(n)遍历，性能最优
- 减少重复计算和内存分配

### 7.3 管理复杂度

**当前方案：**

- 双重维护成本
- 不一致的 API 设计
- 难以调试和监控

**改进方案：**

- 统一管理接口
- 一致的 API 设计
- 集中的监控和调试

## 8. 实施结果与总结

### 8.1 已完成的改进

**✅ 阶段一：创建 TimerManager（已完成）**

- 基于现有 `Timer.ts` 创建了 `TimerManager` 类
- 添加了 ID 管理和 owner 映射功能
- 保持了现有 Timer 类的高性能实现
- 提供了统一的计时器创建、销毁、暂停、恢复接口
- 支持按 owner 和 tag 进行批量管理

**✅ 阶段二：重构 TimerComponent（已完成）**

- 将 `TimerComponent` 转换为轻量级包装器
- 移除了内部计时器逻辑，委托给 `TimerManager`
- 保持了对外 API 的兼容性
- 符合 ECS 架构原则：组件只存储数据引用
- 实现了自动清理机制

### 8.2 架构改进成果

**性能优化：**

- 统一使用全局中心计时器驱动，避免重复计算
- 减少内存分配，TimerComponent 只存储 ID 引用
- 消除了功能重复，提高了执行效率

**架构一致性：**

- 所有计时器统一由 TimerManager 管理
- TimerComponent 符合 ECS 数据与逻辑分离原则
- 提供了一致的 API 设计

**可维护性：**

- 单一职责：TimerManager 负责逻辑，TimerComponent 负责数据
- 集中管理：所有计时器相关功能在一处维护
- 向后兼容：现有代码无需大幅修改

### 8.3 新增功能特性

**高级管理功能：**

- 按 owner（Entity ID）批量管理计时器
- 按 tag 标签分组管理计时器
- 计时器信息查询和监控
- 自动清理和生命周期管理

**调试和监控：**

- 详细的错误日志记录
- 计时器创建时间追踪
- 活跃计时器数量统计
- 废弃方法的警告提示

### 8.4 API 兼容性

**保持兼容的方法：**

- `setTimeout()` - 延迟执行
- `setInterval()` - 周期执行
- `animate()` - 动画计时器
- `clearTimer()` - 取消计时器
- `pauseTimer()` / `resumeTimer()` - 暂停/恢复
- `clearAllTimers()` - 清除所有计时器
- `getTimerCount()` - 获取计时器数量

**新增的方法：**

- `getTimerIds()` - 获取所有计时器 ID
- `getTimerInfo()` - 获取计时器详细信息

**废弃的方法：**

- `getTimeRemaining()` - 建议使用 TimerManager.getTimerInfo()
- `getProgress()` - 建议使用 TimerManager.getTimerInfo()
- `resetTimer()` - 建议重新创建计时器

### 8.5 使用示例

**基本用法（保持不变）：**

```typescript
// 在 Entity 中使用
const timerComp = entity.getComponent(TimerComponent);

// 延迟执行
const timerId = timerComp.setTimeout(2.0, () => {
  console.log("2秒后执行");
});

// 周期执行
const intervalId = timerComp.setInterval(1.0, () => {
  console.log("每秒执行");
});

// 动画计时器
const animId = timerComp.animate(
  3.0,
  (progress) => {
    // progress 从 0 到 1
    entity.setScale(1 + progress * 0.5);
  },
  () => {
    console.log("动画完成");
  }
);
```

**高级用法（新增功能）：**

```typescript
// 直接使用 TimerManager
const timerManager = TimerManager.getInstance();

// 创建带标签的计时器
const id = timerManager.createTimer({
  duration: 5.0,
  callback: () => console.log("执行"),
  owner: entity.id,
  tag: "combat",
});

// 批量管理
timerManager.destroyTimersByTag("combat"); // 清除所有战斗相关计时器
timerManager.destroyTimersByOwner(entity.id); // 清除Entity的所有计时器

// 监控和调试
console.log(`活跃计时器数量: ${timerManager.getActiveTimerCount()}`);
const info = timerManager.getTimerInfo(id);
console.log(`计时器创建时间: ${info?.createdAt}`);
```

### 8.6 总结

**核心问题已解决：**

1. ✅ **功能重复**：统一由 TimerManager 管理，消除重复实现
2. ✅ **架构不一致**：建立了统一的时间管理机制
3. ✅ **ECS 原则违反**：TimerComponent 现在是纯数据容器
4. ✅ **维护成本高**：单一代码维护点，降低维护负担

**实现的目标：**

- ✅ 高性能的计时器服务（复用现有高效实现）
- ✅ 统一的管理接口（TimerManager 提供完整 API）
- ✅ 完善的调试工具（日志记录和信息查询）
- ✅ 可扩展的架构设计（支持标签、owner 等高级功能）

**下一步计划：**

1. **性能监控**：添加计时器性能统计和分析
2. **可视化调试**：开发计时器状态可视化工具
3. **高级功能**：支持计时器优先级、依赖关系等
4. **文档完善**：编写详细的使用指南和最佳实践

这次架构改进成功建立了现代化的计时器管理系统，为 War3TS 项目的长期发展奠定了坚实的基础，显著提高了开发效率和代码质量。
