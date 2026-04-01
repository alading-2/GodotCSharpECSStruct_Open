# ContactDamageComponent 接触伤害组件

## 1. 组件概述

`ContactDamageComponent` 是专职处理“实体碰到敌人时，自身持续受到接触伤害”的逻辑组件。

它严格遵循 ECS 的职责拆分：

- **不直接监听原生 Godot 碰撞信号**
- **不做空间查询**
- **不决定碰撞节点从哪里来**
- **只消费 `CollisionComponent` 转发出来的统一碰撞事件**

## 2. 核心责任边界

本组件只回答一个问题：

> 当我身上的受击感应区碰到了敌对实体，应该如何持续结算接触伤害？

它的职责包括：

- **事件消费**：监听 `CollisionEntered / CollisionExited`
- **类型过滤**：只处理 `CollisionType.Hurtbox`
- **敌对判定**：只对非中立且敌对阵营生效
- **伤害节流**：为每个接触目标维护独立循环计时器
- **伤害结算**：通过 `DamageService.Instance.Process()` 发起标准伤害请求

## 3. 与碰撞系统的协作关系

`ContactDamageComponent` 自己不负责把 `Area2D` 接入事件总线。

当前标准链路是：

```text
实体上的 Hurtbox Sensor 触发原生碰撞
  ↓
CollisionComponent 统一桥接为 CollisionEntered / CollisionExited
  ↓
ContactDamageComponent 按 CollisionType.Hurtbox 过滤
  ↓
对敌对实体结算接触伤害
```

因此，`ContactDamageComponent` 的前置条件是：

- 实体身上有可用的 Hurtbox 碰撞模板
- 实体挂有 `CollisionComponent`

## 4. 工作流程与运行机制

### 4.1 进入接触

- 收到 `CollisionEntered`
- 先过滤 `CollisionType.Hurtbox`
- 再判断 `Target` 是否为敌对实体
- 首次进入时立即调用一次 `ApplyDamageFrom()`
- 为该目标创建独立循环计时器，按 `AttackInterval` 持续扣血

### 4.2 持续接触

- 计时器触发时执行 `OnBodyDamageTick()`
- 若自己已死亡，则清理全部计时器
- 若目标节点失效，则只移除对应目标计时器
- 若仍有效，则继续调用 `ApplyDamageFrom()`

### 4.3 离开接触

- 收到 `CollisionExited`
- 先过滤 `CollisionType.Hurtbox`
- 取消该目标对应的循环计时器

## 5. 伤害来源语义

这里的接触伤害语义是：

- **受害者**：当前挂有 `ContactDamageComponent` 的实体
- **攻击者**：碰到它的敌对实体

也就是：

> 把 `ContactDamageComponent` 挂到某个实体后，该实体自己的 Hurtbox 一旦碰到敌人，就会按敌人的攻击间隔和攻击值持续受伤。

当前数值读取方式：

- 伤害值：攻击者 `DataKey.FinalAttack`
- 伤害间隔：攻击者 `DataKey.AttackInterval`

## 6. 应用方式

标准组合方式如下：

1. 给实体添加 `CollisionComponent`
2. 在实体根节点下放置 `Collision` 容器
3. 在 `Collision` 容器内实例化 Hurtbox 模板（如 `PlayerHurtboxSensor`）
4. 给实体挂上 `ContactDamageComponent`

示意结构：

```text
Entity Root
  ├─ Component
  │   ├─ CollisionComponent
  │   └─ ContactDamageComponent
  └─ Collision
      └─ PlayerHurtboxSensor / EnemyHurtboxSensor
          └─ CollisionShape2D
```

## 7. 兜底与稳定性设计

本组件具备两层稳定性保障：

1. **事件层清理**：收到 `CollisionExited` 时及时取消对应计时器
2. **运行时判活**：每次计时触发前都再次校验自身与目标的有效性

这样即使底层偶发漏掉离开事件，也不会无限制地对无效目标持续结算伤害。

## 8. 关键文件

- 组件实现：`Src/ECS/Component/Collision/ContactDamageComponent/ContactDamageComponent.cs`
- 组件场景：`Src/ECS/Component/Collision/ContactDamageComponent/ContactDamageComponent.tscn`
- 桥接组件：`Src/ECS/Component/Collision/CollisionComponent/CollisionComponent.cs`
- 碰撞总览：`Docs/框架/ECS/Collision/碰撞系统说明.md`
