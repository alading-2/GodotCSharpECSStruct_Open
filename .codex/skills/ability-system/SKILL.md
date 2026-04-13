---
name: ability-system
description: 实现或修改技能功能时使用。适用于：新建技能、配置冷却/充能/目标选择、触发技能流水线、读取触发结果、实现技能效果处理器。触发关键词：技能、AbilitySystem、TryTrigger、CastContext、CooldownComponent、ChargeComponent、TriggerComponent、AbilityEntity、IFeatureHandler、FeatureHandlerId。
---

# AbilitySystem 技能系统规范

## 核心架构

技能流水线：`TryTrigger → CanUse检查 → SelectTargets → ConsumeCharge → StartCooldown → ConsumeCost → FeatureSystem.OnFeatureActivated → IFeatureHandler.OnActivated → Ability.Executed`

内置组件（无需手写）：

- `CooldownComponent` - 冷却管理
- `ChargeComponent` - 充能计数
- `TriggerComponent` - 触发模式（Periodic/OnEvent/Manual）
- `CostComponent` - 资源消耗
- `AbilityTargetSelectionComponent` - 目标选择

## 触发技能（统一入口）

```csharp
// ✅ 标准触发方式（统一走 TryTrigger 事件）
var context = new CastContext
{
    Ability = abilityEntity,
    Caster = ownerEntity,
    ResponseContext = new EventContext()
};
abilityEntity.Events.Emit(
    GameEventType.Ability.TryTrigger,
    new GameEventType.Ability.TryTriggerEventData(context)
);

// ✅ 读取触发结果
var result = context.ResponseContext?.HasResult == true
    ? context.ResponseContext.GetResult<TriggerResult>()
    : TriggerResult.Failed;
// TriggerResult: Success / Failed / WaitingForTarget
```

## 配置技能数据

```csharp
// 通过 Data 配置，内置组件自动响应
ability.Data.Set(DataKey.AbilityEnabled, true);
ability.Data.Set(DataKey.AbilityCooldown, 5.0f);          // CooldownComponent 自动管理
ability.Data.Set(DataKey.IsAbilityUsesCharges, true);     // 启用充能
ability.Data.Set(DataKey.AbilityChargeMax, 3);            // 最大充能数
ability.Data.Set(DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual);

// 目标选择配置
ability.Data.Set(DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.Entity);
ability.Data.Set(DataKey.AbilityTargetGeometry, (int)GeometryType.Circle);
ability.Data.Set(DataKey.AbilityCastRange, 200f);      // 施法距离（索敌/瞄准射程，0=无限制）
ability.Data.Set(DataKey.AbilityEffectRadius, 150f);   // 效果半径（AOE范围/冲刺位移距离）
ability.Data.Set(DataKey.AbilityMaxTargets, 5);
ability.Data.Set(DataKey.AbilityTargetTeamFilter, (int)AbilityTargetTeamFilter.Enemy);
ability.Data.Set(DataKey.TargetSorting, (int)TargetSorting.Nearest);
ability.Data.Set(DataKey.AbilityDamageInterval, 0.5f);      // 持续伤害间隔；0 表示单次
ability.Data.Set(DataKey.AbilityDamageDuration, 3f);        // 持续伤害总时长；0 表示单次
ability.Data.Set(DataKey.AbilityRepeatHitSameTarget, true); // 是否允许同一施法内重复命中同一目标
ability.Data.Set(DataKey.AbilityApplyImmediateDamage, true);// DoT 开始时是否先立即造成一次伤害
```

## 两条时间轴

`Ability` 里有两套完全不同的“时间”语义，不能混为一谈：

- `TriggerComponent + AbilityTriggerMode.Periodic + AbilityCooldown`
- 决定的是“多久重新执行一次整条技能流水线”
- 每次都会重新进入 `TryTrigger → ExecuteAbility`
- 适合“每 N 秒放一次圈伤 / 回血 / 爆炸”

- `DamageApplyOptions.TickInterval + TotalDuration`
- 决定的是“单次 ExecuteAbility 启动后，这次伤害内部要不要继续跳 DoT”
- 不会重新执行技能流水线，只会在当前命中上下文里继续结算伤害
- 适合“这次命中后挂一个持续灼烧 / 毒伤 / 持续范围伤害”

`CircleDamage` 当前就是第一种，不是第二种：

- `TriggerComponent` 会按 `AbilityCooldown` 周期触发技能
- [`CircleDamage.cs`](/mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Data/Data/Ability/Ability/CircleDamage/CircleDamage.cs) 里的 `DamageApplyOptions` 没有设置 `TickInterval / TotalDuration`
- 所以每次触发都是“一次即时范围伤害”，不会在单次技能内部继续跳 DoT

