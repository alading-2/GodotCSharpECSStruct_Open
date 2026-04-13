---
name: ecs-data
description: 在 Entity 或 Component 中读写 Data 数据容器、定义新 DataKey、监听数据变化事件时使用。适用于：存储运行时状态（HP/速度/状态机），跨组件共享数据，从 Resource 批量加载初始数据。触发关键词：Data容器、DataMeta、DataKey、读写状态、PropertyChanged、数据驱动。
---

# ECS Data 数据容器规范

## 核心原则
- **Data 是唯一数据源**：所有运行时业务状态必须存 Data，禁止 Component 私有业务字段
- **类型安全**：DataKey 为 `static readonly DataMeta`，隐式转换为 string，编译期检查
- **元数据内嵌**：每个 DataKey 直接携带类型、默认值、约束等信息
- **自动管理**：对象池回收时 EntityManager 自动 `Clear`，无需手动重置
- **不限制语义统一**：数值型参数表示"不限制"时，统一使用 `-1`，不要使用 `0`

如果你修改的是 `Data/Config`、`Data/Data`、`Data/DataKey`、`Data/EventType` 这些**数据目录配置文件**，应优先参考 `data-authoring`，本 Skill 主要关注 `Src/ECS/Base/Data/` 运行时容器。

## 基础读写

```csharp
// ✅ 读取（DataMeta 隐式转换为 string）
var hp = _data.Get<float>(DataKey.CurrentHp);
var state = _data.Get<int>(DataKey.UnitState);
var name = _data.Get<string>(DataKey.Name);

// ✅ 写入
_data.Set(DataKey.CurrentHp, hp - damage);
_data.Set(DataKey.UnitState, (int)UnitState.Dead);

// ✅ 数值累加
_data.Add(DataKey.Score, 10);
_data.Add(DataKey.CurrentHp, -damage);  // 负数即为减少

// ✅ 带默认值读取（Key 不存在时返回 DataMeta.DefaultValue 或类型默认值）
var maxHp = _data.Get<float>(DataKey.MaxHp);
```

## 按 Category 批量重置为默认值

```csharp
// ✅ 重置单个 Category 下所有已存储的键为 DataMeta.DefaultValue
_data.ResetByCategory(DataCategory_Movement.Orbit);

// ✅ 一次重置多个 Category
_data.ResetByCategories(
    DataCategory_Movement.Target,
    DataCategory_Movement.Orbit,
    DataCategory_Movement.Bezier,
    DataCategory_Movement.Boomerang);

// 典型场景：运动完成 / 对象池复用时清理策略专用参数
// 底层使用 DataRegistry.GetCachedMetaByCategory 缓存查询，适合高频调用
```

## 监听数据变化（必须用 Entity.Events，禁止 Data.On）

```csharp
// ✅ 正确：通过 Entity.Events 监听
_entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
    GameEventType.Data.PropertyChanged,
    evt => {
        if (evt.Key == DataKey.CurrentHp) UpdateHealthBar();
        if (evt.Key == DataKey.UnitState) OnStateChanged();
    }
);

// ❌ 禁止：直接用 Data.On
// _data.On(DataKey.CurrentHp, callback);  // 禁止！
```

## 定义新 DataKey

DataKey 按模块分类存放在 `Data/DataKey/` 目录：

```
Data/DataKey/
├── Base/       → 通用键（Name、Id、Team 等）
├── Attribute/  → 属性系统（BaseAttack、FinalHp、CritRate 等）
├── Unit/       → 单位状态（CurrentHp、UnitState 等）
├── Ability/    → 技能数据（Cooldown、CastRange 等）
├── Movement/   → 运动参数（MoveMode、OrbitRadius 等）
├── AI/         → AI 数据（AIState、DetectionRange 等）
└── Effect/     → 特效数据（EffectScale、EffectPlayRate 等）
```

**新架构（2026-03）**：DataKey 是 `static readonly DataMeta`，不再是 `const string`。

