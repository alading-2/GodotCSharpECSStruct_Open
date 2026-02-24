---
name: ecs-component
description: 创建新 Component、实现 IComponent 接口、在组件中读写 Data、订阅 Entity.Events 事件时使用。适用于：新建功能组件（移动/攻击/血量/动画等），组件间通信，组件生命周期管理。触发关键词：新建组件、Component、IComponent、OnComponentRegistered、组件通信。
---

# ECS Component 规范

## 核心原则
- **单一职责**：一个 Component 只做一件事
- **无业务状态**：禁止私有业务状态字段，所有运行时状态存 `Data`
- **事件驱动**：组件间通信优先级 `Event > Data > GetComponent`
- **允许的私有字段**：仅限组件内部专用引用（`_sprite`、`_currentTarget`、`_availableAnims`）

## 标准结构

```csharp
public partial class MyComponent : Node, IComponent
{
    private IEntity? _entity;
    private Data? _data;

    // ✅ 允许：组件内部专用引用（非业务状态）
    private AnimatedSprite2D? _sprite;

    // ❌ 禁止：业务状态字段
    // private float _currentHp;  // 必须存 Data！

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;
        _data = iEntity.Data;

        // ✅ 在此订阅事件（不要在 _Ready 中订阅）
        _entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged, OnDataChanged);
    }

    public void OnComponentUnregistered()
    {
        // ✅ 无需手动解绑事件（EntityManager 自动 Events.Clear()）
        _data = null;
        _entity = null;
    }

    public override void _Process(double delta)
    {
        // ❌ 禁止在此 new 对象或使用 LINQ
    }
}
```

## Data 读写规范

```csharp
// ✅ 读取（使用 DataKey 常量，禁止字符串字面量）
var hp = _data.Get<float>(DataKey.CurrentHp);
var speed = _data.Get<float>(DataKey.MoveSpeed);

// ✅ 写入
_data.Set(DataKey.CurrentHp, hp - damage);
_data.Add(DataKey.Score, 10);  // 数值累加

// ❌ 禁止
// _data.Get<float>("CurrentHp")  // 字符串字面量
// private float _currentHp;      // 私有业务状态
```

## 事件订阅模式

```csharp
// 监听 Data 属性变化（响应 Spawn 后设置的初始数据）
private void OnDataChanged(GameEventType.Data.PropertyChangedEventData evt)
{
    if (evt.Key != DataKey.CurrentHp) return;
    // 响应 HP 变化
}

// 跨组件通信（通过事件，不直接调用其他组件方法）
private void OnHealRequest(GameEventType.Unit.HealRequestEventData evt)
{
    var healAmount = evt.Amount;
    // 处理治疗，通过事件返回结果
}
```

## 组件注册时机

```csharp
// ⚠️ 关键：许多数据（如 SkillLevel、Target）在 Spawn 之后才设置
// 必须监听 PropertyChanged 事件，不能假设数据在 OnComponentRegistered 时已存在
_entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
    GameEventType.Data.PropertyChanged, OnDataChanged);
```

## 禁止事项
- ❌ `private float _currentHp` 等业务状态字段 → 存 `Data`
- ❌ 在 `_Ready()` 中订阅 `Entity.Events`（应在 `OnComponentRegistered`）
- ❌ 直接调用其他 Component 的方法 → 用 `Entity.Events` 通信
- ❌ 使用 Godot Signal 处理核心逻辑 → 用 `EventBus`
- ❌ `_Process` 中 `new` 对象或 LINQ

## 关键文件路径
- **标准模板**（新建 Component 从这里复制）→ `Src/ECS/Component/TemplateComponent.cs`
- **接口定义** → `Src/ECS/Component/IComponent.cs`
- **开发规范** → `Src/ECS/Component/Component规范.md`
- **设计理念** → `Docs/框架/ECS/Component/Component数据驱动设计理念.md`
- **现有通用组件** → `Src/ECS/Component/Unit/Common/`（HealthComponent、AttackComponent 等）
- **技能组件** → `Src/ECS/Component/Ability/`（CooldownComponent、ChargeComponent 等）
