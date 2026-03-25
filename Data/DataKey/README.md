# DataKey 定义规范

本文档说明 `Data/DataKey/` 目录中 **DataKey / DataMeta** 的定义方式、分类方式与新增规则。

## 1. 这个目录放什么

`Data/DataKey/` 负责定义所有可写入 `Data` 容器的数据键。

当前主流 DataKey 已升级为：

```csharp
public static readonly DataMeta Xxx = DataRegistry.Register(...);
```

而不是旧版的：

```csharp
public const string Xxx = "Xxx";
```

## 2. 当前目录分层

```text
Data/DataKey/
├── Base/       通用基础键
├── Attribute/  属性系统键
├── Unit/       单位运行时状态键
├── Ability/    技能相关键
├── Movement/   运动参数键
├── AI/         AI 状态与参数键
└── Effect/     特效相关键
```

原则：

- **按语义域拆分**，不要把所有键堆进一个文件
- **一个域一个 `DataKey_{域}.cs`**
- **分类枚举跟随域定义**，如 `DataCategory_Attribute`、`DataCategory_Movement`

## 3. DataKey 为什么改成 DataMeta

改造后的收益：

- 一个字段同时拥有 **键名 + 类型 + 默认值 + 约束 + 分类**
- `DataKey.Xxx.DefaultValue` 可直接给 Config 默认值使用
- 通过 `implicit operator string`，继续兼容 `Data.Get/Set(DataKey.Xxx)`
- 新增键时不必再拆到 `DataRegister_*.cs` 单独维护

## 4. 标准写法

### 4.1 普通键

```csharp
public static readonly DataMeta MoveSpeed = DataRegistry.Register(
    new DataMeta {
        Key = nameof(MoveSpeed),
        DisplayName = "移动速度",
        Description = "单位移动速度",
        Category = DataCategory_Attribute.Movement,
        Type = typeof(float),
        DefaultValue = 0f,
        MinValue = 0,
        SupportModifiers = true
    });
```

### 4.2 计算键

```csharp
public static readonly DataMeta FinalAttack = DataRegistry.Register(
    new DataMeta {
        Key = nameof(FinalAttack),
        DisplayName = "最终攻击力",
        Category = DataCategory_Attribute.Computed,
        Type = typeof(float),
        Dependencies = [nameof(BaseAttack), nameof(AttackBonus)],
        Compute = data => {
            float baseAttack = data.Get<float>(nameof(BaseAttack));
            float bonus = data.Get<float>(nameof(AttackBonus));
            return MyMath.AttributeBonusCalculation(baseAttack, bonus);
        }
    });
```

### 4.3 特殊引用键

对于 `Node2D`、运行时对象引用、无法稳定注册的特殊键，允许继续保留 `const string`：

```csharp
public const string TargetNode = "TargetNode";
```

## 5. 必填与常用字段

| 字段 | 作用 | 说明 |
| ---- | ---- | ---- |
| `Key` | 键名 | 必填，推荐始终写 `nameof(字段名)` |
| `Type` | 数据类型 | 必填，如 `typeof(float)` |
| `DefaultValue` | 默认值 | 推荐填写，便于 Config 直读 |
| `DisplayName` | 显示名 | 推荐填写，供 UI/编辑器展示 |
| `Description` | 描述 | 推荐填写 |
| `Category` | 分类 | 推荐填写，用于面板分组 |
| `MinValue/MaxValue` | 数值约束 | 数值型建议填写 |
| `SupportModifiers` | 是否支持修改器 | 属性型数据常用 |
| `Dependencies` | 依赖键列表 | 计算键使用 |
| `Compute` | 计算函数 | 计算键使用 |

## 6. 必须遵守的约定

### 6.1 字段名必须等于 Key

```csharp
// ✅ 正确
public static readonly DataMeta BaseHp = DataRegistry.Register(
    new DataMeta { Key = nameof(BaseHp), Type = typeof(float) });

// ❌ 错误
public static readonly DataMeta Hp = DataRegistry.Register(
    new DataMeta { Key = "BaseHp", Type = typeof(float) });
```

### 6.2 Config 上用 `nameof(DataKey.Xxx)`

```csharp
[DataKey(nameof(DataKey.BaseHp))]
[Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;
```

### 6.3 不要新增同名旧常量

迁移后的域中，不要再同时保留：

```csharp
public const string BaseHp = "BaseHp";
public static readonly DataMeta BaseHp = ...
```

## 7. 新增一个 DataKey 的步骤

1. 找到正确的语义域目录
2. 在 `DataKey_{域}.cs` 中新增 `static readonly DataMeta`
3. 若该域没有分类枚举，补充 `DataCategory_{域}.cs`
4. 如果需要被 Config 使用，在 `Data/Data/` 对应配置类中加 `[DataKey(nameof(DataKey.Xxx))]`
5. 如果是运行时特殊引用键，再评估是否保留 `const string`

## 8. 什么时候不要定义 DataKey

以下内容通常不该放进 `Data/DataKey/`：

- 纯局部临时变量
- 组件内部缓存引用
- 系统级全局常量配置
- 与 Entity 数据容器无关的资源路径索引

## 9. 相关文档

- `Data/README.md`：`Data/` 总目录分工
- `Data/Data/README.md`：配置类如何映射到 `Data`
- `Src/ECS/Data/README.md`：运行时 Data 容器说明
- `Src/ECS/Data/DataMeta.cs`：DataMeta 结构定义
- `Src/ECS/Data/DataRegistry.cs`：元数据注册表
