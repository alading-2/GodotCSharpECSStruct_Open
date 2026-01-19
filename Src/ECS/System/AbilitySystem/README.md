# AbilitySystem (技能系统)

## 概述

技能系统由两部分组成：

1. **`EntityManager` (Ability 扩展)**：管理技能的生命周期（增删查）
2. **`AbilitySystem`**：管理技能的激活逻辑（检查 → 消耗 → 执行）

> **架构风格**：Pseudo-ECS + 静态类
> *   数据管理：`EntityManager.AddAbility/RemoveAbility/GetAbilities`
> *   业务逻辑：`AbilitySystem.TryActivateAbility/CanUseAbility`

---

## 目录结构

```
Src/ECS/Entity/Ability/
├── AbilityEntity.cs          # 技能实体（实现 IEntity + IPoolable）
└── AbilityEntity.tscn        # 技能场景（对象池使用）

Src/ECS/System/AbilitySystem/
├── AbilitySystem.cs          # 激活逻辑（静态类）
├── EntityManager.Ability.cs  # 增删查（EntityManager partial 扩展）
└── README.md                 # 本文档
```

---

## 快速上手

### 添加技能

```csharp
var config = new Dictionary<string, object>
{
    { DataKey.Name, "Fireball" },
    { DataKey.AbilityCooldown, 5f },
    { DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual }
};

// 使用对象池（默认，推荐用于敌人技能等高频场景）
var ability = EntityManager.AddAbility(player, config);

// 不使用对象池（适用于唯一技能）
var ability = EntityManager.AddAbility(player, config, useObjectPool: false);
```

### 激活技能

```csharp
// 方式 1：通过 owner + abilityId（推荐）
AbilitySystem.TryActivateAbility(player, "Fireball");

// 方式 2：检查后激活
var ability = EntityManager.GetAbilityByName(player, "Fireball");
if (ability != null && AbilitySystem.CanUseAbility(ability))
{
    // 播放施法动画...
    AbilitySystem.TryActivateAbility(player, "Fireball");
}
```

### 移除技能

```csharp
// 自动处理对象池归还
EntityManager.RemoveAbility(player, "Fireball");
```

### 查询技能

```csharp
// 获取所有技能
var abilities = EntityManager.GetAbilities(player);

// 按名称获取
var fireball = EntityManager.GetAbilityByName(player, "Fireball");
```

### 获取技能拥有者

```csharp
// 1. 获取 ID
var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;

// 2. 查询 ENTITY_TO_ABILITY 关系的父级
var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
    abilityId, EntityRelationshipType.ENTITY_TO_ABILITY).FirstOrDefault();

// 3. 获取 Entity
var owner = EntityManager.GetEntityById(ownerId) as IEntity;
```

---

## API 参考

### EntityManager (Ability 扩展)

| 方法 | 说明 |
| :--- | :--- |
| `AddAbility(owner, config, useObjectPool = true)` | 为单位添加技能，返回 AbilityEntity |
| `RemoveAbility(owner, abilityName)` | 从单位移除技能（自动归还对象池） |
| `GetAbilities(owner)` | 获取单位的所有技能 |
| `GetAbilityByName(owner, abilityName)` | 按名称获取技能 |

### AbilitySystem

| 方法 | 说明 |
| :--- | :--- |
| `TryActivateAbility(owner, abilityName)` | 尝试激活技能（完整流程） |
| `CanUseAbility(ability)` | 检查技能是否可用（冷却/充能/状态） |

### AbilityEntity

| 方法/属性 | 说明 |
| :--- | :--- |
| `Data` | 技能数据容器 |
| `Events` | 技能事件总线 |

---

## 架构变更说明 (2026-01-18)

### 1. AbilityEntity 对齐标准 Entity

- 实现 `IPoolable` 接口支持对象池复用
- 添加 `_Ready` 注册和 `_ExitTree` 注销
- 移除便捷属性，数据统一从 `Data` 读取

### 2. DataKey.Owner 移除

- 拥有者关系统一由 `EntityRelationshipManager` 管理
- 使用 `ENTITY_TO_ABILITY` 关系类型
- `GetOwner()` 已移除，请直接查询 `EntityRelationshipManager`

### 3. 对象池支持

- `AddAbility` 默认使用对象池（`useObjectPool = true`）
- `RemoveAbility` 通过 `EntityManager.Destroy` 自动归还对象池
- 对象池初始化：`ObjectPoolInit.cs` → `AbilityPool`

---

## 激活流程

当调用 `AbilitySystem.TryActivateAbility(owner, "Fireball")` 时：

1. **查询技能**：`EntityManager.GetAbilityByName(owner, "Fireball")`
2. **就绪检查**：`CanUseAbility(ability)`
   - 启用状态 (`Data.Get<bool>(DataKey.AbilityEnabled)`)
   - 执行状态 (`Data.Get<bool>(DataKey.AbilityIsActive)`)
   - 冷却状态 (`CooldownComponent.IsReady`)
   - 充能状态 (`ChargeComponent.HasCharge`)
3. **消耗资源**：`ChargeComponent.ConsumeCharge()` (主动技能)
4. **启动冷却**：`CooldownComponent.StartCooldown()`
5. **选择目标**：5 层目标系统
6. **发送事件**：`Ability.Activated`
7. **执行效果**：`ExecuteAbilityEffects`

---

## 设计理念

### 为什么分成两个模块？

| 模块 | 职责 | 类比 |
| :--- | :--- | :--- |
| EntityManager.Ability | 数据管理（CRUD） | 数据库层 |
| AbilitySystem | 业务逻辑（激活/检查） | 服务层 |

这种分离符合 **单一职责原则**，便于独立测试、模块化替换、代码复用。

### 为什么是静态类？

- `AbilitySystem` 不需要 `_Process`（触发由 `TriggerComponent` 处理）
- 所有功能都是 **按需调用**，无需挂载到场景树
- 与 `EntityManager` 风格一致，API 统一

### 为什么使用对象池？

- 敌人技能高频生成会造成 GC 压力
- 与 `EnemyEntity` 对象池化设计一致
- 通过 `useObjectPool` 参数可选择性关闭
