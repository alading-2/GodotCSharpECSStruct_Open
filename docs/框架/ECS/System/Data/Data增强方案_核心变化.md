# Data 增强方案 - 核心变化总结

## 🎯 核心决策

**AttributeComponent 将被废弃，所有功能集成到 Data 容器中。**

## 📊 架构对比

### 旧架构（分离）

```
Node
├── Data (通用数据)
│   ├── Name: "Player"
│   ├── Level: 1
│   └── BaseDamage: 10
└── AttributeComponent (属性管理)
    ├── Modifiers: [...]
    └── Get("Damage") → 计算最终值
```

**问题**：

- 数据访问路径分裂
- 开发者需要记住"哪些数据在 Data，哪些在 AttributeComponent"
- 职责重叠

### 新架构（统一）

```
Node
└── Data (统一数据容器)
    ├── 普通数据
    │   ├── Name: "Player"
    │   └── Level: 1
    ├── 属性数据（支持修改器）
    │   ├── Damage: 10 (基础值)
    │   └── Modifiers[Damage]: [+5, ×1.5]
    └── 计算属性（自动计算）
        └── DPS: 自动计算
```

**优势**：

- ✅ 统一访问：`data.Get()` 是唯一入口
- ✅ 零心智负担：不需要记住数据位置
- ✅ 元数据驱动：自动约束、验证、UI 生成

## 🔄 关键变化

### 1. 文件重命名与移动

| 旧文件                                    | 新文件                               | 说明             |
| ----------------------------------------- | ------------------------------------ | ---------------- |
| `Src/ECS/Component/AttributeComponent.cs` | **废弃**                             | 功能集成到 Data  |
| `Src/ECS/Component/AttributeModifier.cs`  | `Src/Tools/Data/Modifier.cs`         | 重命名并移动     |
| -                                         | `Src/Tools/Data/PropertyKey.cs`      | 新增：属性键常量 |
| -                                         | `Src/Tools/Data/PropertyMeta.cs`     | 新增：属性元数据 |
| -                                         | `Src/Tools/Data/PropertyRegistry.cs` | 新增：属性注册表 |
| -                                         | `Src/Tools/Data/ComputedProperty.cs` | 新增：计算属性   |

### 2. 类名变化

| 旧名称              | 新名称             | 说明         |
| ------------------- | ------------------ | ------------ |
| `AttrKey`           | `PropKey`          | 更通用的命名 |
| `AttributeMeta`     | `PropertyMeta`     | 更通用的命名 |
| `AttributeModifier` | `Modifier`         | 更简洁       |
| `AttributeRegistry` | `PropertyRegistry` | 更通用的命名 |
| `ComputedAttribute` | `ComputedProperty` | 更通用的命名 |
| `AttributeCategory` | `PropertyCategory` | 更通用的命名 |

### 3. API 变化

#### 旧 API（分离）

```csharp
// 获取数据
var data = node.GetData();
var attr = node.GetComponent<AttributeComponent>();

// 访问普通数据
string name = data.Get<string>("Name");

// 访问属性数据
float damage = attr.Get(AttrKey.Damage);

// 添加修改器
attr.AddModifier(new AttributeModifier(
    AttrKey.Damage,
    ModifierType.Additive,
    10
));
```

#### 新 API（统一）

```csharp
// 获取数据（唯一入口）
var data = node.GetData();

// 访问普通数据
string name = data.Get<string>(PropKey.Name);

// 访问属性数据（自动应用修改器）
float damage = data.Get<float>(PropKey.Damage);

// 添加修改器
data.AddModifier(PropKey.Damage, new Modifier(
    PropKey.Damage,
    ModifierType.Additive,
    10
));

// 计算属性（自动计算）
float dps = data.Get<float>(PropKey.DPS);
```

## 🛠️ Data 容器增强功能

### 新增方法

```csharp
public class Data
{
    // === 原有方法（保持不变） ===
    public T Get<T>(string key, T defaultValue = default!);
    public bool Set<T>(string key, T value);
    public void Add<T>(string key, T delta) where T : INumber<T>;
    public void Multiply<T>(string key, T factor) where T : INumber<T>;
    public event Action<string, object?, object?>? OnValueChanged;

    // === 新增方法（修改器支持） ===

    /// <summary>
    /// 添加修改器（Buff/Debuff/装备）
    /// </summary>
    public void AddModifier(string key, Modifier modifier);

    /// <summary>
    /// 移除修改器
    /// </summary>
    public void RemoveModifier(string key, string modifierId);

    /// <summary>
    /// 获取指定属性的所有修改器
    /// </summary>
    public IReadOnlyList<Modifier> GetModifiers(string key);

    /// <summary>
    /// 清除指定属性的所有修改器
    /// </summary>
    public void ClearModifiers(string key);
}
```

### 内部实现

```csharp
public class Data
{
    private readonly Dictionary<string, object> _data = new();
    private readonly Dictionary<string, List<Modifier>> _modifiers = new();  // 新增
    private readonly Dictionary<string, object> _cachedValues = new();       // 新增
    private readonly HashSet<string> _dirtyKeys = new();                     // 新增

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
}
```

## 📝 迁移步骤

### 第一阶段：创建新文件（1 天）

