# Data 系统优化分析

> 状态：部分已实施（P4 DataMeta 合并已完成）  
> 前置：配合《DataKey 静态数据类重构方案》一起阅读

---

## 1. 问题全景

通读 `Data.cs` / `DataMeta.cs` / `DataRegistry.cs`，可以识别出以下五类问题：

| 问题 | 位置 | 严重程度 |
|------|------|----------|
| ~~DataMeta 职责过重，运行时携带大量编辑器字段~~ | ~~DataMeta.cs~~ | ~~架构~~ **已解决** |
| 所有 DataKey 统一对待，简单状态键也走完整约束流程 | Data.Get/Set | 架构 |
| 每次 Get/Set 都查 DataRegistry（全局字典查找） | Data.Get/Set | 性能 |
| 修改器计算每次都 LINQ 排序 + 3次迭代 | CalculateFinalValue | 性能 |
| LoadFromResource 每次全量反射，无缓存 | Data.LoadFromResource | 性能 |

---

## 2. 根本矛盾：数据分层缺失

### 2.1 数据的实际需求层次

游戏中 DataKey 天然分为四类，对系统的需求完全不同：

```
Layer 1 · 状态键（State）
    IsDead / LifecycleState / Name / Id / Team / IsInvulnerable
    → 只需读写，无约束，无修改器
    → 不需要注册，更不需要 DataMeta

Layer 2 · 受限键（Constrained）
    CurrentHp / CurrentMana / Level / AbilityCurrentCharges
    → 需要 DefaultValue / MinValue / MaxValue 约束
    → 注册 DataMeta，但不需要修改器管道

Layer 3 · 属性键（Attribute）
    BaseHp / BaseAttack / MoveSpeed / CritDamage
    → 需要修改器系统（装备/Buff 加成）
    → 注册 DataMeta + SupportModifiers = true

Layer 4 · 计算键（Computed）
    FinalHp / FinalAttack / HpPercent / DPS
    → 派生值，不存储，依赖其他键计算
    → 注册 DataMeta + Compute lambda
```

### 2.2 当前系统的处理方式

**每个 `data.Get<T>(key)` 都执行以下全流程：**

```
① DataRegistry.GetMeta(key)           ← 全局字典查找，ALL 键都执行
② 推断默认值                          ← 即使是 IsDead 这类布尔键
③ 检查 meta.IsComputed                ← 属性访问
④ TryGetValue(_data, key)             ← 字典查找
⑤ DataRegistry.SupportModifiers(key)  ← 又一次全局字典查找
⑥ 可能进入 GetModifiedValueBoxed       ← 含 LINQ 排序
```

**Layer 1 的 `IsDead`（布尔，无注册）走完整6步，实际只需步骤 ④。**

---

## 3. 具体问题分析

### 3.1 DataMeta 职责过重 ✅ 已解决

> **已实施**：`DataMetaDisplay` 已合并入 `DataMeta`，`DataKey_*.cs` 主域键已改为 `static readonly DataMeta` 并在静态初始化阶段完成注册；Config 默认值统一改为 `DataKey.*.DefaultValue` 直读。

**重构后 DataMeta 字段布局：**

```
运行时约束字段            展示字段（UI/编辑器）
─────────────────        ──────────────────────────
Key                      DisplayName
Type                     Description
DefaultValue             Category
MinValue                 IconPath
MaxValue                 IsPercentage
SupportModifiers
Compute
Dependencies
Options
```

**架构决策**：放弃"按需加载 DataMetaDisplay"的设想，原因是所有注册点都在启动时一次性初始化，展示字段不存在运行时延迟加载的实际收益。合并后注册代码更简洁，无需同时构造两个对象。

### 3.2 LINQ 在修改器计算路径

`CalculateFinalValue` 每次调用（即每次 `Get<float>(属性键)`）都执行：

```csharp
var sorted = modifiers.OrderBy(m => m.Priority).ToList();   // ← 分配新 List
float additiveSum = sorted.Where(...).Sum(...);              // ← 再迭代
float multiplicativeProduct = sorted.Where(...).Aggregate(...); // ← 再迭代
```

**三次迭代 + 至少一次堆分配**。如果 UI 每帧读取 HP/Attack 用于显示，这会产生持续 GC 压力。

> 项目规则已经明确 `_Process` 禁止 LINQ，但 Get 本身可能被 `_Process` 调用。

### 3.3 LoadFromResource 反射未缓存

```csharp
var properties = resourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
```

