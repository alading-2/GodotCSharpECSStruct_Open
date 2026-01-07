# EntityManager 精简版说明

## 🎯 精简原则

从整体 Entity 框架的角度，EntityManager 应该专注于**核心职责**，移除了所有不必要的复杂功能。

## ❌ 移除的复杂功能

### 1. 过度复杂的性能统计系统

```typescript
// 移除前 - 复杂的统计功能
private stats = {
    totalCreated: 0,
    totalDestroyed: 0,
    updateCalls: 0,
    lastUpdateTime: 0
};

getStats(): ComplexStatsObject { /* 100+ 行代码 */ }
getPerformanceReport(): string { /* 50+ 行代码 */ }
```

**移除原因**：

- 游戏运行时不需要这么详细的统计
- 增加了内存开销和复杂度
- 调试时可以用简单的日志替代

### 2. 复杂的事件系统集成

```typescript
// 移除前 - 复杂的事件总线
private eventBus: EventBus;
private setupEventListeners(): void { /* 复杂的事件监听 */ }
```

**移除原因**：

- Entity 本身已有事件系统
- 管理器层面的事件监听是多余的
- 增加了不必要的耦合

### 3. 过度设计的数据组件化架构支持

**移除原因**：

- 这些功能应该在 Entity 层面处理
- 管理器不应该关心具体的组件架构
- 违反了单一职责原则

### 4. 复杂的批量操作

**移除原因**：

- 简单的循环调用 create()即可
- 复杂的批量操作增加了维护成本
- 实际使用中很少需要这么复杂的批量操作

## ✅ 保留的核心功能

### 1. 统一配置创建与构建工具

```typescript
type DataConfig = { schemaName: string; initialData?: any; isPrimary?: boolean };
type ComponentConfig = { type: ComponentConstructor<any>; props?: any };

createWithConfig(cfg: { entityType: string; id?: string; data?: DataConfig[]; components?: ComponentConfig[] }): Entity
```

- **职责**：一次性装配 DataManager 与 Component（含 props）
- **必要性**：保证“先数据、后组件”的初始化时序；与 Builder/Prefab 打通

### 2. 对象创建和销毁

```typescript
create(interfaceName: string, id?: string): Entity
destroy(id: string): boolean
destroyImmediate(id: string): boolean
```

- **职责**：管理 Entity 的生命周期
- **必要性**：框架的核心功能

### 3. 对象查找和访问

```typescript
get(id: string): Entity | undefined
getByType(interfaceName: string): Entity[]
find(predicate: Function): Entity[]
```

- **职责**：提供灵活的对象查找机制
- **必要性**：游戏逻辑经常需要查找特定对象

### 4. 生命周期管理

```typescript
update(deltaTime: number): void
processDestroyQueue(): void
```

- **职责**：统一的更新和销毁处理
- **必要性**：确保对象正确的生命周期管理

## 🚀 精简后的优势

### 1. **代码量大幅减少**

- **精简前**：652 行代码，功能复杂
- **精简后**：267 行代码，职责清晰

### 2. **性能提升**

- 移除了不必要的统计和事件处理开销
- 简化了对象创建流程
- 减少了内存占用

### 3. **维护性提升**

- 代码逻辑清晰，易于理解
- 减少了出错的可能性
- 新人更容易上手

### 4. **职责单一**

- 专注于 Entity 的核心管理
- 不再承担过多的辅助功能
- 符合单一职责原则

## 📋 使用示例

### 基本使用

```typescript
// 方式1：使用EntityPrefab（推荐）
// 先注册Prefab
EntityPrefab.register("HERO_SWORDSMAN", {
  entityType: "UnitEntity",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "FM剑圣", 等级: 1 },
      isPrimary: true,
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 800, 基础攻击力: 50 },
    },
  ],
  data: [
    {
      schemaName: SCHEMA_TYPES.TRANSFORM_DATA,
      initialData: { position: { x: 0, y: 0, z: 0 }, rotation: { heading: 0, pitch: 0, roll: 0 } }
    }
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent },
  ],
});

// 创建实例
const hero = EntityPrefab.create("HERO_SWORDSMAN", {
  entityId: "hero_001",
  data: [
    {
      // 智能覆盖：相同schemaName会覆盖，不同会追加
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 1000 }, // 覆盖为1000
    },
  ],
  components: [
    {
      // 智能数据合并：相同 schemaName 覆盖
      schemaName: SCHEMA_TYPES.TRANSFORM_DATA,
      initialData: { scale: { overallScale: 1.2 }, rotation: { heading: 0 } },
    },
    {
      type: UnitComponent, // 新增组件类型
      props: { unitType: "FM剑圣", playerComp, position, face: 270 },
    },
  ],
});

// 方式2：直接使用EntityManager.createWithConfig
const hero2 = EntityManager.createWithConfig({
  entityType: "UnitEntity",
  entityId: "hero_002",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "FM剑圣" },
      isPrimary: true,
    },
  ],
  components: [{ type: AttributeComponent }],
});

// 查找对象
const allUnits = EntityManager.getByType("UnitEntity");
const specificUnit = EntityManager.get(hero.getId());

// 销毁对象
EntityManager.destroy(hero.getId());
```

