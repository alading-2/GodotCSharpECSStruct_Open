# Data 目录说明

本目录存放所有**数据侧定义**与**数据配置资产**，与 `Src/` 中的运行时逻辑分离。

## 设计分层

- **`Src/`**：负责运行时逻辑与系统实现。
  - 例如：`Data.cs` 如何读写、`Entity.Events` 如何派发、伤害和技能如何执行。
- **`Data/`**：负责数据目录结构、配置、键定义与事件协议。
  - 例如：有哪些配置字段、有哪些 `DataKey`、有哪些事件类型、某个配置默认值是多少。

## 当前目录职责

### 1. `Config/`

**系统级配置路径**。

- **用途**：存放不属于某个具体 Entity 配置的系统参数、全局规则、生成规则等。
- **典型内容**：波次配置、Spawn 全局规则、全局开关、系统阈值。
- **特点**：更偏“系统配置”，不是 Entity 运行时状态，不直接等同于 `Data` 容器内容。

### 2. `Data/`

**数据配置路径**。

- **用途**：存放会被 `Data.LoadFromResource()` 读取的 Resource/Config 类。
- **典型内容**：`UnitConfig`、`PlayerConfig`、`EnemyConfig`、技能配置、目标指示器配置等。
- **核心职责**：定义“某类配置暴露哪些字段”，并通过 `[DataKey(nameof(DataKey.Xxx))]` 映射到 `Data` 容器。
- **详细说明**：见 [`Data/Data/README.md`](Data/README.md)

### 3. `DataKey/`

**DataKey 定义路径**。

- **用途**：集中定义所有可写入 `Data` 容器的数据键。
- **当前架构**：主流 DataKey 已从 `const string` 升级为 `static readonly DataMeta`。
- **核心职责**：定义键名、类型、默认值、分类、约束、是否支持修改器、是否为计算键等元数据。
- **详细说明**：见 [`Data/DataKey/README.md`](DataKey/README.md)

### 4. `EventType/`

**事件协议路径**。

- **用途**：定义 `Entity.Events` / `GlobalEventBus` 使用的事件类型与事件数据结构。
- **核心职责**：作为模块间通信契约，统一事件名与事件载荷。
- **典型内容**：`GameEventType_Data.cs`、Ability/Unit/Base 等分域事件定义。

### 5. `ResourceManagement/`

**资源注册与加载路径**。

- **用途**：集中管理场景、资源、预制体等资产引用。
- **说明**：这是资源索引系统，不等同于 `Data` 容器数据。
- **详细文档**：见 [`Data/ResourceManagement/README.md`](ResourceManagement/README.md)

## 与 `Src/ECS/Data` 的关系

- **`Data/` 目录**：定义“配置写什么、键叫什么、事件怎么约定”。
- **`Src/ECS/Data/`**：定义“运行时怎么加载、怎么存、怎么约束、怎么计算”。

可理解为：

- `Data/` 决定**数据内容与协议**。
- `Src/ECS/Data/` 决定**数据容器与运行时行为**。

## 常见工作流

### 新增一个可配置数据字段

1. 先在 `Data/DataKey/` 对应域中定义 `DataKey`。
2. 再在 `Data/Data/` 对应配置类中添加 `[DataKey(nameof(DataKey.Xxx))]` 属性。
3. 运行时由 `Data.LoadFromResource()` 注入到 Entity 的 `Data` 容器。

### 新增一个系统级规则

1. 放到 `Data/Config/`。
2. 如果它不是 Entity 运行时状态，通常**不需要**定义 `DataKey`。

### 新增一个事件协议

1. 在 `Data/EventType/` 对应域中定义事件类型与事件数据。
2. 在系统或组件中通过 `Entity.Events` / `GlobalEventBus` 使用。

## 维护规则

- **不要**把运行时业务逻辑写进 `Data/`。
- **不要**在 `Data/Data/` 里重复定义 `DataKey`。
- **不要**新增 `const string` DataKey（特殊引用键除外）。
- **优先使用** `[DataKey(nameof(DataKey.Xxx))]`，避免字符串字面量。
- **系统配置**放 `Config/`，**可注入 Data 的配置**放 `Data/`，**键定义**放 `DataKey/`，**事件契约**放 `EventType/`。