每次 Entity spawn 都全量反射。`EnemyConfig` 可能在波次刷新时被频繁调用。  
反射结果是确定性的（同一个 Type 结果永远相同），完全可以静态缓存。

### 3.4 ModifierType 表达能力不足

当前只有 `Additive` 和 `Multiplicative`，公式为：

```
最终值 = (基础值 + Σ加法) × Π乘法
```

常见游戏需求无法表达：

| 需求 | 场景 | 当前能做？ |
|------|------|-----------|
| 覆盖值 | 特定 Buff 强制将某属性设为固定值 | ❌ |
| 最终加法 | 在乘法之后再追加固定值（暗黑2风格） | ❌ |
| 软上限 | 超过 X 后每点衰减（天际线/PoE 机制） | ❌ |
| 加法封顶 | 同种 Buff 效果不叠加，只取最高值 | ❌ |

---

## 4. 优化方案

### 4.1 核心原则

> **"注册 = 约束，不注册 = 自由读写"**（用户原话）  
> 未注册的 DataKey 应该走零开销的裸字典路径。

### 4.2 Get/Set 快速路径

**改动位置：`Data.Get<T>()` 和 `Data.Set<T>()`**

```csharp
public T Get<T>(string key, T fallback = default!)
{
    // ── 快速路径：未注册键，直接查字典 ──────────────────────
    var meta = DataRegistry.GetMeta(key);
    if (meta == null)
    {
        return _data.TryGetValue(key, out var raw)
            ? (T)raw
            : fallback;
    }

    // ── 计算键：最高优先级 ────────────────────────────────
    if (meta.IsComputed) return (T)GetComputed(key, meta);

    // ── 普通键：查基础值 ─────────────────────────────────
    if (!_data.TryGetValue(key, out var baseValue))
        return (T)meta.GetDefaultValue();

    // ── 属性键：应用修改器 ───────────────────────────────
    if (meta.SupportModifiers == true && _modifiers.ContainsKey(key))
        return (T)GetModified(key, baseValue, meta);

    return (T)ConvertValue(baseValue, typeof(T));
}
```

**关键变化：** 未注册键完全绕过 `DataRegistry`，不产生任何约束开销。

### 4.3 DataMeta 合并 ✅ 已实施

**实际决策：展示字段直接合并入 DataMeta，不再独立 DataMetaDisplay。**

```
DataMeta（当前，运行时约束 + 展示字段合一）
├── Key            string          ← 必填
├── Type           Type            ← 必填
├── DefaultValue   object?
├── MinValue       float?
├── MaxValue       float?
├── SupportModifiers bool?
├── Compute        Func<Data, object>?
├── Dependencies   string[]?
├── Options        List<string>?
├── DisplayName    string          ← 展示
├── Description    string          ← 展示
├── Category       Enum?           ← 展示
├── IconPath       string          ← 展示
└── IsPercentage   bool            ← 展示
```

**DataKey 文件变化：**

- `DataKey_*.cs` 主域键统一为 `static readonly DataMeta`，键名与字段名一致
- 注册在 `DataKey` 静态字段初始化时完成，`DataRegister_*` 逐步收敛为补充注册/迁移兼容职责
- `DataMetaDisplay.cs` 已清空（保留空文件避免 .uid 孤立）
- `DataRegistry` 以单字典维护元数据，继续为约束/计算/修改器提供查询

> **取消按需加载的原因**：所有注册点在程序启动时一次性执行，展示字段根本不存在"延迟加载"的时机。合并后代码量减少约 40%，注册一条记录从两个 `new` 对象变为一个。

### 4.4 修改器列表有序化

**改动位置：`AddModifier`**

```csharp
public void AddModifier(string key, DataModifier modifier)
{
    if (!_modifiers.TryGetValue(key, out var list))
        _modifiers[key] = list = new List<DataModifier>();

    // 插入时维护 Priority 有序（替代每次 Get 时 OrderBy）
    int insertIndex = list.BinarySearch(modifier, ModifierPriorityComparer.Instance);
    if (insertIndex < 0) insertIndex = ~insertIndex;
    list.Insert(insertIndex, modifier);

    MarkDirty(key);
}
```

`CalculateFinalValue` 直接遍历已有序列表，**消除 OrderBy 和 LINQ 分配**：

```csharp
private float CalculateFinalValue(string key, float baseValue)
{
    if (!_modifiers.TryGetValue(key, out var list) || list.Count == 0)
        return baseValue;

    float additive = 0f;
    float multiplicative = 1f;

    foreach (var m in list)  // list 已有序，直接遍历
    {
        if (m.Type == ModifierType.Additive)         additive      += m.Value;
        else if (m.Type == ModifierType.Multiplicative) multiplicative *= m.Value;
    }

    float result = (baseValue + additive) * multiplicative;
    var meta = DataRegistry.GetMeta(key);
    return meta != null ? (float)meta.Clamp(result) : result;
}
```

