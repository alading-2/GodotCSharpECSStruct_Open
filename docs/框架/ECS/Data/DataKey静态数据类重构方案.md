# DataKey 静态数据类重构方案

> 状态：设计草案  
> 目标：统一 DataKey 字符串常量与 DataMeta 元数据，彻底解决 Config 默认值时序问题  
> 样品范围：`Data/DataKeyRegister/Base` 域

---

## 1. 现状与问题

### 1.1 当前双文件结构

每个域都有两个并列文件，职责完全割裂：

```
DataKeyRegister/Base/
    DataKey_Base.cs        ← 只有字符串常量
    DataRegister_Base.cs   ← 只有 DataMeta 注册逻辑
```

**DataKey_Base.cs（信息缺失）：**
```csharp
public static partial class DataKey
{
    public const string Name = "Name";
    public const string BaseHp = "BaseHp";
}
```

**DataRegister_Base.cs（信息分离）：**
```csharp
[ModuleInitializer]
public static void Initialize()
{
    AutoLoad.Register(new AutoLoad.AutoLoadConfig { InitAction = Init, Priority = Core });
}
public static void Init()
{
    DataRegistry.Register(new DataMeta { Key = DataKey.Name, DisplayName = "名称", DefaultValue = "", ... });
    DataRegistry.Register(new DataMeta { Key = DataKey.BaseHp, DisplayName = "基础生命", DefaultValue = 10f, ... });
}
```

### 1.2 问题清单

| 问题 | 说明 |
|------|------|
| **信息分离** | Key 的名字在 DataKey_*.cs，类型/默认值在 DataRegister_*.cs，阅读/修改需跳转两文件 |
| **维护漂移** | 新增一个 Key 必须同时修改两个文件，极易遗漏其中一个 |
| **时序错误** | `DataRegistry.Register` 经 AutoLoad 排队，在 `_Ready()` 后才真正执行 |
| **Config 不可用** | Config 属性初始化器早于 `_Ready()`，`DataRegistry.GetMeta(key)` 返回 null |

### 1.3 时序根因

```
程序集加载
  └─ [ModuleInitializer] 执行
       └─ AutoLoad.Register(排队，不立即执行)
            ↓ ... 等待场景树 ...
Config.tres 资源实例化          ← Config 属性初始化器在此执行
  └─ DataRegistry.GetMeta("BaseHp") → null ← 注册还没发生！
            ↓
     游戏启动 _Ready()
       └─ AutoLoad.Init() 批量执行
            └─ DataRegistry.Register(...)   ← 太晚了
```

---

## 2. 新方案：静态数据类

### 2.1 核心思路

将 DataKey 字符串和 DataMeta 元数据**合并到同一个类的同一个字段**中。

`DataKey.BaseHp` 不再是 `string` 常量，而是持有完整元数据的 `DataMeta` 静态字段。

```
之前：DataKey.BaseHp → "BaseHp"（字符串，无任何信息）
之后：DataKey.BaseHp → DataMeta { Key="BaseHp", DisplayName="基础生命", Type=float, DefaultValue=10f, ... }
```

**静态字段在类首次被访问时立即初始化**（C# 语言保证），不经过 AutoLoad 队列，彻底消除时序问题。

### 2.2 新时序

```
程序集加载
  └─ [ModuleInitializer] 执行（可选，用于提前触发初始化）
       └─ 访问 DataKey.Name → 触发 DataKey 类静态初始化
            └─ DataRegistry.Register(...) 立即执行
Config.tres 资源实例化
  └─ DataKey.BaseHp.DefaultValue → DataKey 已初始化 → 直接返回 10f ✓
游戏启动 _Ready()
  └─ DataRegistry.GetMeta("BaseHp") → 已注册 → 正常返回 ✓
```

---

## 3. 变更详情

### 3.1 DataRegistry.Register 改为返回 DataMeta

**文件：`Src/ECS/Data/DataRegistry.cs`**

```csharp
// 之前
public static void Register(DataMeta meta)
{
    _metaRegistry[meta.Key] = meta;
}

// 之后（返回 DataMeta，便于赋值给静态字段）
public static DataMeta Register(DataMeta meta)
{
    _metaRegistry[meta.Key] = meta;
    return meta;
}
```

### 3.2 DataMeta 增加隐式字符串转换

**文件：`Src/ECS/Data/DataMeta.cs`**

`Data.Get<T>(key)` 当前接受 `string`。新方案中 `DataKey.BaseHp` 是 `DataMeta`，为避免所有调用处改成 `.Key`，在 `DataMeta` 上添加隐式转换：

```csharp
public class DataMeta
{
    // ...现有代码不变...

    // 新增：隐式转换为字符串（Key），使 DataMeta 可直接用于字典键/方法参数
    public static implicit operator string(DataMeta meta) => meta.Key;
}
```

