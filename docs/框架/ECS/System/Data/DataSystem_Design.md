# Data 容器增强设计文档 - 统一数据管理系统

## 📋 核心变化说明

**本文档已将所有命名统一为 Data 系列：**

- `PropKey` → `DataKey`
- `PropertyMeta` → `DataMeta`
- `PropertyRegistry` → `DataRegistry`
- `PropertyType` → `DataType`
- `PropertyCategory` → `DataCategory`
- `ComputedProperty` → `ComputedData`
- `AttributeModifier` → `DataModifier`

**核心理念：Data 是唯一数据源，所有数据（普通数据、可修改数据、计算数据）统一从 Data 容器访问。**

---

## 📋 目录

1. [现状分析](#1-现状分析)
2. [核心问题](#2-核心问题)
3. [设计目标](#3-设计目标)
4. [数据定义优化](#4-数据定义优化)
5. [架构设计](#5-架构设计)
6. [实现方案](#6-实现方案)
7. [使用示例](#7-使用示例)

---

## 1. 现状分析

### 1.1 当前实现

**Data.cs** (已完成增强):

- ✅ 通用键值存储，支持任意类型
- ✅ 事件通知（OnValueChanged）
- ✅ 算术运算（Add, Multiply）
- ✅ 元数据支持（DataMeta）
- ✅ 修改器系统（DataModifier）
- ✅ 计算数据支持（ComputedData）
- ✅ 自动约束验证
- ✅ 类型安全访问（DataKey 常量）

---

## 2. 设计目标（已实现）

### 2.1 核心原则

1. **单一数据源**：所有数据统一从 Data 访问 ✅
2. **元数据驱动**：数据的约束、描述、分类在定义时声明 ✅
3. **按需增强**：只有需要修改器的数据才启用修改器系统 ✅
4. **类型安全**：使用常量代替字符串，编译期检查 ✅
5. **性能优先**：避免反射和过度抽象，保持简洁 ✅

### 2.2 实现方案：Data + 元数据层（已采用）

**核心思想**：

- **Data 容器**：保持通用键值存储的本质，增加元数据支持
- **DataMeta**：定义数据的类型、约束、描述
- **修改器系统**：作为 Data 的可选增强功能
- **计算数据**：自动依赖追踪和缓存

**架构图**：

```
┌─────────────────────────────────────────────────────┐
│  Data (核心数据容器)                                 │
│  - 存储所有数据（基础值 + 修改器）                   │
│  - 支持元数据约束                                    │
│  - 支持计算数据                                      │
│  - 支持修改器（可选）                                │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  DataMeta (数据元数据)                               │
│  - 类型、默认值、约束                                │
│  - 描述、分类、图标                                  │
│  - 是否支持修改器                                    │
└─────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────┐
│  DataKey (常量类)                                    │
│  - 类型安全的数据键                                  │
└─────────────────────────────────────────────────────┘
```

---

## 3. 实现状态

### 3.1 已完成功能

- ✅ **DataKey**：类型安全的数据键常量
- ✅ **DataMeta**：完整的元数据系统
- ✅ **DataRegistry**：静态注册表，管理所有元数据
- ✅ **DataModifier**：修改器系统，支持 Buff/Debuff
- ✅ **ComputedData**：计算数据，自动依赖追踪
- ✅ **Data 容器增强**：集成所有功能的统一数据容器

---

## 4. 数据定义优化

### 4.1 土豆兄弟核心数据

#### 生命系统（4 个）

- `MaxHp` - 最大生命值
- `HpRegen` - 生命恢复/秒
- `LifeSteal` - 生命偷取%
- `Armor` - 护甲值

#### 攻击系统（6 个）

- `Damage` - 基础伤害
- `AttackSpeed` - 攻击速度%
- `CritChance` - 暴击率%
- `CritDamage` - 暴击伤害%
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

#### 计算数据（派生数据）

- `AttackInterval` = 1.0 / (AttackSpeed / 100)
- `EffectiveHp` = MaxHp / (1 - DamageReduction)
- `DPS` = Damage × AttackSpeed × (1 + CritChance × CritDamage)

---

## 5. 架构设计

### 5.1 核心设计思想

**关键决策**：

1. **Data 是唯一数据源**：所有数据都存储在 Data 中
2. **元数据定义数据特性**：DataMeta 描述数据的类型、约束、是否支持修改器
3. **修改器作为可选功能**：只有声明 `SupportModifiers = true` 的数据才启用修改器
4. **计算数据自动处理**：在元数据中定义依赖关系，Data 自动计算和缓存

### 5.2 数据键系统（DataKey）

```csharp
/// <summary>
/// 数据键定义 - 类型安全的数据访问
/// 使用常量而非枚举，支持 Mod 扩展
/// </summary>
public static class DataKey
{
    // === 基础信息 ===
    public const string Name = "Name";
    public const string Level = "Level";

    // === 生命系统 ===
    public const string MaxHp = "MaxHp";
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

    // === 计算数据（只读） ===
    public const string AttackInterval = "AttackInterval";
    public const string EffectiveHp = "EffectiveHp";
    public const string DPS = "DPS";
}
```

### 5.3 数据元数据系统（DataMeta）

```csharp
/// <summary>
/// 数据元数据 - 描述数据的所有特性
/// </summary>
public class DataMeta
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string Description { get; init; } = "";
    public DataCategory Category { get; init; } = DataCategory.Basic;
    public required Type Type { get; init; }
    public object? DefaultValue { get; init; }
    public float MinValue { get; init; } = float.MinValue;
    public float MaxValue { get; init; } = float.MaxValue;
    public bool IsPercentage { get; init; }
    public bool? SupportModifiers { get; init; } // 可空，默认为空时根据类型推断
    public string IconPath { get; init; } = "";
    public List<string>? Options { get; init; } // 固定选项列表（int 存储，string 显示）

    // 智能推断是否支持修改器
    public bool ActualSupportModifiers => SupportModifiers ?? (Type == typeof(float) || Type == typeof(int));

    /// <summary>
    /// 验证并限制数据值
    /// </summary>
    public object Clamp(object value)
    {
        if (Type == typeof(float) || Type == typeof(int))
        {
            float f = Convert.ToSingle(value);
            float clamped = Mathf.Clamp(f, MinValue, MaxValue);
            return Type == typeof(int) ? (int)clamped : clamped;
        }
        return value;
    }
}

/// <summary>
/// 数据分类枚举
/// </summary>
public enum DataCategory
{
    Basic,      // 基础信息
    Health,     // 生命系统
    Attack,     // 攻击系统
    Defense,    // 防御系统
    Movement,   // 移动系统
    Resource,   // 资源系统
    Special,    // 特殊机制
    Computed    // 计算数据
}
```

### 5.4 计算数据系统（ComputedData）

```csharp
/// <summary>
/// 计算数据定义 - 由其他数据派生的只读数据
/// </summary>
public class ComputedData
{
    public string Key { get; init; }
    public string[] Dependencies { get; init; }
    public Func<Data, object> Compute { get; init; }

    public bool DependsOn(string dataKey)
    {
        return Dependencies.Contains(dataKey);
    }
}
```

### 5.5 数据注册表（DataRegistry）

```csharp
/// <summary>
/// 数据注册表 - 管理所有数据的元数据和计算规则
/// 静态初始化，无需运行时注册
/// </summary>
public static class DataRegistry
{
    private static readonly Log _log = new("DataRegistry");

    private static readonly Dictionary<string, DataMeta> _metaRegistry = new();
    private static readonly Dictionary<string, ComputedData> _computedRegistry = new();

    static DataRegistry()
    {
        RegisterBasicData();
        RegisterComputedData();
        _log.Info($"数据注册表初始化完成：{_metaRegistry.Count} 个基础数据，{_computedRegistry.Count} 个计算数据");
    }

    private static void RegisterBasicData()
    {
        // 基础信息（自动推断不支持修改器）
        Register(new DataMeta
        {
            Key = DataKey.Name,
            DisplayName = "名称",
            Description = "实体的名称",
            Category = DataCategory.Basic,
            Type = typeof(string),
            DefaultValue = ""
        });

        // 生命系统（自动推断支持修改器）
        Register(new DataMeta
        {
            Key = DataKey.MaxHp,
            DisplayName = "最大生命值",
            Description = "角色的最大生命值",
            Category = DataCategory.Health,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 1,
            MaxValue = 99999
        });

        // ... 其他数据注册
    }

    private static void RegisterComputedData()
    {
        RegisterComputed(new ComputedData
        {
            Key = DataKey.AttackInterval,
            Dependencies = new[] { DataKey.AttackSpeed },
            Compute = (data) =>
            {
                // 对于已注册的数据键，无需传入默认值
                float attackSpeed = data.Get<float>(DataKey.AttackSpeed);
                return 1.0f / (attackSpeed / 100f);
            }
        });

        RegisterComputed(new ComputedData
        {
            Key = DataKey.DPS,
            Dependencies = new[] { DataKey.Damage, DataKey.AttackSpeed, DataKey.CritChance, DataKey.CritDamage },
            Compute = (data) =>
            {
                // 所有已注册的数据键都会自动使用 DataRegistry 中的默认值
                float damage = data.Get<float>(DataKey.Damage);
                float attackSpeed = data.Get<float>(DataKey.AttackSpeed) / 100f;
                float critChance = data.Get<float>(DataKey.CritChance) / 100f;
                float critDamage = data.Get<float>(DataKey.CritDamage) / 100f;

                float avgDamage = damage * (1f + critChance * (critDamage - 1f));
                return avgDamage * attackSpeed;
            }
        });
    }

    private static void Register(DataMeta meta)
    {
        _metaRegistry[meta.Key] = meta;
    }

    private static void RegisterComputed(ComputedData computed)
    {
        _computedRegistry[computed.Key] = computed;
    }

    // === 公共查询接口 ===

    public static DataMeta? GetMeta(string key)
    {
        return _metaRegistry.TryGetValue(key, out var meta) ? meta : null;
    }

    public static ComputedData? GetComputed(string key)
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

### 5.5 数据修改器 (DataModifier)

```csharp
/// <summary>
/// 数据修改器 - 用于 Buff/Debuff 系统
/// </summary>
public class DataModifier
{
    public string Id { get; init; }
    public ModifierType Type { get; init; }
    public float Value { get; init; }
    public int Priority { get; init; }

    /// <summary>
    /// 修改器来源对象（例如：装备 Entity、Buff 实例）
    /// 用于按来源批量移除修改器
    /// </summary>
    public object? Source { get; init; }

    public DataModifier(ModifierType type, float value, int priority = 0, string? id = null, object? source = null)
    {
        Id = id ?? System.Guid.NewGuid().ToString();
        Type = type;
        Value = value;
        Priority = priority;
        Source = source;
    }
}
```

### 5.6 Data 容器增强

```csharp
/// <summary>
/// Data 容器增强版 - 支持元数据和修改器
/// </summary>
public class Data
{
    private readonly Dictionary<string, object> _data = new();
    private readonly Dictionary<string, List<DataModifier>> _modifiers = new();
    private readonly Dictionary<string, object> _cachedValues = new();
    private readonly HashSet<string> _dirtyKeys = new();

    public event Action<string, object?, object?>? OnValueChanged;

    /// <summary>
    /// 设置基础值（自动应用元数据约束）
    /// </summary>
    public bool Set<T>(string key, T value)
    {
        var meta = DataRegistry.GetMeta(key);
        if (meta != null)
        {
            // 选项验证
            if (meta.HasOptions && !meta.IsValidOption(value!))
            {
                _log.Error($"无效的选项值: {key} = {value}");
                return false;
            }
            value = (T)meta.Clamp(value!);  // 自动应用约束
        }
        // ... 设置逻辑
    }

    /// <summary>
    /// 获取最终值（自动推断类型）
    /// </summary>
    public object Get(string key)
    {
        var meta = DataRegistry.GetMeta(key);
        if (meta == null) return _data.GetValueOrDefault(key);
        return GetTyped(key, meta.Type, meta.GetDefaultValue());
    }

    /// <summary>
    /// 获取最终值（泛型访问）
    /// </summary>
    public T Get<T>(string key, T defaultValue = default!)
    {
        return (T)GetTyped(key, typeof(T), defaultValue!);
    }

    /// <summary>
    /// 添加修改器
    /// </summary>
    public void AddModifier(string key, DataModifier modifier)
    {
        if (!DataRegistry.SupportModifiers(key))
        {
            GD.PrintErr($"数据 {key} 不支持修改器");
            return;
        }

        if (!_modifiers.ContainsKey(key))
        {
            _modifiers[key] = new List<DataModifier>();
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
    /// 根据来源移除所有修改器
    /// </summary>
    public void RemoveModifiersBySource(object source)
    {
        foreach (var key in _modifiers.Keys.ToList())
        {
            if (_modifiers.TryGetValue(key, out var modifiers))
            {
                var removedCount = modifiers.RemoveAll(m => m.Source == source);
                if (removedCount > 0)
                {
                    MarkDirty(key);
                }
            }
        }
    }

    /// <summary>
    /// 将另一个 Data 容器的数据转换为修改器应用到当前容器
    /// </summary>
    public void ApplyDataAsModifiers(Data sourceData, object sourceEntity)
    {
        var allData = sourceData.GetAll();
        foreach (var kvp in allData)
        {
            if (kvp.Value is float || kvp.Value is int)
            {
                float value = Convert.ToSingle(kvp.Value);
                if (DataRegistry.SupportModifiers(kvp.Key))
                {
                    AddModifier(kvp.Key, new DataModifier(ModifierType.Additive, value, source: sourceEntity));
                }
            }
        }
    }

    /// <summary>
    /// 从 Resource 加载数据
    /// </summary>
    public void LoadFromResource(Resource resource)
    {
        // 自动遍历 Resource 属性并 Set 到 Data
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

    private void MarkDirty(string key)
    {
        _dirtyKeys.Add(key);
        _cachedValues.Remove(key);

        // 标记依赖此数据的计算数据为脏
        var dependents = DataRegistry.GetDependentComputedKeys(key);
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

---

## 6. 实现方案

### 6.1 文件结构

```
Src/Tools/Data/
├── Data.cs                        # 核心数据容器
├── DataKey.cs                     # 数据键常量
├── DataMeta.cs                    # 数据元数据
├── DataRegistry.cs                # 数据注册表
├── DataCategory.cs                # 数据分类枚举
├── ComputedData.cs                # 计算数据
└── DataModifier.cs                # 数据修改器
```

**注意**：

- ✅ `AttributeComponent` 已废弃，所有功能已集成到 `Data` 中
- 所有文件位于 `Src/Tools/Data/` 目录下

---

## 7. 使用示例

### 7.1 基础使用

```csharp
public partial class Player : Entity
{
    // ✅ Data 作为 Entity 的属性，不需要扩展到 Node
    public Data Data { get; private set; } = new Data();

    public override void _Ready()
    {
        // ✅ 获取数据（已注册的键无需默认值）
        int level = Data.Get<int>(DataKey.Level);     // 显式指定类型
        var speed = Data.Get(DataKey.Speed);          // 自动推断类型 (var)
        var damage = Data.Get(DataKey.Damage);

        // ✅ 添加修改器
        Data.AddModifier(DataKey.Damage, new DataModifier(
           ModifierType.Additive,
           5,
           id: "Weapon_Sword"
       ));

       // ✅ 计算数据（自动计算，无需默认值）
       float dps = Data.Get<float>(DataKey.DPS);
       float attackInterval = Data.Get<float>(DataKey.AttackInterval);

       // ✅ 元数据查询
       var meta = DataRegistry.GetMeta(DataKey.Damage);
       GD.Print($"{meta.DisplayName}: {meta.FormatValue(damage)}");
   }
}
```

### 7.2 修改器使用（Buff/Debuff）

```csharp
// 添加攻速 Buff（+50%）
var buffId = "Buff_Haste";
Data.AddModifier(DataKey.AttackSpeed, new DataModifier(
    ModifierType.Multiplicative,
    1.5f,  // 150% = 1.5 倍
    priority: 0,
    id: buffId
));

// 5 秒后移除
GetTree().CreateTimer(5.0).Timeout += () => {
    Data.RemoveModifier(DataKey.AttackSpeed, buffId);
};
```

---

## 8. 迁移指南

### 8.1 从旧系统迁移

**旧代码（字符串访问）**:

```csharp
float damage = _attr.Get("Damage", "BaseDamage");
float speed = _attr.Get("Speed", "BaseSpeed");
```

**新代码（类型安全，Entity 中使用）**:

```csharp
// Entity 中直接使用 Data 属性（已注册的键无需默认值）
float damage = Data.Get<float>(DataKey.Damage);
float speed = Data.Get<float>(DataKey.Speed);

// DataRegistry 会自动提供默认值
// 如果数据不存在，返回在 DataMeta 中注册的默认值
```

---

## 9. 总结

### 9.1 核心改进

| 方面         | 改进前         | 改进后             |
| ------------ | -------------- | ------------------ |
| **类型安全** | ❌ 字符串易错  | ✅ 常量 + 智能提示 |
| **元数据**   | ❌ 无描述/约束 | ✅ 完整元数据      |
| **计算数据** | ❌ 手动维护    | ✅ 自动计算        |
| **UI 集成**  | ❌ 硬编码      | ✅ 元数据驱动      |
| **扩展性**   | ❌ 难以扩展    | ✅ Mod 友好        |

### 9.2 关键设计决策

1. **常量 vs 枚举**: 选择常量以支持 Mod 扩展
2. **元数据注册**: 集中管理数据定义，便于维护
3. **计算数据**: 自动依赖追踪，减少手动维护
4. **脏标记缓存**: 平衡性能与灵活性
5. **修改器优先级**: 支持复杂的 Buff 叠加逻辑

---

**文档版本**: v2.3  
**创建日期**: 2025-01-01  
**最后更新**: 2025-01-08  
**作者**: Kiro AI Assistant  
**状态**: ✅ 已实施 (v2.3: 改进默认值处理，无需为已注册键传入默认值)