### 4.5 LoadFromResource 反射缓存

```csharp
// 静态缓存：Type → 需要加载的属性列表
private static readonly Dictionary<Type, (PropertyInfo prop, string key)[]>
    _resourcePropCache = new();

public void LoadFromResource(Resource resource)
{
    var type = resource.GetType();
    if (!_resourcePropCache.TryGetValue(type, out var cached))
        cached = _resourcePropCache[type] = BuildPropertyCache(type);

    foreach (var (prop, key) in cached)
    {
        var value = prop.GetValue(resource);
        if (value != null) Set(key, value);
    }
}

private static (PropertyInfo, string)[] BuildPropertyCache(Type type)
{
    // 反射只在第一次发生，结果永久缓存
    return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead
            && p.DeclaringType != typeof(Resource)
            && p.DeclaringType != typeof(Godot.GodotObject))
        .Select(p =>
        {
            var attr = p.GetCustomAttribute<DataKeyAttribute>();
            return (p, attr?.Key ?? p.Name);
        })
        .ToArray();
}
```

### 4.6 扩展 ModifierType（可选，未来）

```csharp
public enum ModifierType
{
    Additive,           // 基础加法：最终值 = (base + Σ) × Π
    Multiplicative,     // 乘法：同上
    FinalAdditive,      // 最终加法：先算乘法，再在结果上加（不乘）
    Override,           // 覆盖：无视其他修改器，强制设为此值（取最高优先级）
    Cap,                // 上限修改器：限制最终值不超过此值
}

// 新公式：
// step1 = (base + Σ[Additive]) × Π[Multiplicative]
// step2 = step1 + Σ[FinalAdditive]
// step3 = Override 存在时取最高优先级 Override 值
// step4 = Cap 存在时取 min(step, cap)
```

---

## 5. 结合 DataKey 静态数据类方案

两个文档合并后，完整的新架构为：

```
DataKey（静态类）
  ├── IsDead        ← Register(DataMeta)
  ├── CurrentHp     ← Register(DataMeta: DefaultValue/Min/Max)
  ├── BaseHp        ← Register(DataMeta + SupportModifiers=true)
  └── FinalHp       ← Register(DataMeta + Compute lambda)

DataMeta（运行时约束 + 展示字段合一）
  ← 保留 DisplayName/Category/IconPath/Options 等字段，避免双结构维护

DataRegistry
  ← 收集所有 DataMeta 静态字段，提供键查询
  ← 不再是必须依赖，未注册键直接通过

Data.Get<T>()
  ← 未注册键：直接字典读取，无任何框架开销
  ← 注册键：按需约束/修改器/计算
```

---

## 6. 改动规模与建议顺序

| 优先级 | 改动 | 影响范围 | 工作量 |
|--------|------|----------|--------|
| P1 | Get/Set 增加快速路径（未注册跳过） | Data.cs 约20行 | 小 |
| P2 | LoadFromResource 反射缓存 | Data.cs 约30行 | 小 |
| P3 | CalculateFinalValue 去 LINQ | Data.cs + AddModifier 约40行 | 小 |
| ~~P4~~ | ~~DataMeta 瘦身（拆出 DisplayInfo）~~ ✅ **已完成（合并而非拆分）** | `DataMeta.cs` / `DataMetaDisplay.cs` / 所有 `DataKey_*.cs` | - |
| P5 | ModifierType 扩展 | DataModifier.cs + CalculateFinalValue | 中 |

> **建议**：P1+P2+P3 可以立即做，无 Breaking Change，收益明显。  
> P4 配合 DataKey 静态数据类重构一起做，一次性解决"注册门槛高"和"运行时臃肿"两个问题。  
> P5 等有实际需求再做。

---

## 7. 不建议做的事

| 想法 | 原因 |
|------|------|
| 把 `_data` 改成分型存储（`_floats`/`_ints`/`_bools`） | API 完全破坏，收益不对等 |
| 完全取消 DataRegistry | 修改器、计算键仍然需要中心化元数据 |
| 改成 ECS 原生组件（每个属性一个 Component） | Godot ECS 接入成本高，现有架构不匹配 |
| 事件系统拆出 Data | `NotifyChanged` 发事件是合理耦合，不需要动 |