这样现有代码 `data.Get<float>(DataKey.BaseHp)` 在 `DataKey.BaseHp` 变为 `DataMeta` 后**无需修改**，隐式转换自动传 Key 字符串。

### 3.3 DataKeyAttribute 适配

**文件：`Src/ECS/Data/DataKeyAttribute.cs`**

`[DataKey]` Attribute 参数必须是编译期常量（C# 语言约束），无法直接传 `DataMeta` 实例。  
利用 `nameof` 表达式：**强制约定 `DataMeta` 字段名与 `DataMeta.Key` 值相同**，则 `nameof(DataKey.BaseHp)` == `"BaseHp"` == `DataKey.BaseHp.Key`。

```csharp
// 之前（传 const string）
[DataKey(DataKey.BaseHp)]           // DataKey.BaseHp 是 const string = "BaseHp"

// 之后（传 nameof，结果等价）
[DataKey(nameof(DataKey.BaseHp))]   // nameof 结果是 "BaseHp"，与 DataKey.BaseHp.Key 相等
```

> ⚠️ **强制约定**：所有 `DataMeta` 静态字段名必须与其 `Key` 值完全一致。  
> 例：字段名 `BaseHp` → `Key = "BaseHp"`，字段名 `CritDamage` → `Key = "CritDamage"`。

`DataKeyAttribute` 本身无需修改，仍接受 `string key`。

### 3.4 新文件结构（Base 域样品）

**删除** `DataRegister_Base.cs`，**改写** `DataKey_Base.cs`：

```csharp
// Data/DataKeyRegister/Base/DataKey_Base.cs（新格式）
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 基础数据键定义 - DataKey 名称与元数据统一定义
/// DataMeta 静态字段在类首次访问时立即初始化，无需 AutoLoad 队列
/// </summary>
public static partial class DataKey
{
    // ─────────────────────────────────────────────────────
    // [ModuleInitializer] 在程序集加载时触发本类初始化
    // 确保游戏启动前 DataRegistry 中已有 Base 域全部元数据
    // ─────────────────────────────────────────────────────
    [ModuleInitializer]
    internal static void InitBase()
    {
        // 访问任意一个字段即可触发整个 DataKey 类的静态初始化
        _ = Name;
    }

    // ─────────────────────────────────────────────────────
    // 基础信息
    // 字段名 == Key 值（约定，确保 nameof 可作 DataKey Attribute 参数）
    // ─────────────────────────────────────────────────────

    public static readonly DataMeta Name = DataRegistry.Register(new DataMeta
    {
        Key = "Name",
        DisplayName = "名称",
        Description = "名称",
        Category = DataCategory_Base.Basic,
        Type = typeof(string),
        DefaultValue = ""
    });

    public static readonly DataMeta Description = DataRegistry.Register(new DataMeta
    {
        Key = "Description",
        DisplayName = "描述",
        Category = DataCategory_Base.Basic,
        Type = typeof(string),
        DefaultValue = ""
    });

    public static readonly DataMeta Id = DataRegistry.Register(new DataMeta
    {
        Key = "Id",
        DisplayName = "ID",
        Description = "唯一标识符",
        Category = DataCategory_Base.Basic,
        Type = typeof(string),
        DefaultValue = ""
    });

    public static readonly DataMeta Team = DataRegistry.Register(new DataMeta
    {
        Key = "Team",
        DisplayName = "阵营",
        Description = "0:Neutral, 1:Player, 2:Enemy",
        Category = DataCategory_Base.Basic,
        Type = typeof(Team),
        DefaultValue = global::Team.Neutral
    });

    public static readonly DataMeta EntityType = DataRegistry.Register(new DataMeta
    {
        Key = "EntityType",
        DisplayName = "实体类型",
        Description = "Unit/Projectile/Structure/Item...",
        Category = DataCategory_Base.Basic,
        Type = typeof(EntityType),
        DefaultValue = global::EntityType.None
    });
}
```

### 3.5 Config 默认值写法（变更后）

```csharp
// 之前（时序错误，DataRegistry 可能未初始化）
[DataKey(DataKey.Name)]
[Export] public string? Name { get; set; } = (string)DataRegistry.GetMeta(DataKey.Name)!.DefaultValue!;

// 之后（直接访问静态字段，无时序问题）
[DataKey(nameof(DataKey.Name))]
[Export] public string? Name { get; set; } = (string)DataKey.Name.DefaultValue!;
```

### 3.6 数据读取写法（变更后）

```csharp
// 之前
string name = data.Get<string>(DataKey.Name);          // DataKey.Name 是 const string
data.Set(DataKey.BaseHp, 100f);

// 之后（DataMeta 隐式转换为 string，调用处无需修改）
string name = data.Get<string>(DataKey.Name);          // DataKey.Name 是 DataMeta，隐式转 string
data.Set(DataKey.BaseHp, 100f);                        // 同上，完全兼容
```

