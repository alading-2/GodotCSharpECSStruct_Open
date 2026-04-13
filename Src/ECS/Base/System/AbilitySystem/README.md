# AbilitySystem (技能系统)

## 概述

技能系统由两层组成：

1. **EntityManager_Ability**：技能生命周期管理（增删查 + 关系绑定）
2. **AbilitySystem**：施法流水线编排（检查 -> 目标 -> 消耗 -> 执行）

系统核心是事件驱动：

- 触发层发送 `TryTrigger`
- `AbilitySystem` 统一处理
- 组件通过 `EventContext` 协作检查和消耗
- 结果通过 `CastContext.ResponseContext` 回传 `TriggerResult`

---

## 目录结构

```text
Src/ECS/Base/System/AbilitySystem/
├── AbilitySystem.cs          # 施法流水线（统一入口）
├── EntityManager_Ability.cs  # 技能 CRUD + 事件接线
├── AbilityCheckPhase.cs      # CheckCanUse 检查优先级
├── TriggerResult.cs          # Success/Failed/WaitingForTarget
└── README.md                 # 本文档
```

---

## 核心流程（Trigger -> Cast -> Execute）

### 1) Trigger（触发）

触发源：

- `ActiveSkillInputComponent`（玩家手动）
- `TriggerComponent`（事件触发 / 周期触发）

统一方式：向技能实体发 `TryTrigger` 事件，并携带 `CastContext`。

```csharp
var context = new CastContext
{
    Ability = ability,
    Caster = caster,
    ResponseContext = new EventContext()
};

ability.Events.Emit(
    GameEventType.Ability.TryTrigger,
    new GameEventType.Ability.TryTriggerEventData(context)
);
```

### 2) Cast（施法）

`AbilitySystem.TryTriggerAbilityWithContext(context)` 内部流水线：

1. `CanUseAbility`（触发 `CheckCanUse`）
2. `SelectTargets`（触发 `AbilityTargetSelectionComponent`）
3. 目标解析分流：`Entity` / `Point` / `EntityOrPoint` / `None`
4. `ConsumeCharge`
5. `StartCooldown`（周期技能跳过）
6. `ConsumeCost`

其中 Point / EntityOrPoint 在无预选位置时会进入异步瞄准：

- `AbilitySystem` 发 `Targeting.StartTargeting`
- `TargetingManager` 接管输入与状态
- 确认后调用 `ResumeAfterTargeting(context)` 回到流水线

### 3) Execute（执行）

施法通过后：

- 发送 `Ability.Activated`（UI 等监听）
- 构建 `FeatureContext`，把 `CastContext` 放入 `ActivationData`
- 调用 `FeatureSystem.OnFeatureActivated(...)`
- 由对应 `IFeatureHandler.OnActivated(...)` 执行具体技能逻辑
- 发送 `Ability.Executed`

其中 `AbilityConfig.FeatureHandlerId` 必须直接填写完整唯一 `FeatureId`，例如 `Ability.Movement.Dash`；`FeatureGroup` 仅用于分组查询，不参与运行时拼接。

---

## 两条时间轴

Ability 子域里有两套不同的时间语义：

1. `TriggerComponent.Periodic`

- 作用：控制技能多久重新执行一次
- 配置：`AbilityTriggerMode = Periodic` + `AbilityCooldown`
- 结果：每次都会重新进入 `TryTrigger / ExecuteAbility`

2. `DamageApplyOptions.TickInterval / TotalDuration`

- 作用：控制单次技能执行内部是否继续跳 DoT
- 配置位置：`AbilityImpactTool -> DamageApplyOptions`
- 结果：不会重新执行技能流水线，只会让当前这次技能继续跳伤害

两者可以并存；并存时表示“每次周期触发都会再启动一条新的内部 DoT”。

---

## 返回值与请求-响应

`TryTrigger` 的结果不通过额外参数传递，而是放在 `CastContext.ResponseContext`：

```csharp
var result = context.ResponseContext?.HasResult == true
    ? (TriggerResult)context.ResponseContext.GetResult<TriggerResult>()
    : TriggerResult.Failed;
```

`AbilitySystem.HandleTryTrigger` 负责写入结果：

```csharp
context.ResponseContext?.SetResult(result);
```

---

## 关键 API

### EntityManager_Ability

| 方法 | 说明 |
| :--- | :--- |
| `AddAbility` | 添加技能、建立 Owner 关系、接线 `TryTrigger -> AbilitySystem.HandleTryTrigger` |
| `RemoveAbility` | 移除技能与关系 |
| `GetAbilities` | 获取单位所有技能 |
| `GetManualAbilities` | 获取可手动施放技能（输入与 UI 共用） |
| `GetAbilityByName` | 按名称查询技能 |

### AbilitySystem

| 方法 | 说明 |
| :--- | :--- |
| `HandleTryTrigger` | `TryTrigger` 事件入口；写入 `ResponseContext` |
| `ResumeAfterTargeting` | 异步瞄准确认后恢复流水线 |
| `CanUseAbility` | 仅检查可用性（不消耗） |

---

## 相关文档

- 架构总览：`Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- 瞄准子系统：`Src/ECS/Base/System/TargetingSystem/README.md`
- 事件总线：`Src/ECS/Base/Event/README_EventBus.md`
