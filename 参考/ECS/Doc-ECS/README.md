# 现代 ECS 框架总览（War3TS）

本文件系统性说明项目内的 ECS 框架实现，内容与当前代码保持一致，覆盖 Entity/Component/DataManager/Schema/事件系统/System/关系管理等模块，并提供示例与架构图。

## 设计目标与原则

- 组件化与单一职责：按功能拆分为独立组件，Entity 仅做容器与生命周期组织。
- 数据与逻辑彻底分离：DataManager 管数据与校验；业务逻辑在 Component 与 System。
- 事件驱动、低耦合：通过 EventComponent 与全局 EventBus 通信，减少硬依赖。
- 可扩展与可测试：模块边界清晰，接口稳定，支持逐步演进。

对标现代引擎：

- Unity ECS：Entity/Component/System 三段式职责；数据独立、系统逻辑集中。
- Unreal 组件体系：Actor=容器，Component=功能；数据与表现分离。

## 架构组成与职责矩阵

- Entity（`Entity.ts`）：游戏对象基类，容器与生命周期、事件入口、数据容器挂载、关系管理。
- Component/ComponentManager：功能模块化与生命周期管理；提供全局组件实例索引供 System 批处理。
- DataManager/Schema/SchemaRegistry：运行时数据定义、验证与计算属性缓存；与逻辑解耦。
- EventComponent/EventBus：每个 Entity 的事件组件 + 全局事件总线，实现解耦通信。
- System/SystemManager：跨 Entity 的无状态业务处理（如冷却）；统一时序与优先级。
- EntityRelationshipManager：Entity 间的有向关系管理（单位-物品、单位-特效等）。
- EntityManager：实体创建、查询与获取（统一配置创建、按组件查询）。

## 核心模块详解

### Entity（容器与生命周期）

- 关键字段：`id`、`entityType`、`componentManager`、`eventComponent`、`dataManagers`、`createdTime`。
- 生命周期：
  - `initialize()`：子类可覆盖；内部使用 `performInitialize()` 保护一次性执行。
  - `update(deltaTime)`：驱动 `ComponentManager.updateComponents`。
  - `destroy()`：销毁组件、清理数据管理器、移除所有关系、发射销毁事件。
- 组件/数据访问器（强类型映射，来自实际实现）：

```typescript
// 组件映射（示例，实际以代码中导出的组件为准）
entity.component.attr;
entity.component.unit;
entity.component.transform;
entity.component.effect;
// ...

// 数据映射（基于 SCHEMA_TYPES 注册的数据管理器）
entity.data.unit;
entity.data.attr;
entity.data.effect;
// ...
```

- 组件管理 API：`addComponent`/`addComponents`/`getComponent`/`removeComponent`/`hasComponent`。
- 数据管理 API：`addDataManager(schemaName)`/`getDataManager(schemaName)`/`removeDataManager`/`getPrimaryDataManager`。
- 事件 API：`on`/`once`/`off`/`emit`（内部自动确保存在 `EventComponent`）。
- 关系 API：`addRelationship`、`getChildEntities(relationType)`、`getParentEntities(relationType)`、`removeRelationship`。

参考实现：`src/Scripts/ECS/Entity/Entity.ts`

### Component 与 ComponentManager

- Component：统一生命周期与状态控制，拥有者 `owner: Entity`，属性 `props`，启用/禁用与初始化/销毁保护；必须定义唯一 `static getType()`。
- ComponentManager：
  - 负责创建、存储、查询、更新与销毁组件。
  - 维护静态全局索引 `globalComponentInstances`，供 System 直接批量访问同类组件，避免遍历实体。
  - 关键方法：`addComponent`、`getComponent`/`getComponentByName`、`removeComponent`、`updateComponents`、`destroyComponents`、`getAllComponents`、`getComponentCount`、`ComponentManager.getAllComponentInstances`。

参考实现：`src/Scripts/ECS/Component/Component.ts`、`src/Scripts/ECS/Component/ComponentManager.ts`

提示：渲染相关职责已合并到 `EffectComponent`（`RenderComponent` 已移除并整合）。

### DataManager、Schema 与 SchemaRegistry

- DataManager：
  - 运行时数据容器，关联 `schemaName` 与 `Schema` 定义。
  - 提供类型安全访问与校验：`get`、`set`、`add`、`multiplyValue`、`setMultiple`、`getMultiple`、`getData`、`setData`、`reset`、`clone`。
  - 计算属性缓存与失效：读取 `schema.computed`，按依赖自动失效。
  - 事件：属性变更触发 `EventTypes.DATA_PROPERTY_CHANGED`，重置触发 `EventTypes.DATA_RESET`。
  - 属性定义/校验：`getPropertyDefinition`、`hasProperty`、`validateData`。
- Schema：属性定义、类型与约束、计算属性、继承。
- SchemaRegistry：注册、获取、继承解析、定义验证、统计信息。

参考实现：`src/Scripts/ECS/Schema/DataManager.ts`、`Schema.ts`、`SchemaRegistry.ts`

### 事件系统（EventComponent + EventBus）

- 每个 Entity 通过 `EventComponent` 获得事件能力；无则按需自动添加。
- 全局 `EventBus` 提供 `on/once/off/offAll/emit/clear`，按优先级分发；一次性处理器自动回收。

参考实现：`src/Scripts/ECS/EventSystem/EventBus.ts`、`src/Scripts/ECS/Component/Components/事件/EventComponent.ts`

### System 与 SystemManager

- System：无状态、只含业务逻辑；由 `SystemManager` 管生命周期与执行顺序。
- 计时器驱动：基类集成 `TimerManager` 定时回调；子类覆盖 `update()` 实现批处理。
- 常用辅助：`getEntities([Component...])`、`getComponentInstances(Component)`（直接走全局组件索引，高效）。
- 示例：`CooldownSystem` 周期性批量更新所有 `CooldownComponent`，统一冷却处理。