两种模式都可以同时存在，但语义要明确：

- 如果同时配置 `Periodic` 和 `DamageApplyOptions.TickInterval / TotalDuration`
- 那么效果就是“每次周期触发都会再启动一条新的内部 DoT”
- 这是合法设计，但要确认你要的就是叠多层持续伤害，而不是误配

## 推荐：优先复用 AbilityImpactTool

当技能需要"目标查询 + 特效 + 伤害结算"时，优先调用 `AbilityImpactTool`，避免在技能处理器中重复手写 `TargetSelector + EffectTool + foreach + DamageService`：

```csharp
// 固定落点命中（Slam / Dash 落地 / 投射物爆炸）
var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
{
    Query = new TargetSelectorQuery
    {
        Geometry = GeometryType.Circle,
        Origin = impactPosition,
        Range = ability.Data.Get<float>(DataKey.AbilityEffectRadius),
        CenterEntity = caster,
        TeamFilter = AbilityTargetTeamFilter.Enemy,
        Sorting = TargetSorting.Nearest,
        MaxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets)
    },
    Effect = effectScene != null
        ? new EffectSpawnOptions(effectScene, Name: "技能特效")
        : null,
    Damage = new DamageApplyOptions
    {
        Damage = AbilityImpactTool.GetScaledAbilityDamage(context),
        Type = DamageType.Magical,
        Tags = DamageTags.Ability | DamageTags.Area,
        Attacker = casterNode
    }
});

// 以施法者当前位置命中（光环 / CircleDamage 等跟随施法者的技能）
var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
{
    Query = new TargetSelectorQuery
    {
        Geometry = GeometryType.Circle,
        Origin = casterNode.GlobalPosition,
        OriginProvider = () => casterNode.GlobalPosition,
        Range = ability.Data.Get<float>(DataKey.AbilityEffectRadius),
        CenterEntity = caster,
        TeamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter),
        MaxTargets = -1
    },
    Effect = effectScene != null
        ? new EffectSpawnOptions(
            effectScene,
            Name: "光环特效",
            Scale: new Vector2(2f, 2f))
        : null,
    Damage = new DamageApplyOptions
    {
        Damage = AbilityImpactTool.GetScaledAbilityDamage(context),
        Type = DamageType.Magical,
        Tags = DamageTags.Ability | DamageTags.Area,
        Attacker = casterNode,
        // DoT 参数（可选）：
        TickInterval = ability.Data.Get<float>(nameof(DataKey.AbilityDamageInterval)),
        TotalDuration = ability.Data.Get<float>(nameof(DataKey.AbilityDamageDuration)),
        AllowRepeatHitSameTarget = ability.Data.Get<bool>(nameof(DataKey.AbilityRepeatHitSameTarget)),
        ApplyImmediateTick = ability.Data.Get<bool>(DataKey.AbilityApplyImmediateDamage)
    }
});
```

`AbilityImpactTool` 入口说明：

- `Execute(caster, options)` - 统一入口；位置统一放在 `Query` / `Effect` 参数对象里
- 三个 options 字段均为可选（`null` 时跳过该步骤）：`Query?` / `Effect?` / `Damage?`
- `Query.Origin / OriginProvider` 是唯一命中锚点来源
- `Effect.EffectPosition` 仅在特效需要显式偏离命中点时再填写；不填则默认复用 Query 原点
- `Damage.ApplyImmediateTick` 用于控制 DoT 在开始时是否先同步结算一次，默认 `true`
- `Damage != null` 但 `Query == null` 时不会隐式兜底选目标；此时应直接使用 `EffectTool` 或补齐 Query
- DoT 调度与重复命中控制由 `DamageTool` 统一管理（见 damage-system Skill）

## 什么时候写在 ExecuteAbility，什么时候写在 OnGranted

- 写在 `ExecuteAbility(CastContext)`：表示“每次技能被触发时执行一次”
- 配合 `TriggerComponent.Periodic`：表示“每隔一段时间重新执行一次技能”
- 配合 `DamageApplyOptions.TickInterval / TotalDuration`：表示“这次技能执行内部再挂一段 DoT”

- 写在 `IFeatureHandler.OnGranted`：表示“能力一被授予就开始常驻运行”
- 适合常驻光环、被动灼烧圈、长期监听器、长期计时器
- 这类逻辑如果自己创建了 `GameTimer` / 订阅 / 持续特效，必须在 `OnRemoved` 里显式取消和清理

