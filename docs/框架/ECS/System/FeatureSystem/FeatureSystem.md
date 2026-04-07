# FeatureSystem 完整说明

**类型**：系统总览 + 开发手册  
**最后更新**：2026-04-06

---

## 1. 一句话定义

`FeatureSystem` 是建立在 `Data + Event + Entity/Component` 之上的**通用能力层**，统一表达"能力被授予时做什么、何时触发、触发后执行什么、移除时如何回滚"。

```
基础设施层：Data / EventBus / Entity / Component / EntityManager
通用能力层：FeatureSystem（本系统）
主动施法子域：AbilitySystem（FeatureSystem 的激活阶段特化流水线）
```

---

## 2. 核心对象分层

> **设计原则**：FeatureSystem 核心只依赖 `IEntity`，零引用任何子系统专有类型（AbilityEntity、CastContext、AbilitySystem 等）。调用方（如 AbilitySystem）在调用 FeatureSystem 前自行构建 `FeatureContext`，并将专有数据存入 `ActivationData`。

```
FeatureDefinition          配置模板（Godot Resource，编辑器填表）
  ├── 基础信息（Name / Description / Category / FeatureHandlerId / FeatureType）
  ├── 触发配置（FeatureTriggerMode / FeatureTriggerChance / FeatureCooldown）
  └── Modifiers[]（FeatureModifierEntry 列表，授予即生效）

FeatureInstance            运行时实例（Owner + FeatureEntity + 状态快捷访问）
  ├── Owner                宿主实体（IEntity）
  ├── FeatureEntity        Feature 实体（IEntity，承载 Data/Events）
  ├── RuntimeId            本次授予唯一 ID
  ├── GrantedTime          授予时间（秒）
  ├── IsEnabled            当前是否启用
  ├── IsActive             当前是否正在执行
  └── ActivationCount      累计激活次数

FeatureContext             生命周期回调上下文（Granted/Removed/Activated/Ended）
  ├── Owner                IEntity（宿主）
  ├── Feature              IEntity（Feature 实体）
  ├── Instance             FeatureInstance
  ├── ActivationData       object?（调用方放入专有数据，如 CastContext）
  ├── SourceEventData      触发源事件数据（OnEvent 触发时）
  ├── TriggerEventData     SourceEventData 的动作侧语义别名
  └── ExtraData            Action 间共享的临时数据

FeatureId                  Feature 处理器 ID 常量注册表（static partial class）
                           各模块用 partial 文件扩展，避免字符串散落

IFeatureAction             动作接口（最小执行单元）
IFeatureHandler            代码钩子接口（复杂逻辑的生命周期方法）

 AbilitySystem Adapter      Ability 子域适配层
   ├── 负责把 AbilityConfig.FeatureHandlerId 作为完整唯一 FeatureId 透传给 IFeatureHandler
   ├── 负责把 CastContext 装进 FeatureContext.ActivationData
   └── 负责读取 FeatureContext.ExtraData 中的 AbilityExecutedResult 并发出 Ability.Executed
```

---

## 3. 生命周期

```
Granted → Enabled → Activated/Ended（可重复）→ Disabled → Removed
```

| 阶段 | 含义 | 对应 API |
|:---|:---|:---|
| **Granted** | 宿主正式获得 Feature（一次性） | `EntityManager.AddAbility` → `FeatureSystem.OnFeatureGranted` |
| **Enabled** | Feature 开始参与响应 | `FeatureSystem.EnableFeature` |
| **Activated** | 一次激活执行开始 | `AbilitySystem` → `FeatureSystem.OnFeatureActivated` |
| **Ended** | 一次激活执行结束 | `AbilitySystem` → `FeatureSystem.OnFeatureEnded` |
| **Disabled** | 暂停响应（仍在宿主上） | `FeatureSystem.DisableFeature` |
| **Removed** | 彻底卸载，修改器回滚（一次性） | `EntityManager.RemoveAbility` → `FeatureSystem.OnFeatureRemoved` |

---

## 4. 触发模式

| TriggerMode | 说明 | Feature 行为 |
|:---|:---|:---|
| `Permanent` | 授予即生效 | Granted 时自动施加 Modifier，无需主动触发 |
| `OnEvent` | 监听特定事件触发 | TriggerComponent 订阅事件，概率检查后触发 |
| `Periodic` | 周期计时触发 | TriggerComponent 定时器，间隔 = FeatureCooldown |
| `Manual` | 手动输入触发 | 由玩家输入或代码调用 TryTrigger |

---

## 5. 如何使用

### 5.1 纯数据配置（零代码，适合属性加成）

