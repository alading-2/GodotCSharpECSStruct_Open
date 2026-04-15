---
name: data-authoring
description: 编写或修改 Data 目录下的数据配置、Config、DataKey、EventType、Resource 映射规则时使用。适用于：新增配置字段、设计 DataKey 分域、决定字段放 Data/Data 还是 Data/Config、编写事件协议。触发关键词：Data目录、Config配置、DataKey定义、EventType、数据配置、Resource映射。
---

# Data 目录数据配置规范

## 适用范围

当你修改以下目录时，优先遵循本 Skill：

- `Data/Config/`
- `Data/Data/`
- `Data/DataKey/`
- `Data/EventType/`

它关注的是 **数据内容与协议怎么组织**，而不是运行时 `Data` 容器本身如何工作。

## 与 `ecs-data` 的分工

- **`data-authoring`**：负责 `Data/` 目录下的配置结构、DataKey 定义、事件协议、字段映射
- **`ecs-data`**：负责 `Src/ECS/Base/Data/` 运行时容器、`DataMeta`、`DataRegistry`、读写规则

## 目录职责

### `Data/Config/`

放系统级配置：

- 波次规则
- Spawn 全局参数
- 系统开关
- 阈值与限制

不要放：

- 某个 Entity 的初始属性输入
- 需要注入 Entity.Data 的字段

### `Data/Data/`

放会被 `Data.LoadFromResource()` 读取的配置类：

- `UnitConfig`
- `EnemyConfig`
- `PlayerConfig`
- 技能配置
- 特效配置

补充约定：

- `FeatureGroupId` 只表示技能展示分组，应放在 `Data/Data/Ability/AbilityConfig.cs`；测试面板按完整 `FeatureGroupId` 分组和显示；运行时执行器选择必须使用 `FeatureHandlerId`
- 不要再为技能额外维护 `AbilityCategory` 这类重复展示字段；只要运行时实体和系统要读，就属于 `Data/Data/`

推荐写法：

```csharp
[DataKey(nameof(DataKey.BaseHp))]
[Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;
```

### `Data/DataKey/`

放所有 DataKey 与 DataMeta 定义。

主流写法：

```csharp
public static readonly DataMeta BaseHp = DataRegistry.Register(
    new DataMeta {
        Key = nameof(BaseHp),
        Type = typeof(float),
        DefaultValue = 0f
    });
```

特殊引用键才允许保留 `const string`。

### `Data/EventType/`

放模块间通信协议：

- 事件名
- 事件数据结构
- Base / Unit / Ability / Data 等分域事件定义

## 决策规则

### 什么时候字段要进 `Data/Data/`

满足任一项通常就该进入：

- 要成为 Entity 的初始 Data
- 要被 `Data.LoadFromResource()` 自动注入
- 要映射到 `DataKey`
- 要被多个运行时组件/系统读取

### 什么时候字段只放 `Data/Config/`

满足以下特征通常只放系统配置：

- 不属于具体 Entity
- 不是运行时 Data 状态
- 不需要映射到 `DataKey`
- 更像全局规则或系统参数

## 新增配置字段标准流程

1. 先判断它属于 `Config` 还是 `Data`
2. 若属于运行时 Data，先在 `Data/DataKey/` 定义 `DataKey`
3. 补齐 `DataMeta`（类型、默认值、分类、约束）
4. 在 `Data/Data/` 配置类中添加 `[DataKey(nameof(DataKey.Xxx))]`
5. 默认值优先直接读取 `DataKey.Xxx.DefaultValue`
6. 如涉及通信，再补 `Data/EventType/` 契约

## 禁止事项

- ❌ 在 `Data/Data/` 里直接发明字符串键
- ❌ 新增 `const string` DataKey（特殊引用键除外）
- ❌ 把系统配置误塞到 `Data/Data/`
- ❌ 把某个 Entity 的初始字段误塞到 `Data/Config/`
- ❌ 在 `Data/` 目录写运行时业务逻辑

## 推荐联动检查

修改 `Data/` 目录后，通常需要顺手检查：

- `Src/ECS/Base/Data/README.md` 是否仍和现状一致
- `Docs/框架/项目索引.md` 是否需要补导航
- `Docs/框架/ECS/Data/` 主文档是否需要同步
- `.windsurf/skills/ecs-data/SKILL.md` 是否需要联动更新