推荐判断：

- “每隔 N 秒释放一次技能” → `TriggerComponent.Periodic + ExecuteAbility`
- “能力授予后持续存在，直到被移除” → `OnGranted + OnRemoved`
- “单次技能命中后内部继续跳伤害” → `ExecuteAbility + DamageApplyOptions.TickInterval / TotalDuration`

## 目标选择类型

| 类型            | 说明                     | 流水线行为                          |
| --------------- | ------------------------ | ----------------------------------- |
| `Entity`        | 自动索敌                 | 无目标则 Failed                     |
| `Point`         | 玩家指定位置             | 进入异步瞄准，返回 WaitingForTarget |
| `EntityOrPoint` | 先自动索敌，无目标则瞄准 | 同 Point                            |

## 实现技能效果处理器

```csharp
// 推荐：继承 AbilityFeatureHandlerBase，把 CastContext → AbilityExecutedResult 的桥接复用掉
internal class MyAbilityHandler : AbilityFeatureHandlerBase
{
    public override string FeatureId => "技能.主动.我的技能";

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new MyAbilityHandler());
    }

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var targets = context.Targets;       // Entity 类型目标列表
        var position = context.TargetPosition; // Point 类型目标位置
        var caster = context.Caster;

        int hit = 0;
        foreach (var target in targets)
        {
            // 造成伤害
            DamageService.Instance.Process(new DamageInfo
            {
                Attacker = context.Ability,
                Instigator = caster,
                Victim = target,
                BaseDamage = caster.Data.Get<float>(DataKey.AttackDamage),
                DamageType = DamageType.Physical
            });
            hit++;
        }
        return new AbilityExecutedResult { TargetsHit = hit };
    }
}
```

`AbilityConfig` 中 `FeatureGroupId + Name` 自动派生完整 `FeatureHandlerId`：

- `.tres` 只需维护 `FeatureGroupId`（如 `"技能.位移"`）和 `Name`（如 `"冲刺"`）
- 技能展示分组统一直接使用 `FeatureGroupId`，不要再额外维护 `AbilityCategory`
- 调试/UI 删除技能时，如果已经拿到了运行时 `AbilityEntity` 或 `DataKey.Id`，必须按实例移除，不要再退回按 `Name` 删除
- 运行时技能测试面板依赖 `TestSystem.SelectedEntity` 作为操作目标；全局测试场景生成玩家后，应主动把玩家设为当前选中实体，避免左侧技能库点击后只提示未选中实体
- `EntityManager.AddAbility` 在 Spawn 后自动解析写回 `DataKey.FeatureHandlerId = "技能.位移.冲刺"`
- 仅在需要特殊映射时才手动填写 `FeatureHandlerId` 覆盖

`FeatureGroup`（处理器声明的 `IFeatureHandler.FeatureGroup`）仅用于 `FeatureHandlerRegistry.GetByGroup()` 分组查询，不参与运行时前缀拼接。

## 在处理器中使用特效

技能执行时通过 `EffectTool.Spawn` 生成特效（详见 `Docs/框架/ECS/System/特效系统使用指南.md`）：

```csharp
protected override AbilityExecutedResult ExecuteAbility(CastContext context)
{
    var casterNode = context.Caster as Node2D;

    // 在施法者位置生成独立特效（播完自动销毁）
    var effectScene = ResourceManagement.Load<PackedScene>(
        ResourcePaths.Asset_Effect_020, ResourceCategory.Asset);
    if (effectScene != null && casterNode != null)
    {
        EffectTool.Spawn(new EffectSpawnOptions(
            VisualScene: effectScene,
            Name: "技能特效",
            Scale: new Vector2(1.5f, 1.5f),
            EffectPosition: casterNode.GlobalPosition
        ));
    }

    // 附着特效（跟随施法者移动，播完自动销毁）
    var dashEffectScene = ResourceManagement.Load<PackedScene>(
        ResourcePaths.Asset_Effect_004龙卷风, ResourceCategory.Asset);
    if (dashEffectScene != null && casterNode != null)
    {
        EffectTool.Spawn(new EffectSpawnOptions(
            VisualScene: dashEffectScene,
            Host: casterNode   // 传 Host 则为附着模式
        ));
    }
    // ...
}
```

**可用特效常量**（`ResourceCategory.Asset`）：

- `ResourcePaths.Asset_Effect_003` - 光环/范围爆炸
- `ResourcePaths.Asset_Effect_004龙卷风` - 冲刺/位移
- `ResourcePaths.Asset_Effect_020` - 地面撞击/近战AOE
- `ResourcePaths.Asset_Effect_lrsc3` - 闪电命中