1. 在 Godot 编辑器 → 新建资源 → 选 `FeatureDefinition`
2. 填写 `Name`、`Category`
3. `TriggerMode` 设为 `Permanent`
4. 在 `Modifiers` 数组中添加 `FeatureModifierEntry`：
   - `DataKeyName`：如 `"AttackDamage"`
   - `ModifierType`：`Additive`
   - `Value`：`10`
5. 授予：`EntityManager.AddAbility(player, featureDefinition)`
6. 移除：`EntityManager.RemoveAbility(player, "MyFeature")`

 > FeatureSystem 在 Granted/Removed 时自动应用/回滚所有 Modifiers。

### 5.1.1 `Data/Data/Feature` 怎么用

`Data/Data/Feature` 是 **通用 Feature 配置层**，不是“只给技能用的目录”。当前主要包含两类资源：

1. `FeatureDefinition`
   - 定义一个可被授予到实体上的 Feature 模板
   - 负责提供 `Name`、`FeatureHandlerId`、`Description`、`Category`、`EntityType`、`Enabled`、`Modifiers`
   - 适合描述被动词条、装备加成、常驻光环、状态效果模板等

2. `FeatureModifierEntry`
   - 定义一条数据驱动 Modifier
   - 在 `FeatureSystem.OnFeatureGranted` 时转成 `DataModifier` 写入 `owner.Data`
   - 在 `FeatureSystem.OnFeatureRemoved` 时通过 `RemoveModifiersBySource(feature)` 整体回滚

当前项目里，`EntityManager.AddAbility(owner, featureDefinition)` 会复用 `AbilityEntity` 作为 Feature 的运行时承载实体。
也就是说：

- `Data/Data/Feature` 负责定义“一个 Feature 长什么样”
- `FeatureSystem` 负责管理“这个 Feature 被授予、激活、移除时做什么”
- `AbilitySystem` 只是其中“主动施法型 Feature”的专用激活编排器

### 5.2 代码处理器（IFeatureHandler，适合复杂逻辑）

```csharp
// 注册处理器（[ModuleInitializer] 或 Autoload 中调用）
FeatureHandlerRegistry.Register(new MyFeatureHandler());

public class MyFeatureHandler : IFeatureHandler
{
    public string FeatureId => "MyFeature";  // 与 FeatureDefinition.Name 对应

    public void OnGranted(FeatureContext ctx)
    {
        // 授予时订阅事件、初始化状态等
        ctx.Owner?.Events.On("player:hit", HandleHit, ctx.Feature);
    }

    public void OnRemoved(FeatureContext ctx)
    {
        // 移除时解除订阅
        ctx.Owner?.Events.Off("player:hit", HandleHit);
    }

    public void OnActivated(FeatureContext ctx) { /* 每次激活时 */ }
    public void OnEnded(FeatureContext ctx)     { /* 每次激活结束 */ }

    private void HandleHit(object data) { /* 受击响应逻辑 */ }
}
```

### 5.3 使用 IFeatureAction 组合动作

```csharp
 public void OnGranted(FeatureContext ctx)
 {
     var actions = new List<IFeatureAction>
     {
        new ApplyModifierAction { DataKeyName = "AttackDamage", Type = ModifierType.Additive, Value = 20f },
        new EmitEventAction    { EventKey = "feature:stat_changed", EmitOnOwner = true }
    };
    FeatureSystem.ExecuteActions(actions, ctx);
}

public void OnRemoved(FeatureContext ctx)
{
    FeatureSystem.ExecuteActions(
        new[] { new RemoveModifierAction() },
        ctx
     );
 }
 ```

### 5.4 FeatureSystem 如何应用到 AbilitySystem

当前 Ability 执行链已经改为：

```text
TryTrigger
→ AbilitySystem 完成 CanUse / SelectTargets / ConsumeCharge / StartCooldown / ConsumeCost
→ AbilitySystem 构建 FeatureContext（ActivationData = CastContext）
→ FeatureSystem.OnFeatureActivated(featureCtx)
→ FeatureHandlerRegistry.Get(完整 FeatureHandlerId)?.OnActivated(featureCtx)
→ handler 将 AbilityExecutedResult 写入 featureCtx.ExtraData
→ AbilitySystem 读取结果并发出 Ability.Executed
→ FeatureSystem.OnFeatureEnded(featureCtx)
```

这意味着：

- `AbilitySystem` 仍然负责施法编排、目标选择、消耗、冷却
- `FeatureSystem` 负责统一生命周期钩子
- 具体技能效果逻辑不再经过 `AbilityExecutorRegistry`
- 每个技能逻辑类现在应实现 `IFeatureHandler`（或继承 `AbilityFeatureHandlerBase`）
- `AbilityConfig.FeatureHandlerId` 必须直接填写完整唯一 `FeatureId`，例如 `Ability.Movement.Dash`
- `FeatureGroup` 仅用于 `FeatureHandlerRegistry.GetByGroup()` 分组查询，不参与运行时反向拼接

