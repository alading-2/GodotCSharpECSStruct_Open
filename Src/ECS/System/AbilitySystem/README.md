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

## 核心流程 (Trigger -> Cast -> Execute)

### 1. 触发 (Trigger)
由 `TriggerComponent`、AI 或玩家输入发起：
```csharp
// 只表达“想放”，不处理逻辑
AbilitySystem.TryActivateAbility(player, "Fireball");
```

### 2. 施法 (Cast)
`AbilitySystem` 执行核心检查：
1.  **Check**: `RequestCheckCanUse` (CD? 消耗? 标签?)
2.  **Target**: 获取或选择目标
3.  **Cost**: `ConsumeCharge` & `RequestStartCooldown`

### 3. 执行 (Execute)
若施法成功，发出 `Activated` 事件，Payload 包含完整上下文：
```csharp
ability.Events.Emit(GameEventType.Ability.Activated, new ActivatedEventData(
    caster, ability, targets, triggerContext
));
```
具体的伤害、特效逻辑应监听此事件执行。

---

## 系统接口 (API)

### EntityManager (Ability 扩展)
| 方法 | 说明 |
| :--- | :--- |
| `AddAbility` | 为单位添加技能（自动处理关系和对象池） |
| `RemoveAbility` | 移除技能 |
| `GetAbilities` | 获取单位所有技能 |

### AbilitySystem (Static)
| 方法 | 说明 |
| :--- | :--- |
| `TryActivateAbility` | 尝试激活（包含完整检查流程） |
| `CanUseAbility` | 仅检查是否可用 (不消耗) |
| `SelectTargets` | 执行目标选择逻辑 (基于 DataKey 配置) |

---

## 事件驱动机制

**核心原则**：`AbilitySystem` 是调度器，`Component` 是响应器。

| 事件名称 | 方向 |Payload 核心数据 | 描述 |
| :--- | :--- | :--- | :--- |
| `RequestCheckCanUse` | Sys->Cmp | `EventContext` (可设置失败原因) | 询问组件是否允许施法 |
| `ConsumeCharge` | Sys->Cmp | `EventContext` | 请求扣除次数 |
| `RequestStartCooldown`| Sys->Cmp | - | 请求开始冷却 |
| `Activated` | Sys->Exec | `List<IEntity> Targets` | 施法成功，开始执行业务 |

---

## 架构特性

### 1. 静态化逻辑
`AbilitySystem` 是纯静态类，无状态。所有状态存储在 `AbilityEntity` 的 `Data` 中。

### 2. 选择性封装 (Selective Encapsulation)
组件使用 `Data.Get<T>` 读写数据，但对于高频核心数据（如 `CurrentCharges`, `IsOnCooldown`），推荐使用 C# 属性封装以提高代码可读性。

### 3. 统一关系管理
技能归属权统一由 `EntityRelationshipManager` 维护，不依赖组件内的 `_owner` 字段序列化。

---

**维护者**：项目团队  
**文档版本**：v3.0  
**更新日期**：2026-01-20