```csharp
// ✅ 正确：定义普通属性键
public static partial class DataKey
{
    public static readonly DataMeta MyNewKey = DataRegistry.Register(
        new DataMeta {
            Key = nameof(MyNewKey),           // 必填：键名
            DisplayName = "我的新键",          // UI 显示名称
            Description = "用途说明",          // 描述
            Category = DataCategory_Unit.Basic, // 分类枚举
            Type = typeof(float),             // 必填：数据类型
            DefaultValue = 0f,                // 默认值
            MinValue = 0,                     // 可选：最小值
            MaxValue = 100,                   // 可选：最大值
            SupportModifiers = true           // 是否支持修改器
        });
}

// ✅ 正确：定义计算属性键
public static readonly DataMeta FinalAttack = DataRegistry.Register(
    new DataMeta {
        Key = nameof(FinalAttack),
        DisplayName = "攻击力",
        Category = DataCategory_Attribute.Computed,
        Type = typeof(float),
        SupportModifiers = false,
        Dependencies = [nameof(BaseAttack), nameof(AttackBonus)],
        Compute = (data) => {
            float baseAttack = data.Get<float>(nameof(BaseAttack));
            float bonus = data.Get<float>(nameof(AttackBonus));
            return MyMath.AttributeBonusCalculation(baseAttack, bonus);
        }
    });

// ✅ 正确：Node2D 引用等非注册类型仍使用 const string
public const string TargetNode = "TargetNode";
```

**DataMeta 核心字段**：

| 字段 | 用途 | 必填 |
|------|------|------|
| `Key` | 数据键名 | ✅ |
| `Type` | 数据类型（`typeof(float)` 等） | ✅ |
| `DisplayName` | UI 显示名称 | ❌ |
| `Description` | 描述信息 | ❌ |
| `Category` | 分类枚举 | ❌ |
| `DefaultValue` | 默认值 | ❌（自动推断） |
| `MinValue` / `MaxValue` | 数值约束 | ❌ |
| `SupportModifiers` | 是否支持修改器 | ❌ |
| `Dependencies` | 计算数据依赖 | ❌ |
| `Compute` | 计算函数 | ❌ |

## 从 Resource 批量加载初始数据

```csharp
// Entity 初始化时，DataInitComponent 自动从 Config Resource 加载数据
// Config 中定义的字段会自动映射到对应 DataKey
// 推荐显式写 [DataKey(nameof(DataKey.Xxx))]，不要只依赖字段名回退
```

## 私有字段缓存规则

```csharp
// ✅ 允许：组件内部专用引用（不是业务状态）
private AnimatedSprite2D? _sprite;        // 节点引用缓存
private List<string> _availableAnims = new();  // 组件内部计算缓存
private IEntity? _currentTarget;          // 临时目标引用

// ❌ 禁止：业务状态（必须存 Data）
private float _currentHp;    // 禁止！→ DataKey.CurrentHp
private float _moveSpeed;    // 禁止！→ DataKey.MoveSpeed
private int _unitState;      // 禁止！→ DataKey.UnitState
```

## 禁止事项
- ❌ `_data.Get<float>("CurrentHp")` 字符串字面量 → 用 `DataKey.CurrentHp`
- ❌ `Data.On(key, callback)` 监听数据变化 → 用 `Entity.Events`
- ❌ Component 私有业务状态字段 → 存 Data
- ❌ 对象池回收后手动 Clear Data → EntityManager 自动处理
- ❌ 新增 `const string` DataKey → 用 `static readonly DataMeta`

## 关键文件路径
- **核心容器** → `Src/ECS/Base/Data/Data.cs`
- **元数据类** → `Src/ECS/Base/Data/DataMeta.cs`
- **使用指南** → `Src/ECS/Base/Data/README.md`
- **DataKey 目录** → `Data/DataKey/`（按模块分类）
- **数据注册** → `Src/ECS/Base/Data/DataRegistry.cs`
