# Data 容器系统 - 统一数据管理

## 📋 概述

Data 容器是一个增强版的动态数据管理系统，提供类型安全、元数据驱动、修改器支持和计算数据的统一数据访问方案。

**核心理念**：Data 是唯一数据源，所有数据（普通数据、可修改数据、计算数据）统一从 Data 容器访问。

## ✨ 核心特性

- ✅ **类型安全**：使用常量代替字符串，编译期检查，智能提示
- ✅ **元数据驱动**：数据的类型、约束、描述在定义时声明
- ✅ **修改器系统**：支持 Buff/Debuff，自动计算最终值
- ✅ **计算数据**：自动依赖追踪，缓存优化
- ✅ **事件监听**：数据变更自动通知
- ✅ **性能优化**：脏标记缓存，避免重复计算
- ✅ **Mod 友好**：使用常量而非枚举，支持扩展

## 🏗️ 架构设计

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

## 📦 核心组件

### 1. Data 容器 (`Data.cs`)

核心数据容器，提供统一的数据访问接口。

**核心公式**：

```
最终值 = (基础值 + Σ加法修改器) × Π乘法修改器
```

### 2. DataKey (`DataKey.cs`)

类型安全的数据键常量定义。

**数据分类**：

- **基础信息**：Name, Level
- **生命系统**：MaxHp, HpRegen, LifeSteal, Armor
- **攻击系统**：Damage, AttackSpeed, CritChance, CritDamage, Range, Knockback
- **防御系统**：DodgeChance, DamageReduction, Thorns
- **移动系统**：Speed
- **资源系统**：PickupRange, ExpGain, LuckBonus
- **特殊机制**：Pierce, ProjectileCount, AreaSize
- **计算数据**：AttackInterval, EffectiveHp, DPS

### 3. DataMeta (`DataMeta.cs`)

数据元数据，描述数据的所有特性。

**包含信息**：

- 键名、显示名称、描述
- 数据类型（`System.Type`）、分类
- 默认值、最小值、最大值
- 是否为百分比
- 是否支持修改器（根据类型智能推断）
- 图标路径
- 固定选项列表（用于枚举类数据，如：稀有度、状态）

### 4. DataRegistry (`DataRegistry.cs`)

数据注册表，管理所有数据的元数据和计算规则。

**功能**：

- 静态初始化，无需运行时注册
- 提供元数据查询接口
- 管理计算数据的依赖关系

### 5. DataModifier (`DataModifier.cs`)

数据修改器，用于实现 Buff/Debuff 系统。

**修改器类型**：

- **Additive（加法）**：直接加到基础值
- **Multiplicative（乘法）**：作为乘数（1.0 = 100%，1.5 = 150%）

### 6. ComputedData (`ComputedData.cs`)

计算数据定义，由其他数据派生的只读数据。

**示例**：

- `AttackInterval = 1.0 / (AttackSpeed / 100)`
- `EffectiveHp = MaxHp / (1 - DamageReduction)`
- `DPS = Damage × AttackSpeed × (1 + CritChance × CritDamage)`

## 🚀 快速开始

### 基础使用

```csharp
public partial class Player : Entity
{
    // Data 作为 Entity 的属性
    public Data Data { get; private set; } = new Data();

    public override void _Ready()
    {
        // ✅ 设置基础数据
        Data.Set(DataKey.Name, "玩家");
        Data.Set(DataKey.Level, 1);
        Data.Set(DataKey.MaxHp, 100f);
        Data.Set(DataKey.Damage, 10f);
        Data.Set(DataKey.Speed, 300f);

        // ✅ 获取数据（两种方式效果一致）
        float maxHp = Data.Get<float>(DataKey.MaxHp); // 显式指定类型
        var damage = Data.Get(DataKey.Damage);        // 自动推断类型 (var)
        var name = Data.Get(DataKey.Name);

        // ✅ 算术运算
        Data.Add(DataKey.MaxHp, 20f);      // 生命值 +20
        Data.Multiply(DataKey.Damage, 1.5f); // 伤害 ×1.5

        // ✅ 获取计算数据（自动计算）
        float dps = Data.Get<float>(DataKey.DPS);
        float attackInterval = Data.Get<float>(DataKey.AttackInterval);
    }
}
```

### 修改器系统（Buff/Debuff）

