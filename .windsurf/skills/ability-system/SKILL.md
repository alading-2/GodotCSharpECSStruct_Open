---
name: ability-system
description: 实现或修改技能功能时使用。适用于：新建技能、配置冷却/充能/目标选择、触发技能流水线、读取触发结果、实现技能效果执行器。触发关键词：技能、AbilitySystem、TryTrigger、CastContext、CooldownComponent、ChargeComponent、TriggerComponent、AbilityEntity、技能执行器。
---

# AbilitySystem 技能系统规范

## 核心架构
技能流水线：`TryTrigger → CanUse检查 → SelectTargets → ConsumeCharge → StartCooldown → ConsumeCost → Execute`

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
ability.Data.Set(DataKey.AbilityTargetSorting, (int)AbilityTargetSorting.Nearest);
```

## 目标选择类型

| 类型 | 说明 | 流水线行为 |
|------|------|-----------|
| `Entity` | 自动索敌 | 无目标则 Failed |
| `Point` | 玩家指定位置 | 进入异步瞄准，返回 WaitingForTarget |
| `EntityOrPoint` | 先自动索敌，无目标则瞄准 | 同 Point |

## 实现技能效果执行器

```csharp
// 在 AbilityExecutorRegistry 中注册执行器
public class MyAbilityExecutor : IAbilityExecutor
{
    public AbilityExecuteResult Execute(CastContext context)
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
        return new AbilityExecuteResult { TargetsHit = hit };
    }
}
```

## 在执行器中使用特效

技能执行时通过 `EffectTool.Spawn` 生成特效（详见 `Docs/框架/ECS/System/特效系统使用指南.md`）：

```csharp
public AbilityExecutedResult Execute(CastContext context)
{
    var casterNode = context.Caster as Node2D;

    // 在施法者位置生成独立特效（播完自动销毁）
    var effectScene = ResourceManagement.Load<PackedScene>(
        ResourcePaths.Asset_Effect_020, ResourceCategory.Asset);
    if (effectScene != null && casterNode != null)
    {
        EffectTool.Spawn(casterNode.GlobalPosition, new EffectSpawnOptions(
            VisualScene: effectScene,
            Name: "技能特效",
            Scale: new Vector2(1.5f, 1.5f)
        ));
    }

    // 附着特效（跟随施法者移动，播完自动销毁）
    var dashEffectScene = ResourceManagement.Load<PackedScene>(
        ResourcePaths.Asset_Effect_004龙卷风, ResourceCategory.Asset);
    if (dashEffectScene != null && casterNode != null)
    {
        EffectTool.Spawn(Vector2.Zero, new EffectSpawnOptions(
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
- ❌ 绕过 `TryTrigger` 直接调用执行逻辑
- ❌ 在 `_Process` 中直接触发技能（用 `TriggerComponent` 的 Periodic 模式）

## 关键文件路径
- **架构设计（唯一概念文档）** → `Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- **核心系统** → `Src/ECS/System/AbilitySystem/AbilitySystem.cs`
- **技能 CRUD** → `Src/ECS/System/AbilitySystem/EntityManager_Ability.cs`
- **模块说明** → `Src/ECS/System/AbilitySystem/README.md`
- **技能实体** → `Src/ECS/Entity/Ability/AbilityEntity.cs`
- **施法上下文** → `Data/EventType/Ability/CastContext.cs`
- **事件定义** → `Data/EventType/Ability/GameEventType_Ability.cs`
- **枚举定义** → `Data/DataKeyRegister/Ability/AbilityEnums.cs`
- **触发组件** → `Src/ECS/Component/Ability/TriggerComponent/`
- **冷却组件** → `Src/ECS/Component/Ability/CooldownComponent/`
- **充能组件** → `Src/ECS/Component/Ability/ChargeComponent/`
- **目标选择组件** → `Src/ECS/Component/Ability/AbilityTargetSelectionComponent/`