## 在处理器中使用投射物

投射物技能直接从 `AbilityConfig.ProjectileScene` 读取视觉场景，通过 `ProjectileTool.Spawn` 生成，不再维护独立的 `Data/Data/Projectile/*.tres`：

```csharp
protected override AbilityExecutedResult ExecuteAbility(CastContext context)
{
    var casterNode = context.Caster as Node2D;
    var ability = context.Ability;
    if (casterNode == null || ability == null)
    {
        return new AbilityExecutedResult { TargetsHit = 0 };
    }

    var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);
    var projectile = ProjectileTool.Spawn(
        casterNode.GlobalPosition,
        new ProjectileSpawnOptions(projectileScene, "AbilityProjectile"));
    if (projectile == null)
    {
        return new AbilityExecutedResult { TargetsHit = 0 };
    }

    projectile.Events.Emit(
        GameEventType.Unit.MovementStarted,
        new GameEventType.Unit.MovementStartedEventData(
            MoveMode.SineWave,
            new MovementParams { DestroyOnCollision = true }));

    return new AbilityExecutedResult { TargetsHit = 1 };
}
```

投射物接入运动策略时，必须保证 `MovementParams` 和策略契约一致：

- `MoveMode.BezierCurve`
  - 至少提供 `ActionSpeed` 或 `MaxDuration` 之一
  - 两者都缺失会被视为无效配置，策略会直接完成以避免实体永久滞留
- `MoveMode.Boomerang`
  - 必须显式传入 `TargetNode = casterNode`
  - 回旋镖返程依赖该宿主节点，不能依赖祖先回溯兜底
- `MoveMode.SineWave`
  - 需保证存在明确的推进参数与结束参数，常见组合为 `ActionSpeed + MaxDistance`

如果一个投射物技能需要“飞行后自动销毁”，除了设置 `DestroyOnComplete = true`，还必须先确认对应策略一定存在可达成的完成条件。

## 技能增删查

```csharp
// 添加技能到 Entity
var ability = EntityManager.AddAbility(ownerEntity, abilityConfig);

// 获取所有技能
var abilities = EntityManager.GetAbilities(ownerEntity);

// 移除技能
EntityManager.RemoveAbility(ownerEntity, ability);
```

## 禁止事项

- ❌ 手写冷却计时逻辑 → 用 `CooldownComponent`
- ❌ 手写充能计数 → 用 `ChargeComponent`
- ❌ 手写范围检测 → 用 `AbilityTargetSelectionComponent` + `TargetSelector`
- ❌ 同类技能重复手写“查目标 + 发伤害 + DoT 定时” → 优先复用 `AbilityImpactTool`
- ❌ 绕过 `TryTrigger` 直接调用执行逻辑
- ❌ 新增 `IAbilityExecutor` / `AbilityExecutorRegistry`
- ❌ 绕过 `FeatureSystem` 直接手工分发技能效果
- ❌ 在 `_Process` 中直接触发技能（用 `TriggerComponent` 的 Periodic 模式）

## 关键文件路径

- **架构设计（唯一概念文档）** → `Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- **核心系统** → `Src/ECS/Base/System/AbilitySystem/AbilitySystem.cs`
- **Ability → Feature 桥接基类** → `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandlerBase.cs`
- **技能命中工具** → `Src/ECS/Base/System/AbilitySystem/AbilityImpactTool.cs`
- **技能 CRUD** → `Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs`
- **模块说明** → `Src/ECS/Base/System/AbilitySystem/README.md`
- **Feature 生命周期系统** → `Src/ECS/Base/System/FeatureSystem/FeatureSystem.cs`
- **技能实体** → `Src/ECS/Base/Entity/Ability/AbilityEntity.cs`
- **施法上下文** → `Data/EventType/Ability/CastContext.cs`
- **事件定义** → `Data/EventType/Ability/GameEventType_Ability.cs`
- **技能枚举定义** → `Data/DataKey/Ability/AbilityEnums.cs`
- **目标排序枚举** → `Src/ECS/Tools/TargetSelector/TargetSorting.cs`
- **触发组件** → `Src/ECS/Base/Component/Ability/TriggerComponent/`
- **冷却组件** → `Src/ECS/Base/Component/Ability/CooldownComponent/`
- **充能组件** → `Src/ECS/Base/Component/Ability/ChargeComponent/`
- **目标选择组件** → `Src/ECS/Base/Component/Ability/AbilityTargetSelectionComponent/`
