# Entity 基类详细说明

## 概述

`Entity`是现代游戏对象系统的核心基类，所有游戏 Entity（单位、技能、物品、Buff）都继承自此类。它基于 Schema 系统提供了类型安全的属性访问接口、数据验证、计算属性支持、生命周期管理、组件系统集成和事件系统集成。

## 核心特性

1. **Schema 驱动设计**：基于 Schema 系统实现类型安全的数据定义和验证
2. **统一的属性访问**：使用类型安全的方法获取和设置对象属性
3. **计算属性支持**：支持依赖其他属性的自动计算属性，带缓存机制
4. **数据验证**：自动验证属性类型和约束条件
5. **生命周期管理**：标准化的创建、初始化、更新和销毁流程
6. **组件系统集成**：支持动态添加、移除和访问组件
7. **事件系统集成**：内置事件发射和处理机制

## 关键属性

- `id`: 游戏对象的唯一标识符
- `interfaceName`: 接口名称，用于获取对应的 Schema 定义
- `schema`: Schema 定义对象，包含属性定义、计算属性和验证规则
- `data`: 存储对象的所有属性数据
- `computedCache`: 计算属性缓存，提高性能
- `componentManager`: 组件管理器，用于管理对象的组件
- `eventComponent`: 事件组件，用于处理对象特定事件
- `isInitialized`: 对象是否已初始化
- `isDestroyed`: 对象是否已销毁
- `isActive`: 对象是否处于激活状态

## 主要方法

### 属性访问方法

```typescript
// 获取属性值
get<K extends keyof TInterface>(key: K): TInterface[K]

// 设置属性值
set<K extends keyof TInterface>(key: K, value: TInterface[K]): boolean

// 添加数值到属性
add(key: string, delta: number): boolean

// 乘以属性值
multiply(key: string, multiplier: number): boolean

// 批量设置属性
setMultiple(properties: Partial<TInterface>): boolean

// 批量获取属性
getMultiple<K extends keyof TInterface>(keys: K[]): Pick<TInterface, K>

// 监听属性变更
onPropertyChanged<K extends keyof TInterface>(
  key: K,
  listener: (oldValue: TInterface[K], newValue: TInterface[K]) => void
): void
```

### 组件管理方法

```typescript
// 添加组件
addComponent<T extends Component>(componentType: ComponentConstructor<T>, props?: any): T

// 获取组件
getComponent<T extends Component>(componentType: ComponentConstructor<T>): T | null

// 移除组件
removeComponent<T extends Component>(componentType: ComponentConstructor<T>): boolean

// 检查是否拥有组件
hasComponent<T extends Component>(componentType: ComponentConstructor<T>): boolean
```

### 事件系统方法

```typescript
// 注册事件监听器
on<T>(eventType: string, handler: EventHandler<T>, priority?: EventPriority, once?: boolean): EventSubscription

// 注册一次性事件监听器
once<T>(eventType: string, handler: EventHandler<T>, priority?: EventPriority): EventSubscription

// 取消事件监听器
off(subscription: EventSubscription): boolean

// 发射事件
emit<T>(eventType: string, data?: T, source?: any, priority?: EventPriority): boolean
```

### 生命周期方法

```typescript
// 初始化对象（抽象方法，需子类实现）
abstract initialize(): void

// 执行初始化流程（内部使用）
protected performInitialize(): void

// 更新对象状态
update(deltaTime: number): void

// 销毁对象
destroy(): void

// 清理资源
protected cleanup(): void

// 重置为默认状态
resetToDefaults(): void
```

## 使用示例

### 创建自定义游戏对象

```typescript
class MyEntity extends Entity<Inte_Unit> {
  constructor(id: string) {
    super("Inte_Unit", id);

    // 设置默认属性
    this.set("名称", "自定义对象");
    this.set("生命值", 100);
    this.set("魔法值", 50);
  }

  initialize(): void {
    // 添加组件
    this.addComponent(AttributeComponent);
    this.addComponent(TimerComponent);

    // 注册事件监听器
    this.on("Unit.Damaged", (data) => {
      console.log(`单位受到伤害: ${data.damage}`);
    });

    console.log(`${this.get("名称")} 初始化完成`);
  }

  // 自定义方法
  takeDamage(amount: number): void {
    const currentHealth = this.get("生命值");
    this.set("生命值", Math.max(0, currentHealth - amount));

    // 发射伤害事件
    this.emit("Unit.Damaged", { damage: amount, source: this });
  }
}
```

