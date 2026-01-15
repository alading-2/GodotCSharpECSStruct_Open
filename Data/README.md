# Data 目录说明

本目录存放所有的**游戏数据定义**、**配置**与**资源**，严格与 `Src/` 目录下的逻辑代码分离。

## 设计理念：逻辑与数据分离 (Logic vs Data)

- **Src/** (Logic): 负责 **"怎么做" (How)**。
  - 包含 ECS 框架、系统运行逻辑、功能实现。
  - *例如：生命值如何被扣除、事件如何分发。*
- **Data/** (Data): 负责 **"是什么" (What)**。
  - 包含 属性定义、事件类型定义、平衡性数值、具体游戏内容。
  - *例如：有哪些属性（HP, MP）、有哪些事件（Dead, Hitted）、基础血量是多少。*

## 目录结构详解

### 1. [Config](Config/)
存放**全局静态配置**。通常是 `static class` 或 `const` 常量。
- **用途**：定义游戏的基础规则参数，如波次时长、最大属性限制等。
- **示例**：`Config.cs`, `SpawnSystemConfig.cs`

### 2. [Data](Data/)
存放**数据元定义 (Schema)** 与 **注册表 (Registry)**。
- **用途**：
    - **DataKey**: 定义游戏中存在哪些属性键名（如 `DataKey.BaseHp`）。
    - **Register**: 将这些属性键名注册到 ECS 系统中，并定义其默认值、计算公式等元数据。
- **示例**：`AttributeDataRegister.cs`, `DataKey_Attribute.cs`

### 3. [EventType](EventType/)
存放**事件协议定义**。
- **用途**：定义游戏中存在哪些事件类型（字符串常量）以及事件携带的数据结构（Record Struct）。这里不仅是字符串，更是模块间通信的**契约**。
- **示例**：`GameEventType_Data.cs`

### 4. [Resources](Resources/)
存放**具体游戏资产** (Godot Resources)。
- **用途**：Godot 的 `.tscn` 文件。代表具体的游戏实体或组件预制体。
- **示例**：`Enemy.tscn` (敌人实体模板), `HealthComponent.tscn`.
- **注意**：纯数值数据（如敌人属性）不再使用 `.tres` 文件，而是使用 `.cs` 静态类定义。

### 5. [ResourceRegistry](ResourceRegistry.cs)

**资源注册表** - 统一管理项目中所有 **资产 (Assets)** 的引用。

> 📖 **详细文档**：请参阅 [资源管理系统手册](ResourceManagement.md)

- **核心功能**：管理场景预制体等 Godot 资源。
- **配置方式**：在 `Data/ResourceRegistry.tscn` 场景中直接配置。
- **API 示例**：
    ```csharp
    // 加载场景
    ResourceRegistry.Load<PackedScene>("EnemyEntity");
    ```

### 6. 纯数据类 (Pure Data Classes)
- **位置**: `Data/Data/` (如 `EnemyData.cs`, `PlayerData.cs`)
- **用途**: 定义游戏单位的数值属性和生成规则。
- **优势**: 纯代码管理，易于重构和搜索，避免了 `.tres` 文件维护的繁琐。
- **示例**:
    ```csharp
    var config = EnemyData.Configs["豺狼人"];
    ```

---

## 维护指南

- **添加新属性**：在 `Data/Data/` 下对应的分类文件中添加 `DataKey`，并在 `Register` 文件中注册。
- **添加新事件**：在 `Data/EventType/` 下添加新的 `GameEventType` 定义。
- **修改平衡性**：
    - 全局规则：修改 `Data/Config/`。
    - 属性默认值/公式：修改 `Data/Data/` 下的 `Register`。
    - 具体单位数值：修改 `Data/Resources/` 下的 `.tres` 文件。
