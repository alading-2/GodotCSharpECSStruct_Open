### 背景与问题

在当前实现中，`EntityManager.create` 会直接 `new Entity`，而不少 `Entity` 构造函数会“自动添加组件”。但组件经常需要外部“构建参数”（如 `UnitComponent` 需要 `unitType/player/position/face`），或依赖 Schema 数据（如单位属性、类别等）。如果创建阶段没有统一注入这些数据，组件在 `initialize()` 时就可能读不到数据或产生时序问题。

为对齐现代 ECS 架构（Unity ECS/Unreal 组件体系），需要一个可复用、一次性完成“数据与组件装配”的创建管线：先注入数据，再添加组件（可带 props），最后初始化，避免后补和隐式依赖。

### 现代架构原则

- **Entity（容器）**: 只负责 ID、组件集合、生命周期与关系，不含业务。
- **DataManager（数据）**: 只负责 Schema 定义的数据存储、验证、监听，严格与逻辑解耦。
- **Component（逻辑）**: 仅包含逻辑；需要的“构建参数”通过 props 一次性注入；运行期状态从 DataManager 读取。
- **Builder/Prefab（构建器）**: 单一入口一次性装配 Entity：注入 Schema 初始数据 + 注册组件及其 props + 建立关系。

### 标准创建管线（强制顺序）

1. 创建 Entity（只创建容器）
2. 注入 DataManager（含 initialData，立即 initialize，确保可读）
3. 添加组件（可传入 props）
4. 初始化（各组件 `initialize()` 执行，读取 props 与 DataManager）

该顺序消除组件初始化的时序隐患：逻辑永远在数据准备之后执行。

### API 设计（推荐）

- 统一的创建配置结构

```ts
type DataConfig = {
  schemaName: string;
  initialData?: any;
  isPrimary?: boolean;
};

type ComponentConfig = {
  type: ComponentConstructor<any>;
  props?: any;
};

type EntityCreateConfig = {
  entityType: string;
  id?: string;
  data?: DataConfig[];
  components?: ComponentConfig[];
};

function createWithConfig(cfg: EntityCreateConfig): Entity {
  const e = EntityManager.create(cfg.entityType, cfg.id);
  // 1) DataManagers（初始化要先于组件）
  (cfg.data || []).forEach((d) => {
    e.addDataManager(d.schemaName, d.initialData); // add后立即 DataManager.initialize()
    if (d.isPrimary) {
      const dm = e.getDataManager(d.schemaName);
      if (dm) e.setPrimaryDataManager(dm);
    }
  });
  // 2) Components + props
  (cfg.components || []).forEach((c) => e.addComponent(c.type, c.props));
  return e;
}
```

- Prefab/Archetype（可复用的装配模板）

```ts
EntityPrefab.register("SWORDSMAN", {
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
    {
      schemaName: SCHEMA_TYPES.TRANSFORM_DATA,
      initialData: { position: { x: 0, y: 0, z: 0 }, rotation: { heading: 0 } },
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent, props: { canRevive: true } },
    // 需要外部依赖的组件（如 UnitComponent）实例化时再补 props
  ],
});

// 实例化时补齐一次性构建参数
EntityPrefab.instantiate("SWORDSMAN", {
  components: [
    {
      type: UnitComponent,
      props: { unitType: "FM剑圣", playerComp, position, face: 270 },
    },
  ],
});
```

### 组件编写规范（关键）

- 不要在“构造函数”里做重逻辑（如创建 War3 单位句柄）。将重逻辑统一放在 `initialize()`。
- 在 `initialize()` 中读取：
  - 优先 `this.props`（一次性构建参数，如玩家、位置、朝向与类型）。
  - 其次 `this.owner.data.xxx`（Schema 数据；允许纯数据驱动）。
- 在 `initialize()` 开头做必需参数校验（缺失则报错并 `disable()` 组件，避免崩溃）。
- 有组件依赖时用 `requireComponent()` 或显式检查；失败应记录错误并中止自身初始化。

示意（以 `UnitComponent` 为例）：

