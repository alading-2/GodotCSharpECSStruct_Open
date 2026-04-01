# CollisionComponent 说明

## 1. 组件定位

`CollisionComponent` 是当前项目统一的碰撞采样与事件桥接组件。

它本身**不做伤害结算、不做拾取业务、不做运动销毁决策**，只负责把实体身上已经存在的碰撞节点统一接入 `Entity.Events`，向外发布标准化的：

- `GameEventType.Collision.CollisionEntered`
- `GameEventType.Collision.CollisionExited`

这样碰撞系统的职责边界被清晰拆分为：

- `CollisionComponent`：采样与转发
- `ContactDamageComponent`：接触伤害
- `EntityMovementComponent`：非默认运动模式下的碰撞完成与销毁
- 其它业务组件：按 `CollisionType` 订阅并处理自己的逻辑

## 2. 统一采样范围

`CollisionComponent` 会在组件注册时统一扫描两类碰撞来源：

### 2.1 `VisualRoot/CollisionShape2D`

这是 `SpriteFramesGenerator` 根据 `addons/SpriteFramesGenerator/sprite_frames_config.gd` 中的 `collision_scene_path` 自动注入的视觉碰撞模板实例。

常见来源：

- `Src/ECS/Component/Presets/Collision/Unit/PlayerCollision.tscn`
- `Src/ECS/Component/Presets/Collision/Unit/EnemyCollision.tscn`
- `Src/ECS/Component/Presets/Collision/Effect/EffectCollision.tscn`

如果该节点是：

- `Area2D`：绑定 `BodyEntered / BodyExited / AreaEntered / AreaExited`
- `CharacterBody2D`：不重复绑定，由 `EntityMovementComponent` 在 `MoveAndSlide()` 路径处理

### 2.2 `Entity/Collision` 容器下的模板实例

实体根节点下允许放置一个名为 `Collision` 的 `Node2D` 容器，里面挂受击区、拾取区等碰撞模板实例，例如：

- `PlayerHurtboxSensor`
- `EnemyHurtboxSensor`
- `PlayerPickupSensor`

这些模板场景统一存放于：

`Src/ECS/Component/Presets/Collision/`

`CollisionComponent` 会扫描该容器下所有带 `CollisionShape2D` 或 `CollisionPolygon2D` 的子节点：

- `Area2D` → 绑定碰撞信号
- `CharacterBody2D` → 记录为物理体，由运动系统处理

## 3. CollisionType 的来源

`CollisionComponent` 不靠节点名判断语义，而是通过碰撞节点自身的：

- `collision_layer`
- `collision_mask`

反查 `CollisionType`。

查询入口：

- `CollisionTypeQuery.TryFromLayerMask(layer, mask, out type)`
- `CollisionTypeRegistry`（自动生成映射表）

这意味着：

- 碰撞语义与节点名字解耦
- 业务组件只关心 `CollisionType`
- 只要模板场景的 layer/mask 合法，运行期就能被统一识别

## 4. 事件数据语义

当前碰撞事件结构位于：

`Data/EventType/Base/Collision/GameEventType_Collision.cs`

核心字段：

- `Source`：当前拥有 `CollisionComponent` 的实体
- `Target`：进入或离开的目标节点
- `CollisionType`：**被触发的本方碰撞节点类型**，不是目标类型

这里最关键的语义是：

> `CollisionType` 表示“我身上的哪个碰撞节点触发了事件”。

例如：

- 角色自己的 `PlayerHurtboxSensor` 被敌人碰到 → `CollisionType = PlayerHurtboxSensor`
- 特效视觉碰撞体 `EffectCollision` 触发 → `CollisionType = EffectCollision`

因此消费者应当：

- 先按 `CollisionType` 过滤自己关心的碰撞来源
- 再根据 `Target` 判断碰到了谁

## 5. 标准节点结构

### 5.1 角色实体

```text
Player / Enemy (CharacterBody2D)
  ├─ Component
  │   ├─ CollisionComponent
  │   └─ ContactDamageComponent（可选）
  ├─ Collision
  │   ├─ PlayerHurtboxSensor / EnemyHurtboxSensor / PlayerPickupSensor
  │   │   └─ CollisionShape2D
  ├─ CollisionShape2D              ← 根物理体碰撞形状
  └─ VisualRoot
      └─ CollisionShape2D          ← 生成器注入的视觉碰撞模板实例
          └─ CollisionShape2D      ← 生成器首次注入的默认形状
```

### 5.2 特效 / 子弹实体

```text
EffectEntity / BulletEntity (Node2D 或 Area2D)
  ├─ Component
  │   ├─ CollisionComponent
  │   └─ EntityMovementComponent
  └─ VisualRoot
      └─ CollisionShape2D
          └─ CollisionShape2D
```

## 6. 与其它系统的分工

### 6.1 与 `ContactDamageComponent`

`ContactDamageComponent` 监听碰撞事件，只处理：

- `CollisionType.Hurtbox`

即：实体挂上该组件后，只要**自己的受击感应区**碰到敌对实体，就会按攻击间隔持续受到接触伤害。

### 6.2 与 `EntityMovementComponent`

`EntityMovementComponent` 只在**非默认运动模式**下关心碰撞：

- `Area2D` 路径：监听 `CollisionEntered`
- `CharacterBody2D` 路径：监听 `MoveAndSlide()` 的 slide collision

如果 `DestroyOnCollision = true`，则先发 `MovementCollision`，再完成移动并销毁实体。

### 6.3 与 `SpriteFramesGenerator`

`SpriteFramesGenerator` 只负责把碰撞模板实例化到生成的视觉场景里，不负责运行时碰撞逻辑。

运行时真正统一接入事件总线的是 `CollisionComponent`。

## 7. 使用建议

### 7.1 何时需要挂 `CollisionComponent`

当实体需要满足任一条件时，应挂载该组件：

- 需要让 `Area2D` 型视觉碰撞体发出碰撞事件
- 需要让 `Collision` 容器下的受击区/拾取区参与事件总线
- 需要让 `ContactDamageComponent`、拾取组件、移动销毁等逻辑接收统一碰撞事件

### 7.2 何时不需要额外自写碰撞监听

以下场景应优先复用 `CollisionComponent`，不要再单独在组件里直接监听 Godot 原生 `BodyEntered`：

- 接触伤害
- 拾取逻辑
- 特效命中
- 非默认运动模式下的命中即销毁

### 7.3 何时需要新增 `CollisionType`

当你新增了一个新的碰撞模板场景，并希望业务层能按语义过滤它时：

1. 在 `Src/ECS/Component/Presets/Collision/` 新增对应 `.tscn`
2. 在 `CollisionType.cs` 手动补一个单场景位标志
3. 运行 `Tools/ResourceGenerator` 重新生成 `CollisionTypeRegistry.cs`
4. 在业务组件中按新的 `CollisionType` 订阅过滤

## 8. 关键文件

- 组件实现：`Src/ECS/Component/Collision/CollisionComponent/CollisionComponent.cs`
- 组件场景：`Src/ECS/Component/Collision/CollisionComponent/CollisionComponent.tscn`
- 碰撞类型枚举：`Src/ECS/Component/Presets/Collision/CollisionType.cs`
- 查询工具：`Src/ECS/Component/Presets/Collision/CollisionTypeQuery.cs`
- 自动生成注册表：`Src/ECS/Component/Presets/Collision/CollisionTypeRegistry.cs`
- 碰撞事件：`Data/EventType/Base/Collision/GameEventType_Collision.cs`
- 框架总览：`Docs/框架/ECS/Collision/碰撞系统说明.md`
