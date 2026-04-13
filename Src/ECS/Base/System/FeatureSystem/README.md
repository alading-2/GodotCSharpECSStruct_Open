# FeatureSystem

## 定位

`FeatureSystem` 是项目里的通用能力生命周期系统。

它不关心一个能力是不是“技能”，也不关心它是不是玩家主动释放。
它只关心一件事：

- 一个 Feature 被授予时做什么
- 一个 Feature 被激活时做什么
- 一个 Feature 被结束时做什么
- 一个 Feature 被移除时如何回滚

当前项目里，`AbilitySystem` 已经作为 `FeatureSystem` 的一个主动施法子域接入。

---

## 核心职责

`FeatureSystem` 负责：

- `OnFeatureGranted(feature, owner)`
  - 初始化 Feature 基础状态
  - 自动应用 `FeatureModifiers`
  - 调用 `IFeatureHandler.OnGranted`
  - 发出 `Feature.Granted`

- `OnFeatureActivated(featureCtx)`
  - 标记 `FeatureIsActive=true`
  - 调用 `IFeatureHandler.OnActivated`
  - 递增 `FeatureActivationCount`
  - 发出 `Feature.Activated`

- `OnFeatureEnded(featureCtx)`
  - 标记 `FeatureIsActive=false`
  - 调用 `IFeatureHandler.OnEnded`
  - 发出 `Feature.Ended`

- `OnFeatureRemoved(feature, owner)`
  - 调用 `IFeatureHandler.OnRemoved`
  - 按 `source=feature` 回滚全部 Modifier
  - 发出 `Feature.Removed`

- `EnableFeature / DisableFeature`
  - 切换 `FeatureEnabled`
  - 发出对应生命周期事件

---

## 关键对象

### 1. FeatureDefinition

路径：`Data/Data/Feature/FeatureDefinition.cs`

作用：定义一个 Feature 模板。

常用字段：

- `Name`
- `FeatureHandlerId`
- `Description`
- `Category`
- `EntityType`
- `Enabled`
- `Modifiers`

适用场景：

- 被动加成
- 常驻光环
- 装备词条
- Buff / Debuff 模板
- 任何需要“授予/移除/激活/结束”生命周期的通用能力

### 2. FeatureModifierEntry

路径：`Data/Data/Feature/FeatureModifierEntry.cs`

作用：定义一条授予即生效、移除即回滚的 `DataModifier`。

字段：

- `DataKeyName`
- `ModifierType`
- `Value`
- `Priority`

### 3. FeatureContext

路径：`Src/ECS/Base/System/FeatureSystem/FeatureContext.cs`

作用：贯穿生命周期回调的统一上下文。

关键字段：

- `Owner`
- `Feature`
- `Instance`
- `ActivationData`
- `SourceEventData`
- `ExtraData`

其中：

- `ActivationData` 用来承载子系统专有上下文
- `ExtraData` 用来承载执行阶段产物或跨步骤临时数据

### 4. IFeatureHandler

路径：`Src/ECS/Base/System/FeatureSystem/IFeatureHandler.cs`

作用：承载复杂逻辑的代码钩子。

生命周期：

- `OnGranted`
- `OnRemoved`
- `OnActivated`
- `OnEnded`

### 5. FeatureHandlerRegistry

路径：`Src/ECS/Base/System/FeatureSystem/FeatureHandlerRegistry.cs`

作用：维护 `完整 FeatureHandlerId -> IFeatureHandler` 的映射。

通常通过 `[ModuleInitializer]` 自动注册。

---

## `Data/Data/Feature` 怎么用

`Data/Data/Feature` 不是技能专用目录，而是通用 Feature 配置目录。

推荐用法分两类：

### 1. 纯数据 Feature

适合：

- 固定攻击加成
- 固定移速加成
- 常驻属性词条

做法：

1. 创建 `FeatureDefinition`
2. 填写 `Name` / `Category`
3. 在 `Modifiers` 里配置一组 `FeatureModifierEntry`
4. 调用 `EntityManager.AddAbility(owner, featureDefinition)` 授予
5. 调用 `EntityManager.RemoveAbility(owner, featureName)` 移除

效果：

- 授予时自动把 Modifier 施加到 `owner.Data`
- 移除时自动按 `source=feature` 整体回滚

### 2. 数据 + 代码 Feature

适合：

- 需要订阅事件
- 需要定时器
- 需要主动生成投射物/特效
- 需要复杂激活逻辑

做法：

1. 创建 `FeatureDefinition`
2. 配置 `FeatureHandlerId`
3. 实现对应 `IFeatureHandler`
4. 在 `OnGranted/OnActivated/OnRemoved/OnEnded` 中写逻辑

---

