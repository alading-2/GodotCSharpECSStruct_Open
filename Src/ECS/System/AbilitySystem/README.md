# AbilitySystem (技能系统)

## 概述

技能系统由两部分组成：

1. **`EntityManager` (Ability 扩展)**：管理技能的生命周期（CRUD）
2. **`AbilitySystem`**：提供技能的激活逻辑（检查 → 消耗 → 冷却 → 执行）

> **架构风格**：Pseudo-ECS + 静态逻辑类
> - **数据管理**：`EntityManager.AddAbility/RemoveAbility/GetAbilities`
> - **业务中心**：`AbilitySystem.TryActivateAbility/CanUseAbility`

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

## 核心流程

### 1. 添加技能
```csharp
var config = new Dictionary<string, object>
{
    { DataKey.Name, "Fireball" },
    { DataKey.AbilityCooldown, 5f },
    { DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual }
};

// 自动建立 ENTITY_TO_ABILITY 关系，并默认使用对象池
var ability = EntityManager.AddAbility(player, config);
```

### 2. 激活技能
```csharp
// 推荐：由 AbilitySystem 统一处理检查、消耗和冷却
AbilitySystem.TryActivateAbility(player, "Fireball");
```

---

## 系统接口 (API)

### EntityManager (Ability 扩展)
| 方法 | 说明 |
| :--- | :--- |
| `AddAbility(owner, config)` | 为单位添加技能（自动处理关系和对象池） |
| `RemoveAbility(owner, name)` | 移除技能并归还对象池 |
| `GetAbilities(owner)` | 获取单位所有技能列表 |
| `GetAbilityByName(owner, name)` | 按名称查找技能 |

### AbilitySystem
| 方法 | 说明 |
| :--- | :--- |
| `TryActivateAbility(owner, name)` | 尝试完整激活流程 |
| `CanUseAbility(ability)` | 检查可用性（发送 `RequestCheckCanUse` 事件） |

---

## 事件驱动机制

系统核心流程完全依赖事件解耦：

| 事件名称 | 发送者 | 预期响应者 | 描述 |
| :--- | :--- | :--- | :--- |
| `RequestCheckCanUse` | `AbilitySystem` | `CooldownComponent` 等 | 检查是否就绪 |
| `ConsumeCharge` | `AbilitySystem` | `ChargeComponent` | 扣除充能次数 |
| `RequestStartCooldown`| `AbilitySystem` | `CooldownComponent` | 启动冷却计时 |
| `Activated` | `AbilitySystem` | `AbilityEffect` 系统 | 触发实际业务结果 |

---

## 架构特性

### 1. 静态化逻辑
`AbilitySystem` 不需要挂载到场景树，所有方法均为 `static`，通过传入 `AbilityEntity` 进行操作。

### 2. 增强的关系管理
不再在 `Data` 中存储 `Owner` 引用，统一使用 `EntityRelationshipManager` 的 `ENTITY_TO_ABILITY` 类型维护技能与单位的关系。

### 3. 对象池集成
`AbilityEntity` 默认支持对象池。`EntityManager.AddAbility` 会自动从 `AbilityPool` 申请实例，`RemoveAbility` 会自动执行 `Destroy` 逻辑归还对象池。

---

**维护者**：项目团队  
**文档版本**：v2.5  
**更新日期**：2026-01-19
