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
// ✅ 读取（使用 DataKey / DataMeta，禁止字符串字面量）
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

## EntityMovementComponent（策略调度器）

`EntityMovementComponent` 已重构为**策略模式调度器**，不再包含内联运动逻辑。

### 架构
- **调度器**：`EntityMovementComponent` 检测 `DataKey.MoveMode` 变化，自动切换 `IMovementStrategy`
- **策略接口**：`IMovementStrategy`（Update 纯计算只写 `DataKey.Velocity` / OnEnter / OnExit）；如视觉朝向不应直接取 `Velocity`，通过 `MovementUpdateResult.Continue(distance, facingDirection)` 显式返回朝向
- **注册表**：`MovementStrategyRegistry`（MoveMode → Strategy 的静态映射）
- **辅助方法**：`MovementHelper`（朝向旋转、到达距离） + `ScalarDriver`（策略内标量参数演化）
- **统一执行路径**：所有实体经 `VelocityResolver.Resolve()` 合成速度后应用位移，`CharacterBody2D` 额外调用 `MoveAndSlide()` 处理碰撞，其他用 `GlobalPosition +=`
- **帧率选择**：由策略 `UsePhysicsProcess` 声明走 `_Process` 或 `_PhysicsProcess`，与节点类型无关，两条路径逻辑完全相同
- **策略约束**：禁止直接操作 `GlobalPosition`，所有位移由调度器统一执行
- **曲线性能约束**：`ArcLengthLut` 只用于静态曲线的进入阶段预计算；动态追踪目标时禁止在 `Update` 中逐帧重建 LUT，改用 `Evaluate/EvaluateTangent` + 轻量长度估算

### 朝向语义
- `Velocity` = “本帧怎么移动”，服务于位移执行与速度分层合成
- `FacingDirection` = “本帧朝哪看”，服务于 `VisualRoot.FlipH` 或 `Node2D.RotationDegrees`
- 已接入显式朝向的曲线路径：`SineWaveStrategy`（正弦切线）、`BezierCurveStrategy`（贝塞尔切线）、`OrbitStrategy`（切向+径向合成切线）
- 直线/追踪/输入类策略若 `Velocity` 本身就是想看的方向，可继续只返回 `Continue(distance)`

### 14 种运动模式
FixedDirection / TargetPoint / TargetEntity / OrbitPoint / OrbitEntity / Spiral / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc

- 所有 12 种模式对 Node2D/Area2D 和 CharacterBody2D 通用
- `AIControlled` 读取 `AIMoveDirection/AIMoveSpeedMultiplier`，AI 行为树在非 `AIControlled` 模式下暂停写入移动意图

### 附着跟随模式（AttachToHost）
- `EffectComponent` 仅负责查找宿主与生命周期监听
- 位置跟随由 `AttachToHostStrategy` 执行：`GlobalPosition = Host + EffectOffset`
- 宿主无效时策略返回 `-1`，由调度器走统一完成流程

### 扩展新运动模式
1. `MovementEnums.cs` 添加 MoveMode 枚举值
2. 创建策略类实现 `IMovementStrategy`
3. 用 `[ModuleInitializer]` 自注册到 `MovementStrategyRegistry`
4. 如果新参数只是当前策略私有配置，优先加到 `MovementParams`；若属于“同策略内部连续变化的标量”，优先复用 `ScalarDriverParams`
5. 如果策略轨迹是曲线/环绕/波形，先判断视觉朝向是否应直接取 `Velocity`；若否，显式返回 `facingDirection`

### ⚠️ 策略实例语义
注册表保存的是工厂函数，不是单例对象。每次切换运动模式都会新建策略实例：

- **允许** 持有策略私有运行态（如 `_currentAngle`、`ScalarDriverState`、贝塞尔 LUT）
- **禁止** 把可跨系统观察的业务状态偷偷藏在策略里；这类状态仍应进入 `Data`
- `ScalarDriverState` 这类“仅服务当前策略公式”的局部运行态，应该由策略实例私有持有

### Velocity 分层合成
`VelocityResolver.Resolve(data)` 解决多组件写入冲突：
- `IsMovementLocked=true` → Zero
- `VelocityOverride != Zero` → Override
- 否则 → `Velocity + VelocityImpulse`

## 关键文件路径
- **标准模板**（新建 Component 从这里复制）→ `Src/ECS/Component/TemplateComponent.cs`
- **接口定义** → `Src/ECS/Component/IComponent.cs`
- **开发规范** → `Src/ECS/Component/Component规范.md`
- **设计理念** → `Docs/框架/ECS/Component/Component数据驱动设计理念.md`
- **现有通用组件** → `Src/ECS/Component/Unit/Common/`（HealthComponent、AttackComponent 等）
- **技能组件** → `Src/ECS/Component/Ability/`（CooldownComponent、ChargeComponent 等）
- **运动策略调度器** → `Src/ECS/Component/Movement/EntityMovementComponent.cs`
- **运动策略接口** → `Src/ECS/System/Movement/IMovementStrategy.cs`
- **运动策略实现** → `Src/ECS/System/Movement/Strategies/`
- **速度合成** → `Src/ECS/System/Movement/VelocityResolver.cs`
- **运动说明文档** → `Src/ECS/Component/Movement/EntityMovementComponent说明.md`
- **移动系统README** → `Src/ECS/System/Movement/README.md`
- **移动系统设计文档** → `Docs/框架/ECS/System/移动系统设计说明.md`