### 使用EntityPrefab创建对象（推荐）

#### 方式1：先注册Prefab，再通过`EntityPrefab.create`创建（智能配置合并）

```typescript
// 注册Prefab
EntityPrefab.register("MY_ENTITY", {
  entityType: "MyEntity",
  data: [
    {
      schemaName: "Inte_Unit",
      initialData: {
        名称: "自定义对象",
        生命值: 100,
        魔法值: 50,
      },
      isPrimary: true,
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: TimerComponent },
  ],
});

// 创建实例
const entity = EntityPrefab.create("MY_ENTITY", {
  id: "obj1",
});

// 创建实例并智能合并配置
const enhancedEntity = EntityPrefab.create("MY_ENTITY", {
  id: "obj1_enhanced",
  data: [
    {
      schemaName: "Inte_Unit",
      initialData: {
        生命值: 200, // 智能覆盖：生命值=200，名称和魔法值保持原值
        攻击力: 80,  // 新增属性
      },
    },
  ],
  components: [
    {
      // 智能组件合并：相同类型组件覆盖，符合现代ECS设计原则
      type: TimerComponent,
      props: { maxTimers: 10, autoCleanup: true }, // 覆盖原有TimerComponent配置
    },
    {
      type: AbilityComponent, // 新增组件类型
      props: { abilityId: "A001", abilityName: "自定义技能" },
    },
  ],
});

// 获取组件
const timerComponent = entity.getComponent(TimerComponent);
if (timerComponent) {
  // 使用组件功能
  timerComponent.setTimeout(5, () => {
    console.log("5秒后执行");
  });
}
```

#### 方式1.5：使用EntityPrefab链式创建方法（推荐）

```typescript
// 使用createWithComponents添加额外组件（智能组件合并）
const builderEntity = EntityPrefab.createWithComponents("MY_ENTITY", [
  {
    type: AbilityComponent,
    props: {
      abilityId: "A002",
      abilityName: "链式构建技能",
    },
  },
  {
    type: TimerComponent,
    props: {
      maxTimers: 20,
      autoCleanup: false,
    } // 智能覆盖原有TimerComponent
  }
], [
  {
    schemaName: "Inte_Buff",
    initialData: {
      当前Buff列表: ["力量增强"],
      Buff持续时间: 30,
    },
  }
]);

// 使用createWithData添加额外数据
const dataEntity = EntityPrefab.createWithData("MY_ENTITY", [
  {
    schemaName: "Inte_Equipment",
    initialData: { 装备列表: [], 装备数量上限: 6 }
  }
]);
```

#### 方式2：一次性创建（不推荐，仅用于特殊情况）

```typescript
// 一次性创建（不使用Prefab注册，适合临时测试）
// 注意：不推荐直接使用EntityManager.createWithConfig，应统一使用EntityPrefab
const tempPrefab = {
  entityType: "MyEntity",
  data: [
    {
      schemaName: "Inte_Unit",
      initialData: {
        名称: "临时对象",
        生命值: 150,
        魔法值: 75,
      },
      isPrimary: true,
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: TimerComponent },
  ],
};

// 推荐：先注册临时Prefab，再创建
EntityPrefab.register("TEMP_ENTITY", tempPrefab);
const entity2 = EntityPrefab.create("TEMP_ENTITY", { id: "obj2" });
```

### 属性操作

```typescript
// 设置属性
entity.set("名称", "勇者");
entity.set("等级", 5);

// 获取属性
const name = entity.get("名称");
const level = entity.get("等级");

// 修改数值属性
entity.add("经验值", 1000);
entity.multiply("伤害加成", 1.5);

// 批量操作
entity.setMultiple({
  攻击力: 100,
  防御力: 50,
  暴击率: 0.2,
});

const stats = entity.getMultiple(["攻击力", "防御力", "暴击率"]);

### 使用`EntityPrefab.create`进行智能数据和组件合并

```typescript
// 基础Prefab
EntityPrefab.register("BaseUnit", {
  entityType: "Unit",
  data: [
    { schemaName: "UnitData", initialData: { hp: 100, mp: 50, name: "基础单位" } },
    { schemaName: "PositionData", initialData: { x: 0, y: 0 } }
  ],
  data: [
    { schemaName: SCHEMA_TYPES.TRANSFORM_DATA, initialData: { scale: { x: 1, y: 1, z: 1, overallScale: 1 }, rotation: { heading: 0, pitch: 0, roll: 0 } } }
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent }
  ]
});

