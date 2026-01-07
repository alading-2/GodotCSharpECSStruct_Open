# 组件系统详细说明

## 概述

组件系统是现代游戏对象架构的核心部分，它实现了游戏功能的模块化和重用。通过将功能拆分为独立的组件，可以实现高内聚、低耦合的代码结构，便于维护和扩展。

## 核心类

### Component 基类

`Component`是所有组件的抽象基类，定义了组件的基本结构和生命周期方法：

```typescript
export abstract class Component<TProps = any> {
  protected owner: Entity; // 组件所属的游戏对象
  protected isEnabled: boolean; // 组件是否启用
  protected isInitialized: boolean; // 组件是否已初始化
  protected isDestroyed: boolean; // 组件是否已销毁
  protected props: TProps; // 组件属性

  // 获取组件类型
  static getType(): string;

  // 构造函数
  constructor(owner: Entity, props?: TProps);

  // 生命周期方法（需要子类实现）
  abstract initialize(): void;
  abstract update(deltaTime: number): void;
  abstract destroy(): void;

  // 启用/禁用组件
  enable(): void;
  disable(): void;

  // 获取状态
  isComponentEnabled(): boolean;
  isComponentInitialized(): boolean;
  isComponentDestroyed(): boolean;

  // 获取所有者
  getOwner(): Entity;

  // 获取/设置属性
  getProps(): TProps;
  setProps(props: Partial<TProps>): void;

  // 获取组件类型
  getType(): string;
}
```

### ComponentManager

`ComponentManager`负责管理 Entity 上的所有组件，处理组件的添加、获取、移除和更新：

```typescript
export class ComponentManager {
  // 构造函数
  constructor(owner: Entity);

  // 添加组件
  addComponent<T extends Component>(
    componentType: ComponentConstructor<T>,
    props?: any
  ): T;

  // 获取组件
  getComponent<T extends Component>(
    componentType: ComponentConstructor<T>
  ): T | null;

  // 移除组件
  removeComponent<T extends Component>(
    componentType: ComponentConstructor<T>
  ): boolean;

  // 检查是否有组件
  hasComponent<T extends Component>(
    componentType: ComponentConstructor<T>
  ): boolean;

  // 更新所有组件
  updateComponents(deltaTime: number): void;

  // 销毁所有组件
  destroyComponents(): void;

  // 组件依赖管理
  static registerDependency(
    componentType: ComponentConstructor,
    dependencyType: ComponentConstructor,
    isOptional?: boolean
  ): void;

  // 获取所有组件
  getAllComponents(): Component[];

  // 获取组件数量
  getComponentCount(): number;
}
```

## 内置组件

系统提供了几个常用的内置组件：

### AttributeComponent

管理复杂属性计算，支持依赖关系和缓存：

```typescript
export class AttributeComponent extends Component<AttributeComponentProps> {
  // 注册计算属性
  registerComputedAttribute(key: string, dependencies: string[]): void;

  // 计算属性值
  calculateAttribute(key: string): any;

  // 检查是否为计算属性
  isComputedAttribute(key: string): boolean;

  // 清除缓存
  clearCache(): void;

  // 使所有缓存失效
  invalidateAll(): void;
}
```

### TimerComponent

管理与对象相关的计时器，提供超时、间隔和动画功能：

```typescript
export class TimerComponent extends Component<TimerComponentProps> {
  // 创建一次性或重复的计时器
  setTimeout(duration: number, callback: () => void, repeat?: boolean): string;

  // 创建重复计时器
  setInterval(interval: number, callback: () => void): string;

  // 创建动画计时器
  animate(
    duration: number,
    onUpdate: (progress: number) => void,
    onComplete?: () => void
  ): string;

  // 计时器操作
  clearTimer(id: string): boolean;
  pauseTimer(id: string): boolean;
  resumeTimer(id: string): boolean;
  resetTimer(id: string): boolean;

  // 获取剩余时间
  getTimeRemaining(id: string): number;

  // 获取进度（0到1）
  getProgress(id: string): number;

  // 批量操作
  clearAllTimers(): void;
  pauseAllTimers(): void;
  resumeAllTimers(): void;
}
```

### EventComponent

为游戏对象提供本地事件处理能力：

```typescript
export class EventComponent extends Component<EventComponentProps> {
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

  // 移除事件处理器
  off(subscription: EventSubscription): boolean;

  // 移除特定类型的所有处理器
  offAll(eventType: string): boolean;

  // 发射事件
  emit<T>(
    eventType: string,
    data?: T,
    source?: any,
    priority?: EventPriority
  ): boolean;

  // 清除所有订阅
  clearAllSubscriptions(): void;

  // 获取订阅数量
  getSubscriptionCount(): number;

  // 获取事件总线
  getEventBus(): EventBus;
}
```

### EffectComponent

管理视觉效果和附加到对象的特效：