```ts
initialize(): void {
  const unitType = this.props.unitType ?? this.owner.data.unit?.get("单位类型");
  const player = this.props.playerComp;
  const position = this.props.position ?? new Position(0, 0);
  const face = this.props.face ?? 0;

  if (!unitType || !player) {
    // 记录错误并禁用组件，避免全局崩溃
    this.disable();
    return;
  }

  // 在此创建 War3 单位句柄、写入 DataManager 初始值等重逻辑
}
```

### Entity 默认装配策略

- 对“需要 props 的组件”（如 `UnitComponent`），不应在 `Entity` 构造中自动添加，应交由 Builder/Prefab 在创建时连同 props 一起添加。
- 可选策略：
  - 保留仅依赖 DataManager 且可无参初始化的组件在 `Entity` 构造中添加；
  - 或将所有组件的添加都交给 Builder/Prefab，保持一致性（推荐在新内容中采用）。

### 迁移路线（低风险）

1. 确保 `addDataManager()` 添加后立即对 DataManager 执行 `initialize()`，保证组件初始化前数据可读。
2. 将需要外部构建参数的组件（如 `UnitComponent`）把重逻辑从构造函数迁移到 `initialize()`，并支持从 DataManager 回退读取必要字段。
3. 引入 `createWithConfig`/Prefab，逐步把 `Entity` 构造中“自动添加此类组件”的逻辑迁移到 Builder/Prefab。
4. 新功能统一使用 Builder/Prefab；旧功能按模块逐步迁移，确保行为一致。

### 为什么这样做

- 对齐 Unity ECS/Unreal 组件架构：数据与逻辑分离、构建入口一致、依赖清晰、生命周期稳定。
- 杜绝时序坑：组件初始化永远在数据准备之后。
- 更易扩展与测试：Prefab/Archetype 可复用、可脚本化生成与 A/B 测试。

### 快速对照清单

- 创建时：先 DataManager（含初始数据、立即初始化），后 Components（可带 props）。
- 组件：重逻辑放 `initialize()`；优先读 props，再读 DataManager；缺参禁用并记录错误。
- Entity：避免在构造里“自动添加需要 props 的组件”。
- 构建：推荐通过 `createWithConfig` 或 Prefab/Archetype 完成一次性装配。

### 使用示例（落地）

以下示例统一从 `src/Scripts/ECS` 导出管理器和构建工具，从 `src/Scripts/ECS/Component` 导出组件，从 `src/Scripts/ECS/types/SchemaTypes` 导出 Schema 常量。

2. EntityPrefab 统一创建模式（推荐）

```ts
import { EntityPrefab } from "src/Scripts/ECS";
import {
  AttributeComponent,
  LifecycleComponent,
  UnitComponent,
  AbilityComponent,
} from "src/Scripts/ECS/Component";
import { SCHEMA_TYPES } from "src/Scripts/ECS/types/SchemaTypes";

// 先注册基础Prefab
EntityPrefab.register("SWORDSMAN_BASE", {
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
    {
      schemaName: SCHEMA_TYPES.TRANSFORM_DATA,
      initialData: { position: { x: 0, y: 0, z: 0 }, rotation: { heading: 0 } },
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent, props: { canRevive: true } },
    // 需要外部依赖的组件（如 UnitComponent）实例化时再补 props
  ],
});

// 使用EntityPrefab直接创建（智能组件合并）
const unit = EntityPrefab.create("SWORDSMAN_BASE", {
  components: [
    {
      type: UnitComponent,
      props: {
        unitType: "FM剑圣",
        playerComp,
        position,
        face: 270,
      },
    },
  ],
  data: [
    {
      schemaName: SCHEMA_TYPES.BUFF_DATA,
      initialData: { 当前Buff列表: [] },
    }, // 智能数据合并
  ],
});

// 高级用法：智能组件覆盖
const enhancedUnit = EntityPrefab.create("SWORDSMAN_BASE", {
  components: [
    {
      type: UnitComponent,
      props: {
        unitType: "精英剑圣", // 直接使用最终配置
        playerComp,
        position,
        face: 180,
      },
    },
    {
      type: LifecycleComponent,
      props: { canRevive: false, reviveTime: 0 }, // 智能覆盖原有配置
    },
  ],
}); // 最终只有一个UnitComponent实例，配置为精英剑圣
```

