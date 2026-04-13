---
name: feature-system
description: 实现或修改通用能力生命周期时使用。适用于：新增 FeatureDefinition、配置 Modifiers、实现 IFeatureHandler、把子系统上下文装入 FeatureContext、将 FeatureSystem 接入 AbilitySystem。触发关键词：FeatureSystem、IFeatureHandler、FeatureContext、FeatureDefinition、FeatureModifierEntry、FeatureHandlerRegistry、FeatureHandlerId。
---

# FeatureSystem 使用规范

## 系统定位

`FeatureSystem` 是项目里的通用能力生命周期层。

它统一处理：

- Granted
- Activated
- Ended
- Removed
- Enable
- Disable

它本身不依赖 `AbilitySystem`、`CastContext`、`AbilityEntity` 等子域类型。
子系统如果要接入，只能通过 `FeatureContext` 传入专有上下文。

---

## 什么时候该用 FeatureSystem

优先使用 `FeatureSystem` 的场景：

- 新增一个可被授予/移除的能力
- 新增一个被动词条、装备效果、常驻光环、状态效果
- 希望把复杂逻辑挂在统一生命周期上
- 希望用 `FeatureModifierEntry` 管理授予即生效、移除即回滚的属性改动
- 想让主动技能和通用能力共享一套执行生命周期
- 希望让运行时测试系统复用正式 Feature 生命周期，而不是额外实现一套调试版能力链路

如果只是普通运行时状态读写，优先走：

- `Src/ECS/Base/Data` 运行时容器
- `Data/DataKey/*` 定义键

如果是在写资源模板与配置资源，走：

- `Data/Data/Feature`
- `Data/DataKey/Feature`
- `Data/EventType/Feature`

---

## 核心对象

### `FeatureDefinition`

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

### `FeatureModifierEntry`

路径：`Data/Data/Feature/FeatureModifierEntry.cs`

作用：定义一条会在 Granted 时施加、Removed 时回滚的 `DataModifier`。

字段：

- `DataKeyName`
- `ModifierType`
- `Value`
- `Priority`

### `FeatureContext`

路径：`Src/ECS/Base/System/FeatureSystem/FeatureContext.cs`

关键字段：

- `Owner`
- `Feature`
- `Instance`
- `ActivationData`
- `SourceEventData`
- `ExtraData`

约定：

- 子系统专有上下文放 `ActivationData`
- 运行阶段的中间结果放 `ExtraData`

### `IFeatureHandler`

路径：`Src/ECS/Base/System/FeatureSystem/IFeatureHandler.cs`

生命周期：

- `OnGranted`
- `OnRemoved`
- `OnActivated`
- `OnEnded`

### `FeatureHandlerRegistry`

路径：`Src/ECS/Base/System/FeatureSystem/FeatureHandlerRegistry.cs`

负责维护：

- `完整 FeatureHandlerId -> IFeatureHandler`

---

## 标准接入方式

### 1. 纯数据 Feature

适合：

- 固定攻击加成
- 固定移速加成
- 固定最大生命值提升

做法：

1. 创建 `FeatureDefinition`
2. 在 `Modifiers` 中配置 `FeatureModifierEntry`
3. 用 `EntityManager.AddAbility(owner, featureDefinition)` 授予
4. 用 `EntityManager.RemoveAbility(owner, featureName)` 移除

### 2. 数据 + 代码 Feature

适合：

- 需要订阅事件
- 需要定时器
- 需要复杂激活逻辑
- 需要生成子弹/特效/调用伤害服务

做法：

```csharp
using System.Runtime.CompilerServices;

internal class MyFeatureHandler : IFeatureHandler
{
    public string FeatureId => "MyFeature";

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new MyFeatureHandler());
    }

    public void OnGranted(FeatureContext context)
    {
    }

    public void OnRemoved(FeatureContext context)
    {
    }

    public void OnActivated(FeatureContext context)
    {
    }

    public void OnEnded(FeatureContext context)
    {
    }
}
```

补充约定：`FeatureGroup` 仅用于 `FeatureHandlerRegistry.GetByGroup()` 分组查询，不参与运行时反向拼接。

---

## 如何接入 AbilitySystem

当前项目里，`AbilitySystem` 已经作为 `FeatureSystem` 的主动施法子域。

执行链：