```typescript
export class EffectComponent extends Component<EffectComponentProps> {
  // 创建特效
  createEffect(
    type: string,
    x: number,
    y: number,
    z?: number,
    scale?: number,
    duration?: number
  ): string;

  // 附加特效到目标
  attachEffect(
    effectId: string,
    target: any,
    offsetX?: number,
    offsetY?: number,
    offsetZ?: number
  ): boolean;

  // 分离特效
  detachEffect(effectId: string): boolean;

  // 销毁特效
  destroyEffect(effectId: string): boolean;

  // 设置特效缩放
  setEffectScale(effectId: string, scale: number): boolean;

  // 设置特效位置
  setEffectPosition(
    effectId: string,
    x: number,
    y: number,
    z?: number
  ): boolean;

  // 获取特效
  getEffect(effectId: string): Effect | null;

  // 销毁所有特效
  destroyAllEffects(): void;

  // 启用/禁用所有特效
  enableEffects(): void;
  disableEffects(destroyExisting?: boolean): void;
}
```

## 组件依赖管理

组件系统支持组件之间的依赖关系：

```typescript
// 注册依赖关系
ComponentManager.registerDependency(AttributeComponent, TimerComponent, true);

// 添加组件时会自动检查和添加依赖项
Entity.addComponent(AttributeComponent); // 会自动添加TimerComponent（如果尚未添加）
```

## 使用示例

### 创建自定义组件

```typescript
// 定义组件属性接口
interface HealthComponentProps {
  maxHealth?: number;
  healthRegen?: number;
}

// 创建自定义健康组件
class HealthComponent extends Component<HealthComponentProps> {
  protected static readonly TYPE: string = "HealthComponent";
  private currentHealth: number = 0;

  static getType(): string {
    return HealthComponent.TYPE;
  }

  constructor(owner: Entity, props?: HealthComponentProps) {
    super(owner, props || { maxHealth: 100, healthRegen: 1 });
    this.currentHealth = this.props.maxHealth || 100;
  }

  initialize(): void {
    // 获取TimerComponent用于健康恢复
    const timerComponent = this.getOwner().getComponent(TimerComponent);
    if (timerComponent && this.props.healthRegen > 0) {
      timerComponent.setInterval(1, this.regenerateHealth.bind(this));
    }

    // 监听伤害事件
    this.getOwner().on("Unit.Damaged", this.onDamaged.bind(this));
  }

  update(deltaTime: number): void {
    // 每帧更新逻辑（如有）
  }

  destroy(): void {
    // 清理资源
    this.getOwner().off("Unit.Damaged", this.onDamaged);
  }

  // 自定义方法
  private regenerateHealth(): void {
    if (!this.isComponentEnabled()) return;

    const maxHealth = this.props.maxHealth || 100;
    if (this.currentHealth < maxHealth) {
      this.currentHealth = Math.min(
        maxHealth,
        this.currentHealth + this.props.healthRegen
      );
      this.getOwner().emit("Health.Changed", {
        currentHealth: this.currentHealth,
        maxHealth: maxHealth,
      });
    }
  }

  private onDamaged(data: any): void {
    if (!this.isComponentEnabled()) return;

    const damage = data.damage || 0;
    this.currentHealth = Math.max(0, this.currentHealth - damage);

    this.getOwner().emit("Health.Changed", {
      currentHealth: this.currentHealth,
      maxHealth: this.props.maxHealth,
    });

    // 检查是否死亡
    if (this.currentHealth <= 0) {
      this.getOwner().emit("Unit.Died", { source: data.source });
    }
  }

  // 公开API
  getHealth(): number {
    return this.currentHealth;
  }

  setHealth(value: number): void {
    const oldHealth = this.currentHealth;
    this.currentHealth = Math.max(0, Math.min(this.props.maxHealth, value));

    if (oldHealth !== this.currentHealth) {
      this.getOwner().emit("Health.Changed", {
        currentHealth: this.currentHealth,
        maxHealth: this.props.maxHealth,
      });
    }
  }

  getMaxHealth(): number {
    return this.props.maxHealth;
  }

  isDead(): boolean {
    return this.currentHealth <= 0;
  }
}
```

### 使用组件

```typescript
// 创建游戏对象
const unit = new ModernUnit("hero1");

// 添加健康组件
const healthComponent = unit.addComponent(HealthComponent, {
  maxHealth: 200,
  healthRegen: 2,
});

// 使用组件功能
console.log(`当前生命值: ${healthComponent.getHealth()}`);
healthComponent.setHealth(150);

// 监听组件事件
unit.on("Health.Changed", (data) => {
  console.log(`生命值变化: ${data.currentHealth}/${data.maxHealth}`);
});

// 禁用组件
healthComponent.disable();

// 启用组件
healthComponent.enable();

// 移除组件
unit.removeComponent(HealthComponent);
```

## 与其他系统的交互

- **Entity**：组件通过 `owner`引用访问其所属的游戏对象
- **事件系统**：组件可以通过 `owner`发射和接收事件
- **接口定义系统**：组件可以通过 `owner`访问和修改游戏对象的属性

## 最佳实践

1. **组件职责单一**：每个组件应只负责一个功能领域
2. **组件无依赖优先**：尽量减少组件之间的强依赖
3. **使用组件通信**：组件间通过事件系统通信，而非直接调用
4. **组件属性配置**：通过 props 参数配置组件，而非硬编码
5. **资源清理**：在 destroy()方法中清理所有资源和事件订阅
6. **组件复用**：设计通用组件以便在不同类型的游戏对象间复用
7. **延迟初始化**：组件的重量级初始化应放在 initialize()而非构造函数中