3. EntityPrefab 智能数据和组件合并

```ts
import { EntityPrefab } from "src/Scripts/ECS";
import {
  AttributeComponent,
  LifecycleComponent,
  UnitComponent,
  AbilityComponent,
} from "src/Scripts/ECS/Component";
import { SCHEMA_TYPES } from "src/Scripts/ECS/types/SchemaTypes";

// 注册基础剑士Prefab
EntityPrefab.register("SWORDSMAN", {
  entityType: "UnitEntity",
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "FM剑圣", 等级: 1 },
      isPrimary: true,
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 500, 基础攻击力: 30 },
    },
  ],
  components: [
    { type: AttributeComponent },
    { type: LifecycleComponent, props: { canRevive: true } },
  ],
});

// 方式1：直接实例化并智能合并数据和组件
const unit1 = EntityPrefab.create("SWORDSMAN", {
  data: [
    {
      // 相同schemaName智能覆盖
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 800, 基础攻击力: 50 }, // 智能覆盖原有属性
    },
  ],
  components: [
    {
      type: UnitComponent,
      props: { unitType: "FM剑圣", playerComp, position, face: 270 },
    },
    {
      // 智能组件合并：相同类型组件覆盖，不同类型组件追加
      type: LifecycleComponent,
      props: { canRevive: false, reviveTime: 0 }, // 覆盖原有LifecycleComponent配置
    },
    {
      type: AbilityComponent, // 新增组件类型
      props: { abilityId: "A001", abilityName: "剑气斩" },
    },
  ],
});

// 方式2：派生新的Prefab（精英剑士）
EntityPrefab.derive("SWORDSMAN", "ELITE_SWORDSMAN", {
  data: [
    {
      schemaName: SCHEMA_TYPES.UNIT_DATA,
      initialData: { 单位类型: "精英剑圣", 等级: 5 }, // 智能覆盖等级，保留其他属性
    },
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 1000, 基础攻击力: 80 }, // 智能提升属性
    },
  ],
  components: [
    {
      type: LifecycleComponent,
      props: { canRevive: true, reviveTime: 5 }, // 智能覆盖原有LifecycleComponent
    },
    {
      type: AbilityComponent, // 新增技能组件
      props: { abilityId: "A002", abilityName: "精英剑术" },
    },
  ],
});

// 使用派生的Prefab
const eliteUnit = EntityPrefab.create("ELITE_SWORDSMAN", {
  components: [
    {
      type: UnitComponent,
      props: { unitType: "精英剑圣", playerComp, position, face: 180 },
    },
    {
      // 再次智能覆盖AbilityComponent
      type: AbilityComponent,
      props: { abilityId: "A003", abilityName: "终极剑术" },
    },
  ],
});
```

### 统一 EntityPrefab 设计（推荐方案）

基于现代 ECS 最佳实践和框架简化原则，我们采用统一的 EntityPrefab 设计：

#### 核心特性

1. **智能配置合并**：实现数据和组件的智能覆盖

   - 相同 `schemaName`的数据配置智能覆盖（而非重复添加）
   - 不同 `schemaName`的数据配置追加到配置列表
   - **相同类型组件智能覆盖**：符合现代 ECS 设计原则，一个 Entity 只能拥有一个同类型组件实例
   - 所有合并逻辑统一实现，零重复代码
   - 解决了"数据管理器已存在"和"组件重复添加"的问题

2. **模板继承与统一创建**：支持基于现有 Prefab 创建派生模板

   - `EntityPrefab.create()` - 统一实例化入口，支持智能数据和组件合并
   - `EntityPrefab.derive()` - 基于现有 Prefab 派生新 Prefab
   - `EntityPrefab.createBatch()` - 批量创建 Entity 实例
   - `EntityPrefab.createWithComponents()` - 创建并添加额外组件
   - `EntityPrefab.createWithData()` - 创建并添加额外数据

3. **现代游戏引擎特性**：

   - 批量注册支持（`registerBatch`）
   - 统计监控（`getStats`）
   - 配置缓存和延迟初始化
   - 深拷贝保护，避免意外修改原始配置

