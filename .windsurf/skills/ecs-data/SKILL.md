---
name: ecs-data
description: 在 Entity 或 Component 中读写 Data 数据容器、定义新 DataKey、监听数据变化事件时使用。适用于：存储运行时状态（HP/速度/状态机），跨组件共享数据，从 Resource 批量加载初始数据。触发关键词：Data容器、DataKey、读写状态、PropertyChanged、数据驱动。
---

# ECS Data 数据容器规范

## 核心原则
- **Data 是唯一数据源**：所有运行时业务状态必须存 Data，禁止 Component 私有业务字段
- **类型安全**：必须使用 `DataKey` 常量，禁止字符串字面量
- **自动管理**：对象池回收时 EntityManager 自动 `Clear`，无需手动重置

## 基础读写

```csharp
// ✅ 读取
var hp = _data.Get<float>(DataKey.CurrentHp);
var state = _data.Get<int>(DataKey.UnitState);
var name = _data.Get<string>(DataKey.Name);

// ✅ 写入
_data.Set(DataKey.CurrentHp, hp - damage);
_data.Set(DataKey.UnitState, (int)UnitState.Dead);

// ✅ 数值累加
_data.Add(DataKey.Score, 10);
_data.Add(DataKey.CurrentHp, -damage);  // 负数即为减少

// ✅ 带默认值读取（Key 不存在时返回默认值）
var maxHp = _data.Get<float>(DataKey.MaxHp);  // 未设置时返回 0f
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

DataKey 按模块分类存放在 `Data/DataKeyRegister/` 目录：

```
Data/DataKeyRegister/
├── Base/       → 通用键（Name、Id、Team 等）
├── Unit/       → 单位属性（HP、Speed、State 等）
├── Ability/    → 技能数据（Cooldown、Range、TriggerMode 等）
├── Attribute/  → 属性系统（Attack、Defense、CritRate 等）
└── AI/         → AI 数据（MoveDirection、Target 等）
```

新增 DataKey 示例：

```csharp
// 在对应分类文件中添加
public static class DataKey
{
    // 使用 [DataKey] 特性标记，支持 DataForge 编辑器
    [DataKey] public static readonly string MyNewKey = nameof(MyNewKey);
}
```

## 从 Resource 批量加载初始数据

```csharp
// Entity 初始化时，DataInitComponent 自动从 Config Resource 加载数据
// Config 中定义的字段会自动映射到对应 DataKey
// 无需手动逐一 Set，只需确保 Config 字段名与 DataKey 一致
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

## 关键文件路径
- **核心容器** → `Src/ECS/Data/Data.cs`
- **使用指南** → `Src/ECS/Data/README.md`
- **架构设计** → `Docs/框架/ECS/Data/DataSystem_Design.md`
- **DataKey 目录** → `Data/DataKeyRegister/`（Base / Unit / Ability / Attribute / AI 分类）
- **数据注册** → `Src/ECS/Data/DataRegistry.cs`
- **DataForge 编辑器** → `addons/DataForge/README.md`
