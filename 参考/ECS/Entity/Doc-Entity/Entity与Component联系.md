# Entity 与 Component 联系详解（新版：EntityPrefab 统一构建）

## 🏗️ Entity-Component 架构模式

### 核心概念

**Entity** 是一个**容器**，**Component** 是**功能模块**。这是一种经典的组合模式（Composition Pattern）：

```typescript
// Entity = 容器 + 管理器
class Entity {
  private components: Map<string, Component> = new Map();

  // 添加功能模块
  addComponent<T extends Component>(componentType: ComponentConstructor<T>): T {
    const component = new componentType(this); // 将自己传给组件
    this.components.set(componentType.name, component);
    return component;
  }
}

// Component = 具体功能实现
class Component {
  constructor(protected owner: Entity) {
    // 组件知道自己属于哪个Entity
  }
}
```

### 关系图解

```
Entity (容器)
├── UnitComponent (单位功能)
├── AttributeComponent (属性功能)
├── AbilityComponent (技能功能)
├── BuffComponent (Buff功能)
└── TimerComponent (计时器功能)
```

## 🤔 为什么 UnitComponent 继承 Component 而不是 Entity？

### 1. **职责分离原则**

```typescript
// ❌ 错误的设计 - 如果UnitComponent继承Entity
class UnitComponent extends Entity {
  // 问题：UnitComponent既要管理War3单位，又要管理其他组件
  // 这违反了单一职责原则
}

// ✅ 正确的设计 - UnitComponent继承Component
class UnitComponent extends Component {
  // 职责：专门管理War3单位相关功能
  // Entity负责：管理所有组件
}
```

### 2. **组合优于继承**

```typescript
// 使用组合模式的优势
const unitEntity = new Entity("Inte_Unit", "unit_123");

// 可以灵活组合不同功能
unitEntity.addComponent(UnitComponent); // 单位功能
unitEntity.addComponent(AttributeComponent); // 属性功能
unitEntity.addComponent(AbilityComponent); // 技能功能

// 如果不需要某个功能，就不添加对应组件
// 如果需要新功能，就添加新组件
```

### 3. **实际的工作流程**

让我用具体代码展示它们是如何协作的：

```typescript
export class UnitComponent extends Component<UnitComponentProps> {
  constructor(owner: Entity, props?: UnitComponentProps) {
    super(owner, props || {}); // 将Entity传给父类Component
    // UnitComponent知道自己属于哪个Entity
  }

  initialize(): void {
    // UnitComponent可以访问Entity的数据
    this.owner.set("单位类型", this.unitType);
    this.owner.set("是否飞行", this.isFlying);

    // UnitComponent可以触发Entity的事件
    this.owner.emit("Unit.PositionChanged", { x, y });
  }
}
```

## 🔄 完整的协作流程

### 创建过程

```typescript
// 推荐：使用EntityPrefab统一构建（智能配置合并）
// 方式1：注册Prefab后实例化
EntityPrefab.register("INFANTRY", {
  entityType: "UnitEntity",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "步兵" },
      isPrimary: true,
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 500 },
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent },
  ],
});

// 创建实例并智能合并配置
const unit = EntityPrefab.create("INFANTRY", {
  components: [
    {
      type: UnitComponent,
      props: { unitType: "步兵", playerComp, position, face: 0 },
    },
  ],
});

// 方式2：直接使用EntityManager.createWithConfig（适合一次性创建）
const unit2 = EntityManager.createWithConfig({
  entityType: "UnitEntity",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "步兵" },
      isPrimary: true,
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 500 },
    },
  ],
  data: [
    {
      schemaName: SCHEMA_TYPES.TRANSFORM_DATA,
      initialData: { position: { x: 0, y: 0, z: 0 }, rotation: { heading: 0, pitch: 0, roll: 0 }, scale: { x: 1, y: 1, z: 1, overallScale: 1 } }
    }
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent },
    {
      type: UnitComponent,
      props: { unitType: "步兵", playerComp, position, face: 0 },
    },
  ],
});
```

### 运行时协作

```typescript
// Entity提供统一的数据访问接口
Entity.set("当前生命值", 100);
Entity.get("当前生命值");

// UnitComponent提供War3特定的功能
const unitComponent = Entity.getComponent(UnitComponent);
unitComponent.setPosition(100, 100);
unitComponent.setFacing(90);

// 组件内部可以修改Entity的数据
class UnitComponent extends Component {
  setPosition(x: number, y: number): void {
    jasscj.SetUnitPosition(this.unitHandle, x, y);
    // 通过owner访问Entity，更新数据
    this.owner.emit("Unit.PositionChanged", { x, y });
  }
}
```

## 🎯 这种设计的优势

### 1. **灵活性**

```typescript
// 可以为不同类型的Entity添加不同组合的组件
const hero = new Entity("Inte_Unit", "hero_1");
hero.addComponent(UnitComponent);
hero.addComponent(AbilityComponent); // 英雄有技能
hero.addComponent(InventoryComponent); // 英雄有背包

const normalUnit = new Entity("Inte_Unit", "unit_1");
normalUnit.addComponent(UnitComponent); // 普通单位只有基础功能
```

### 2. **可扩展性**

```typescript
// 需要新功能？添加新组件即可
class AIComponent extends Component {
  // AI逻辑
}

// 为单位添加AI
Entity.addComponent(AIComponent);
```

### 3. **解耦合**