4. **类型安全**：完整的 TypeScript 类型支持
5. **开发者友好**：提供清空、统计等调试和测试辅助功能
6. **简化设计原则**：

   - **单一职责**：EntityPrefab 专注于 Entity 创建和配置管理
   - **统一入口**：所有 Entity 创建都通过 EntityPrefab 系统
   - **简洁 API**：移除冗余的 Builder 类，直接使用配置对象
   - **类型安全**：统一的 Prefab 系统提供更好的类型推导
   - **现代设计**：符合现代游戏引擎的简洁高效原则

#### 设计优势

**统一的创建入口**

- 所有 Entity 创建都通过 EntityPrefab.create()完成
- 支持智能配置合并，避免重复数据和组件
- 提供现代游戏引擎特性：批量创建、链式操作等

**简化的 API 设计**

- 移除了复杂的 Builder 模式，直接使用配置对象
- 减少学习成本和维护负担
- 更好的 IDE 支持和类型推导

**现代游戏引擎特性**

- 批量操作：createBatch、registerBatch
- 链式操作：createWithComponents、createWithData
- 统计监控：getStats、配置验证等

4. 批量注册和统计监控（现代游戏引擎特性）

```ts
// 批量注册Prefab配置
const prefabConfigs = {
  WARRIOR: {
    entityType: "UnitEntity",
    data: [
      { schemaName: SCHEMA_TYPES.UNIT_DATA, initialData: { 单位类型: "战士" } },
    ],
    components: [{ type: AttributeComponent }],
  },
  MAGE: {
    entityType: "UnitEntity",
    data: [
      { schemaName: SCHEMA_TYPES.UNIT_DATA, initialData: { 单位类型: "法师" } },
    ],
    components: [{ type: AttributeComponent }, { type: AbilityComponent }],
  },
  ARCHER: {
    entityType: "UnitEntity",
    data: [
      {
        schemaName: SCHEMA_TYPES.UNIT_DATA,
        initialData: { 单位类型: "弓箭手" },
      },
    ],
    components: [{ type: AttributeComponent }],
  },
};

EntityPrefab.registerBatch(prefabConfigs);

// 获取统计信息
const stats = EntityPrefab.getStats();
console.log(`总Prefab数量: ${stats.totalPrefabs}`);
console.log(`平均组件数: ${stats.averageComponentsPerPrefab}`);
console.log(`按类型分布:`, stats.prefabsByEntityType);
console.log(`内存使用情况:`, stats.memoryUsage);

// 清空所有Prefab（用于测试或重新加载）
EntityPrefab.clear();

// 高级用法：链式创建（添加额外组件和数据）
const advancedWarrior = EntityPrefab.createWithComponents(
  "WARRIOR",
  [
    {
      type: AbilityComponent,
      props: {
        abilityId: "A001",
        abilityName: "战士技能",
      },
    },
  ],
  [
    {
      schemaName: SCHEMA_TYPES.BUFF_DATA,
      initialData: { 当前Buff列表: ["力量增强"] },
    },
  ]
);

// 基于现有Prefab派生并注册
EntityPrefab.derive("WARRIOR", "ELITE_WARRIOR", {
  data: [
    {
      schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
      initialData: { 基础生命值: 1000, 基础攻击力: 100 },
    },
  ],
  components: [
    {
      type: AbilityComponent,
      props: { abilityId: "A002", abilityName: "精英战技" },
    },
  ],
});
```

#### 性能与架构优势

- **减少 API 表面积**：单一入口降低学习成本
- **统一配置管理**：所有实体创建都通过 Prefab 系统
- **智能合并策略**：避免重复数据管理器和组件实例，符合现代 ECS 设计原则
- **更好的 IDE 支持**：统一的类型系统提供更好的自动补全
- **缓存优化**：Prefab 可以预编译和缓存配置
- **版本管理**：支持 Prefab 版本化和热更新
- **内存优化**：深拷贝保护和智能合并减少内存占用
- **开发者体验**：链式构建、批量注册、统计监控等现代开发工具

建议在 EntityManager 构建流程、DataManager 初始化、Component 添加/初始化处输出结构化日志，便于排查。
