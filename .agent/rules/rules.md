---
trigger: always_on
---

# 项目规则 - Godot 4.5 C# (.NET 8.0)

> [!IMPORTANT]
> 本文档是AI开发的**强制检查清单**,遇到不确定的问题优先查阅本文档。
> 详细架构文档: [项目索引](../../Docs/框架/项目索引.md)

## 1. 项目规则
- 必须使用中文回复
- 避免删除再创建文件，尽量修改文件

## 2. C# 代码规范

- **概率值**: 统一0-100(计算时/100)
- **注释**: 使用 `<summary>` 而非转义字符
- **性能**: `_Process`中禁止`new`对象和LINQ


## ⚡ 开发前必读检查清单

在编写代码前,先问自己:
- [ ] 需要定时器? → **TimerManager** | 详见: [§1.1](#11-timermanager---计时器系统)
- [ ] 需要查找敌人/范围检测? → **TargetSelector** | 详见: [§1.2](#12-targetselector---目标选择工具)
- [ ] 需要存储组件状态? → **Entity.Data** | 详见: [§1.3](#13-data---数据容器系统)
- [ ] 需要组件间通信? → **Entity.Events** | 详见: [§1.4](#14-eventbus---事件系统)
- [ ] 需要频繁生成/销毁对象? → **EntityManager + 对象池** | 详见: [§2.1](#21-entitymanager---实体生命周期管理)
- [ ] 需要处理伤害计算? → **DamageSystem** | 详见: [§2.3](#23-damagesystem---伤害计算系统)
- [ ] 需要实现技能? → **AbilitySystem** | 详见: [§2.2](#22-abilitysystem---技能系统)

---

## 2. 核心工具类使用规范

### 2.1 TimerManager - 计时器系统

**禁止**: ❌ `new Timer()` / ❌ `GetTree().CreateTimer()`  
**理由**: 不走对象池,GC压力大

**最简示例**:
```csharp
// 延迟执行
TimerManager.Instance.Delay(2.0f).OnComplete(() => DoSomething());

// 循环执行
var timer = TimerManager.Instance.Loop(1.0f).OnLoop(() => DamageOverTime());

// 清理 (重要!)
public override void _ExitTree() { timer?.Cancel(); }
```

**详细文档**: [Src/Tools/Timer/TimerManager.md](../../Src/Tools/Timer/TimerManager.md)

---

### 2.2 TargetSelector - 目标选择工具

**禁止**: ❌ `GetTree().GetNodesInGroup()` / ❌ 手写距离计算  
**理由**: 已有高性能几何查询工具

**最简示例**:
```csharp
// 查找周围200范围内最近的5个敌人
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Circle,
    Origin = caster.GlobalPosition,
    Range = 200f,
    CenterEntity = caster,
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    Sorting = AbilityTargetSorting.Nearest,
    MaxTargets = 5
});
```

**详细文档**: [Src/Tools/TargetSelector/README.md](../../Src/Tools/TargetSelector/README.md)

---

### 2.3 Data - 数据容器系统

**核心原则**: ✅ Data是唯一数据源 | ❌ Component禁止私有状态字段

**最简示例**:
```csharp
// ✅ 正确: 数据存Data
var hp = _entity.Data.Get<float>(DataKey.CurrentHp);
_entity.Data.Set(DataKey.CurrentHp, hp - damage);

// ❌ 错误: 私有字段
private float _currentHp; // 禁止!
```

**事件监听**: ⚠️ 严禁使用 `Data.On`,必须用 `Entity.Events`:
```csharp
// ✅ 正确
entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
    GameEventType.Data.PropertyChanged, 
    evt => { if (evt.Key == DataKey.CurrentHp) UpdateUI(); }
);
```

**详细文档**: [Src/ECS/Data/README.md](../../Src/ECS/Data/README.md)

---

### 2.4 EventBus - 事件系统

**禁止**: ❌ 使用Godot Signal处理核心逻辑 / ❌ 直接调用其他组件方法

**最简示例**:
```csharp
// 发布事件
_entity.Events.Emit(GameEventType.Ability.TryTrigger, eventData);

// 订阅事件
_entity.Events.On<EventDataType>(GameEventType.SomeEvent, OnEventHandler);
```

**详细文档**: [Src/ECS/Event/README_EventBus.md](../../Src/ECS/Event/README_EventBus.md)

---

### 2.5 ObjectPool - 对象池系统

**强制场景**: 子弹、伤害数字、特效、敌人  
**禁止**: ❌ 手动 `new` + `QueueFree()`

**最简示例**:
```csharp
// 通过EntityManager使用对象池
var bullet = EntityManager.Spawn<BulletEntity>(new EntitySpawnConfig
{
    Config = bulletData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.BulletPool,
    Position = position
});

EntityManager.Destroy(bullet); // 自动归还对象池
```

**详细文档**: [Src/Tools/ObjectPool/ObjectPool.md](../../Src/Tools/ObjectPool/ObjectPool.md)

---

## 3. 核心系统使用规范

### 3.1 EntityManager - 实体生命周期管理

**核心职责**: 统一管理Entity的生成(Spawn)、注册(Register)、销毁(Destroy)

**禁止**: ❌ 直接`new`实体 / ❌ 直接`QueueFree()`销毁实体

**最简示例**:
```csharp
// 生成敌人(使用对象池)
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPos
});

// 查询Component
var health = EntityManager.GetComponent<HealthComponent>(enemy);

// 销毁(自动判断是归还对象池还是QueueFree)
EntityManager.Destroy(enemy);
```

**详细文档**: [Src/ECS/Entity/Core/EntityManager.md](../../Src/ECS/Entity/Core/EntityManager.md)

---

### 3.2 AbilitySystem - 技能系统

**核心职责**: 管理技能激活、目标选择、效果执行

**禁止**: ❌ 手写冷却管理 / ❌ 手写充能计数 / ❌ 手写范围检测

**内置组件**: `CooldownComponent`、`ChargeComponent`、`TriggerComponent`

**最简示例**:
```csharp
// 通过Data配置技能,Component自动处理
ability.Data.Set(DataKey.AbilityCooldown, 5.0f);  // CooldownComponent监听
ability.Data.Set(DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Circle);  // 自动使用TargetSelector
ability.Data.Set(DataKey.AbilityRange, 200f);

// 触发技能(AbilitySystem会自动处理冷却、充能、目标选择)
AbilitySystem.TryTriggerAbility(owner, "FireballAbility", context);
```

**详细架构**: [Docs/框架/ECS/Ability/技能系统架构设计理念.md](../../Docs/框架/ECS/Ability/技能系统架构设计理念.md)

---

### 3.3 DamageSystem - 伤害计算系统

**核心职责**: 管道式伤害计算(闪避→暴击→护盾→护甲→结算)

**禁止**: ❌ 直接修改`CurrentHp` / ❌ 手写暴击/闪避判定

**最简示例**:
```csharp
// 构造伤害信息
var damageInfo = new DamageInfo
{
    Attacker = bullet,         // 直接来源
    Instigator = player,       // 真正施法者(统计归属)
    Victim = enemy,
    BaseDamage = 50f,
    DamageType = DamageType.Physical
};

// 通过DamageService处理(自动计算暴击、护甲等)
DamageService.Instance.Process(damageInfo);
```

**处理流程**: 闪避判定→暴击判定→护盾抵扣→护甲减免→生命值结算→统计记录

**详细文档**: [Src/ECS/System/DamageSystem/README.md](../../Src/ECS/System/DamageSystem/README.md)

---

## 4. 架构模式核心规范

### 4.1 Entity 规范

- **定义**: Scene即Entity,实现 `IEntity` 接口
- **管理**: 必须通过 `EntityManager.Spawn/Register/Destroy`
- **模板**: [Src/ECS/Entity/TemplateEntity.cs](../../Src/ECS/Entity/TemplateEntity.cs)
- **详细**: [Src/ECS/Entity/Entity规范.md](../../Src/ECS/Entity/Entity规范.md)

### 4.2 Component 规范

- **原则**: 单一职责、无状态、事件驱动
- **通信优先级**: Event > Data > GetComponent
- **模板**: [Src/ECS/Component/TemplateComponent.cs](../../Src/ECS/Component/TemplateComponent.cs)
- **详细**: [Src/ECS/Component/Component规范.md](../../Src/ECS/Component/Component规范.md)

---

## 5. 完整文档索引

| 类别 | 文档路径 |
|------|---------|
| **架构总览** | [Docs/框架/项目索引.md](../../Docs/框架/项目索引.md) |
| **EntityManager** | [Src/ECS/Entity/Core/EntityManager.md](../../Src/ECS/Entity/Core/EntityManager.md) |
| **Data系统** | [Src/ECS/Data/README.md](../../Src/ECS/Data/README.md) |
| **Event系统** | [Src/ECS/Event/README_EventBus.md](../../Src/ECS/Event/README_EventBus.md) |
| **Timer系统** | [Src/Tools/Timer/TimerManager.md](../../Src/Tools/Timer/TimerManager.md) |
| **对象池** | [Src/Tools/ObjectPool/ObjectPool.md](../../Src/Tools/ObjectPool/ObjectPool.md) |
| **目标选择** | [Src/Tools/TargetSelector/README.md](../../Src/Tools/TargetSelector/README.md) |
| **技能系统(架构)** | [Docs/框架/ECS/Ability/技能系统架构设计理念.md](../../Docs/框架/ECS/Ability/技能系统架构设计理念.md) |
| **伤害系统** | [Src/ECS/System/DamageSystem/README.md](../../Src/ECS/System/DamageSystem/README.md) |
| **Entity规范** | [Src/ECS/Entity/Entity规范.md](../../Src/ECS/Entity/Entity规范.md) |
| **Component规范** | [Src/ECS/Component/Component规范.md](../../Src/ECS/Component/Component规范.md) |