// ConfigMerger智能数据和组件合并：相同schemaName/类型智能覆盖，不同类型追加
const unit = EntityPrefab.create("BaseUnit", {
  data: [
    { schemaName: "UnitData", initialData: { hp: 200 } }, // ConfigMerger智能覆盖：只覆盖hp，mp和name保持原值
    { schemaName: "InventoryData", initialData: { items: [] } } // ConfigMerger追加新的数据管理器
  ],
  components: [
    {
      // 智能数据合并：相同 schemaName 覆盖
      schemaName: SCHEMA_TYPES.TRANSFORM_DATA,
      initialData: { scale: { overallScale: 1.5 }, rotation: { heading: 90 } }
    },
    {
      type: AbilityComponent, // 新增组件类型
      props: { abilityId: "A001", abilityName: "基础技能" }
    }
  ]
});

// 最终结果（通过ConfigMerger处理）：
// 数据：
// UnitData: { hp: 200, mp: 50, name: "基础单位" }
// PositionData: { x: 0, y: 0 }
// InventoryData: { items: [] }
// 组件：
// AttributeComponent (保持原有)
// LifecycleComponent (新增生命周期管理)
// AbilityComponent (新增：{ abilityId: "A001", abilityName: "基础技能" })
// 数据：
// TRANSFORM_DATA (智能覆盖：{ scale: { overallScale: 1.5 }, rotation: { heading: 90 } })
```
```

## 与其他系统的交互

- **组件系统**：通过`componentManager`实现组件的添加、获取和移除
- **事件系统**：通过`eventComponent`或直接调用事件方法实现事件的发射和处理
- **接口定义系统**：通过`interfaceManager`实现类型安全的属性访问和验证
- **错误处理系统**：集成错误处理，确保对象操作的安全性

## 注意事项

1. 子类必须实现`initialize()`方法
2. 调用`destroy()`后，对象将不再可用
3. `update()`方法应定期调用以更新对象状态
4. 组件会自动获得对所有者对象的引用
5. 事件处理器应在对象销毁前取消注册，避免内存泄漏

## 最佳实践

- 使用泛型参数指定对象的接口类型，提供更好的类型安全
- 组织相关功能到组件中，而不是直接扩展 Entity
- 使用事件系统进行对象间通信，避免直接依赖
- 在初始化阶段设置默认属性值
- 使用`performInitialize()`而非直接调用`initialize()`来确保完整的初始化流程

## Schema 系统集成示例

```typescript
// 定义单位Schema
const unitSchema: Schema<Inte_Unit> = {
  properties: [
    {
      key: "当前生命值",
      type: "number",
      defaultValue: 100,
      constraints: { min: 0, max: 1000 },
    },
    {
      key: "最大生命值",
      type: "number",
      defaultValue: 100,
      constraints: { min: 1 },
    },
    {
      key: "攻击力",
      type: "number",
      defaultValue: 10,
      constraints: { min: 0 },
    },
  ],
  computed: [
    {
      key: "生命值百分比",
      dependencies: ["当前生命值", "最大生命值"],
      compute: (data) => (data.当前生命值 / data.最大生命值) * 100,
      cache: true,
    },
  ],
};

// 注册Schema
SchemaRegistry.registerSchema("Inte_Unit", unitSchema);

// 创建Entity
class MyUnit extends Entity<Inte_Unit> {
  constructor(id: string) {
    super("Inte_Unit", id);
  }

  initialize(): void {
    // 设置属性（自动验证）
    this.set("当前生命值", 80);
    this.set("最大生命值", 120);

    // 获取计算属性（自动计算和缓存）
    const healthPercent = this.get("生命值百分比"); // 66.67

    // 监听属性变更
    this.onPropertyChanged("当前生命值", (oldValue, newValue) => {
      console.log(`生命值从 ${oldValue} 变为 ${newValue}`);
    });
  }
}
```

这个基类为所有游戏对象提供了统一的接口和行为，确保了系统的一致性和可维护性。通过 Schema 系统、组件系统和事件系统的集成，可以实现复杂的游戏逻辑，同时保持代码的模块化和可重用性。