### 高级查找

```typescript
// 查找满足条件的对象
const aliveUnits = EntityManager.find((obj) => {
  const unitData = obj.data.unit;
  return unitData && !unitData.get("死亡");
});

// 按类型批量销毁
EntityManager.destroyAllByType("Inte_Unit");
```

### EntityPrefab高级用法

```typescript
// 1. 使用PrefabBuilder链式构建器（智能组件合并）
const customHero = EntityPrefab.builder("HERO_SWORDSMAN")
  .addData(SCHEMA_TYPES.SKILL_DATA, { 技能等级: 5 })
  .addComponent(SkillComponent, { skillId: "CRITICAL_STRIKE" })
  .addData(SCHEMA_TYPES.TRANSFORM_DATA, { scale: { overallScale: 1.5 }, rotation: { heading: 45 } }) // 智能覆盖 Transform 数据
  .build();

// 2. 派生新的Prefab（智能数据和组件合并）
EntityPrefab.derive("HERO_SWORDSMAN", "ELITE_SWORDSMAN", {
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "精英剑圣", 等级: 10 }, // 覆盖等级
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 1500, 基础攻击力: 100 }, // 提升属性
    },
  ],
  components: [
    {
      type: AttributeComponent,
      props: { bonusMultiplier: 1.5 }, // 智能覆盖原有AttributeComponent
    },
    {
      type: SkillComponent, 
      props: { skillId: "WHIRLWIND" }, // 添加新技能组件
    },
  ],
});

// 使用派生的Prefab
const eliteHero = EntityPrefab.create("ELITE_SWORDSMAN", {
  entityId: "elite_hero_001",
});

// 3. 注册为新的Prefab（PrefabBuilder高级功能）
const builderResult = EntityPrefab.builder("HERO_SWORDSMAN")
  .addData(SCHEMA_TYPES.EQUIPMENT_DATA, { 武器: "传说之剑" })
  .addComponent(EquipmentComponent, { maxSlots: 8, autoEquip: true })
  .addComponent(AttributeComponent, { bonusMultiplier: 2.0 }) // 智能覆盖
  .registerAs("LEGENDARY_SWORDSMAN"); // 注册为新Prefab

// 后续可以直接使用注册的Prefab
const legendaryHero = EntityPrefab.create("LEGENDARY_SWORDSMAN");

// 4. 批量注册和统计监控（现代游戏引擎特性）
const heroConfigs = {
  "WARRIOR": {
    entityType: "UnitEntity",
    data: [{ schemaName: SCHEMA_TYPES.UNIT_DATA, initialData: { 单位类型: "战士" } }],
    data: [{ schemaName: SCHEMA_TYPES.TRANSFORM_DATA, initialData: { position: { x: 0, y: 0, z: 0 } } }],
    components: [{ type: AttributeComponent }, { type: LifecycleComponent }]
  },
  "MAGE": {
    entityType: "UnitEntity",
    data: [{ schemaName: SCHEMA_TYPES.UNIT_DATA, initialData: { 单位类型: "法师" } }],
    components: [{ type: AttributeComponent }, { type: SkillComponent }]
  }
};

EntityPrefab.registerBatch(heroConfigs);

// 获取统计信息
const stats = EntityPrefab.getStats();
console.log(`总Prefab数量: ${stats.totalPrefabs}`);
console.log(`平均组件数: ${stats.averageComponentsPerPrefab}`);
```

## 🎯 设计哲学

精简后的 EntityManager 遵循以下设计哲学：

1. **简单即美**：只保留最核心的功能
2. **职责单一**：专注于对象生命周期管理
3. **性能优先**：移除不必要的开销
4. **易于维护**：代码清晰，逻辑简单

这个精简版本更符合 Entity 框架的整体设计理念，让管理器专注于它应该做的事情，而不是试图做所有的事情。
