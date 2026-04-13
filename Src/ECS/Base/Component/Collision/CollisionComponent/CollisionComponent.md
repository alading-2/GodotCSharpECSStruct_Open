# CollisionComponent 说明

## 1. 组件定位

`CollisionComponent` 是当前项目统一的视觉体碰撞桥接组件。

它本身**不做伤害结算、不做拾取业务、不做运动销毁决策**，只负责把 **Entity 根节点为 `Area2D`** 的视觉体碰撞统一接入 `Entity.Events`，向外发布标准化的：

- `GameEventType.Collision.CollisionEntered`
- `GameEventType.Collision.CollisionExited`

其中 `Hurtbox` 语义已经拆分给 `HurtboxComponent`，由其专用事件独立处理。

这样碰撞系统的职责边界被清晰拆分为：

- `CollisionComponent`：视觉体碰撞桥接
- `HurtboxComponent`：受击区桥接
- `ContactDamageComponent`：接触伤害
- `EntityMovementComponent`：非默认运动模式下的碰撞完成与销毁
- 其它业务组件：按专用事件订阅并处理自己的逻辑

## 2. 统一桥接范围

`CollisionComponent` 只桥接一类碰撞来源：

### 2.1 Entity 根节点 `Area2D`

适用于子弹、特效等视觉体实体。

- 绑定 `BodyEntered / BodyExited / AreaEntered / AreaExited`
- 向 `Entity.Events` 发出 `CollisionEntered / CollisionExited`
- 不再反查旧的碰撞语义映射

### 2.2 与 `HurtboxComponent` 的分工

受击区现在由 `HurtboxComponent` 自身作为 `Area2D` 处理，`CollisionComponent` 不再参与这条链路。

## 3. 事件数据语义

当前碰撞事件结构位于：

`Data/EventType/Base/Collision/GameEventType_Collision.cs`

核心字段：

- `Source`：当前拥有 `CollisionComponent` 的实体
- `Target`：进入或离开的目标节点
`CollisionComponent` 事件里已经不再包含额外的碰撞语义标记。

通用消费者只需要判断：

- `Source`：当前拥有 `CollisionComponent` 的实体
- `Target`：进入或离开的目标节点

而 `Hurtbox` 业务应直接消费：

- `GameEventType.Collision.HurtboxEntered`
- `GameEventType.Collision.HurtboxExited`

## 5. 标准节点结构

### 4.1 角色实体

```text
Player / Enemy (CharacterBody2D)
  ├─ Component
  │   ├─ CollisionComponent（可选：仅当实体根节点需要参与视觉体碰撞时）
  │   ├─ HurtboxComponent（受击区，直接继承 Area2D）
  │   └─ ContactDamageComponent（可选）
  ├─ CollisionShape2D              ← 根物理体碰撞形状
  └─ VisualRoot
      └─ CollisionShape2D          ← 生成器注入的视觉碰撞模板实例
```

### 4.2 特效 / 子弹实体

```text
EffectEntity / BulletEntity (Area2D)
  ├─ Component
  │   ├─ CollisionComponent
  │   └─ EntityMovementComponent
  └─ VisualRoot
      └─ CollisionShape2D
```

## 5. 与其它系统的分工

### 6.1 与 `ContactDamageComponent`

`ContactDamageComponent` 不再监听统一碰撞事件，而是直接消费：

- `HurtboxEntered`
- `HurtboxExited`

即：实体挂上该组件后，只要**自己的受击感应区**碰到敌对实体，就会按攻击间隔持续受到接触伤害。

### 5.2 与 `EntityMovementComponent`

`EntityMovementComponent` 只在**非默认运动模式**下关心碰撞：

- `Area2D` 路径：监听 `CollisionEntered`
- `CharacterBody2D` 路径：监听 `MoveAndSlide()` 的 slide collision

如果 `DestroyOnCollision = true`，则先发 `MovementCollision`，再完成移动并销毁实体。

### 5.3 与 `SpriteFramesGenerator`

`SpriteFramesGenerator` 只负责把碰撞模板实例化到生成的视觉场景里，不负责运行时碰撞逻辑。

运行时真正统一接入事件总线的是 `CollisionComponent`。

## 7. 使用建议

### 6.1 何时需要挂 `CollisionComponent`

当实体需要满足任一条件时，应挂载该组件：

- 需要让 `Area2D` 型视觉碰撞体发出碰撞事件
- 需要让视觉体本身作为 `Area2D` 参与统一碰撞事件
- 需要让移动销毁等逻辑接收统一碰撞事件

受击区推荐改由 `HurtboxComponent` 负责桥接。

### 6.2 何时不需要额外自写碰撞监听

以下场景应优先复用 `CollisionComponent`，不要再单独在组件里直接监听 Godot 原生 `BodyEntered`：

- 拾取逻辑
- 特效命中
- 非默认运动模式下的命中即销毁

接触伤害推荐复用 `HurtboxComponent` 的专用事件，而不是重复绑定原生受击区信号。

### 6.3 受击区分工

`Hurtbox` 业务由 `HurtboxComponent` 自身处理：

- 受击区直接由 `HurtboxComponent` 触发并转发专用事件
- `ContactDamageComponent` 只消费 `HurtboxEntered / HurtboxExited`

## 7. 关键文件

- 组件实现：`Src/ECS/Base/Component/Collision/CollisionComponent/CollisionComponent.cs`
- 组件场景：`Src/ECS/Base/Component/Collision/CollisionComponent/CollisionComponent.tscn`
- 受击区桥接：`Src/ECS/Base/Component/Collision/HurtboxComponent/HurtboxComponent.cs`
- 碰撞事件：`Data/EventType/Base/Collision/GameEventType_Collision.cs`
- 框架总览：`Docs/框架/ECS/Collision/碰撞系统说明.md`
