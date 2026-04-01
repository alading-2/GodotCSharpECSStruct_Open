# Collision 预设模板目录说明

## 1. 目录定位

`Src/ECS/Component/Presets/Collision/` 用于存放**碰撞模板场景**与对应的**碰撞语义注册辅助代码**。

这一层不是业务组件目录，而是碰撞系统的“模板资源层”。

它承担三类职责：

- 为实体和视觉场景提供可实例化的碰撞模板 `.tscn`
- 为运行时提供 `CollisionType` 与 `layer/mask` 的统一语义映射
- 作为 `SpriteFramesGenerator` 与 `ResourceGenerator` 的公共输入目录

## 2. 目录结构

```text
Src/ECS/Component/Presets/Collision/
  CollisionType.cs
  CollisionTypeQuery.cs
  CollisionTypeRegistry.cs        ← ResourceGenerator 自动生成
  Effect/
    EffectCollision.tscn
  Sensor/
    EnemyHurtboxSensor.tscn
    PlayerHurtboxSensor.tscn
    PlayerPickupSensor.tscn
  Unit/
    EnemyCollision.tscn
    PlayerCollision.tscn
```

## 3. 模板场景的职责边界

这些 `.tscn` 都属于“碰撞模板”，不是完整功能组件。

统一原则：

- 模板场景主要提供节点类型与 `collision_layer / collision_mask`
- `Unit` / `Effect` 类型模板供 `SpriteFramesGenerator` 注入 `VisualRoot`
- `Sensor` 类型模板供实体根节点下的 `Collision` 容器实例化
- 运行时事件桥接由 `CollisionComponent` 完成
- 业务逻辑由 `ContactDamageComponent`、拾取组件、移动系统等消费碰撞事件完成

## 4. 三类模板说明

### 4.1 `Unit/`

用于角色视觉体碰撞模板。

当前包含：

- `PlayerCollision.tscn`
- `EnemyCollision.tscn`

特点：

- 根节点通常为 `CharacterBody2D`
- 主要表达“视觉体/物理体”的碰撞层语义
- 不负责命中检测逻辑
- `SpriteFramesGenerator` 会把它们实例化到生成的 `VisualRoot/CollisionShape2D`
- 生成器会在其下补一个实际 `CollisionShape2D`，首次生成默认是胶囊体，后续智能更新保留手工调整

### 4.2 `Effect/`

用于特效或飞行体这类视觉节点的碰撞模板。

当前包含：

- `EffectCollision.tscn`

特点：

- 根节点通常为 `Area2D`
- 适合由 `CollisionComponent` 直接桥接为 `CollisionEntered/Exited`
- 只提供基础碰撞语义，具体业务仍由命中特效、子弹、技能组件决定

### 4.3 `Sensor/`

用于实体自身的受击区与拾取区等传感器模板。

当前包含：

- `PlayerHurtboxSensor.tscn`
- `EnemyHurtboxSensor.tscn`
- `PlayerPickupSensor.tscn`

特点：

- 一般实例化到实体根节点的 `Collision` 容器下
- 根节点为 `Area2D`
- 由 `CollisionComponent` 统一绑定信号
- `ContactDamageComponent` 等业务组件按 `CollisionType` 消费这些事件

## 5. 与 `sprite_frames_config.gd` 的关系

`addons/SpriteFramesGenerator/sprite_frames_config.gd` 中的 `RULES[*].collision_scene_path` 会直接引用这里的模板场景。

当前规则示例：

- 玩家资源 → `res://Src/ECS/Component/Presets/Collision/Unit/PlayerCollision.tscn`
- 敌人资源 → `res://Src/ECS/Component/Presets/Collision/Unit/EnemyCollision.tscn`
- 特效资源 → `res://Src/ECS/Component/Presets/Collision/Effect/EffectCollision.tscn`

因此：

- **给生成器用的视觉碰撞模板，要放这里**
- 规则路径改动时，需要同步更新 `sprite_frames_config.gd`

## 6. 与 `ResourceGenerator` 的关系

`Tools/ResourceGenerator/ResourceGenerator.cs` 会扫描本目录下的所有 `.tscn`，自动生成：

- `CollisionTypeRegistry.cs`

自动生成内容包括：

- `CollisionType -> (Layer, Mask)` 正向映射
- `(Layer, Mask) -> CollisionType` 反向映射

这套映射会被：

- `CollisionComponent`
- `CollisionTypeQuery`
- 其它按碰撞语义过滤的业务组件

共同使用。

## 7. `CollisionType.cs` 与自动生成代码的分工

### 7.1 手动维护

`CollisionType.cs` 需要手动维护：

- 单场景位标志
- 组合语义类型（如 `VisualBody`、`Hurtbox`、`Pickup`）

### 7.2 自动生成

`CollisionTypeRegistry.cs` 由 `ResourceGenerator` 自动生成：

- 不要手改
- 重新运行工具会覆盖

### 7.3 手动查询工具

`CollisionTypeQuery.cs` 手动维护，用于封装查询入口：

- `TryFromLayerMask()`
- `TryGetLayerMask()`

## 8. 新增碰撞模板的标准流程

如果你要新增一个新的碰撞模板，例如新的命中区或特殊视觉体：

1. 在本目录合适的子目录下新增 `.tscn`
2. 设置根节点类型与 `collision_layer / collision_mask`
3. 在 `CollisionType.cs` 增加对应位标志
4. 运行 `ResourceGenerator` 重新生成 `CollisionTypeRegistry.cs`
5. 如果该模板要被 `SpriteFramesGenerator` 自动注入，再去更新 `sprite_frames_config.gd`
6. 在消费方组件里按新的 `CollisionType` 编写逻辑

## 9. 命名约定

推荐命名统一表达“用途 + Collision/Sensor”：

- 视觉物理体：`PlayerCollision`、`EnemyCollision`、`EffectCollision`
- 感应器：`PlayerHurtboxSensor`、`EnemyHurtboxSensor`、`PlayerPickupSensor`

这样可保证：

- 场景文件名
- `CollisionType` 枚举名
- 自动生成注册表项

三者保持一致。

## 10. 关键文件

- 目录入口：`Src/ECS/Component/Presets/Collision/README.md`
- 语义枚举：`Src/ECS/Component/Presets/Collision/CollisionType.cs`
- 查询工具：`Src/ECS/Component/Presets/Collision/CollisionTypeQuery.cs`
- 自动生成：`Src/ECS/Component/Presets/Collision/CollisionTypeRegistry.cs`
- 生成配置：`addons/SpriteFramesGenerator/sprite_frames_config.gd`
- 生成工具：`Tools/ResourceGenerator/ResourceGenerator.cs`
- 系统总览：`Docs/框架/ECS/Collision/碰撞系统说明.md`
