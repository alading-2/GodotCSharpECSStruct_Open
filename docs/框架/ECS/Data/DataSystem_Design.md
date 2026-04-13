# Data 系统说明

本文档是 **Data 系统唯一主说明文档**，描述项目当前正在使用的 Data 架构。

## 1. 系统定位

Data 系统解决两件事：

- **运行时状态容器**：Entity 的业务状态统一存入 `Data`
- **数据协议与配置映射**：`DataKey`、Config、EventType 统一描述“有哪些数据、如何进入运行时”

核心原则：**Data 是唯一数据源**。

## 2. 目录分工

### 2.1 运行时层：`Src/ECS/Base/Data/`

负责“运行时怎么存、怎么取、怎么约束、怎么计算”。

- `Data.cs`：核心数据容器
- `DataMeta.cs`：键的元数据定义
- `DataRegistry.cs`：元数据注册表
- `DataKeyAttribute.cs`：Config 字段到 DataKey 的映射特性

### 2.2 数据内容层：`Data/`

负责“配置写什么、键叫什么、事件怎么约定”。

```text
Data/
├── Config/      系统级配置
├── Data/        可被 Data.LoadFromResource() 读取的配置类
├── DataKey/     DataKey 与 DataMeta 定义
└── EventType/   事件协议定义
```

## 3. 当前核心架构

```text
Config Resource
   ↓  [DataKey(nameof(DataKey.Xxx))]
Data.LoadFromResource()
   ↓
Data（运行时容器）
   ↓
DataRegistry 查询 DataMeta
   ↓
约束 / 默认值 / 修改器 / 计算键
```

## 4. DataKey 的当前方案

当前主流 DataKey 已从 `const string` 升级为 `static readonly DataMeta`。

```csharp
public static readonly DataMeta BaseHp = DataRegistry.Register(
    new DataMeta {
        Key = nameof(BaseHp),
        DisplayName = "基础生命",
        Category = DataCategory_Attribute.Health,
        Type = typeof(float),
        DefaultValue = 0f,
        MinValue = 0,
        SupportModifiers = true
    });
```

收益：

- 一个字段同时持有 **键名、类型、默认值、约束、分类**
- `Config` 默认值可直接读取 `DataKey.Xxx.DefaultValue`
- `Data.Get/Set(DataKey.Xxx)` 保持兼容

特殊引用键（如 `Node2D` 引用）仍允许保留 `const string`。

## 5. Data 容器的运行规则

### 5.1 普通键

- 写入时应用 `DataMeta` 约束
- 读取时使用 `DataMeta.DefaultValue` 或类型默认值

### 5.2 属性键

- 当 `SupportModifiers = true` 时支持修改器系统
- 最终值公式：

```text
最终值 = (基础值 + Σ加法修改器) × Π乘法修改器
```

### 5.3 计算键

- 通过 `Dependencies` + `Compute` 定义
- Data 会根据依赖关系进行计算与脏标记失效

### 5.4 数据变化通知

- 不再使用 `Data.On()`
- 统一通过 `Entity.Events` 监听 `GameEventType.Data.PropertyChanged`

## 6. Config 映射规则

`Data/Data/` 下的配置类推荐写法：

```csharp
[DataKey(nameof(DataKey.BaseHp))]
[Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;
```

规则：

- `[DataKey]` 参数使用 `nameof(DataKey.Xxx)`
- 默认值优先直接读取 `DataKey.Xxx.DefaultValue`
- 核心字段不要依赖“按属性名回退”

## 7. 常见工作流

### 新增一个可配置字段

1. 在 `Data/DataKey/` 中新增 `DataKey`
2. 补齐 `DataMeta`（类型、默认值、分类、约束）
3. 在 `Data/Data/` 配置类中增加 `[DataKey(nameof(DataKey.Xxx))]`
4. 运行时通过 `Data.LoadFromResource()` 注入到 Entity.Data

### 新增一个系统级规则

1. 放到 `Data/Config/`
2. 如果不是 Entity 运行时状态，通常不需要定义 `DataKey`

### 新增一个事件协议

1. 在 `Data/EventType/` 中定义事件类型与事件数据
2. 通过 `Entity.Events` 或 `GlobalEventBus` 使用

## 8. 必须遵守的约定

- ❌ 不要在业务代码里写字符串字面量访问 Data
- ❌ 不要新增主流 `const string` DataKey
- ❌ 不要使用 `Data.On()` 监听数据变化
- ❌ 不要把系统全局配置误放进 `Data/Data/`
- ✅ 运行时规则看 `Src/ECS/Base/Data/README.md`
- ✅ 数据目录写法看 `Data/README.md`、`Data/Data/README.md`、`Data/DataKey/README.md`

## 9. 相关文档

- `Src/ECS/Base/Data/README.md`：运行时 Data 容器使用指南
- `Data/README.md`：`Data/` 顶层目录职责
- `Data/Data/README.md`：Config / Resource 映射规范
- `Data/DataKey/README.md`：DataKey / DataMeta 定义规范
- `Docs/框架/项目索引.md`：项目级导航入口