---

## 6. 架构流程图

### 6.1 Granted 流程

```
EntityManager.AddAbility(owner, featureDefinition)
  └─ Spawn AbilityEntity + LoadFromResource(featureDefinition)
  └─ owner.Events.Subscribe(TryTrigger, AbilitySystem.HandleTryTrigger)
  └─ owner.Events.Emit(Ability.Added)
  └─ FeatureSystem.OnFeatureGranted(feature, owner)
       ├─ new FeatureInstance(owner, feature, time)
       ├─ ApplyModifiers → owner.Data.AddModifier × N
       ├─ FeatureHandlerRegistry.Get(featureId)?.OnGranted(ctx)
       └─ owner.Events.Emit(Feature.Granted)
```

### 6.2 Activated/Ended 流程

```
 TriggerComponent → owner.Events.Emit(Ability.TryTrigger)
 AbilitySystem.HandleTryTrigger
  └─ CheckCanUse / SelectTargets / Consume / StartCooldown
  └─ ability.Events.Emit(Ability.Activated)
  └─ 构建 FeatureContext（ActivationData = CastContext）
  └─ FeatureSystem.OnFeatureActivated(featureCtx)
       ├─ IFeatureHandler.OnActivated(ctx)
       └─ ability.Events.Emit(Feature.Activated)
  └─ AbilitySystem 从 featureCtx.ExtraData 读取 AbilityExecutedResult
  └─ ability.Events.Emit(Ability.Executed)
  └─ FeatureSystem.OnFeatureEnded(featureCtx)
       ├─ IFeatureHandler.OnEnded(ctx)
       └─ ability.Events.Emit(Feature.Ended)
 ```

### 6.3 Removed 流程

```
EntityManager.RemoveAbility(owner, name)
  └─ FeatureSystem.OnFeatureRemoved(feature, owner)   ← Destroy 之前
       ├─ IFeatureHandler.OnRemoved(ctx)
       ├─ owner.Data.RemoveModifiersBySource(feature)
       └─ owner.Events.Emit(Feature.Removed)
  └─ EntityManager.Destroy(feature)
  └─ owner.Events.Emit(Ability.Removed)
```

---

## 7. 代码文件索引

### 配置层（Data/）

| 文件 | 类型 | 说明 |
|:---|:---|:---|
| `Data/Data/Feature/FeatureDefinition.cs` | `[GlobalClass] Resource` | Feature 配置资源基类，编辑器填表 |
| `Data/Data/Feature/FeatureModifierEntry.cs` | `[GlobalClass] Resource` | 单条修改器配置条目 |
| `Data/DataKey/Feature/DataKey_Feature.cs` | `partial DataKey` | FeatureCategory / FeatureHandlerId / FeatureType / FeatureTriggerMode / FeatureEnabled / FeatureIsActive / FeatureActivationCount |
| `Data/DataKey/Feature/FeatureEnums.cs` | `enum` | FeatureType / FeatureTriggerMode 通用枚举 |
| `Data/DataKey/Feature/DataCategory_Feature.cs` | `enum` | Feature 数据分类（Basic / Trigger / Modifier / State）|
| `Data/EventType/Feature/GameEventType_Feature.cs` | `partial GameEventType` | 生命周期事件（Granted / Enabled / Disabled / Activated / Ended / Removed）|

### 系统层（Src/ECS/System/FeatureSystem/）

| 文件 | 类型 | 说明 |
|:---|:---|:---|
| `FeatureSystem.cs` | `static class` | **核心入口**：生命周期钩子 + Modifier 管理 + Action 执行 |
| `FeatureInstance.cs` | `class` | 运行时实例（轻量 Owner+Feature 包装） |
| `FeatureContext.cs` | `class` | 统一上下文：生命周期回调 + Action 执行（含 TriggerEventData / ExtraData） |
| `IFeatureHandler.cs` | `interface` | 代码驱动钩子（OnGranted/OnRemoved/OnActivated/OnEnded） |
| `FeatureHandlerRegistry.cs` | `static class` | IFeatureHandler 注册表 |
| `IFeatureAction.cs` | `interface` | 动作最小单元（Execute） |
| `Action/ApplyModifierAction.cs` | `class : IFeatureAction` | 施加修改器 |
| `Action/RemoveModifierAction.cs` | `class : IFeatureAction` | 回滚修改器（按 Source） |
| `Action/EmitEventAction.cs` | `class : IFeatureAction` | 在 Owner/Feature 上发出事件 |

### 接入点（修改的现有文件）