1. 创建 `Src/Tools/Data/PropertyKey.cs`
2. 创建 `Src/Tools/Data/PropertyMeta.cs`
3. 创建 `Src/Tools/Data/PropertyType.cs`
4. 创建 `Src/Tools/Data/PropertyCategory.cs`
5. 创建 `Src/Tools/Data/PropertyRegistry.cs`
6. 创建 `Src/Tools/Data/ComputedProperty.cs`
7. 重命名 `AttributeModifier.cs` → `Modifier.cs` 并移动到 `Src/Tools/Data/`

### 第二阶段：增强 Data 容器（1-2 天）

1. 在 `Data.cs` 中添加修改器支持
   - 添加 `_modifiers` 字典
   - 添加 `_cachedValues` 字典
   - 添加 `_dirtyKeys` 集合
2. 修改 `Get<T>()` 方法，支持：
   - 计算属性自动计算
   - 修改器自动应用
   - 元数据约束自动验证
3. 添加新方法：
   - `AddModifier()`
   - `RemoveModifier()`
   - `GetModifiers()`
   - `ClearModifiers()`

### 第三阶段：迁移现有代码（2-3 天）

1. 全局搜索 `AttributeComponent`，替换为 `Data`
2. 全局搜索 `AttrKey`，替换为 `PropKey`
3. 全局搜索 `AttributeModifier`，替换为 `Modifier`
4. 更新所有使用属性的组件：
   - `HealthComponent`
   - `VelocityComponent`
   - `DamageSystem`
5. 删除 `AttributeComponent.cs`

### 第四阶段：测试与验证（1 天）

1. 单元测试：修改器计算
2. 集成测试：战斗系统
3. 性能测试：1000 个实体
4. 功能测试：Buff/Debuff/装备

## ⚠️ 注意事项

### 1. 向后兼容

如果需要保留旧代码兼容性，可以创建一个 `AttributeComponent` 的空壳：

```csharp
/// <summary>
/// AttributeComponent - 已废弃，仅保留向后兼容
/// 所有功能已集成到 Data 容器中
/// </summary>
[Obsolete("请直接使用 node.GetData() 访问属性")]
public partial class AttributeComponent : Node
{
    private Data _data;

    public override void _Ready()
    {
        _data = GetParent()?.GetData();
    }

    [Obsolete("请使用 data.Get(PropKey.XXX)")]
    public float Get(string attrKey)
    {
        return _data?.Get<float>(attrKey) ?? 0f;
    }

    [Obsolete("请使用 data.AddModifier()")]
    public void AddModifier(Modifier modifier)
    {
        _data?.AddModifier(modifier.PropertyKey, modifier);
    }
}
```

### 2. 性能考虑

- **缓存机制**：修改器计算结果会被缓存，只在值变化时重新计算
- **脏标记**：使用 `_dirtyKeys` 追踪需要重新计算的属性
- **按需计算**：计算属性只在访问时才计算

### 3. 元数据驱动

所有属性的行为由 `PropertyRegistry` 定义：

- 哪些属性支持修改器？→ `SupportModifiers = true`
- 哪些属性有范围限制？→ `MinValue` / `MaxValue`
- 哪些属性是计算属性？→ 在 `_computedRegistry` 中注册

## 🎉 最终效果

### 使用示例

```csharp
public partial class Player : CharacterBody2D
{
    private Data _data;

    public override void _Ready()
    {
        _data = this.GetData();

        // ✅ 统一访问：所有数据都从 Data 获取
        string name = _data.Get<string>(PropKey.Name, "Player");
        int level = _data.Get<int>(PropKey.Level, 1);
        float damage = _data.Get<float>(PropKey.Damage, 10);
        float speed = _data.Get<float>(PropKey.Speed, 300);

        // ✅ 添加修改器
        _data.AddModifier(PropKey.Damage, new Modifier(
            PropKey.Damage,
            ModifierType.Additive,
            5,
            id: "Weapon_Sword"
        ));

        // ✅ 计算属性（自动计算）
        float dps = _data.Get<float>(PropKey.DPS);
        float attackInterval = _data.Get<float>(PropKey.AttackInterval);

        // ✅ 元数据查询
        var meta = PropertyRegistry.GetMeta(PropKey.Damage);
        GD.Print($"{meta.DisplayName}: {meta.FormatValue(damage)}");
    }
}
```

### 与其他组件集成

```csharp
public partial class HealthComponent : Node
{
    private Data _data;

    public float MaxHp => _data.Get<float>(PropKey.MaxHp, 100);
    public float CurrentHp { get; private set; }

    public override void _Ready()
    {
        _data = GetParent().GetData();
        _data.OnValueChanged += OnDataChanged;
        CurrentHp = MaxHp;
    }

    private void OnDataChanged(string key, object? oldVal, object? newVal)
    {
        if (key == PropKey.MaxHp)
        {
            // 最大生命值变化时，按比例调整当前生命值
            float oldMaxHp = (float)(oldVal ?? 100);
            float newMaxHp = (float)(newVal ?? 100);
            if (newMaxHp > oldMaxHp)
            {
                float ratio = CurrentHp / oldMaxHp;
                CurrentHp = newMaxHp * ratio;
            }
        }
    }
}
```

## 📚 相关文档

- [完整设计文档](./AttributeSystem_Design.md)
- [Data 容器文档](../../Tools/Data.md)
- [项目规则](../../../project_rules.md)

---

**文档版本**: v1.0  
**创建日期**: 2025-01-01  
**作者**: Kiro AI Assistant  
**状态**: ✅ 设计完成，待实施