```typescript
// 组件之间通过Entity的事件系统通信，不直接依赖
class UnitComponent extends Component {
  takeDamage(damage: number): void {
    this.owner.emit("Unit.TakeDamage", { damage });
  }
}

class BuffComponent extends Component {
  initialize(): void {
    this.owner.on("Unit.TakeDamage", (data) => {
      // 处理伤害减免等逻辑
    });
  }
}
```

## 📚 类比理解

可以把这种关系类比为：

- **Entity** = 汽车底盘
- **UnitComponent** = 发动机
- **AttributeComponent** = 仪表盘
- **AbilityComponent** = 音响系统

发动机不需要"继承"汽车底盘，而是"安装"到汽车底盘上。每个部件都有自己的职责，通过底盘进行协调工作。

## 🔧 实际使用示例

```typescript
// 使用 EntityPrefab 统一创建模式（智能配置合并）
// 先注册圣骑士Prefab
EntityPrefab.register("PALADIN", {
  entityType: "UnitEntity",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "圣骑士" },
      isPrimary: true,
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 800 },
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: AbilityComponent, props: { abilityId: "A001", abilityName: "圣光术" } },
  ],
});

// 使用createWithComponents添加实例特定组件（智能组件合并）
const unit = EntityPrefab.createWithComponents("PALADIN", [
  {
    type: UnitComponent,
    props: {
      unitType: "圣骑士",
      playerComp,
      position,
      face: 90,
    },
  },
  {
    type: AbilityComponent,
    props: {
      abilityId: "A002", 
      abilityName: "神圣打击" 
    } // 智能覆盖原有AbilityComponent
  }
]);

// 或者直接创建并智能合并配置（组件智能覆盖）
const unit2 = EntityPrefab.create("PALADIN", {
  components: [
    {
      type: UnitComponent,
      props: {
        unitType: "圣骑士",
        playerComp,
        position,
        face: 90,
      },
    },
    {
      // 智能覆盖原有AbilityComponent，符合现代ECS设计原则
      type: AbilityComponent,
      props: {
        abilityId: "A003",
        abilityName: "终极圣光",
      },
    },
  ],
});
```

## 🔍 深入理解：数据流和控制流

### 数据流向

```
War3原生API → UnitComponent → Entity数据存储 → 其他组件访问
     ↓              ↓              ↓                ↓
  SetUnitX()    this.owner.set()  data["x"] = 100   this.owner.get()
```

### 事件流向

```
UnitComponent → Entity事件系统 → 其他组件监听
     ↓               ↓                    ↓
  emit("event")   eventBus.trigger()   on("event", callback)
```

### 生命周期管理

```
Entity.initialize()
    ↓
各个Component.initialize() (按添加顺序)
    ↓
Entity.update()
    ↓
各个Component.update() (每帧调用)
    ↓
Entity.destroy()
    ↓
各个Component.destroy() (清理资源)
```

## 🎮 War3 特定的设计考虑

### 1. **War3 句柄管理**（在组件 initialize 中执行）

```typescript
class UnitComponent extends Component {
  private unitHandle: any; // War3原生句柄

  initialize(): void {
    // 根据 props 与 DataManager 创建句柄
    // Entity 不直接感知句柄
  }
}
```

### 2. **属性同步**

```typescript
class UnitComponent extends Component {
  update(deltaTime: number): void {
    // 从War3获取最新数据
    const currentHP = jasscj.GetUnitState(this.unitHandle, UNIT_STATE_LIFE);

    // 同步到Entity
    this.owner.set("当前生命值", currentHP);

    // 其他组件可以监听变化
    if (currentHP !== this.lastHP) {
      this.owner.emit("Unit.HealthChanged", {
        oldValue: this.lastHP,
        newValue: currentHP,
      });
    }
  }
}
```

### 3. **组件间协作**

```typescript
// 技能组件需要检查单位状态
class AbilityComponent extends Component {
  canCast(): boolean {
    // 通过Entity获取单位状态，而不是直接访问UnitComponent
    const isAlive = !this.owner.get("死亡");
    const currentMana = this.owner.get("当前魔法值");

    return isAlive && currentMana >= this.manaCost;
  }
}

// Buff组件影响单位属性
class BuffComponent extends Component {
  applyEffect(): void {
    // 通过Entity修改属性，UnitComponent会自动同步到War3
    const currentAttack = this.owner.get("攻击力");
    this.owner.set("攻击力", currentAttack + this.attackBonus);
  }
}
```

## 🚀 性能优化考虑

### 1. **组件按需加载**

```typescript
// 只有需要技能的单位才添加AbilityComponent
if (unitType === "英雄") {
  Entity.addComponent(AbilityComponent);
}

// 只有需要AI的单位才添加AIComponent
if (isAIControlled) {
  Entity.addComponent(AIComponent);
}
```

### 2. **事件优化**

```typescript
class UnitComponent extends Component {
  private lastPosition: Position;

  update(deltaTime: number): void {
    const currentPos = this.getPosition();

    // 只有位置真正改变时才触发事件
    if (!currentPos.equals(this.lastPosition)) {
      this.owner.emit("Unit.PositionChanged", currentPos);
      this.lastPosition = currentPos;
    }
  }
}
```

### 3. **内存管理**

```typescript
class Entity {
  destroy(): void {
    // 先销毁所有组件
    this.components.forEach((component) => {
      component.destroy();
    });

    // 清理组件引用
    this.components.clear();

    // 清理事件监听器
    this.eventBus.removeAllListeners();
  }
}
```

这样的设计让每个组件都专注于自己的职责，同时通过 Entity 这个统一的容器进行协调，既保持了代码的清晰性，又提供了极大的灵活性。