```csharp
// 添加攻速 Buff（+50%）
var buffId = "Buff_Haste";
Data.AddModifier(DataKey.AttackSpeed, new DataModifier(
    ModifierType.Multiplicative,
    1.5f,  // 150% = 1.5 倍
    priority: 0,
    id: buffId
));

// 添加伤害 Buff（+10 点）
Data.AddModifier(DataKey.Damage, new DataModifier(
    ModifierType.Additive,
    10f,
    id: "Buff_Strength"
));

// 5 秒后移除 Buff
GetTree().CreateTimer(5.0).Timeout += () => {
    Data.RemoveModifier(DataKey.AttackSpeed, buffId);
};

// 检查是否拥有 Buff
bool hasBuff = Data.HasModifier(DataKey.AttackSpeed, buffId);

// 获取所有修改器
var modifiers = Data.GetModifiers(DataKey.Damage);

// 清除所有修改器
Data.ClearModifiers(DataKey.Damage);
```

### 事件监听

```csharp
// 全局监听所有数据变更
Data.OnValueChanged += (key, oldValue, newValue) => {
    GD.Print($"数据变更: {key} = {oldValue} -> {newValue}");
};

// 监听特定数据变更
Data.On(DataKey.MaxHp, (oldValue, newValue) => {
    GD.Print($"生命值变更: {oldValue} -> {newValue}");
});

// 取消监听
Action<object?, object?> callback = (old, @new) => { /* ... */ };
Data.On(DataKey.MaxHp, callback);
Data.Off(DataKey.MaxHp, callback);
```

### 元数据查询

```csharp
// 获取元数据
var meta = DataRegistry.GetMeta(DataKey.Damage);
if (meta != null)
{
    GD.Print($"显示名称: {meta.DisplayName}");
    GD.Print($"描述: {meta.Description}");
    GD.Print($"默认值: {meta.DefaultValue}");
    GD.Print($"范围: {meta.MinValue} - {meta.MaxValue}");
    GD.Print($"支持修改器: {meta.SupportModifiers}");

    // 格式化显示
    float damage = Data.Get<float>(DataKey.Damage);
    GD.Print($"伤害: {meta.FormatValue(damage)}");
}

// 按分类查询
var attackData = DataRegistry.GetMetaByCategory(DataCategory.Attack);
foreach (var meta in attackData)
{
    GD.Print($"{meta.DisplayName}: {meta.Description}");
}
```

### 批量操作

```csharp
// 批量设置
Data.SetMultiple(new Dictionary<string, object>
{
    { DataKey.MaxHp, 150f },
    { DataKey.Damage, 20f },
    { DataKey.Speed, 350f }
});

// 获取所有数据
var allData = Data.GetAll();
foreach (var kvp in allData)
{
    GD.Print($"{kvp.Key} = {kvp.Value}");
}

// 清空所有数据
Data.Clear();
```

## 📊 计算数据详解

计算数据是由其他数据派生的只读数据，自动追踪依赖关系并缓存结果。

### 内置计算数据

#### 1. 攻击间隔 (AttackInterval)

```csharp
// 公式：1.0 / (攻击速度 / 100)
// 依赖：AttackSpeed
float attackInterval = Data.Get<float>(DataKey.AttackInterval);
```

#### 2. 有效生命值 (EffectiveHp)

```csharp
// 公式：最大生命值 / (1 - 伤害减免)
// 依赖：MaxHp, DamageReduction
float effectiveHp = Data.Get<float>(DataKey.EffectiveHp);
```

#### 3. 每秒伤害 (DPS)

```csharp
// 公式：伤害 × 攻击速度 × (1 + 暴击率 × (暴击伤害 - 1))
// 依赖：Damage, AttackSpeed, CritChance, CritDamage
float dps = Data.Get<float>(DataKey.DPS);
```

### 自动依赖追踪

当基础数据变更时，依赖它的计算数据会自动标记为脏并重新计算：

```csharp
// 设置基础数据
Data.Set(DataKey.Damage, 10f);
Data.Set(DataKey.AttackSpeed, 100f);

// 获取计算数据（自动计算）
float dps1 = Data.Get<float>(DataKey.DPS); // 计算：10 × 1.0 = 10

// 修改基础数据
Data.Set(DataKey.Damage, 20f);

// 计算数据自动更新
float dps2 = Data.Get<float>(DataKey.DPS); // 重新计算：20 × 1.0 = 20
```

## 🔧 高级用法

### 获取基础值（不应用修改器）

```csharp
// 获取最终值（包含修改器）
float finalDamage = Data.Get<float>(DataKey.Damage);

// 获取基础值（不包含修改器）
float baseDamage = Data.GetBase<float>(DataKey.Damage);
```

### 修改器优先级

```csharp
// 优先级越小越先计算
Data.AddModifier(DataKey.Damage, new DataModifier(
    ModifierType.Additive,
    10f,
    priority: 0,  // 先计算
    id: "Buff_1"
));

Data.AddModifier(DataKey.Damage, new DataModifier(
    ModifierType.Multiplicative,
    1.5f,
    priority: 1,  // 后计算
    id: "Buff_2"
));

// 计算顺序：(基础值 + 10) × 1.5
```

