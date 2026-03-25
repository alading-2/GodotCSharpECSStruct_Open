# EntityMovementComponent 说明

## 1. 组件定位

`EntityMovementComponent` 是**策略调度器**，通过事件驱动切换运动策略，委托当前策略计算运动意图并统一执行位移。

- 组件职责：怎么移动（执行层 + 策略分发）
- 业务职责：为什么移动（由技能/武器/AI/EffectTool 写入参数）
- 适用实体：`Node2D + IEntity` 与 `CharacterBody2D + IEntity`
- 双路径执行（调度器负责）：`Node2D/Area2D` 走 `_Process + GlobalPosition`，`CharacterBody2D` 走 `_PhysicsProcess + VelocityResolver + MoveAndSlide`
- 附着跟随：通过 `MoveMode.AttachToHost` 策略实现，不再硬编码跳过

## 2. 架构（策略模式）

```text
EntityMovementComponent（调度器）
  ├── 监听 MovementStarted 事件 → SwitchStrategy() 切换策略
  ├── RunMovementLogic()：委托当前策略计算运动意图（策略只写 DataKey.Velocity）
  ├── Node2D/Area2D：ApplyNodeMovement() 执行 GlobalPosition += Velocity * delta
  ├── CharacterBody2D：VelocityResolver.Resolve() + MoveAndSlide()
  ├── AccumulateTravel() 累计统计
  └── CheckEndConditions() / OnMoveComplete() 结束流程 + 自动回退默认模式
```

- `IMovementStrategy`：策略接口（Update 纯计算只写 Velocity / OnEnter / OnExit）
- `MovementStrategyRegistry`：MoveMode → Strategy 的静态映射
- `MovementHelper`：朝向旋转、到达距离等共用方法
- 策略通过 `[ModuleInitializer]` 自注册，无需手动配置

## 3. 运动模式（12 种）

| MoveMode | 策略类 | 说明 |
|----------|--------|------|
| FixedDirection | FixedDirectionStrategy | 沿 Velocity 向量直线飞行 |
| TargetPoint | TargetPointStrategy | 向目标点冲锋，到达后完成 |
| TargetEntity | TargetEntityStrategy | 追踪目标实体，丢失后降级为直线 |
| OrbitPoint | OrbitPointStrategy | 围绕固定点圆周运动 |
| OrbitEntity | OrbitEntityStrategy | 围绕目标实体动态环绕 |
| Spiral | SpiralStrategy | 螺旋运动（半径渐变） |
| SineWave | SineWaveStrategy | 正弦波形前进 |
| BezierCurve | BezierCurveStrategy | N 阶贝塞尔曲线运动（支持匀速） |
| Boomerang | BoomerangStrategy | 回旋镖（去程→可选停顿→回程） |
| AttachToHost | AttachToHostStrategy | 附着跟随宿主位置 + 偏移 |
| PlayerInput | PlayerInputStrategy | 玩家输入驱动速度 |
| AIControlled | AIControlledStrategy | AI 移动意图驱动速度 |

> 所有 12 种模式对 Node2D/Area2D 和 CharacterBody2D 通用。策略只写 `DataKey.Velocity`，调度器根据节点类型统一执行位移。

## 4. 默认模式与打断机制

### DefaultMoveMode（DataKey 配置）

由 Entity 初始化时设置 `DataKey.DefaultMoveMode`（如玩家 `PlayerInput`、敌人 `AIControlled`）。

- `OnComponentRegistered` 时读取 `DataKey.DefaultMoveMode` 并自动进入默认策略
- 临时运动完成后，`OnMoveComplete` 自动回退到默认模式
- 业务方无需手动还原 MoveMode

### 策略切换方式（事件驱动）

业务方通过发布 `MovementStarted` 事件触发临时运动切换：

```csharp
// 先写入运动参数
entity.Data.Set(DataKey.MoveSpeed, 500f);
entity.Data.Set(DataKey.MoveTargetPoint, targetPos);
entity.Data.Set(DataKey.MoveMaxDuration, 0.5f);

// 然后发布事件触发切换
entity.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.TargetPoint));
```

流程示例：

```text
PlayerInput(默认) → 技能发布 MovementStarted(TargetPoint) → 冲锋完成 → OnMoveComplete → 自动回 PlayerInput
```

### CanBeInterrupted（策略属性）

`IMovementStrategy.CanBeInterrupted`（默认 `true`）控制当前策略是否可被外部打断切换：

- `true`：收到 `MovementStarted` 事件时立即切换
- `false`：调度器拒绝切换，待当前策略自然完成后才回退

典型用法：冲锋/击退等不可打断运动，在策略类中覆写 `CanBeInterrupted => false`。

### 统计数据重置

每次策略切换（`SwitchStrategy`）时自动重置 `MoveElapsedTime`、`MoveTraveledDistance`、`MoveCompleted`，防止临时运动之间带旧值。

## 5. 结束条件语义（重要）

时间与距离结束条件统一采用：

- `-1`：不限制
- `>= 0`：有效限制值

对应键：`DataKey.MoveMaxDuration` / `DataKey.MoveMaxDistance`

策略也可通过返回 `-1` 主动触发完成（如 TargetPoint 到达目标、Boomerang 返回起点、AttachToHost 宿主消失）。

## 6. Velocity 分层合成

`VelocityResolver` 解决多组件写入 `DataKey.Velocity` 的冲突：

```
IsMovementLocked=true → 返回 Zero
VelocityOverride != Zero → 返回 Override
否则 → 返回 Velocity + VelocityImpulse
```

已接入：`EntityMovementComponent`（CharacterBody2D 路径）。

> AI 行为树在非 `MoveMode.AIControlled` 时不会写入 `AIMoveDirection/AIMoveSpeedMultiplier`，用于让位给击退、冲刺等特殊移动模式。

## 7. 最小接入示例

```csharp
// Entity 初始化时设置默认模式（在 Entity 类或 Resource 中）
entity.Data.Set(DataKey.DefaultMoveMode, MoveMode.PlayerInput);
entity.Data.Set(DataKey.MoveSpeed, 240f);

// 业务方触发临时运动（如技能冲锋）
entity.Data.Set(DataKey.MoveTargetPoint, new Vector2(900, 360));
entity.Data.Set(DataKey.MoveMaxDuration, 0.5f);
entity.Data.Set(DataKey.RotateToVelocity, true);

entity.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.TargetPoint));
```

## 8. 扩展新运动模式

1. 在 `MovementEnums.cs` 的 `MoveMode` 枚举添加新值
2. 创建策略类实现 `IMovementStrategy`
3. 添加 `[ModuleInitializer]` 静态方法调用 `MovementStrategyRegistry.Register`
4. 如需新参数，在 `DataKey_Movement.cs` 添加 DataKey

## 9. 测试场景

- `Src/Test/SingleTest/ECS/Movement/MovementComponentTestScene.tscn`

运行后默认演示 `TargetPoint` 模式，可在脚本中切换 `MoveMode` 与参数进行验证。
