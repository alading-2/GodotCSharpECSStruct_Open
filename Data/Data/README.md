# Godot Resource 属性映射规则 (DataKey 机制)

本文档说明 `Data/Data/` 目录下的 **Config / Resource 数据配置类** 如何映射到运行时 `Data` 容器。

## 1. 这个目录放什么

`Data/Data/` 存放的是**可被 `Data.LoadFromResource()` 读取的数据配置类**，例如：

- `UnitConfig`
- `PlayerConfig`
- `EnemyConfig`
- 技能配置
- 目标指示器配置

它们的职责不是实现逻辑，而是声明：

- 这个配置暴露哪些字段
- 每个字段对应哪个 `DataKey`
- 每个字段的默认值是多少

## 2. 当前推荐写法

### 2.1 使用 `[DataKey(nameof(DataKey.Xxx))]`

当前 Data 系统中，主流 `DataKey` 已升级为 `static readonly DataMeta`，因此 **Attribute 参数必须使用 `nameof`**，而不是直接传 `DataKey.Xxx`。

```csharp
[DataKey(nameof(DataKey.BaseHp))]
[Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;
```

### 2.2 默认值直接读取 `DataKey.Xxx.DefaultValue`

```csharp
[DataKey(nameof(DataKey.MoveSpeed))]
[Export] public float MoveSpeed { get; set; } = (float)DataKey.MoveSpeed.DefaultValue!;

[DataKey(nameof(DataKey.Team))]
[Export] public Team Team { get; set; } = (Team)DataKey.Team.DefaultValue!;
```

这样可以保证：

- 配置默认值与 `DataMeta` 注册值保持一致
- 不需要手动再去查 `DataRegistry.GetMeta(...)`
- `DataKey` 初始化时就会完成注册，避免旧方案的时序问题

## 3. 映射原理

系统在调用 `Data.LoadFromResource(resource)` 时，会执行以下逻辑：

1. 遍历 Resource 对象的公共属性
2. 检查属性上是否存在 `DataKeyAttribute`
3. 确定键名：
   - 有 `[DataKey]`：使用特性中的 `Key`
   - 没有 `[DataKey]`：回退到属性名
4. 将该属性值写入 `Data` 容器对应键位

## 4. 推荐与兼容写法

### 推荐写法

```csharp
[DataKey(nameof(DataKey.AttackRange))]
[Export] public float AttackRange { get; set; } = (float)DataKey.AttackRange.DefaultValue!;
```

### 兼容写法（按属性名回退）

```csharp
[Export] public float MoveSpeed { get; set; }
```

> [!WARNING]
> 按属性名回退没有编译期保护。
> 如果属性名改了，而目标 `DataKey` 没改，加载会静默偏离预期。
> **核心战斗数据、系统关键字段、跨组件共享字段必须显式写 `[DataKey(nameof(DataKey.Xxx))]`。**

## 5. 放到 `Data/Data/` 还是 `Data/Config/`

放到 `Data/Data/` 的情况：

- 会被 `Data.LoadFromResource()` 注入 Entity 的 `Data` 容器
- 字段本质上属于某个 Entity / Ability / Effect 的配置输入
- 字段需要映射到 `DataKey`

放到 `Data/Config/` 的情况：

- 是系统级全局参数
- 不是某个 Entity 的 Data 初始值
- 不需要通过 `DataKey` 注入运行时 Data 容器

## 6. 新增一个配置字段的步骤

1. 先在 `Data/DataKey/` 对应域中新增 `DataKey`
2. 为该键补齐 `DataMeta`（类型、默认值、分类、约束）
3. 在 `Data/Data/` 对应配置类中增加 `[Export]` 属性
4. 使用 `[DataKey(nameof(DataKey.Xxx))]` 显式绑定
5. 默认值优先直接取 `DataKey.Xxx.DefaultValue`

## 7. 常见误区

- ❌ `[DataKey(DataKey.BaseHp)]`
- ❌ 手写字符串：`[DataKey("BaseHp")]`
- ❌ 配置默认值自己重复写一份，与 `DataMeta.DefaultValue` 脱节
- ❌ 把系统全局规则也塞进 `Data/Data/`

## 8. 正确示例

```csharp
[GlobalClass]
public partial class UnitConfig : Resource
{
    [DataKey(nameof(DataKey.Name))]
    [Export] public string Name { get; set; } = (string)DataKey.Name.DefaultValue!;

    [DataKey(nameof(DataKey.BaseHp))]
    [Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;

    [DataKey(nameof(DataKey.MoveSpeed))]
    [Export] public float MoveSpeed { get; set; } = (float)DataKey.MoveSpeed.DefaultValue!;
}
```

## 9. 相关文档

- `Data/README.md`：`Data/` 顶层目录分工
- `Data/DataKey/README.md`：DataKey 与 DataMeta 定义规范
- `Src/ECS/Base/Data/README.md`：运行时 Data 容器说明