参考实现：`src/Scripts/ECS/System/System.ts`、`SystemManager.ts`、`Systems/CooldownSystem.ts`

### EntityRelationshipManager（关系管理）

- 单向关系类型常量，如：`relationship.unit.item`、`relationship.unit.effect` 等。
- API：`addRelationship(parent, child, relationType, data?)`、`getRelationshipsByParentAndType`、`getRelationshipsByChildAndType`、`removeRelationship`、`removeAllRelationships`。
- Entity 侧便捷方法：`addRelationship`、`getChildEntities(relationType)`、`getParentEntities(relationType)`。

参考实现：`src/Scripts/ECS/Entity/EntityRelationshipManager.ts`

## 典型实体与用法

### 单位与物品实体

```typescript
// 单位（统一配置创建）
const unit = EntityManager.createWithConfig({
  entityType: "UnitEntity",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "FM剑圣" },
      isPrimary: true,
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 500 },
    },
  ],
  data: [
    { schemaName: SCHEMA_TYPES.TRANSFORM_DATA, initialData: { position: { x: 0, y: 0, z: 0 }, rotation: { heading: 270, pitch: 0, roll: 0 }, scale: { overallScale: 1 } } }
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent, props: { canRevive: true } },
    {
      type: UnitComponent,
      props: { unitType: "FM剑圣", playerComp, position, face: 270 },
    },
  ],
});

// 物品（统一配置创建）
const item = EntityManager.createWithConfig({
  entityType: "ItemEntity",
  data: [{ schemaName: SCHEMA_TYPES.ITEM_DATA, isPrimary: true }],
  components: [{ type: ItemComponent }],
});

// 建立单位-物品关系
unit.addRelationship(item, unit.entityRelationshipType.UNIT_TO_ITEM);

// 查询单位持有的物品
const items = unit.getChildEntities(unit.entityRelationshipType.UNIT_TO_ITEM);
```

### 特效实体（RenderComponent 合并说明）

```typescript
const effect = new EffectEntity("some-effect-id");
// 数据：SCHEMA_TYPES.EFFECT_DATA
// 数据：SCHEMA_TYPES.TRANSFORM_DATA
// 组件：TimerComponent、EffectComponent
// 说明：原 RenderComponent 的职责已合并进 EffectComponent；空间变换改为使用 TransformSchema 数据驱动
```

### 数据访问与事件

```typescript
// 类型安全的数据访问（DataManager）
const unitData = unit.data.unit;
unitData.set("生命值", 1200);
unitData.add("生命值", -100);

// 监听属性变更（走 Entity 事件）
unit.on("Entity.Data.PropertyChanged", (e) => {
  // e.key / e.oldValue / e.newValue
});
```

### 组件与系统

```typescript
// 组件添加/查询
unit.addComponent(EffectComponent);
const effectComp = unit.getComponent(EffectComponent);

// 系统侧批处理（示意）
class SomeSystem extends System {
  public static readonly TYPE = "SomeSystem";
  public static readonly PRIORITY = 50;
  public update(): void {
    const cooldowns = this.getComponentInstances(CooldownComponent);
    // 批处理所有冷却组件
  }
}
```

## 关键变更与一致性说明

- RenderComponent 已移除，其职责整合入 `EffectComponent`；请更新所有引用。
- 引入组件全局实例索引，系统可直接批处理同类组件，避免 Entity 遍历开销。
- `SCHEMA_TYPES` 统一标识数据管理器类型；Entity 的 `data`/`component` 提供强类型映射访问。
- Entity 在 `destroy()` 时会：清空数据管理器、销毁组件、移除所有关系并发送销毁事件。

## 架构图（更新版）

```mermaid
graph TD
  subgraph Entity Layer
    E[Entity]\n(id,type)
    CM[ComponentManager]
    DM[(DataManager*)]
    EC[EventComponent]
  end

  subgraph Data Layer
    SR[SchemaRegistry]
    S[(Schema*)]
  end

  subgraph Logic Layer
    C[(Components)]
    SYS[System]
    SM[SystemManager]
  end

  subgraph Infra
    BUS[EventBus]
    REL[EntityRelationshipManager]
  end

  E --> CM
  E --> DM
  E --> EC
  CM --> C
  DM --> SR
  SR --> S
  EC --- BUS
  SYS --> C
  SM --> SYS
  E --- REL
```

## 最佳实践

- 在 Entity 构造中仅挂载必要数据与核心组件，不写业务逻辑；业务放入组件/系统。
- 组件只处理自身职责，状态保存在 DataManager；跨对象协作用事件/关系。
- 系统优先批处理全局组件实例，避免 O(N×M) 实体-组件查询。
- 关系类型保持单向定义，反向查询通过索引完成，减少类型爆炸。
- Schema 以计算属性替代冗余字段，并合理使用缓存与失效策略。

## 参考文件

- `src/Scripts/ECS/Entity/Entity.ts`
- `src/Scripts/ECS/Component/Component.ts`
- `src/Scripts/ECS/Component/ComponentManager.ts`
- `src/Scripts/ECS/Schema/DataManager.ts`
- `src/Scripts/ECS/Schema/Schema.ts`
- `src/Scripts/ECS/Schema/SchemaRegistry.ts`
- `src/Scripts/ECS/EventSystem/EventBus.ts`
- `src/Scripts/ECS/System/System.ts`
- `src/Scripts/ECS/System/SystemManager.ts`
- `src/Scripts/ECS/Entity/EntityRelationshipManager.ts`
- `src/Scripts/ECS/Entity/Entitys/*.ts`