| 文件 | 修改内容 |
|:---|:---|
| `EntityManager_Ability.cs` | `AddAbility` 后调 `OnFeatureGranted`；`RemoveAbility` 前调 `OnFeatureRemoved` |
| `AbilitySystem.cs` | 构建 `FeatureContext`，在 Execute 阶段调用 `OnFeatureActivated / OnFeatureEnded`，并从 `ExtraData` 读取 `AbilityExecutedResult` |
| `Src/ECS/System/AbilitySystem/AbilityFeatureHandlerBase.cs` | Ability 子域对 `IFeatureHandler` 的桥接基类：把 `CastContext` 转成 `ExecuteAbility` 调用并写回执行结果 |

---

## 8. 扩展指南

### 新增 IFeatureHandler

```csharp
[ModuleInitializer]
public static void Init()
{
    FeatureHandlerRegistry.Register(new SpeedBoostHandler());
}

public class SpeedBoostHandler : IFeatureHandler
{
    public string FeatureId => "SpeedBoost";

    public void OnGranted(FeatureContext ctx)
    {
        // 永久加速（也可直接用 FeatureDefinition.Modifiers 代替这里）
        ctx.Owner?.Data.AddModifier("MoveSpeed",
            new DataModifier(ModifierType.Multiplicative, 1.3f, source: ctx.Feature));
    }

    public void OnRemoved(FeatureContext ctx)
    {
        ctx.Owner?.Data.RemoveModifiersBySource(ctx.Feature);
    }
}
```

### 新增 IFeatureAction

```csharp
public class SpawnProjectileAction : IFeatureAction
{
    public string ProjectileId { get; set; } = "";

    public void Execute(FeatureContext ctx)
    {
        if (string.IsNullOrEmpty(ProjectileId) || ctx.Owner == null) return;
        // 调用已有 ProjectileSystem 或 EntityManager 生成投射物
        // ProjectileSystem.Spawn(ctx.Owner, ProjectileId);
    }
}
```

### Enable/Disable Feature

```csharp
// 获取已授予的 AbilityEntity
var feature = EntityManager.GetAbility(owner, "MyFeature");
if (feature != null)
{
    FeatureSystem.DisableFeature(feature, owner);  // 暂停
    FeatureSystem.EnableFeature(feature, owner);   // 恢复
}
```

---

## 9. 设计原则

1. **Granted ≠ Activated**：授予不等于触发，Permanent Feature 授予即生效但不走激活流水线
2. **Ended ≠ Removed**：一次激活结束不等于 Feature 彻底卸载
3. **Modifier 托管**：持续效果优先走 `DataModifier + RemoveModifiersBySource`，不自造数值系统
4. **AbilitySystem 负责 Cast，FeatureSystem 负责 Execute 生命周期**：AbilitySystem 保留施法编排，具体效果统一经 `IFeatureHandler.OnActivated`
5. **IFeatureHandler vs IFeatureAction**：前者是代码钩子（生命周期入口），后者是可组合的最小业务单元

---

## 10. 与其他系统的边界

| 系统 | 边界 |
|:---|:---|
| `DamageSystem` | 伤害计算仍走 DamageService，Feature 可在 OnActivated 触发 |
| `TargetSelector` | 目标选择仍由 AbilityTargetSelectionComponent 负责 |
| `ResourceManagement` | 资源加载仍走 ResourceManagement.Load |
| `TimerManager` | Feature 内定时器需在 OnGranted 注册、OnRemoved 取消 |
| `AbilitySystem` | FeatureSystem 是通用核心；AbilitySystem 只是适配层，负责透传完整 `FeatureHandlerId` 并在 Activated/Ended 阶段传入 CastContext |

### 10.1 状态所有权边界

| 数据键 | 所有者 | 说明 |
|:---|:---|:---|
| `FeatureHandlerId` | `FeatureSystem` 核心 | Feature 处理器查找主键 |
| `FeatureCategory` | `FeatureSystem` 核心 | Feature 分类信息 |
| `FeatureEnabled` / `FeatureIsActive` / `FeatureActivationCount` | `FeatureSystem` 核心 | Feature 通用运行时状态 |
| `AbilityTriggerMode` / `AbilityCooldown` / `AbilityTarget*` | `AbilitySystem` 适配层 | 主动施法子域的施法配置与选择规则 |
| `FeatureHandlerId`（由 AbilityConfig 提供） | `AbilitySystem` 适配层 | Ability 与 IFeatureHandler 的精确桥接键，必须是完整唯一 ID |

### 10.2 推荐约束

1. Feature 核心代码只读写 `Feature*` 键，不读写 `Ability*` 键。
2. Ability 子域可以保留 `Ability*` 配置键，但执行逻辑应统一经 `完整 FeatureHandlerId + IFeatureHandler` 接入。
3. 新增非 Ability 的 Feature 子域时，应直接使用 `FeatureDefinition + Feature*` 键，不要重新引入独立执行器注册表。
