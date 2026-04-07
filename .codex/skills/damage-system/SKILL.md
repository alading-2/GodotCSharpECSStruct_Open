---
name: damage-system
description: 处理伤害计算、造成伤害、扩展伤害处理器时使用。适用于：子弹/技能命中造成伤害，实现暴击/闪避/护甲等伤害修正，扩展新的伤害处理阶段。触发关键词：伤害、DamageService、DamageInfo、IDamageProcessor、暴击、闪避、护甲减伤、吸血、造成伤害。
---

# DamageSystem 伤害计算系统规范

## 核心原则

- **禁止直接修改 HP**：所有伤害必须通过 `DamageService.Instance.Process()`
- **禁止手写暴击/闪避**：管道内置处理，自动按优先级执行
- **职责链模式**：每个 Processor 只处理一个计算阶段
- **语义分层**：`DamageType` 表示物理/魔法/真实等数值语义，`DamageTags` 表示 Attack/Ability/Area 等来源与表现语义
- **死亡来源规则**：伤害系统仅在 **`Attacker` 自身已死亡** 且标签包含 `Attack` 时拦截，不追拥有者/父链；`Ability` 标签伤害不在此规则内统一拦截
- **接触伤害恢复规则**：`ContactDamageComponent` 在单位死亡时只暂停持续伤害计时器，不丢弃当前接触集合；单位复活并收到 `Revived` 后，需要为仍在接触的敌对目标恢复持续伤害，避免依赖底层重新派发一次 `HurtboxEntered`

## 造成伤害（标准用法）

```csharp
// ✅ 构造 DamageInfo 并提交给 DamageService
DamageService.Instance.Process(new DamageInfo
{
    Attacker = bulletEntity,      // 直接来源（子弹/技能实体）
    Victim = enemyEntity,
    Damage = 50f,
    Type = DamageType.Physical,
    Tags = DamageTags.Attack | DamageTags.Ranged
});

// 技能造成伤害（在执行器中）
DamageService.Instance.Process(new DamageInfo
{
    Attacker = context.Caster as Node,
    Victim = target,
    Damage = context.Caster.Data.Get<float>(DataKey.AbilityDamage) * 1.5f,
    Type = DamageType.Magical,
    Tags = DamageTags.Ability | DamageTags.Area
});
```

## 内置处理管道（按优先级顺序）

| 优先级 | 处理器                            | 职责                                                                       |
| ------ | --------------------------------- | -------------------------------------------------------------------------- |
| 100    | BaseDamageProcessor               | 基础检查（目标死亡/无敌/基础数值/`Attacker` 自身已死亡的 Attack 标签伤害） |
| 200    | DodgeProcessor                    | 闪避判定                                                                   |
| 300    | CritProcessor                     | 暴击判定与计算                                                             |
| 400    | ShieldProcessor                   | 护盾抵扣                                                                   |
| 500    | DefenseProcessor                  | 护甲减伤                                                                   |
| 600    | DamageTakenAmplificationProcessor | 受伤倍率（易伤效果）                                                       |
| 700    | FlatReductionProcessor            | 固定值减伤                                                                 |
| 800    | LifestealProcessor                | 吸血回血                                                                   |
| 900    | HealthExecutionProcessor          | 生命值结算                                                                 |
| 1000   | StatisticsProcessor               | 数据统计                                                                   |

## 扩展自定义处理器

```csharp
// 1. 实现 IDamageProcessor
public class MyDamageProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.IsDodged) return;  // 已闪避则跳过
        if (info.Amount <= 0) return;

        // 自定义计算逻辑
        // 例：燃烧状态额外增伤 20%
        var victim = info.Victim;
        if (victim?.Data.Get<bool>(DataKey.IsBurning) == true)
        {
            info.Amount *= 1.2f;
            info.AddLog("燃烧增伤 x1.2");
        }
    }
}

// 2. 在 DamageService 初始化时注册（DamageServiceRegister 方法中）
RegisterProcessor(new MyDamageProcessor(), 650);  // 在护甲后、固定减伤前
```

## DamageInfo 关键字段

```csharp
public class DamageInfo
{
    public Node Attacker;           // 直接来源
    public IUnit Victim;            // 受害者
    public float Damage;            // 基础伤害
    public DamageType Type;         // Physical / Magical / True
    public DamageTags Tags;         // Attack / Ability / Area / ...

    // 管道中间状态（处理器读写）
    public float FinalDamage;       // 当前最终伤害
    public bool IsDodged;           // 是否已闪避
    public bool IsCritical;         // 是否暴击
    public bool IsEnd;              // 是否提前终止管道
    public List<string> Logs;       // 调试日志
}
```

## DamageTool（批量伤害 / DoT）

多目标或持续伤害场景优先使用 `DamageTool`，不要手写 foreach / TimerManager：

```csharp
// 单次批量伤害
DamageTool.ApplyToList(targets, new DamageApplyOptions
{
    Damage = 50f,
    Type = DamageType.Physical,
    Tags = DamageTags.Ability | DamageTags.Area,
    Attacker = casterNode
});

// DoT：每 1s 对新一批目标结算，持续 5s
DamageTool.ScheduleDoT(
    () => EntityTargetSelector.Query(new TargetSelectorQuery { ... }),
    new DamageApplyOptions
    {
        Damage = 20f,
        Type = DamageType.Magical,
        Tags = DamageTags.Ability | DamageTags.Area,
        Attacker = casterNode,
        TickInterval = 1.0f,
        TotalDuration = 5.0f,
        AllowRepeatHitSameTarget = false  // 每目标只命中一次
    },
    guardian: casterNode,               // 施法者失效时自动取消计时器
    hitRegistry: DamageTool.CreateHitRegistry()
);
```

字段规则：

- `TickInterval <= 0` 或 `TotalDuration <= 0`：退化为单次伤害
- `AllowRepeatHitSameTarget = false`：配合 `hitRegistry` 实现整个 DoT 周期内每目标只命中一次
- `guardian`：守护节点失效时提前终止 DoT，防止僵尸 tick
- **绝大多数技能**通过 `AbilityImpactTool.Execute / ExecuteAroundCaster` 间接调用 `DamageTool`，不需要直接使用

关键文件：`Src/ECS/System/DamageSystem/DamageTool.cs`

## 禁止事项

- ❌ `victim.Data.Set(DataKey.CurrentHp, hp - damage)` 直接改 HP
- ❌ 手写 `Random.NextDouble() < critRate` 暴击判定
- ❌ 手写闪避判定
- ❌ 在管道外修改 `DamageInfo.Amount`
- ❌ 手写 foreach + DamageService.Process 多目标循环 → 用 `DamageTool.ApplyToList`
- ❌ 手写 TimerManager 持续伤害调度 → 用 `DamageTool.ScheduleDoT`

## 关键文件路径

- **核心服务** → `Src/ECS/System/DamageSystem/DamageService.cs`
- **伤害信息** → `Src/ECS/System/DamageSystem/DamageInfo.cs`
- **处理器接口** → `Src/ECS/System/DamageSystem/IDamageProcessor.cs`
- **接触伤害组件** → `Src/ECS/Component/Collision/ContactDamageComponent/ContactDamageComponent.cs`
- **扩展指南** → `Src/ECS/System/DamageSystem/README.md`
- **内置处理器目录** → `Src/ECS/System/DamageSystem/Processors/`
- **设计理念** → `Docs/框架/ECS/System/伤害系统设计理念.md`
