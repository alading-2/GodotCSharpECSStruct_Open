# Data 容器增强设计文档 - 统一属性与数据管理

## 📋 目录

1. [现状分析](#1-现状分析)
2. [核心问题](#2-核心问题)
3. [设计目标](#3-设计目标)
4. [属性定义优化](#4-属性定义优化)
5. [架构设计](#5-架构设计)
6. [实现方案](#6-实现方案)
7. [使用示例](#7-使用示例)

---

## 1. 现状分析

### 1.1 当前实现

**Data.cs**:

- ✅ 通用键值存储，支持任意类型
- ✅ 事件通知（OnValueChanged）
- ✅ 算术运算（Add, Multiply）
- ❌ 无元数据支持（类型、约束、描述）
- ❌ 无修改器系统（Buff/Debuff）
- ❌ 无计算属性支持
- ❌ 无自动约束验证

**AttributeComponent.cs**:

- ✅ 支持修改器系统（Additive/Multiplicative）
- ✅ 脏标记缓存机制
- ✅ 事件通知
- ❌ 与 Data 职责重叠
- ❌ 数据访问路径分裂
- ❌ 使用字符串访问属性（易错、无智能提示）

**AttributeModifier.cs**:

- ✅ 支持优先级
- ✅ 支持唯一 ID
- ✅ 设计良好，可直接复用

### 1.2 参考系统分析（AttributeSchema.ts）

**优点**:

- ✅ 属性元数据完整（类型、默认值、约束、描述）
- ✅ 计算属性支持（依赖关系、缓存）
- ✅ 属性分类清晰

**缺点**:

- ❌ 过度设计（Schema 注册、版本管理对小项目无意义）
- ❌ 属性过多（力量/敏捷/智力等传统 RPG 属性不适合土豆兄弟）

---

## 2. 核心问题与架构反思

### 2.1 当前架构的根本问题

**问题 1：数据访问路径分裂**

```csharp
// ❌ 当前状态：数据分散在两个系统中
var data = node.GetData();
var attr = node.GetComponent<AttributeComponent>();

// 基础数据在 Data 中
float baseHp = data.Get<float>("BaseMaxHp", 100);

// 最终值在 AttributeComponent 中
float finalHp = attr.Get("MaxHp");

// 问题：开发者需要记住哪些数据在哪里，心智负担大
```

**问题 2：职责重叠与边界模糊**

- **Data 容器**：通用键值存储，支持任意类型，无约束
- **AttributeComponent**：专门处理数值属性，支持修改器、缓存、事件

两者都在管理"属性数据"，但边界不清晰：

- 为什么 HP 需要修改器，而 Name 不需要？
- 为什么 Speed 需要范围限制，而 Tag 不需要？
- 如果 Data 中的某个数值也需要范围限制怎么办？

**问题 3：元数据缺失导致的重复验证**

```csharp
// ❌ 当前：每个使用属性的地方都要手动验证
float critChance = attr.Get("CritChance");
if (critChance > 100) critChance = 100;  // 手动限制
if (critChance < 0) critChance = 0;

// 如果有元数据，验证应该在设置时自动完成
```

### 2.2 TypeScript Schema 方案分析

**优点**：

1. ✅ **统一数据定义**：所有属性的类型、默认值、约束在一处定义
2. ✅ **元数据驱动**：UI、验证、序列化都基于 Schema 自动生成
3. ✅ **计算属性支持**：依赖追踪、自动缓存
4. ✅ **类型安全**：TypeScript 接口提供编译期检查

**缺点（过度设计）**：

1. ❌ **Schema 注册表**：小项目不需要运行时注册，增加复杂度
2. ❌ **继承系统**：Schema 继承对 Brotato 这种简单游戏意义不大
3. ❌ **版本管理**：Schema 版本控制对单机游戏无用
4. ❌ **嵌套 Schema**：支持复杂对象嵌套，但 Brotato 只需要扁平数值
5. ❌ **验证器系统**：自定义验证器过于灵活，简单约束即可

**核心启示**：

- **好的部分**：元数据定义（类型、默认值、约束、描述）
- **坏的部分**：过度抽象（注册表、继承、版本、嵌套）

### 2.3 理想架构的设计目标

**核心原则**：

1. **单一数据源**：所有数据统一从 Data 访问，无论是否需要修改器
2. **元数据驱动**：属性的约束、描述、分类在定义时声明
3. **按需增强**：只有需要修改器的属性才启用修改器系统
4. **零心智负担**：开发者不需要记住"这个属性在 Data 还是 Attribute"

**设计方案对比**：

| 方案                        | 优点                           | 缺点                     | 推荐度         |
| --------------------------- | ------------------------------ | ------------------------ | -------------- |
| 方案 A：保持分离            | 职责清晰                       | 数据访问分裂，心智负担大 | ❌             |
| 方案 B：Attribute 吞并 Data | 统一访问                       | Attribute 过于臃肿       | ❌             |
| 方案 C：Data 吞并 Attribute | 统一访问，Data 是核心          | Data 变复杂              | ⭐⭐           |
| **方案 D：Data + 元数据层** | **统一访问，职责分离，可扩展** | **需要重构 Data**        | **⭐⭐⭐⭐⭐** |

### 2.4 推荐方案：Data + 元数据层

**核心思想**：

- **Data 容器**：保持通用键值存储的本质，但增加元数据支持
- **PropertyMeta**：定义属性的类型、约束、描述（类似 TS Schema）
- **修改器系统**：作为 Data 的可选增强功能，而非独立组件

**架构图**：

```
┌─────────────────────────────────────────────────────┐
│  Data (核心数据容器)                                 │
│  - 存储所有数据（基础值 + 修改器）                   │
│  - 支持元数据约束                                    │
│  - 支持计算属性                                      │
│  - 支持修改器（可选）                                │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  PropertyMeta (属性元数据)                           │
│  - 类型、默认值、约束                                │
│  - 描述、分类、图标                                  │
│  - 是否支持修改器                                    │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  PropertyKey (常量类)                                │
│  - 类型安全的属性键                                  │
└─────────────────────────────────────────────────────┘
```

**使用示例**：

```csharp
// ✅ 统一访问：所有数据都从 Data 获取
var data = node.GetData();

// 普通属性（无修改器）
string name = data.Get<string>(PropKey.Name);
int level = data.Get<int>(PropKey.Level);

// 数值属性（支持修改器）
float damage = data.Get<float>(PropKey.Damage);  // 自动应用修改器
float speed = data.Get<float>(PropKey.Speed);

// 添加修改器（Data 内部处理）
data.AddModifier(PropKey.Damage, new Modifier(ModifierType.Additive, 10));

// 计算属性（自动计算）
float dps = data.Get<float>(PropKey.DPS);  // 基于 Damage 和 AttackSpeed 计算
```

**关键改进**：

1. **统一入口**：`data.Get()` 是唯一的数据访问方式
2. **自动约束**：设置值时自动应用元数据约束（范围、类型）
3. **按需修改器**：只有声明支持修改器的属性才启用修改器系统
4. **计算属性**：在元数据中定义，Data 自动处理依赖和缓存

---

## 3. 设计目标与方案选择

### 3.1 核心原则

1. **单一数据源**：所有数据统一从 Data 访问，消除"这个属性在哪里"的困惑
2. **元数据驱动**：属性的约束、描述、分类在定义时声明，自动应用
3. **按需增强**：只有需要修改器的属性才启用修改器系统
4. **类型安全**：使用常量代替字符串，编译期检查
5. **性能优先**：避免反射和过度抽象，保持简洁

### 3.2 方案选择：Data 增强方案

**为什么不是 Attribute 吞并 Data？**

- Data 是 Node 的核心扩展，存储所有类型的数据（字符串、对象、数组）
- Attribute 只是数值属性的特化场景
- 让特化场景吞并通用容器会导致职责混乱

**为什么不是保持分离？**

- 开发者需要记住"哪些数据在 Data，哪些在 Attribute"
- 数据访问路径分裂，心智负担大
- 无法统一处理约束和验证

**推荐方案：Data + 元数据层**

核心思想：

1. **Data 保持通用性**：仍然是键值存储，支持任意类型
2. **增加元数据支持**：属性可以声明类型、约束、描述
3. **修改器作为可选功能**：只有声明支持修改器的属性才启用
4. **统一访问接口**：`data.Get()` 是唯一入口，内部自动处理修改器

**架构对比**：

| 特性       | 当前方案（分离）            | Data 增强方案        |
| ---------- | --------------------------- | -------------------- |
| 数据访问   | `data.Get()` + `attr.Get()` | `data.Get()`（统一） |
| 修改器支持 | 只在 Attribute              | Data 内部可选支持    |
| 元数据     | 无                          | PropertyMeta 定义    |
| 约束验证   | 手动                        | 自动（基于元数据）   |
| 计算属性   | 手动                        | 自动（基于依赖）     |
| 心智负担   | 高（需记住数据位置）        | 低（统一入口）       |
| 扩展性     | 中（需同步两个系统）        | 高（只需扩展元数据） |

### 3.3 功能需求

- ✅ 类型安全的属性访问（常量 Key）
- ✅ 属性元数据（类型、默认值、约束、描述、分类）
- ✅ 自动约束验证（设置时自动应用 min/max）
- ✅ 计算属性（自动依赖追踪和缓存）
- ✅ 修改器系统（可选，按需启用）
- ✅ 事件通知（值变化、修改器变化）
- ✅ 编辑器友好（可视化配置）

---

## 4. 属性定义优化

### 4.1 土豆兄弟核心属性分析

**参考原版游戏机制**:

- 生存类 Roguelike
- 自动攻击 + 手动移动
- 装备/升级驱动成长
- 波次递增难度

**必需属性（20 个核心）**:

#### 生命系统（4 个）

- `MaxHp` - 最大生命值
- `HpRegen` - 生命恢复/秒
- `LifeSteal` - 生命偷取%
- `Armor` - 护甲值

#### 攻击系统（6 个）

- `Damage` - 基础伤害
- `AttackSpeed` - 攻击速度%（100 = 基准）
- `CritChance` - 暴击率%
- `CritDamage` - 暴击伤害%（150 = 1.5 倍）
- `Range` - 攻击范围
- `Knockback` - 击退力度

#### 防御系统（3 个）

- `DodgeChance` - 闪避率%
- `DamageReduction` - 伤害减免%
- `Thorns` - 反伤%

#### 移动系统（1 个）

- `Speed` - 移动速度

#### 资源系统（3 个）

- `PickupRange` - 拾取范围
- `ExpGain` - 经验获取%
- `LuckBonus` - 幸运值（影响掉落）

#### 特殊机制（3 个）

- `Pierce` - 穿透数量
- `ProjectileCount` - 投射物数量
- `AreaSize` - 范围倍率%

### 4.2 计算属性（派生属性）

这些属性不需要存储，由其他属性计算得出：

- `AttackInterval` = 1.0 / (AttackSpeed / 100)
- `EffectiveHp` = MaxHp / (1 - DamageReduction)
- `DPS` = Damage × (AttackSpeed / 100) × (1 + CritChance × CritDamage)

---

## 5. 架构设计（Data 增强方案）

### 5.1 核心设计思想

**关键决策**：

1. **Data 是唯一数据源**：所有属性（无论是否需要修改器）都存储在 Data 中
2. **元数据定义属性特性**：PropertyMeta 描述属性的类型、约束、是否支持修改器
3. **修改器作为可选功能**：只有声明 `SupportModifiers = true` 的属性才启用修改器
4. **计算属性自动处理**：在元数据中定义依赖关系，Data 自动计算和缓存

**与 TypeScript Schema 的对比**：

| 特性        | TS Schema 方案   | 本方案（Data 增强） | 说明                   |
| ----------- | ---------------- | ------------------- | ---------------------- |
| 数据存储    | 独立对象         | Data 容器           | 复用现有 Data 系统     |
| 元数据定义  | Schema 接口      | PropertyMeta 类     | 简化，去掉继承和版本   |
| 注册表      | SchemaRegistry   | PropertyRegistry    | 简化，去掉运行时注册   |
| 计算属性    | ComputedProperty | ComputedProperty    | 保留，核心功能         |
| 修改器系统  | 无               | 内置支持            | 游戏特有需求           |
| 嵌套 Schema | 支持             | 不支持              | Brotato 不需要复杂嵌套 |
| 继承系统    | 支持             | 不支持              | 过度设计               |
| 版本管理    | 支持             | 不支持              | 单机游戏无需版本控制   |

### 5.2 属性键系统（PropKey）

**为什么用常量而非枚举？**

- 常量支持扩展（Mod 可添加自定义属性）
- 枚举是封闭的，无法在运行时扩展

```csharp
/// <summary>
/// 属性键定义 - 类型安全的属性访问
/// 使用常量而非枚举，支持 Mod 扩展
/// </summary>
public static class PropKey
{
    // === 基础信息 ===
    public const string Name = "Name";
    public const string Level = "Level";
    public const string Experience = "Experience";

    // === 生命系统 ===
    public const string MaxHp = "MaxHp";
    public const string CurrentHp = "CurrentHp";
    public const string HpRegen = "HpRegen";
    public const string LifeSteal = "LifeSteal";
    public const string Armor = "Armor";

    // === 攻击系统 ===
    public const string Damage = "Damage";
    public const string AttackSpeed = "AttackSpeed";
    public const string CritChance = "CritChance";
    public const string CritDamage = "CritDamage";
    public const string Range = "Range";
    public const string Knockback = "Knockback";

    // === 防御系统 ===
    public const string DodgeChance = "DodgeChance";
    public const string DamageReduction = "DamageReduction";
    public const string Thorns = "Thorns";

    // === 移动系统 ===
    public const string Speed = "Speed";

    // === 资源系统 ===
    public const string PickupRange = "PickupRange";
    public const string ExpGain = "ExpGain";
    public const string LuckBonus = "LuckBonus";

    // === 特殊机制 ===
    public const string Pierce = "Pierce";
    public const string ProjectileCount = "ProjectileCount";
    public const string AreaSize = "AreaSize";

    // === 计算属性（只读） ===
    public const string AttackInterval = "AttackInterval";
    public const string EffectiveHp = "EffectiveHp";
    public const string DPS = "DPS";
}
```

### 5.3 属性元数据系统（PropertyMeta）

**核心改进**：增加 `SupportModifiers` 标记，区分普通属性和可修改属性

```csharp
/// <summary>
/// 属性元数据 - 描述属性的所有特性
/// </summary>
public class PropertyMeta
{
    public string Key { get; init; }
    public string DisplayName { get; init; }
    public string Description { get; init; }
    public PropertyCategory Category { get; init; }
    public PropertyType Type { get; init; }  // 新增：属性类型
    public object DefaultValue { get; init; }
    public float MinValue { get; init; } = float.MinValue;
    public float MaxValue { get; init; } = float.MaxValue;
    public bool IsPercentage { get; init; }
    public bool SupportModifiers { get; init; }  // 新增：是否支持修改器
    public string IconPath { get; init; } = "";

    /// <summary>
    /// 验证并限制属性值
    /// </summary>
    public object Clamp(object value)
    {
        if (Type == PropertyType.Number && value is float f)
        {
            return Mathf.Clamp(f, MinValue, MaxValue);
        }
        return value;
    }

    /// <summary>
    /// 格式化显示
    /// </summary>
    public string FormatValue(object value)
    {
        if (Type == PropertyType.Number && value is float f)
        {
            return IsPercentage ? $"{f:F1}%" : $"{f:F1}";
        }
        return value?.ToString() ?? "";
    }
}

/// <summary>
/// 属性类型枚举
/// </summary>
public enum PropertyType
{
    Number,   // 数值（支持修改器）
    String,   // 字符串
    Boolean,  // 布尔值
    Object    // 对象引用
}

/// <summary>
/// 属性分类枚举
/// </summary>
public enum PropertyCategory
{
    Basic,      // 基础信息
    Health,     // 生命系统
    Attack,     // 攻击系统
    Defense,    // 防御系统
    Movement,   // 移动系统
    Resource,   // 资源系统
    Special,    // 特殊机制
    Computed    // 计算属性
}
```

### 5.4 计算属性系统（ComputedProperty）

**保持不变**：计算属性的设计已经很好，直接复用

```csharp
/// <summary>
/// 计算属性定义 - 由其他属性派生的只读属性
/// </summary>
public class ComputedProperty
{
    public string Key { get; init; }
    public string[] Dependencies { get; init; }
    public Func<Data, object> Compute { get; init; }

    public bool DependsOn(string propKey)
    {
        return Dependencies.Contains(propKey);
    }
}
```

### 5.5 属性注册表（PropertyRegistry）

**简化设计**：去掉运行时注册，改为静态初始化

```csharp
/// <summary>
/// 属性注册表 - 管理所有属性的元数据和计算规则
/// 静态初始化，无需运行时注册
/// </summary>
public static class PropertyRegistry
{
    private static readonly Log _log = new("PropertyRegistry");

    // 元数据存储
    private static readonly Dictionary<string, PropertyMeta> _metaRegistry = new();

    // 计算属性存储
    private static readonly Dictionary<string, ComputedProperty> _computedRegistry = new();

    // 静态构造函数：自动初始化
    static PropertyRegistry()
    {
        RegisterBasicProperties();
        RegisterComputedProperties();
        _log.Info($"属性注册表初始化完成：{_metaRegistry.Count} 个基础属性，{_computedRegistry.Count} 个计算属性");
    }

    /// <summary>
    /// 注册基础属性元数据
    /// </summary>
    private static void RegisterBasicProperties()
    {
        // 基础信息（不支持修改器）
        Register(new PropertyMeta
        {
            Key = PropKey.Name,
            DisplayName = "名称",
            Description = "实体的名称",
            Category = PropertyCategory.Basic,
            Type = PropertyType.String,
            DefaultValue = "",
            SupportModifiers = false
        });

        Register(new PropertyMeta
        {
            Key = PropKey.Level,
            DisplayName = "等级",
            Description = "当前等级",
            Category = PropertyCategory.Basic,
            Type = PropertyType.Number,
            DefaultValue = 1f,
            MinValue = 1,
            MaxValue = 100,
            SupportModifiers = false
        });

        // 生命系统（支持修改器）
        Register(new PropertyMeta
        {
            Key = PropKey.MaxHp,
            DisplayName = "最大生命值",
            Description = "角色的最大生命值",
            Category = PropertyCategory.Health,
            Type = PropertyType.Number,
            DefaultValue = 100f,
            MinValue = 1,
            MaxValue = 99999,
            SupportModifiers = true  // 支持修改器
        });

        Register(new PropertyMeta
        {
            Key = PropKey.HpRegen,
            DisplayName = "生命恢复",
            Description = "每秒恢复的生命值",
            Category = PropertyCategory.Health,
            Type = PropertyType.Number,
            DefaultValue = 0f,
            MinValue = 0,
            SupportModifiers = true
        });

        // ... 其他属性注册（省略，与原设计相同）
    }

    /// <summary>
    /// 注册计算属性
    /// </summary>
    private static void RegisterComputedProperties()
    {
        RegisterComputed(new ComputedProperty
        {
            Key = PropKey.AttackInterval,
            Dependencies = new[] { PropKey.AttackSpeed },
            Compute = (data) =>
            {
                float attackSpeed = data.Get<float>(PropKey.AttackSpeed, 100);
                return 1.0f / (attackSpeed / 100f);
            }
        });

        RegisterComputed(new ComputedProperty
        {
            Key = PropKey.EffectiveHp,
            Dependencies = new[] { PropKey.MaxHp, PropKey.DamageReduction },
            Compute = (data) =>
            {
                float maxHp = data.Get<float>(PropKey.MaxHp, 100);
                float reduction = data.Get<float>(PropKey.DamageReduction, 0) / 100f;
                return maxHp / Mathf.Max(0.1f, 1f - reduction);
            }
        });

        RegisterComputed(new ComputedProperty
        {
            Key = PropKey.DPS,
            Dependencies = new[] { PropKey.Damage, PropKey.AttackSpeed, PropKey.CritChance, PropKey.CritDamage },
            Compute = (data) =>
            {
                float damage = data.Get<float>(PropKey.Damage, 10);
                float attackSpeed = data.Get<float>(PropKey.AttackSpeed, 100) / 100f;
                float critChance = data.Get<float>(PropKey.CritChance, 0) / 100f;
                float critDamage = data.Get<float>(PropKey.CritDamage, 150) / 100f;

                float avgDamage = damage * (1f + critChance * (critDamage - 1f));
                return avgDamage * attackSpeed;
            }
        });

        // 为计算属性注册元数据
        Register(new PropertyMeta
        {
            Key = PropKey.AttackInterval,
            DisplayName = "攻击间隔",
            Description = "两次攻击之间的时间（秒）",
            Category = PropertyCategory.Computed,
            Type = PropertyType.Number,
            DefaultValue = 1.0f,
            SupportModifiers = false
        });

        // ... 其他计算属性元数据
    }

    private static void Register(PropertyMeta meta)
    {
        if (_metaRegistry.ContainsKey(meta.Key))
        {
            _log.Warn($"属性 {meta.Key} 已存在，将被覆盖");
        }
        _metaRegistry[meta.Key] = meta;
    }

    private static void RegisterComputed(ComputedProperty computed)
    {
        if (_computedRegistry.ContainsKey(computed.Key))
        {
            _log.Warn($"计算属性 {computed.Key} 已存在，将被覆盖");
        }
        _computedRegistry[computed.Key] = computed;
    }

    // === 公共查询接口 ===

    public static PropertyMeta? GetMeta(string key)
    {
        return _metaRegistry.TryGetValue(key, out var meta) ? meta : null;
    }

    public static ComputedProperty? GetComputed(string key)
    {
        return _computedRegistry.TryGetValue(key, out var computed) ? computed : null;
    }

    public static bool IsComputed(string key)
    {
        return _computedRegistry.ContainsKey(key);
    }

    public static bool SupportModifiers(string key)
    {
        var meta = GetMeta(key);
        return meta?.SupportModifiers ?? false;
    }

    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _computedRegistry
            .Where(kvp => kvp.Value.DependsOn(baseKey))
            .Select(kvp => kvp.Key);
    }
}
```

### 5.6 Data 容器增强

**核心改进**：Data 内部集成修改器系统

```csharp
/// <summary>
/// Data 容器增强版 - 支持元数据和修改器
/// </summary>
public class Data
{
    private readonly Dictionary<string, object> _data = new();
    private readonly Dictionary<string, List<Modifier>> _modifiers = new();
    private readonly Dictionary<string, object> _cachedValues = new();
    private readonly HashSet<string> _dirtyKeys = new();

    public event Action<string, object?, object?>? OnValueChanged;

    /// <summary>
    /// 设置基础值（不触发修改器计算）
    /// </summary>
    public bool SetBase<T>(string key, T value)
    {
        var meta = PropertyRegistry.GetMeta(key);
        if (meta != null)
        {
            value = (T)meta.Clamp(value);  // 自动应用约束
        }

        object? oldValue = null;
        if (_data.TryGetValue(key, out var existing))
        {
            oldValue = existing;
            if (Equals(existing, value)) return false;
        }

        _data[key] = value!;
        MarkDirty(key);
        NotifyChanged(key, oldValue, value);
        return true;
    }

    /// <summary>
    /// 获取最终值（自动应用修改器和计算属性）
    /// </summary>
    public T Get<T>(string key, T defaultValue = default!)
    {
        // 1. 检查是否为计算属性
        var computed = PropertyRegistry.GetComputed(key);
        if (computed != null)
        {
            if (!_dirtyKeys.Contains(key) && _cachedValues.TryGetValue(key, out var cached))
            {
                return (T)cached;
            }

            var result = computed.Compute(this);
            _cachedValues[key] = result;
            _dirtyKeys.Remove(key);
            return (T)result;
        }

        // 2. 获取基础值
        if (!_data.TryGetValue(key, out var baseValue))
        {
            return defaultValue;
        }

        // 3. 检查是否支持修改器
        if (!PropertyRegistry.SupportModifiers(key))
        {
            return (T)baseValue;  // 不支持修改器，直接返回
        }

        // 4. 应用修改器
        if (!_dirtyKeys.Contains(key) && _cachedValues.TryGetValue(key, out var cachedFinal))
        {
            return (T)cachedFinal;
        }

        var finalValue = CalculateFinalValue(key, (float)baseValue);
        _cachedValues[key] = finalValue;
        _dirtyKeys.Remove(key);
        return (T)(object)finalValue;
    }

    /// <summary>
    /// 添加修改器
    /// </summary>
    public void AddModifier(string key, Modifier modifier)
    {
        if (!PropertyRegistry.SupportModifiers(key))
        {
            GD.PrintErr($"属性 {key} 不支持修改器");
            return;
        }

        if (!_modifiers.ContainsKey(key))
        {
            _modifiers[key] = new List<Modifier>();
        }

        _modifiers[key].Add(modifier);
        MarkDirty(key);
    }

    /// <summary>
    /// 移除修改器
    /// </summary>
    public void RemoveModifier(string key, string modifierId)
    {
        if (_modifiers.TryGetValue(key, out var modifiers))
        {
            modifiers.RemoveAll(m => m.Id == modifierId);
            MarkDirty(key);
        }
    }

    /// <summary>
    /// 计算最终值：(基础值 + Σ加法) × Π乘法
    /// </summary>
    private float CalculateFinalValue(string key, float baseValue)
    {
        if (!_modifiers.TryGetValue(key, out var modifiers) || modifiers.Count == 0)
        {
            return baseValue;
        }

        var sorted = modifiers.OrderBy(m => m.Priority).ToList();

        float additiveSum = sorted
            .Where(m => m.Type == ModifierType.Additive)
            .Sum(m => m.Value);

        float multiplicativeProduct = sorted
            .Where(m => m.Type == ModifierType.Multiplicative)
            .Aggregate(1f, (acc, m) => acc * m.Value);

        return (baseValue + additiveSum) * multiplicativeProduct;
    }

    /// <summary>
    /// 标记属性为脏（需要重新计算）
    /// </summary>
    private void MarkDirty(string key)
    {
        _dirtyKeys.Add(key);
        _cachedValues.Remove(key);

        // 同时标记依赖此属性的计算属性为脏
        var dependents = PropertyRegistry.GetDependentComputedKeys(key);
        foreach (var depKey in dependents)
        {
            _dirtyKeys.Add(depKey);
            _cachedValues.Remove(depKey);
        }
    }

    private void NotifyChanged(string key, object? oldValue, object? newValue)
    {
        OnValueChanged?.Invoke(key, oldValue, newValue);
    }
}
```

### 5.7 完整架构图

```
┌─────────────────────────────────────────────────────┐
│  使用层 (Usage Layer)                                │
│  - 业务代码统一通过 data.Get() 访问                  │
│  - 无需关心数据是否有修改器                          │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  Data 容器 (Data Container)                          │
│  - 存储基础值                                        │
│  - 管理修改器（可选）                                │
│  - 计算最终值                                        │
│  - 处理计算属性                                      │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  PropertyRegistry (属性注册表)                       │
│  - 属性元数据（类型、约束、是否支持修改器）          │
│  - 计算属性定义                                      │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  PropKey (属性键常量)                                │
│  - 类型安全的属性访问                                │
└─────────────────────────────────────────────────────┘
```

---

## 6. 实现方案

### 6.1 文件结构

```
Src/Tools/Data/
├── Data.cs                        # 核心数据容器（需增强）
├── PropertyKey.cs                 # 属性键常量（新增）
├── PropertyMeta.cs                # 属性元数据（新增）
├── PropertyRegistry.cs            # 属性注册表（新增）
├── PropertyType.cs                # 属性类型枚举（新增）
├── PropertyCategory.cs            # 属性分类枚举（新增）
├── ComputedProperty.cs            # 计算属性（新增）
└── Modifier.cs                    # 修改器（从 AttributeModifier 重命名）
```

**注意**：

- `AttributeComponent` 将被废弃，所有功能集成到 `Data` 中
- `AttributeModifier` 重命名为 `Modifier`（更通用）
- 所有新增文件放在 `Src/Tools/Data/` 目录下

### 6.2 核心代码实现

#### 6.2.1 PropertyKey.cs - 属性键常量

```csharp
/// <summary>
/// 属性键定义 - 提供类型安全的属性访问
/// 使用常量而非枚举，支持扩展（Mod 可添加自定义属性）
/// </summary>
public static class PropKey
{
    // === 基础信息 ===
    public const string Name = "Name";
    public const string Level = "Level";
    public const string Experience = "Experience";

    // === 生命系统 ===
    public const string MaxHp = "MaxHp";
    public const string CurrentHp = "CurrentHp";
    public const string HpRegen = "HpRegen";
    public const string LifeSteal = "LifeSteal";
    public const string Armor = "Armor";

    // === 攻击系统 ===
    public const string Damage = "Damage";
    public const string AttackSpeed = "AttackSpeed";
    public const string CritChance = "CritChance";
    public const string CritDamage = "CritDamage";
    public const string Range = "Range";
    public const string Knockback = "Knockback";

    // === 防御系统 ===
    public const string DodgeChance = "DodgeChance";
    public const string DamageReduction = "DamageReduction";
    public const string Thorns = "Thorns";

    // === 移动系统 ===
    public const string Speed = "Speed";

    // === 资源系统 ===
    public const string PickupRange = "PickupRange";
    public const string ExpGain = "ExpGain";
    public const string LuckBonus = "LuckBonus";

    // === 特殊机制 ===
    public const string Pierce = "Pierce";
    public const string ProjectileCount = "ProjectileCount";
    public const string AreaSize = "AreaSize";

    // === 计算属性（只读） ===
    public const string AttackInterval = "AttackInterval";
    public const string EffectiveHp = "EffectiveHp";
    public const string DPS = "DPS";
}
```

#### 6.2.2 PropertyType.cs - 属性类型枚举

```csharp
/// <summary>
/// 属性类型枚举
/// </summary>
public enum PropertyType
{
    Number,   // 数值（支持修改器）
    String,   // 字符串
    Boolean,  // 布尔值
    Object    // 对象引用
}
```

#### 6.2.3 PropertyCategory.cs - 属性分类

```csharp
/// <summary>
/// 属性分类 - 用于 UI 分组显示
/// </summary>
public enum PropertyCategory
{
    Basic,      // 基础信息
    Health,     // 生命系统
    Attack,     // 攻击系统
    Defense,    // 防御系统
    Movement,   // 移动系统
    Resource,   // 资源系统
    Special,    // 特殊机制
    Computed    // 计算属性
}
```

#### 6.2.4 PropertyMeta.cs - 属性元数据

```csharp
/// <summary>
/// 属性元数据 - 描述属性的所有特性
/// </summary>
public class PropertyMeta
{
    public string Key { get; init; }
    public string DisplayName { get; init; }
    public string Description { get; init; }
    public PropertyCategory Category { get; init; }
    public PropertyType Type { get; init; }
    public object DefaultValue { get; init; }
    public float MinValue { get; init; } = float.MinValue;
    public float MaxValue { get; init; } = float.MaxValue;
    public bool IsPercentage { get; init; }
    public bool SupportModifiers { get; init; }  // 🔑 关键：是否支持修改器
    public string IconPath { get; init; } = "";

    /// <summary>
    /// 验证并限制属性值
    /// </summary>
    public object Clamp(object value)
    {
        if (Type == PropertyType.Number && value is float f)
        {
            return Mathf.Clamp(f, MinValue, MaxValue);
        }
        return value;
    }

    /// <summary>
    /// 格式化显示（自动处理百分比）
    /// </summary>
    public string FormatValue(object value)
    {
        if (Type == PropertyType.Number && value is float f)
        {
            return IsPercentage ? $"{f:F1}%" : $"{f:F1}";
        }
        return value?.ToString() ?? "";
    }
}
```

#### 6.2.5 ComputedProperty.cs - 计算属性

```csharp
/// <summary>
/// 计算属性定义 - 由其他属性派生的只读属性
/// </summary>
public class ComputedProperty
{
    public string Key { get; init; }
    public string[] Dependencies { get; init; }
    public Func<Data, object> Compute { get; init; }  // 注意：参数是 Data 而非 AttributeComponent

    /// <summary>
    /// 检查是否依赖指定属性
    /// </summary>
    public bool DependsOn(string propKey)
    {
        return Dependencies.Contains(propKey);
    }
}
```

#### 6.2.6 Modifier.cs - 修改器（重命名自 AttributeModifier）

```csharp
/// <summary>
/// 修改器 - 用于临时或永久修改属性值（Buff/Debuff/装备）
/// </summary>
public class Modifier
{
    public string PropertyKey { get; init; }  // 修改的属性键
    public ModifierType Type { get; init; }
    public float Value { get; init; }
    public int Priority { get; init; }
    public string Id { get; init; }

    public Modifier(
        string propertyKey,
        ModifierType type,
        float value,
        int priority = 0,
        string? id = null)
    {
        PropertyKey = propertyKey;
        Type = type;
        Value = value;
        Priority = priority;
        Id = id ?? System.Guid.NewGuid().ToString();
    }
}

/// <summary>
/// 修改器类型
/// </summary>
public enum ModifierType
{
    Additive,        // 加法：+10
    Multiplicative   // 乘法：×1.5
}
```

#### 6.2.7 PropertyRegistry.cs - 属性注册表（简化版）

```csharp
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 属性注册表 - 管理所有属性的元数据和计算规则
/// 静态初始化，无需运行时注册
/// </summary>
public static class PropertyRegistry
{
    private static readonly Log _log = new("PropertyRegistry");

    // 元数据存储
    private static readonly Dictionary<string, PropertyMeta> _metaRegistry = new();

    // 计算属性存储
    private static readonly Dictionary<string, ComputedProperty> _computedRegistry = new();

    // 静态构造函数：自动初始化
    static PropertyRegistry()
    {
        RegisterBasicProperties();
        RegisterComputedProperties();
        _log.Info($"属性注册表初始化完成：{_metaRegistry.Count} 个基础属性，{_computedRegistry.Count} 个计算属性");
    }

    /// <summary>
    /// 注册基础属性元数据
    /// </summary>
    private static void RegisterBasicProperties()
    {
        // 基础信息（不支持修改器）
        Register(new PropertyMeta
        {
            Key = PropKey.Name,
            DisplayName = "名称",
            Description = "实体的名称",
            Category = PropertyCategory.Basic,
            Type = PropertyType.String,
            DefaultValue = "",
            SupportModifiers = false
        });

        // 生命系统（支持修改器）
        Register(new PropertyMeta
        {
            Key = PropKey.MaxHp,
            DisplayName = "最大生命值",
            Description = "角色的最大生命值",
            Category = PropertyCategory.Health,
            Type = PropertyType.Number,
            DefaultValue = 100f,
            MinValue = 1,
            MaxValue = 99999,
            SupportModifiers = true  // 🔑 支持修改器
        });

        Register(new PropertyMeta
        {
            Key = PropKey.Damage,
            DisplayName = "伤害",
            Description = "基础攻击伤害",
            Category = PropertyCategory.Attack,
            Type = PropertyType.Number,
            DefaultValue = 10f,
            MinValue = 0,
            SupportModifiers = true
        });

        // ... 其他属性注册（省略，参考完整列表）
    }

    /// <summary>
    /// 注册计算属性
    /// </summary>
    private static void RegisterComputedProperties()
    {
        RegisterComputed(new ComputedProperty
        {
            Key = PropKey.AttackInterval,
            Dependencies = new[] { PropKey.AttackSpeed },
            Compute = (data) =>
            {
                float attackSpeed = data.Get<float>(PropKey.AttackSpeed, 100);
                return 1.0f / (attackSpeed / 100f);
            }
        });

        RegisterComputed(new ComputedProperty
        {
            Key = PropKey.DPS,
            Dependencies = new[] { PropKey.Damage, PropKey.AttackSpeed, PropKey.CritChance, PropKey.CritDamage },
            Compute = (data) =>
            {
                float damage = data.Get<float>(PropKey.Damage, 10);
                float attackSpeed = data.Get<float>(PropKey.AttackSpeed, 100) / 100f;
                float critChance = data.Get<float>(PropKey.CritChance, 0) / 100f;
                float critDamage = data.Get<float>(PropKey.CritDamage, 150) / 100f;

                float avgDamage = damage * (1f + critChance * (critDamage - 1f));
                return avgDamage * attackSpeed;
            }
        });

        // 为计算属性注册元数据
        Register(new PropertyMeta
        {
            Key = PropKey.AttackInterval,
            DisplayName = "攻击间隔",
            Description = "两次攻击之间的时间（秒）",
            Category = PropertyCategory.Computed,
            Type = PropertyType.Number,
            DefaultValue = 1.0f,
            SupportModifiers = false
        });

        // ... 其他计算属性元数据
    }

    private static void Register(PropertyMeta meta)
    {
        _metaRegistry[meta.Key] = meta;
    }

    private static void RegisterComputed(ComputedProperty computed)
    {
        _computedRegistry[computed.Key] = computed;
    }

    // === 公共查询接口 ===

    public static PropertyMeta? GetMeta(string key)
    {
        return _metaRegistry.TryGetValue(key, out var meta) ? meta : null;
    }

    public static ComputedProperty? GetComputed(string key)
    {
        return _computedRegistry.TryGetValue(key, out var computed) ? computed : null;
    }

    public static bool IsComputed(string key)
    {
        return _computedRegistry.ContainsKey(key);
    }

    public static bool SupportModifiers(string key)
    {
        var meta = GetMeta(key);
        return meta?.SupportModifiers ?? false;
    }

    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _computedRegistry
            .Where(kvp => kvp.Value.DependsOn(baseKey))
            .Select(kvp => kvp.Key);
    }

    public static IEnumerable<PropertyMeta> GetByCategory(PropertyCategory category)
    {
        return _metaRegistry.Values.Where(m => m.Category == category);
    }
}
```

---

## 7. 使用示例

### 7.1 基础使用（类型安全）

```csharp
public partial class Player : CharacterBody2D
{
    private AttributeComponent _attr;

    public override void _Ready()
    {
        _attr = GetNode<AttributeComponent>("AttributeComponent");

        // ✅ 使用常量，有智能提示，编译期检查
        float damage = _attr.Get(AttrKey.Damage);
        float speed = _attr.Get(AttrKey.Speed);

        // ❌ 旧方式：字符串易错
        // float damage = _attr.Get("Damge");  // 拼写错误，运行时才发现
    }
}
```

### 7.2 修改器使用（Buff/Debuff）

```csharp
// 添加攻速 Buff（+50%）
var buffId = "Buff_Haste";
_attr.AddModifier(new AttributeModifier(
    AttrKey.AttackSpeed,
    ModifierType.Multiplicative,
    1.5f,  // 150% = 1.5 倍
    priority: 0,
    id: buffId
));

// 5 秒后移除
GetTree().CreateTimer(5.0).Timeout += () => {
    _attr.RemoveModifier(buffId);
};
```

### 7.3 计算属性使用

```csharp
// 自动计算，无需手动维护
float attackInterval = _attr.Get(AttrKey.AttackInterval);
float dps = _attr.Get(AttrKey.DPS);
float effectiveHp = _attr.Get(AttrKey.EffectiveHp);

// 当依赖属性变化时，计算属性自动更新
_attr.AddModifier(new AttributeModifier(
    AttrKey.AttackSpeed,
    ModifierType.Additive,
    50  // +50%
));

// AttackInterval 和 DPS 自动重新计算
float newInterval = _attr.Get(AttrKey.AttackInterval);  // 已更新
```

### 7.4 属性元数据使用（UI 显示）

```csharp
// 获取元数据
var meta = AttributeRegistry.GetMeta(AttrKey.CritChance);

// 显示在 UI 上
label.Text = meta.DisplayName;  // "暴击率"
tooltip.Text = meta.Description;  // "触发暴击的概率"

// 格式化数值
float critChance = _attr.Get(AttrKey.CritChance);
valueLabel.Text = meta.FormatValue(critChance);  // "15.0%"

// 验证范围
float newValue = meta.Clamp(150);  // 自动限制在 0-100
```

### 7.5 属性分类查询（装备面板）

```csharp
// 显示所有攻击属性
var attackAttrs = AttributeRegistry.GetByCategory(AttributeCategory.Attack);
foreach (var meta in attackAttrs)
{
    float value = _attr.Get(meta.Key);
    AddStatRow(meta.DisplayName, meta.FormatValue(value));
}
```

### 7.6 优化后的 AttributeComponent 使用

```csharp
public partial class AttributeComponent : Node
{
    private static readonly Log _log = new("AttributeComponent");

    private Data _data;
    private readonly List<AttributeModifier> _modifiers = new();
    private readonly Dictionary<string, float> _cachedValues = new();
    private bool _isDirty = true;

    public event Action? AttributeChanged;

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent == null)
        {
            _log.Error("AttributeComponent 必须作为实体的子节点");
            return;
        }

        _data = parent.GetData();
        _data.OnValueChanged += OnDataChanged;

        RecalculateAll();
        _log.Debug("属性组件初始化完成");
    }

    public override void _ExitTree()
    {
        if (_data != null)
        {
            _data.OnValueChanged -= OnDataChanged;
        }

        AttributeChanged = null;
        _modifiers.Clear();
        _cachedValues.Clear();
    }

    /// <summary>
    /// 获取属性最终值（类型安全版本）
    /// </summary>
    public float Get(string attrKey)
    {
        // 1. 检查是否为计算属性
        var computed = AttributeRegistry.GetComputed(attrKey);
        if (computed != null)
        {
            return computed.Compute(this);
        }

        // 2. 尝试从缓存获取
        if (!_isDirty && _cachedValues.TryGetValue(attrKey, out float cached))
        {
            return cached;
        }

        // 3. 获取元数据
        var meta = AttributeRegistry.GetMeta(attrKey);
        float defaultValue = meta?.DefaultValue ?? 0f;

        // 4. 从 Data 获取基础值
        string baseKey = "Base" + attrKey;
        float baseValue = _data?.Get<float>(baseKey, defaultValue) ?? defaultValue;

        // 5. 计算最终值
        float finalValue = CalculateFinalValue(attrKey, baseValue);

        // 6. 应用约束
        if (meta != null)
        {
            finalValue = meta.Clamp(finalValue);
        }

        // 7. 缓存结果
        _cachedValues[attrKey] = finalValue;

        return finalValue;
    }
```

    /// <summary>
    /// 设置基础属性值
    /// </summary>
    public void SetBase(string attrKey, float value)
    {
        var meta = AttributeRegistry.GetMeta(attrKey);
        if (meta != null)
        {
            value = meta.Clamp(value);
        }

        string baseKey = "Base" + attrKey;
        _data.Set(baseKey, value);

        // 标记脏并通知依赖的计算属性
        MarkDirty(attrKey);
    }

    /// <summary>
    /// 添加修改器
    /// </summary>
    public void AddModifier(AttributeModifier modifier)
    {
        if (modifier == null)
        {
            _log.Warn("尝试添加空修改器");
            return;
        }

        if (_modifiers.Any(m => m.Id == modifier.Id))
        {
            _log.Warn($"修改器 {modifier.Id} 已存在");
            return;
        }

        _modifiers.Add(modifier);
        MarkDirty(modifier.AttributeName);

        _log.Debug($"添加修改器: {modifier.Id} ({modifier.Type} {modifier.Value} -> {modifier.AttributeName})");
    }

    /// <summary>
    /// 移除修改器
    /// </summary>
    public void RemoveModifier(string modifierId)
    {
        var modifier = _modifiers.FirstOrDefault(m => m.Id == modifierId);
        if (modifier == null) return;

        _modifiers.Remove(modifier);
        MarkDirty(modifier.AttributeName);

        _log.Debug($"移除修改器: {modifierId}");
    }

    /// <summary>
    /// 标记属性为脏（需要重新计算）
    /// </summary>
    private void MarkDirty(string attrKey)
    {
        _isDirty = true;
        _cachedValues.Remove(attrKey);

        // 同时标记依赖此属性的计算属性为脏
        var dependents = AttributeRegistry.GetDependentComputedKeys(attrKey);
        foreach (var depKey in dependents)
        {
            _cachedValues.Remove(depKey);
        }

        AttributeChanged?.Invoke();
    }

    /// <summary>
    /// 计算最终值：(基础值 + Σ加法) × Π乘法
    /// </summary>
    private float CalculateFinalValue(string attrKey, float baseValue)
    {
        var attrModifiers = _modifiers
            .Where(m => m.AttributeName == attrKey)
            .OrderBy(m => m.Priority)
            .ToList();

        if (attrModifiers.Count == 0) return baseValue;

        // 加法修改器
        float additiveSum = attrModifiers
            .Where(m => m.Type == ModifierType.Additive)
            .Sum(m => m.Value);

        // 乘法修改器
        float multiplicativeProduct = attrModifiers
            .Where(m => m.Type == ModifierType.Multiplicative)
            .Aggregate(1f, (acc, m) => acc * m.Value);

        return (baseValue + additiveSum) * multiplicativeProduct;
    }

    private void RecalculateAll()
    {
        _cachedValues.Clear();
        _isDirty = false;
    }

    private void OnDataChanged(string key, object? oldVal, object? newVal)
    {
        // 如果是基础属性变化，标记为脏
        if (key.StartsWith("Base"))
        {
            string attrKey = key.Substring(4); // 移除 "Base" 前缀
            MarkDirty(attrKey);
        }
    }

}

````

---

## 8. 优化方案与最佳实践

### 8.1 性能优化

#### 8.1.1 避免热路径中的 LINQ

```csharp
// ❌ 不好：每次调用都创建新的迭代器
private float CalculateFinalValue(string attrKey, float baseValue)
{
    var attrModifiers = _modifiers
        .Where(m => m.AttributeName == attrKey)  // LINQ 分配
        .OrderBy(m => m.Priority)                // LINQ 分配
        .ToList();                               // 分配 List
}

// ✅ 优化：缓存过滤结果
private readonly List<AttributeModifier> _tempModifiers = new();

private float CalculateFinalValue(string attrKey, float baseValue)
{
    _tempModifiers.Clear();  // 复用 List

    // 手动过滤和排序
    foreach (var mod in _modifiers)
    {
        if (mod.AttributeName == attrKey)
        {
            _tempModifiers.Add(mod);
        }
    }

    if (_tempModifiers.Count == 0) return baseValue;

    // 手动排序（如果需要）
    _tempModifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

    // ... 计算逻辑
}
````

#### 8.1.2 按属性分组存储修改器（高级优化）

```csharp
// 如果修改器数量 > 50，考虑按属性分组
private readonly Dictionary<string, List<AttributeModifier>> _modifiersByAttr = new();

public void AddModifier(AttributeModifier modifier)
{
    if (!_modifiersByAttr.ContainsKey(modifier.AttributeName))
    {
        _modifiersByAttr[modifier.AttributeName] = new List<AttributeModifier>();
    }

    _modifiersByAttr[modifier.AttributeName].Add(modifier);
    MarkDirty(modifier.AttributeName);
}

private float CalculateFinalValue(string attrKey, float baseValue)
{
    if (!_modifiersByAttr.TryGetValue(attrKey, out var modifiers))
    {
        return baseValue;
    }

    // 直接使用分组后的修改器，无需过滤
    // ...
}
```

### 8.2 扩展性设计

#### 8.2.1 支持自定义修改器类型

```csharp
/// <summary>
/// 扩展修改器类型
/// </summary>
public enum ModifierType
{
    Additive,           // 加法：+10
    Multiplicative,     // 乘法：×1.5
    Override,           // 覆盖：直接设置为指定值
    PercentageBase,     // 基于基础值的百分比：基础值 × 10%
    PercentageFinal     // 基于最终值的百分比：最终值 × 10%（递归计算）
}

// 计算逻辑扩展
private float CalculateFinalValue(string attrKey, float baseValue)
{
    var modifiers = GetModifiersForAttribute(attrKey);

    // 1. 检查是否有覆盖修改器（优先级最高）
    var overrideModifier = modifiers.FirstOrDefault(m => m.Type == ModifierType.Override);
    if (overrideModifier != null)
    {
        return overrideModifier.Value;
    }

    // 2. 基于基础值的百分比
    float percentageBaseSum = modifiers
        .Where(m => m.Type == ModifierType.PercentageBase)
        .Sum(m => m.Value);

    // 3. 加法修改器
    float additiveSum = modifiers
        .Where(m => m.Type == ModifierType.Additive)
        .Sum(m => m.Value);

    // 4. 乘法修改器
    float multiplicativeProduct = modifiers
        .Where(m => m.Type == ModifierType.Multiplicative)
        .Aggregate(1f, (acc, m) => acc * m.Value);

    // 公式：(基础值 × (1 + 基础百分比) + 加法) × 乘法
    float result = (baseValue * (1f + percentageBaseSum) + additiveSum) * multiplicativeProduct;

    // 5. 基于最终值的百分比（递归应用）
    float percentageFinalSum = modifiers
        .Where(m => m.Type == ModifierType.PercentageFinal)
        .Sum(m => m.Value);

    result *= (1f + percentageFinalSum);

    return result;
}
```

#### 8.2.2 支持条件修改器（高级特性）

```csharp
/// <summary>
/// 条件修改器 - 仅在满足条件时生效
/// </summary>
public class ConditionalModifier : AttributeModifier
{
    public Func<bool> Condition { get; init; }

    public ConditionalModifier(
        string attrName,
        ModifierType type,
        float value,
        Func<bool> condition,
        int priority = 0,
        string? id = null)
        : base(attrName, type, value, priority, id)
    {
        Condition = condition;
    }

    public bool IsActive() => Condition?.Invoke() ?? true;
}

// 使用示例：低血量时增加伤害
var lowHpDamageBuff = new ConditionalModifier(
    AttrKey.Damage,
    ModifierType.Multiplicative,
    1.5f,
    condition: () => {
        var health = GetNode<HealthComponent>("Health");
        return health.CurrentHp < health.MaxHp * 0.3f;  // 血量 < 30%
    },
    id: "Buff_LowHpDamage"
);

_attr.AddModifier(lowHpDamageBuff);
```

#### 8.2.3 支持时限修改器（自动过期）

```csharp
/// <summary>
/// 时限修改器 - 自动过期的 Buff/Debuff
/// </summary>
public class TimedModifier : AttributeModifier
{
    public float Duration { get; init; }
    public float ElapsedTime { get; set; }

    public TimedModifier(
        string attrName,
        ModifierType type,
        float value,
        float duration,
        int priority = 0,
        string? id = null)
        : base(attrName, type, value, priority, id)
    {
        Duration = duration;
        ElapsedTime = 0;
    }

    public bool IsExpired() => ElapsedTime >= Duration;
}

// AttributeComponent 中添加更新逻辑
public override void _Process(double delta)
{
    bool hasExpired = false;

    foreach (var modifier in _modifiers.ToList())
    {
        if (modifier is TimedModifier timed)
        {
            timed.ElapsedTime += (float)delta;
            if (timed.IsExpired())
            {
                RemoveModifier(timed.Id);
                hasExpired = true;
            }
        }
    }

    if (hasExpired)
    {
        _log.Debug("有修改器已过期");
    }
}
```

### 8.3 调试与可视化

#### 8.3.1 属性调试面板

```csharp
/// <summary>
/// 获取属性调试信息
/// </summary>
public string GetDebugInfo(string attrKey)
{
    var meta = AttributeRegistry.GetMeta(attrKey);
    if (meta == null) return $"未知属性: {attrKey}";

    float baseValue = _data.Get<float>("Base" + attrKey, meta.DefaultValue);
    float finalValue = Get(attrKey);

    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"=== {meta.DisplayName} ({attrKey}) ===");
    sb.AppendLine($"基础值: {baseValue}");

    // 列出所有修改器
    var modifiers = _modifiers.Where(m => m.AttributeName == attrKey).ToList();
    if (modifiers.Count > 0)
    {
        sb.AppendLine("修改器:");
        foreach (var mod in modifiers)
        {
            string typeStr = mod.Type == ModifierType.Additive ? "+" : "×";
            sb.AppendLine($"  [{mod.Id}] {typeStr}{mod.Value} (优先级: {mod.Priority})");
        }
    }

    sb.AppendLine($"最终值: {meta.FormatValue(finalValue)}");

    return sb.ToString();
}

// 使用示例
GD.Print(_attr.GetDebugInfo(AttrKey.Damage));
// 输出：
// === 伤害 (Damage) ===
// 基础值: 10
// 修改器:
//   [Weapon_Sword] +5 (优先级: 0)
//   [Buff_Strength] ×1.5 (优先级: 0)
// 最终值: 22.5
```

#### 8.3.2 编辑器工具（Godot Inspector）

```csharp
#if TOOLS
/// <summary>
/// 编辑器专用：显示所有属性的当前值
/// </summary>
[Export(PropertyHint.MultilineText)]
public string EditorDebugInfo
{
    get
    {
        if (!Engine.IsEditorHint()) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 属性快照 ===");

        foreach (var category in Enum.GetValues<AttributeCategory>())
        {
            var attrs = AttributeRegistry.GetByCategory(category);
            if (!attrs.Any()) continue;

            sb.AppendLine($"\n[{category}]");
            foreach (var meta in attrs)
            {
                float value = Get(meta.Key);
                sb.AppendLine($"  {meta.DisplayName}: {meta.FormatValue(value)}");
            }
        }

        return sb.ToString();
    }
}
#endif
```

### 8.4 数据持久化

#### 8.4.1 保存/加载属性

```csharp
/// <summary>
/// 序列化属性数据（用于存档）
/// </summary>
public Dictionary<string, float> Serialize()
{
    var data = new Dictionary<string, float>();

    // 只保存基础属性（修改器不保存，由装备/Buff 系统重建）
    foreach (var key in AttributeRegistry.GetAllBasicKeys())
    {
        string baseKey = "Base" + key;
        if (_data.Has(baseKey))
        {
            data[key] = _data.Get<float>(baseKey);
        }
    }

    return data;
}

/// <summary>
/// 反序列化属性数据
/// </summary>
public void Deserialize(Dictionary<string, float> data)
{
    foreach (var kvp in data)
    {
        SetBase(kvp.Key, kvp.Value);
    }

    _log.Info($"加载了 {data.Count} 个属性");
}

// 使用示例
// 保存
var saveData = _attr.Serialize();
string json = System.Text.Json.JsonSerializer.Serialize(saveData);
File.WriteAllText("save.json", json);

// 加载
string json = File.ReadAllText("save.json");
var saveData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, float>>(json);
_attr.Deserialize(saveData);
```

---

## 9. 与其他系统集成

### 9.1 与 HealthComponent 集成

```csharp
public partial class HealthComponent : Node
{
    private AttributeComponent _attr;

    public float MaxHp => _attr?.Get(AttrKey.MaxHp) ?? 100;
    public float CurrentHp { get; private set; }

    public override void _Ready()
    {
        _attr = GetNode<AttributeComponent>("../AttributeComponent");

        // 监听属性变化，自动调整当前生命值
        _attr.AttributeChanged += OnAttributeChanged;

        CurrentHp = MaxHp;
    }

    private void OnAttributeChanged()
    {
        // 如果最大生命值增加，按比例恢复当前生命值
        float oldMaxHp = MaxHp;
        float newMaxHp = _attr.Get(AttrKey.MaxHp);

        if (newMaxHp > oldMaxHp)
        {
            float ratio = CurrentHp / oldMaxHp;
            CurrentHp = newMaxHp * ratio;
        }
        else if (CurrentHp > newMaxHp)
        {
            CurrentHp = newMaxHp;
        }
    }

    public override void _Process(double delta)
    {
        // 生命恢复
        float regen = _attr.Get(AttrKey.HpRegen);
        if (regen > 0 && CurrentHp < MaxHp)
        {
            Heal(regen * (float)delta);
        }
    }
}
```

### 9.2 与 DamageSystem 集成

```csharp
public partial class DamageSystem : Node
{
    public static DamageSystem Instance { get; private set; }

    /// <summary>
    /// 计算最终伤害（考虑暴击、护甲、闪避等）
    /// </summary>
    public float CalculateDamage(Node attacker, Node target, float baseDamage)
    {
        var attackerAttr = attacker.GetNode<AttributeComponent>("AttributeComponent");
        var targetAttr = target.GetNode<AttributeComponent>("AttributeComponent");

        if (attackerAttr == null || targetAttr == null)
        {
            return baseDamage;
        }

        float finalDamage = baseDamage;

        // 1. 攻击方伤害加成
        float attackerDamage = attackerAttr.Get(AttrKey.Damage);
        finalDamage += attackerDamage;

        // 2. 暴击判定
        float critChance = attackerAttr.Get(AttrKey.CritChance) / 100f;
        if (GD.Randf() < critChance)
        {
            float critDamage = attackerAttr.Get(AttrKey.CritDamage) / 100f;
            finalDamage *= critDamage;
            ShowCritEffect(target);
        }

        // 3. 闪避判定
        float dodgeChance = targetAttr.Get(AttrKey.DodgeChance) / 100f;
        if (GD.Randf() < dodgeChance)
        {
            ShowDodgeEffect(target);
            return 0;  // 完全闪避
        }

        // 4. 护甲减伤
        float armor = targetAttr.Get(AttrKey.Armor);
        float armorReduction = armor / (armor + 100f);  // 护甲公式
        finalDamage *= (1f - armorReduction);

        // 5. 伤害减免
        float damageReduction = targetAttr.Get(AttrKey.DamageReduction) / 100f;
        finalDamage *= (1f - damageReduction);

        // 6. 反伤
        float thorns = targetAttr.Get(AttrKey.Thorns) / 100f;
        if (thorns > 0)
        {
            float thornsDamage = finalDamage * thorns;
            ApplyThornsDamage(attacker, thornsDamage);
        }

        // 7. 生命偷取
        float lifeSteal = attackerAttr.Get(AttrKey.LifeSteal) / 100f;
        if (lifeSteal > 0)
        {
            float healAmount = finalDamage * lifeSteal;
            var health = attacker.GetNode<HealthComponent>("HealthComponent");
            health?.Heal(healAmount);
        }

        return Mathf.Max(1, finalDamage);  // 至少造成 1 点伤害
    }

    private void ShowCritEffect(Node target) { /* 暴击特效 */ }
    private void ShowDodgeEffect(Node target) { /* 闪避特效 */ }
    private void ApplyThornsDamage(Node attacker, float damage) { /* 反伤逻辑 */ }
}
```

### 9.3 与装备系统集成

```csharp
/// <summary>
/// 装备数据
/// </summary>
[GlobalClass]
public partial class EquipmentData : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public Godot.Collections.Array<AttributeBonus> Bonuses { get; set; }
}

/// <summary>
/// 属性加成配置
/// </summary>
[GlobalClass]
public partial class AttributeBonus : Resource
{
    [Export] public string AttributeKey { get; set; }
    [Export] public ModifierType Type { get; set; }
    [Export] public float Value { get; set; }
}

/// <summary>
/// 装备管理器
/// </summary>
public partial class EquipmentManager : Node
{
    private AttributeComponent _attr;
    private readonly Dictionary<string, List<string>> _equipmentModifiers = new();

    /// <summary>
    /// 装备物品
    /// </summary>
    public void Equip(EquipmentData equipment)
    {
        var modifierIds = new List<string>();

        foreach (var bonus in equipment.Bonuses)
        {
            string modifierId = $"Equipment_{equipment.Id}_{bonus.AttributeKey}";

            var modifier = new AttributeModifier(
                bonus.AttributeKey,
                bonus.Type,
                bonus.Value,
                priority: 10,  // 装备优先级较高
                id: modifierId
            );

            _attr.AddModifier(modifier);
            modifierIds.Add(modifierId);
        }

        _equipmentModifiers[equipment.Id] = modifierIds;
    }

    /// <summary>
    /// 卸下物品
    /// </summary>
    public void Unequip(string equipmentId)
    {
        if (!_equipmentModifiers.TryGetValue(equipmentId, out var modifierIds))
        {
            return;
        }

        foreach (var modifierId in modifierIds)
        {
            _attr.RemoveModifier(modifierId);
        }

        _equipmentModifiers.Remove(equipmentId);
    }
}
```

### 9.4 与升级系统集成

```csharp
/// <summary>
/// 升级选项数据
/// </summary>
[GlobalClass]
public partial class UpgradeOption : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string AttributeKey { get; set; }
    [Export] public float Value { get; set; }
    [Export] public bool IsPercentage { get; set; }
}

/// <summary>
/// 升级系统
/// </summary>
public partial class UpgradeSystem : Node
{
    /// <summary>
    /// 应用升级（永久修改基础值）
    /// </summary>
    public void ApplyUpgrade(Node entity, UpgradeOption upgrade)
    {
        var attr = entity.GetNode<AttributeComponent>("AttributeComponent");
        if (attr == null) return;

        if (upgrade.IsPercentage)
        {
            // 百分比加成：添加修改器
            string modifierId = $"Upgrade_{upgrade.Id}_{System.Guid.NewGuid()}";
            var modifier = new AttributeModifier(
                upgrade.AttributeKey,
                ModifierType.Multiplicative,
                1f + upgrade.Value / 100f,
                priority: 5,
                id: modifierId
            );
            attr.AddModifier(modifier);
        }
        else
        {
            // 固定值加成：直接修改基础值
            float currentBase = attr.Get(upgrade.AttributeKey);
            attr.SetBase(upgrade.AttributeKey, currentBase + upgrade.Value);
        }

        ShowUpgradeEffect(entity, upgrade);
    }

    /// <summary>
    /// 生成随机升级选项
    /// </summary>
    public List<UpgradeOption> GenerateUpgradeOptions(int count = 3)
    {
        var options = new List<UpgradeOption>();
        var allUpgrades = LoadAllUpgrades();  // 从资源加载

        // 随机选择
        for (int i = 0; i < count && allUpgrades.Count > 0; i++)
        {
            int index = GD.RandRange(0, allUpgrades.Count - 1);
            options.Add(allUpgrades[index]);
            allUpgrades.RemoveAt(index);
        }

        return options;
    }

    private void ShowUpgradeEffect(Node entity, UpgradeOption upgrade) { /* 升级特效 */ }
    private List<UpgradeOption> LoadAllUpgrades() { return new(); }
}
```

---

## 10. 实施计划

### 10.1 第一阶段：核心重构（1-2 天）

**目标**：建立类型安全的属性访问基础

- [ ] 创建 `AttrKey.cs`（属性常量定义）
- [ ] 创建 `AttributeCategory.cs`（分类枚举）
- [ ] 创建 `AttributeMeta.cs`（元数据类）
- [ ] 创建 `AttributeRegistry.cs`（注册表）
- [ ] 注册 20 个核心属性的元数据
- [ ] 修改现有 `AttributeComponent.cs`，支持 `Get(AttrKey.Damage)` 语法

**验收标准**：

```csharp
// 可以这样使用
float damage = _attr.Get(AttrKey.Damage);  // 有智能提示
var meta = AttributeRegistry.GetMeta(AttrKey.Damage);
GD.Print(meta.DisplayName);  // "伤害"
```

### 10.2 第二阶段：计算属性（1 天）

**目标**：实现自动计算的派生属性

- [ ] 创建 `ComputedAttribute.cs`
- [ ] 在 `AttributeRegistry` 中注册 3 个计算属性
  - `AttackInterval`
  - `EffectiveHp`
  - `DPS`
- [ ] 修改 `AttributeComponent.Get()` 支持计算属性
- [ ] 实现依赖追踪（属性变化时自动更新计算属性）

**验收标准**：

```csharp
_attr.SetBase(AttrKey.AttackSpeed, 200);  // 修改攻速
float interval = _attr.Get(AttrKey.AttackInterval);  // 自动重新计算
```

### 10.3 第三阶段：系统集成（2-3 天）

**目标**：将新属性系统集成到现有组件

- [ ] 重构 `HealthComponent`（使用 `AttrKey.MaxHp`）
- [ ] 重构 `VelocityComponent`（使用 `AttrKey.Speed`）
- [ ] 创建 `DamageSystem`（集成暴击、护甲、闪避）
- [ ] 测试战斗流程

**验收标准**：

- 玩家和敌人的生命值、移动速度从属性系统读取
- 伤害计算考虑暴击、护甲、闪避
- 添加 Buff 后属性正确变化

### 10.4 第四阶段：高级特性（可选，1-2 天）

**目标**：增强系统功能

- [ ] 实现条件修改器（`ConditionalModifier`）
- [ ] 实现时限修改器（`TimedModifier`）
- [ ] 添加调试面板（`GetDebugInfo`）
- [ ] 实现属性序列化（存档系统）

---

## 11. 测试用例

### 11.1 单元测试

```csharp
namespace BrotatoMy.Test
{
    using Godot;
    using System;

    public class AttributeSystemTests
    {
        [Test]
        public void TestBasicAttributeAccess()
        {
            var entity = new Node2D();
            var attr = new AttributeComponent();
            entity.AddChild(attr);

            // 设置基础值
            attr.SetBase(AttrKey.Damage, 10);

            // 验证获取
            Assert.AreEqual(10, attr.Get(AttrKey.Damage));
        }

        [Test]
        public void TestAdditiveModifier()
        {
            var attr = CreateAttributeComponent();
            attr.SetBase(AttrKey.Damage, 10);

            // 添加 +5 伤害
            attr.AddModifier(new AttributeModifier(
                AttrKey.Damage,
                ModifierType.Additive,
                5
            ));

            Assert.AreEqual(15, attr.Get(AttrKey.Damage));
        }

        [Test]
        public void TestMultiplicativeModifier()
        {
            var attr = CreateAttributeComponent();
            attr.SetBase(AttrKey.Damage, 10);

            // 添加 ×1.5 伤害
            attr.AddModifier(new AttributeModifier(
                AttrKey.Damage,
                ModifierType.Multiplicative,
                1.5f
            ));

            Assert.AreEqual(15, attr.Get(AttrKey.Damage));
        }

        [Test]
        public void TestCombinedModifiers()
        {
            var attr = CreateAttributeComponent();
            attr.SetBase(AttrKey.Damage, 10);

            // 加法：+5
            attr.AddModifier(new AttributeModifier(
                AttrKey.Damage,
                ModifierType.Additive,
                5
            ));

            // 乘法：×2
            attr.AddModifier(new AttributeModifier(
                AttrKey.Damage,
                ModifierType.Multiplicative,
                2f
            ));

            // (10 + 5) × 2 = 30
            Assert.AreEqual(30, attr.Get(AttrKey.Damage));
        }

        [Test]
        public void TestComputedAttribute()
        {
            var attr = CreateAttributeComponent();
            attr.SetBase(AttrKey.AttackSpeed, 100);

            // 攻击间隔 = 1.0 / (100 / 100) = 1.0
            Assert.AreEqual(1.0f, attr.Get(AttrKey.AttackInterval), 0.01f);

            // 修改攻速为 200
            attr.SetBase(AttrKey.AttackSpeed, 200);

            // 攻击间隔 = 1.0 / (200 / 100) = 0.5
            Assert.AreEqual(0.5f, attr.Get(AttrKey.AttackInterval), 0.01f);
        }

        [Test]
        public void TestAttributeClamp()
        {
            var attr = CreateAttributeComponent();

            // 暴击率最大 100%
            attr.SetBase(AttrKey.CritChance, 150);
            Assert.AreEqual(100, attr.Get(AttrKey.CritChance));

            // 移动速度最小 50
            attr.SetBase(AttrKey.Speed, 10);
            Assert.AreEqual(50, attr.Get(AttrKey.Speed));
        }

        private AttributeComponent CreateAttributeComponent()
        {
            var entity = new Node2D();
            var attr = new AttributeComponent();
            entity.AddChild(attr);
            return attr;
        }
    }
}
```

### 11.2 集成测试

```csharp
namespace BrotatoMy.Test
{
    /// <summary>
    /// 战斗系统集成测试
    /// </summary>
    public class CombatIntegrationTests
    {
        [Test]
        public void TestCriticalHit()
        {
            var attacker = CreateTestEntity();
            var target = CreateTestEntity();

            var attackerAttr = attacker.GetNode<AttributeComponent>("AttributeComponent");
            attackerAttr.SetBase(AttrKey.Damage, 10);
            attackerAttr.SetBase(AttrKey.CritChance, 100);  // 100% 暴击
            attackerAttr.SetBase(AttrKey.CritDamage, 200);  // 2 倍伤害

            var damageSystem = new DamageSystem();
            float damage = damageSystem.CalculateDamage(attacker, target, 0);

            // 10 × 2.0 = 20
            Assert.AreEqual(20, damage, 0.1f);
        }

        [Test]
        public void TestArmorReduction()
        {
            var attacker = CreateTestEntity();
            var target = CreateTestEntity();

            var attackerAttr = attacker.GetNode<AttributeComponent>("AttributeComponent");
            attackerAttr.SetBase(AttrKey.Damage, 100);

            var targetAttr = target.GetNode<AttributeComponent>("AttributeComponent");
            targetAttr.SetBase(AttrKey.Armor, 100);  // 50% 减伤

            var damageSystem = new DamageSystem();
            float damage = damageSystem.CalculateDamage(attacker, target, 0);

            // 100 × (1 - 100/(100+100)) = 50
            Assert.AreEqual(50, damage, 0.1f);
        }

        [Test]
        public void TestLifeSteal()
        {
            var attacker = CreateTestEntity();
            var target = CreateTestEntity();

            var attackerAttr = attacker.GetNode<AttributeComponent>("AttributeComponent");
            attackerAttr.SetBase(AttrKey.Damage, 100);
            attackerAttr.SetBase(AttrKey.LifeSteal, 20);  // 20% 吸血

            var attackerHealth = attacker.GetNode<HealthComponent>("HealthComponent");
            attackerHealth.TakeDamage(50);  // 受伤 50
            float hpBefore = attackerHealth.CurrentHp;

            var damageSystem = new DamageSystem();
            damageSystem.CalculateDamage(attacker, target, 0);

            // 应该恢复 100 × 0.2 = 20 生命值
            Assert.AreEqual(hpBefore + 20, attackerHealth.CurrentHp, 0.1f);
        }

        private Node2D CreateTestEntity()
        {
            var entity = new Node2D();

            var attr = new AttributeComponent();
            entity.AddChild(attr);

            var health = new HealthComponent();
            entity.AddChild(health);

            return entity;
        }
    }
}
```

---

## 12. 常见问题 (FAQ)

### Q1: 为什么用常量而不是枚举？

**A**: 常量支持扩展（Mod 可添加自定义属性），而枚举是封闭的。

```csharp
// ✅ 常量：Mod 可以扩展
public static class AttrKey
{
    public const string Damage = "Damage";
}

// Mod 代码
public static class ModAttrKey
{
    public const string MagicDamage = "MagicDamage";  // 新增属性
}

// ❌ 枚举：无法扩展
public enum AttrKey
{
    Damage,
    // Mod 无法添加新值
}
```

### Q2: 计算属性会影响性能吗？

**A**: 不会。计算属性只在依赖变化时重新计算，且结果会缓存。

```csharp
// 第一次调用：执行计算
float dps1 = _attr.Get(AttrKey.DPS);  // 计算

// 第二次调用：返回缓存
float dps2 = _attr.Get(AttrKey.DPS);  // 缓存

// 修改依赖属性后：重新计算
_attr.SetBase(AttrKey.Damage, 20);
float dps3 = _attr.Get(AttrKey.DPS);  // 重新计算
```

### Q3: 如何处理属性上限/下限？

**A**: 在 `AttributeMeta` 中定义约束，`Get()` 方法自动应用。

```csharp
// 定义约束
Register(new AttributeMeta
{
    Key = AttrKey.CritChance,
    MinValue = 0,
    MaxValue = 100  // 暴击率最大 100%
});

// 自动限制
_attr.SetBase(AttrKey.CritChance, 150);
float crit = _attr.Get(AttrKey.CritChance);  // 返回 100
```

### Q4: 修改器的优先级如何工作？

**A**: 数值越小越先计算，同优先级按添加顺序。

```csharp
// 优先级 0：先计算
_attr.AddModifier(new AttributeModifier(
    AttrKey.Damage, ModifierType.Additive, 5, priority: 0
));

// 优先级 10：后计算
_attr.AddModifier(new AttributeModifier(
    AttrKey.Damage, ModifierType.Multiplicative, 2f, priority: 10
));

// 计算顺序：(10 + 5) × 2 = 30
```

### Q5: 如何实现"每 10 点力量增加 1 点伤害"？

**A**: 使用计算属性或在 `SetBase` 时自动添加修改器。

```csharp
// 方案 1：计算属性
RegisterComputed(new ComputedAttribute
{
    Key = AttrKey.Damage,
    Dependencies = new[] { "Strength" },
    Compute = (attr) =>
    {
        float baseDamage = attr.GetBase(AttrKey.Damage);
        float strength = attr.Get("Strength");
        return baseDamage + Mathf.Floor(strength / 10f);
    }
});

// 方案 2：监听力量变化
_attr.AttributeChanged += () =>
{
    float strength = _attr.Get("Strength");
    float bonusDamage = Mathf.Floor(strength / 10f);

    // 移除旧修改器
    _attr.RemoveModifier("StrengthBonus");

    // 添加新修改器
    _attr.AddModifier(new AttributeModifier(
        AttrKey.Damage,
        ModifierType.Additive,
        bonusDamage,
        id: "StrengthBonus"
    ));
};
```

### Q6: 如何避免修改器 ID 冲突？

**A**: 使用命名规范或 GUID。

```csharp
// 方案 1：命名规范
string modifierId = $"{source}_{attrKey}_{uniqueId}";
// 例如：Weapon_Sword_Damage, Buff_Haste_AttackSpeed

// 方案 2：GUID（推荐）
string modifierId = System.Guid.NewGuid().ToString();

// 方案 3：组合
string modifierId = $"Buff_Haste_{System.Guid.NewGuid()}";
```

### Q7: 如何实现"攻击速度不能低于 10"？

**A**: 在元数据中设置 `MinValue`。

```csharp
Register(new AttributeMeta
{
    Key = AttrKey.AttackSpeed,
    MinValue = 10,  // 最低 10%
    MaxValue = 500  // 最高 500%
});

// 自动限制
_attr.AddModifier(new AttributeModifier(
    AttrKey.AttackSpeed,
    ModifierType.Multiplicative,
    0.01f  // 尝试降低到 1%
));

float speed = _attr.Get(AttrKey.AttackSpeed);  // 返回 10（最小值）
```

---

## 13. 性能基准测试

### 13.1 测试场景

```csharp
/// <summary>
/// 性能测试：1000 个实体，每个 20 个属性，10 个修改器
/// </summary>
public class PerformanceBenchmark
{
    [Benchmark]
    public void TestAttributeAccess()
    {
        var entities = CreateTestEntities(1000);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var entity in entities)
        {
            var attr = entity.GetNode<AttributeComponent>("AttributeComponent");

            // 访问所有属性
            float damage = attr.Get(AttrKey.Damage);
            float speed = attr.Get(AttrKey.Speed);
            float hp = attr.Get(AttrKey.MaxHp);
            // ... 其他 17 个属性
        }

        stopwatch.Stop();
        GD.Print($"访问 20000 次属性耗时: {stopwatch.ElapsedMilliseconds}ms");
        // 目标：< 10ms
    }

    [Benchmark]
    public void TestModifierAddRemove()
    {
        var entities = CreateTestEntities(1000);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var entity in entities)
        {
            var attr = entity.GetNode<AttributeComponent>("AttributeComponent");

            // 添加 10 个修改器
            for (int i = 0; i < 10; i++)
            {
                attr.AddModifier(new AttributeModifier(
                    AttrKey.Damage,
                    ModifierType.Additive,
                    5,
                    id: $"Mod_{i}"
                ));
            }

            // 移除 10 个修改器
            for (int i = 0; i < 10; i++)
            {
                attr.RemoveModifier($"Mod_{i}");
            }
        }

        stopwatch.Stop();
        GD.Print($"添加/移除 20000 个修改器耗时: {stopwatch.ElapsedMilliseconds}ms");
        // 目标：< 50ms
    }
}
```

### 13.2 预期性能指标

| 操作                   | 数量     | 目标耗时 | 说明              |
| ---------------------- | -------- | -------- | ----------------- |
| 属性访问（缓存命中）   | 10000 次 | < 1ms    | 直接返回缓存      |
| 属性访问（缓存未命中） | 10000 次 | < 10ms   | 需要计算          |
| 添加修改器             | 1000 次  | < 5ms    | 标记脏 + 列表添加 |
| 移除修改器             | 1000 次  | < 5ms    | 列表移除 + 标记脏 |
| 计算属性（简单）       | 1000 次  | < 2ms    | 1-2 个依赖        |
| 计算属性（复杂）       | 1000 次  | < 10ms   | 4+ 个依赖         |

---

## 14. 迁移指南

### 14.1 从旧系统迁移

**旧代码（字符串访问）**:

```csharp
float damage = _attr.Get("Damage", "BaseDamage");
float speed = _attr.Get("Speed", "BaseSpeed");
```

**新代码（类型安全）**:

```csharp
float damage = _attr.Get(AttrKey.Damage);
float speed = _attr.Get(AttrKey.Speed);
```

**迁移步骤**:

1. 全局搜索 `_attr.Get("`
2. 替换为 `_attr.Get(AttrKey.`
3. 使用 IDE 的智能提示选择正确的常量
4. 删除第二个参数（BaseKey 自动推断）

### 14.2 兼容性处理

如果需要同时支持旧代码，可以保留旧方法：

```csharp
/// <summary>
/// 旧版本兼容方法（已弃用）
/// </summary>
[Obsolete("请使用 Get(AttrKey.XXX) 代替")]
public float Get(string attrName, string baseAttrKey, float defaultBaseValue = 0f)
{
    return Get(attrName);
}
```

---

## 15. 总结

### 15.1 核心改进

| 方面         | 改进前         | 改进后             |
| ------------ | -------------- | ------------------ |
| **类型安全** | ❌ 字符串易错  | ✅ 常量 + 智能提示 |
| **元数据**   | ❌ 无描述/约束 | ✅ 完整元数据      |
| **计算属性** | ❌ 手动维护    | ✅ 自动计算        |
| **UI 集成**  | ❌ 硬编码      | ✅ 元数据驱动      |
| **扩展性**   | ❌ 难以扩展    | ✅ Mod 友好        |
| **调试**     | ❌ 难以追踪    | ✅ 调试面板        |

### 15.2 关键设计决策

1. **常量 vs 枚举**: 选择常量以支持 Mod 扩展
2. **元数据注册**: 集中管理属性定义，便于维护
3. **计算属性**: 自动依赖追踪，减少手动维护
4. **脏标记缓存**: 平衡性能与灵活性
5. **修改器优先级**: 支持复杂的 Buff 叠加逻辑

### 15.3 适用场景

✅ **适合使用本系统**:

- RPG 游戏（属性复杂）
- Roguelike 游戏（大量 Buff/Debuff）
- MOBA/ARPG（装备系统）
- 策略游戏（单位属性）

❌ **不适合使用本系统**:

- 极简游戏（属性 < 5 个）
- 纯动作游戏（无数值成长）
- 解谜游戏（无战斗系统）

### 15.4 后续优化方向

1. **属性公式系统**: 支持自定义计算公式（如 "Damage = Strength × 2 + Level × 5"）
2. **属性组**: 批量操作相关属性（如"战斗属性"、"防御属性"）
3. **属性快照**: 保存/恢复属性状态（用于技能预览）
4. **属性动画**: 属性变化时的过渡动画
5. **网络同步**: 多人游戏的属性同步优化

---

## 16. 参考资料

### 16.1 相关文档

- [项目框架文档](../../../框架/项目框架.md)
- [Data 容器文档](../../../Tools/Data.md)
- [组件系统设计](../README.md)

### 16.2 推荐阅读

- [Diablo 3 属性系统分析](https://www.gamasutra.com/view/news/174587/InDepth_Diablo_IIIs_attribute_system.php)
- [Path of Exile 修改器系统](https://pathofexile.fandom.com/wiki/Modifiers)
- [Unity ECS 属性系统](https://docs.unity3d.com/Packages/com.unity.entities@latest)

---

**文档版本**: v1.0  
**创建日期**: 2024-12-31  
**最后更新**: 2024-12-31  
**作者**: Kiro AI Assistant  
**状态**: ✅ 设计完成，待实施

---

## 附录 A：完整属性列表

| 属性键          | 显示名称   | 分类     | 默认值 | 范围      | 说明                       |
| --------------- | ---------- | -------- | ------ | --------- | -------------------------- |
| MaxHp           | 最大生命值 | Health   | 100    | 1-99999   | 角色的最大生命值           |
| HpRegen         | 生命恢复   | Health   | 0      | 0+        | 每秒恢复的生命值           |
| LifeSteal       | 生命偷取   | Health   | 0      | 0-100%    | 造成伤害时恢复生命的百分比 |
| Armor           | 护甲       | Defense  | 0      | 0+        | 减少受到的物理伤害         |
| Damage          | 伤害       | Attack   | 10     | 0+        | 基础攻击伤害               |
| AttackSpeed     | 攻击速度   | Attack   | 100    | 10-500%   | 攻击速度百分比             |
| CritChance      | 暴击率     | Attack   | 5      | 0-100%    | 触发暴击的概率             |
| CritDamage      | 暴击伤害   | Attack   | 150    | 100-1000% | 暴击时的伤害倍率           |
| Range           | 攻击范围   | Attack   | 200    | 0+        | 武器的攻击距离             |
| Knockback       | 击退       | Attack   | 0      | 0+        | 攻击时的击退力度           |
| DodgeChance     | 闪避率     | Defense  | 0      | 0-75%     | 完全躲避攻击的概率         |
| DamageReduction | 伤害减免   | Defense  | 0      | 0-90%     | 减少受到伤害的百分比       |
| Thorns          | 反伤       | Defense  | 0      | 0+        | 反弹受到伤害的百分比       |
| Speed           | 移动速度   | Movement | 300    | 50-1000   | 角色的移动速度             |
| PickupRange     | 拾取范围   | Resource | 100    | 0+        | 自动拾取物品的距离         |
| ExpGain         | 经验获取   | Resource | 100    | 0+        | 获得经验的加成百分比       |
| LuckBonus       | 幸运       | Resource | 0      | 0+        | 影响掉落品质和数量         |
| Pierce          | 穿透       | Special  | 0      | 0+        | 投射物可穿透的敌人数量     |
| ProjectileCount | 投射物数量 | Special  | 1      | 1-50      | 每次攻击发射的投射物数量   |
| AreaSize        | 范围大小   | Special  | 100    | 10-500%   | 技能和武器的范围倍率       |

## 附录 B：计算属性公式

| 属性键         | 公式                                                                           | 依赖                                        |
| -------------- | ------------------------------------------------------------------------------ | ------------------------------------------- |
| AttackInterval | 1.0 / (AttackSpeed / 100)                                                      | AttackSpeed                                 |
| EffectiveHp    | MaxHp / (1 - DamageReduction / 100)                                            | MaxHp, DamageReduction                      |
| DPS            | Damage × (AttackSpeed / 100) × (1 + CritChance / 100 × (CritDamage / 100 - 1)) | Damage, AttackSpeed, CritChance, CritDamage |

---

**🎉 属性系统设计文档完成！**

下一步：开始实施第一阶段（核心重构）