### 资源自动映射

```csharp
// 从 Resource 自动映射数据
// 内部调用 LoadFromResource
Data.ApplyResource("PlayerData");

// 或者直接传入 Resource 对象
Data.LoadFromResource(resource);
```

### 装备系统集成示例

```csharp
// 假设 ItemEntity 有自己的 Data
var itemData = itemEntity.GetData();

// 将 Item 的数据作为修改器应用到 Player
// 自动将 Item 的数值属性（Damage, Speed 等）转换为 Player 的 Additive Modifiers
// 并将 sourceEntity 设置为 itemEntity，以便后续追踪
playerData.ApplyDataAsModifiers(itemData, itemEntity);

// 卸载装备时，通过 Source 批量移除
playerData.RemoveModifiersBySource(itemEntity);
```

### 对象池复用

```csharp
// 重置数据容器（用于对象池）
Data.Reset();

// 注意：不会清除事件监听器，需要手动管理
```

## 📝 扩展指南

### 添加新数据

#### 1. 在 DataKey 中定义常量

```csharp
public static class DataKey
{
    // 添加新数据键
    public const string ManaRegen = "ManaRegen";
}
```

#### 2. 在 DataRegistry 中注册元数据

```csharp
private static void RegisterBasicData()
{
    Register(new DataMeta
    {
        Key = DataKey.ManaRegen,
        DisplayName = "魔法恢复",
        Description = "每秒恢复的魔法值",
        Category = DataCategory.Resource,
        Type = typeof(float),
        DefaultValue = 0f,
        MinValue = 0,
        MaxValue = 1000,
        SupportModifiers = true  // 是否支持修改器
    });
}
```

#### 3. 使用新数据

```csharp
Data.Set(DataKey.ManaRegen, 5f);
float manaRegen = Data.Get<float>(DataKey.ManaRegen);
```

### 添加计算数据

#### 1. 在 DataKey 中定义常量

```csharp
public const string MaxMana = "MaxMana";
public const string EffectiveMana = "EffectiveMana";
```

#### 2. 在 DataRegistry 中注册计算规则

```csharp
private static void RegisterComputedData()
{
    RegisterComputed(new ComputedData
    {
        Key = DataKey.EffectiveMana,
        Dependencies = new[] { DataKey.MaxMana, DataKey.ManaRegen },
        Compute = (data) =>
        {
            float maxMana = data.Get<float>(DataKey.MaxMana);
            float manaRegen = data.Get<float>(DataKey.ManaRegen);
            return maxMana + manaRegen * 10f; // 示例公式
        }
    });
}
```

#### 3. 使用计算数据

```csharp
// 自动计算，无需手动维护
float effectiveMana = Data.Get<float>(DataKey.EffectiveMana);
```

## ⚠️ 注意事项

### 1. 修改器支持

只有在元数据中声明 `SupportModifiers = true` 的数据才支持修改器：

```csharp
// ✅ 支持修改器（数值类型）
Data.AddModifier(DataKey.Damage, modifier);

// ❌ 不支持修改器（字符串类型）
Data.AddModifier(DataKey.Name, modifier); // 会输出警告并忽略
```

### 2. 计算数据只读

计算数据是只读的，不能直接设置：

```csharp
// ❌ 错误：计算数据不能直接设置
Data.Set(DataKey.DPS, 100f);

// ✅ 正确：修改依赖的基础数据
Data.Set(DataKey.Damage, 20f);
float dps = Data.Get<float>(DataKey.DPS); // 自动重新计算
```

### 3. 事件监听清理

在 `_ExitTree` 中清理事件监听，避免内存泄漏，一般事件监听和注销都在 Component 设置：

```csharp
public override void _ExitTree()
{
    Data.OnValueChanged -= OnDataChanged;
    Data.Off(DataKey.MaxHp, OnHpChanged);
}
```

### 4. 类型转换

使用泛型方法时注意类型匹配：

```csharp
// ✅ 正确
float damage = Data.Get<float>(DataKey.Damage);
int level = Data.Get<int>(DataKey.Level);

// ⚠️ 类型不匹配时会尝试自动转换
int damageInt = Data.Get<int>(DataKey.Damage); // float -> int
```

### 5. 默认值处理

当数据不存在或为 null 时，返回默认值：

```csharp
// 数据不存在，返回默认值 0
float unknown = Data.Get<float>("UnknownKey", 0f);

// 数据为 null，返回默认值
Data.Set(DataKey.Damage, null);
float damage = Data.Get<float>(DataKey.Damage, 10f); // 返回 10
```