---

## 4. 影响分析

### 4.1 需要修改的文件

| 文件 | 改动类型 | 说明 |
|------|----------|------|
| `Src/ECS/Data/DataRegistry.cs` | 小改 | `Register()` 改为返回 `DataMeta` |
| `Src/ECS/Data/DataMeta.cs` | 小改 | 增加 `implicit operator string` |
| `Data/DataKeyRegister/Base/DataKey_Base.cs` | 重写 | 合并 DataMeta，改为静态字段 |
| `Data/DataKeyRegister/Base/DataRegister_Base.cs` | **删除** | 职责已并入 DataKey_Base.cs |

### 4.2 不需要立即改的文件

| 文件 | 原因 |
|------|------|
| 其他 `DataKey_*.cs` / `DataRegister_*.cs` | 按域逐步迁移，旧格式可与新格式并存 |
| `Data.Get<T>()` / `Data.Set<T>()` | DataMeta 隐式转 string，无需改 |
| `DataKeyAttribute.cs` | 接口不变，只需调用处改 `nameof` |
| 所有 Config 文件中的 `[DataKey]` | 逐步改为 `nameof` 写法，不影响运行 |

### 4.3 迁移期间兼容性

- **旧格式 DataRegister** 仍可运行（AutoLoad 方式依然有效）
- **新格式 DataKey 静态字段** 在 `[ModuleInitializer]` 时已注册
- **两者并存**：`DataRegistry._metaRegistry` 只是按 Key 字符串存 DataMeta，不论来源

---

## 5. 约定与规范

重构后须遵守以下约定，否则 `nameof` 用法失效：

### 约定 1：字段名 == Key 字符串值

```csharp
// ✅ 正确
public static readonly DataMeta BaseHp = DataRegistry.Register(new DataMeta { Key = "BaseHp", ... });

// ❌ 错误（字段名与 Key 不一致，nameof 结果与 Key 不匹配）
public static readonly DataMeta Hp = DataRegistry.Register(new DataMeta { Key = "BaseHp", ... });
```

### 约定 2：[DataKey] Attribute 改用 nameof

```csharp
// ✅ 新写法（编译期安全）
[DataKey(nameof(DataKey.BaseHp))]

// ⚠️ 旧写法（仍可运行，但需逐步替换）
[DataKey("BaseHp")]
```

### 约定 3：partial class DataKey 不再有 const string

每个 `DataKey_*.cs` 迁移后，文件中只保留 `static readonly DataMeta` 字段，删除同名的 `const string`。  
如有特殊场景需要编译期常量，可保留单独命名的 `const string xxxKey` 作为过渡。

---

## 6. 样品迁移步骤

以 `Base` 域为样品，验证整套流程可行性：

1. **改 DataRegistry.cs** - `Register()` 返回 `DataMeta`
2. **改 DataMeta.cs** - 增加 `implicit operator string`
3. **改写 DataKey_Base.cs** - 按 3.4 节新格式
4. **删除 DataRegister_Base.cs**
5. **验证编译** - 确认无报错
6. **验证运行** - 检查 DataRegistry 中 Base 域 Key 是否已注册
7. **改一个 Config 样品**（如 UnitConfig 的 Name/Team/EntityType 字段）改用新写法，验证默认值正确

---

## 7. 尚待决策的问题

| 问题 | 选项A | 选项B |
|------|-------|-------|
| `DataKey.Meta_BaseHp` 还是 `DataKey.BaseHp`？ | 去掉 Meta_ 前缀（更简洁，但 breaking change） | 保留前缀（兼容旧常量过渡期） |
| 计算属性（含 `Compute` lambda）是否也合并到 DataKey？ | 是（代码最集中） | 否（Compute 引用 DataKey，可能循环依赖） |
| `[ModuleInitializer]` 放在 DataKey 自身还是外部？ | DataKey 内部（`InitBase`，如样品所示） | 保留 DataRegister_ 文件但只做触发 |

> **推荐**：DataKey 内部加 `[ModuleInitializer]` 触发自身初始化，删除 DataRegister_ 文件。  
> 计算属性暂时仍在 DataRegister_ 中定义（避免循环依赖），等进一步重构。

---

## 8. 完成后收益

- ✅ 一个 Key 的所有信息在一处查看/修改
- ✅ 新增 Key 只需在 DataKey_*.cs 加一行 `static readonly DataMeta`
- ✅ Config 默认值 `= (T)DataKey.BaseHp.DefaultValue` 永远不会因时序出错
- ✅ 删除所有 DataRegister_*.cs，减少文件数量
- ✅ DataRegistry 依然可用，只是变成被动收集而非主动驱动