## FeatureSystem 如何应用到 AbilitySystem

当前项目里的 Ability 已经不是独立执行体系，而是 `FeatureSystem` 的主动施法子域。

执行路径如下：

```text
TryTrigger
→ AbilitySystem 做 CanUse / SelectTargets / ConsumeCharge / StartCooldown / ConsumeCost
→ AbilitySystem 构建 FeatureContext
→ FeatureContext.ActivationData = CastContext
→ FeatureSystem.OnFeatureActivated(featureCtx)
→ FeatureHandlerRegistry.Get(完整 FeatureHandlerId)?.OnActivated(featureCtx)
→ handler 把 AbilityExecutedResult 写入 featureCtx.ExtraData
→ AbilitySystem 读取结果并发出 Ability.Executed
→ FeatureSystem.OnFeatureEnded(featureCtx)
```

这条链说明：

- `AbilitySystem` 负责施法编排
- `FeatureSystem` 负责统一生命周期
- `IFeatureHandler` 负责真正的技能效果逻辑
- `AbilityConfig.FeatureHandlerId` 必须直接填写完整唯一 `FeatureId`，如 `Ability.Movement.Dash`
- `FeatureGroup` 只用于 `GetByGroup` 分组查询，不参与运行时反向拼接

---

## Ability 子域里的两种持续行为

在 Ability/Feature 混合模型里，常见的“持续效果”其实有两种来源：

### 1. TriggerComponent 的周期触发

适合：

- 每隔 N 秒重新放一次技能
- 每次都完整走一遍 `TryTrigger → ExecuteAbility`

典型配置：

- `AbilityTriggerMode = Periodic`
- `AbilityCooldown = 间隔秒数`
- 逻辑写在 `ExecuteAbility(CastContext)`

这类模型下，重复执行来自 `TriggerComponent` 的周期计时器，而不是技能内部 DoT。

### 2. FeatureHandler 自己持有的长期逻辑

适合：

- 能力一授予就持续存在的光环
- 常驻监听器
- 自己管理的长期计时器或持续特效

典型写法：

- 在 `OnGranted` 里创建 `GameTimer` / 订阅事件 / 生成常驻特效
- 在 `OnRemoved` 里显式取消计时器、取消订阅、销毁特效

如果漏掉 `OnRemoved` 清理，就会留下悬空计时器或残留效果。

---

## Ability 子域推荐写法

当前项目新增了：

- `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandlerBase.cs`

它的职责是：

- 把 `FeatureContext.ActivationData` 安全转换成 `CastContext`
- 调用 `ExecuteAbility(CastContext)`
- 把 `AbilityExecutedResult` 写回 `FeatureContext.ExtraData`
- 兜底处理异常，避免单个技能逻辑直接打断主流水线

所以 Ability 子域里推荐：

1. 继承 `AbilityFeatureHandlerBase`
2. 实现 `FeatureId`
3. 实现 `ExecuteAbility(CastContext context)`
4. 在 `[ModuleInitializer]` 中注册到 `FeatureHandlerRegistry`
5. 在 `AbilityConfig.FeatureHandlerId` 中填写同名完整唯一 `FeatureId`

---

## 新增一个 Feature 的最小步骤

### 通用 Feature

1. 创建 `FeatureDefinition`
2. 需要纯加成就只配 `Modifiers`
3. 需要复杂逻辑就实现 `IFeatureHandler`
4. 使用 `EntityManager.AddAbility` 授予

### Ability 型 Feature

1. 创建 `AbilityConfig`
2. 填写 `FeatureHandlerId`
3. 在 `Data/Data/Ability/Ability/` 下实现处理器
4. 继承 `AbilityFeatureHandlerBase`
5. `[ModuleInitializer]` 中注册到 `FeatureHandlerRegistry`
6. 通过 `TryTrigger` 统一触发

---

## 不要再这样做

- 不要新增 `IAbilityExecutor`
- 不要恢复 `AbilityExecutorRegistry`
- 不要绕过 `TryTrigger` 直接调用技能逻辑
- 不要在 Feature 核心里直接绑定 Ability 专有状态
- 不要把持续数值效果手写成私有字段，优先使用 `FeatureModifierEntry + DataModifier`

---

## 相关文件

- `Src/ECS/Base/System/FeatureSystem/FeatureSystem.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureContext.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureHandlerRegistry.cs`
- `Src/ECS/Base/System/FeatureSystem/IFeatureHandler.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilitySystem.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandlerBase.cs`
- `Data/Data/Feature/FeatureDefinition.cs`
- `Data/Data/Feature/FeatureModifierEntry.cs`
- `Docs/框架/ECS/System/FeatureSystem/FeatureSystem.md`