### 6. 默认值的智能处理

Data 系统会自动处理默认值，优先级如下：

```csharp
// 优先级 1: 用户传入的默认值（可选参数）
float damage = Data.Get<float>(DataKey.Damage, 999f);  // 如果不存在，返回 999f

// 优先级 2: DataRegistry 中注册的默认值
float damage = Data.Get<float>(DataKey.Damage);  // 如果不存在，返回 DataMeta 中定义的默认值

// 优先级 3: 类型推断的默认值
float unknownValue = Data.Get<float>("UnknownKey");  // 如果不存在且未注册，返回 0f (float 的默认值)
```

**最佳实践**：

```csharp
// ✅ 推荐：对于已注册的数据键，无需传入默认值参数
float armor = Data.Get<float>(DataKey.Armor);
float critChance = Data.Get<float>(DataKey.CritRate);

// ✅ 可选：对于未注册但需要特定默认值的情况，传入默认值参数
float customValue = Data.Get<float>("CustomKey", 100f);

// ❌ 不推荐：对已注册的数据键传入与 DataMeta 定义不同的默认值
float damage = Data.Get<float>(DataKey.Damage, 999f);  // 可能导致行为不一致
```

## 🎯 最佳实践

### 1. 使用常量而非字符串

```csharp
// ❌ 不推荐：字符串易错，无智能提示
float damage = Data.Get<float>("Damage");

// ✅ 推荐：使用常量，类型安全
float damage = Data.Get<float>(DataKey.Damage);
```

### 2. 利用元数据驱动 UI

```csharp
// 自动生成属性面板
var attackData = DataRegistry.GetMetaByCategory(DataCategory.Attack);
foreach (var meta in attackData)
{
    var value = Data.Get<float>(meta.Key);
    var label = new Label();
    label.Text = $"{meta.DisplayName}: {meta.FormatValue(value)}";
    AddChild(label);
}
```

### 3. 统一修改器命名

```csharp
// 推荐命名规范：类型_来源_效果
"Buff_Weapon_Damage"
"Debuff_Poison_Speed"
"Passive_Skill_CritChance"
```

### 4. 批量操作优化

```csharp
// ❌ 不推荐：多次触发事件
Data.Set(DataKey.MaxHp, 100f);
Data.Set(DataKey.Damage, 10f);
Data.Set(DataKey.Speed, 300f);

// ✅ 推荐：批量设置，减少事件触发
Data.SetMultiple(new Dictionary<string, object>
{
    { DataKey.MaxHp, 100f },
    { DataKey.Damage, 10f },
    { DataKey.Speed, 300f }
});
```

### 5. 性能优化

```csharp
// ✅ 缓存频繁访问的数据
private float _cachedDamage;

public override void _Ready()
{
    _cachedDamage = Data.Get<float>(DataKey.Damage);

    // 监听变更，更新缓存
    Data.On(DataKey.Damage, (old, @new) => {
        _cachedDamage = (@new as float?) ?? 0f;
    });
}

public override void _Process(double delta)
{
    // 使用缓存值，避免重复计算
    ApplyDamage(_cachedDamage);
}
```

## 📚 相关文档

- [设计文档](../../../../Docs/框架/ECS/System/Data/DataSystem_Design.md) - 详细的架构设计和决策记录
- [项目规则](../../../../.agent/rules/project_rules.md) - 项目级别的使用规范

## 🔄 版本历史

- **v2.3** (2025-01-08)
  - 改进 `Data.Get` 方法的默认值处理
  - 无需为已注册的数据键传入默认值参数
  - 自动使用 `DataRegistry` 中定义的默认值
  - 更新所有 Damage System Processor 代码以遵循新的最佳实践

- **v2.2** (2025-01-03) - `DataModifier` 新增 `Source` 属性，支持按来源追踪 - 新增 `RemoveModifiersBySource`，优化装备/Buff 移除逻辑 - 新增 `ApplyDataAsModifiers`，支持将 Data 转换为修改器 - 新增 `LoadFromResource`，统一 Resource 注入逻辑（原 `EntityManager.InjectResourceData`）

  - **v2.1** (2025-01-03)

  - 使用 `System.Type` 替代 `DataType` 枚举
  - 支持 `Data.Get(key)` 自动推断返回类型
  - 支持 `Options` 固定选项约束（int 存储，string 显示）
  - 优化 `DataMeta` 注册，支持智能默认值推断

- **v2.0** (2025-01-03)

- **v1.0** (2024-12-XX)

  - 初始版本
  - 基础键值存储
  - 事件监听

---

**维护者**: Trae AI
**最后更新**: 2025-01-08