```text
Ability.TryTrigger
→ AbilitySystem 完成 CanUse / SelectTargets / ConsumeCharge / StartCooldown / ConsumeCost
→ AbilitySystem 构建 FeatureContext
→ FeatureContext.ActivationData = CastContext
→ FeatureSystem.OnFeatureActivated(featureCtx)
→ FeatureHandlerRegistry.Get(完整 FeatureHandlerId)?.OnActivated(featureCtx)
→ handler 把 AbilityExecutedResult 写入 featureCtx.ExtraData
→ AbilitySystem 发出 Ability.Executed
→ FeatureSystem.OnFeatureEnded(featureCtx)
```

### Ability 子域推荐写法

不要再实现 `IAbilityExecutor`。

请改为：

1. 继承 `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandlerBase.cs`
2. 实现 `FeatureId`
3. 实现 `ExecuteAbility(CastContext context)`
4. 在 `[ModuleInitializer]` 中注册到 `FeatureHandlerRegistry`
5. 在 `.tres` 中填写 `FeatureGroupId`（如 `"Ability.Movement"`），`FeatureHandlerId` 由 `EntityManager.AddAbility` 自动派生（`FeatureGroupId + "." + Name`）

示例结构：

```csharp
using System.Runtime.CompilerServices;

internal class DashExecutor : AbilityFeatureHandlerBase
{
    public override string FeatureId => "Ability.Movement.Dash";

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new DashExecutor());
    }

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        return new AbilityExecutedResult();
    }
}
```

---

## `Data/Data/Feature` 和 `Data/Data/Ability` 的边界

### `Data/Data/Feature`

负责通用 Feature 配置模板：

- 一个 Feature 的基本信息
- 一个 Feature 的 Modifier 列表
- 与 Ability 无关的通用生命周期配置入口

适合：

- 被动效果
- 通用词条
- 状态模板
- 非主动施法子域的能力

### `Data/Data/Ability`

负责 Ability 子域配置与具体技能逻辑：

- `AbilityConfig`
- 主动施法参数
- 目标选择参数
- 冷却/充能/消耗参数
- 具体技能处理器实现

但其执行生命周期已经统一接到 `FeatureSystem`。

换句话说：

- `FeatureSystem` 负责生命周期
- `AbilitySystem` 负责施法编排
- `Data/Data/Ability` 只是主动施法子域的数据与逻辑目录

---

## 允许的执行结果桥接方式

如果 Feature 被 Ability 子域调用，允许：

- 在 `IFeatureHandler.OnActivated` 中读取 `context.ActivationData as CastContext`
- 把 `AbilityExecutedResult` 写入 `context.ExtraData`

不允许：

- 让 `FeatureSystem` 直接依赖 `CastContext`
- 在 `FeatureSystem` 核心里引用 `AbilityEntity`
- 新建第二套“技能执行器注册表”

---

## 与 TestSystem 的边界

`TestSystem` 应作为 `FeatureSystem` 的调试前台，而不是并列的第二套能力系统。

推荐分层：

- `TestSystem`：负责 UI、选中实体、模块切换、刷新与调试输入
- `FeatureDebugService` 之类的适配层：负责把测试操作转发到正式运行时链路
- `FeatureSystem` / `AbilitySystem` / `EntityManager`：负责真正的 Feature 生命周期与执行

调试技能/Feature 时，应优先复用：

- `EntityManager.AddAbility()`
- `EntityManager.RemoveAbility()`
- `FeatureSystem.EnableFeature()`
- `FeatureSystem.DisableFeature()`
- `AbilitySystem` 的正式触发流水线

不要在 `TestSystem` 里重新实现：

- Feature 授予/移除生命周期
- 技能启停状态切换逻辑
- 独立的测试版 Handler / Registry / Executor 分发链

---

## 禁止事项

- ❌ 不要新增 `IAbilityExecutor`
- ❌ 不要恢复 `AbilityExecutorRegistry`
- ❌ 不要把数值型持续效果写成私有业务字段
- ❌ 不要绕过 `FeatureHandlerRegistry` 手工查表分发技能逻辑
- ❌ 不要在 Feature 核心里写死 Ability 专有流程
- ❌ 不要用字符串字面量访问 DataKey

---

## 关键文件

- `Src/ECS/Base/System/FeatureSystem/FeatureSystem.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureContext.cs`
- `Src/ECS/Base/System/FeatureSystem/IFeatureHandler.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureHandlerRegistry.cs`
- `Data/Data/Feature/FeatureDefinition.cs`
- `Data/Data/Feature/FeatureModifierEntry.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandlerBase.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilitySystem.cs`
- `Docs/框架/ECS/System/FeatureSystem/FeatureSystem.md`
- `Src/ECS/Base/System/FeatureSystem/README.md`
