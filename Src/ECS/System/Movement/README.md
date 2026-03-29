# 运动系统 (Movement System)

## 核心定位

让实体按照指定轨迹稳定移动。策略只写 `DataKey.Velocity`，调度器统一执行位移。

## 调用方式

所有运动参数通过 `MovementParams` 一次性传入事件，不再分散写 DataKey：

```csharp
entity.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.TargetPoint, new MovementParams
    {
        Mode        = MoveMode.TargetPoint,
        TargetPoint = new Vector2(900, 360),
        MaxDistance = 300f,         // 可选，-1 不限制
        DestroyOnComplete = true,   // 可选
    }));
```

## MoveMode 与策略速查

| MoveMode | 策略类 | 典型用途 | 关键 MovementParams 字段 |
|----------|--------|----------|--------------------------|
| `FixedDirection` | FixedDirectionStrategy | 直线飞行 | MaxDistance（先写 DataKey.Velocity） |
| `TargetPoint` | TargetPointStrategy | 冲向指定坐标 | TargetPoint, ReachDistance |
| `TargetEntity` | TargetEntityStrategy | 追踪实体 | TargetNode, ReachDistance |
| `OrbitPoint` | OrbitPointStrategy | 围绕固定点环绕 | OrbitCenter, OrbitRadius, OrbitAngularSpeed |
| `OrbitEntity` | OrbitEntityStrategy | 围绕目标实体 | TargetNode, OrbitRadius, OrbitAngularSpeed |
| `Spiral` | SpiralStrategy | 螺旋收缩/扩张 | OrbitCenter, OrbitRadius, OrbitTargetRadius, OrbitAngularSpeed |
| `SineWave` | SineWaveStrategy | 正弦波弹道 | WaveAmplitude / WaveFrequency + 可选 `WaveAmplitudeScalarDriver` / `WaveFrequencyScalarDriver` |
| `BezierCurve` | BezierCurveStrategy | 曲线弹道 | BezierPoints, BezierDuration |
| `Boomerang` | BoomerangStrategy | 去程 + 回程 | TargetPoint, BoomerangPauseTime |
| `AttachToHost` | AttachToHostStrategy | 附着特效 | TargetNode（+DataKey.EffectOffset） |
| `PlayerInput` | PlayerInputStrategy | 玩家常驻（DefaultMoveMode） | 无，读 DataKey.MoveSpeed/Acceleration |
| `AIControlled` | AIControlledStrategy | AI 常驻（DefaultMoveMode） | 无，读 DataKey.AIMoveDirection 等 |

## 职责分工

- **业务层**：构建 `MovementParams`，触发 `MovementStarted` 事件，监听 `MovementCompleted`
- **策略**：读 `in MovementParams`，计算本帧意图写入 `DataKey.Velocity`，私有字段存运行时状态
- **组件**：持有 `_params`/`_elapsedTime`/`_traveledDistance`，切换策略，执行位移，检查结束，发事件

## 结束条件

- `MaxDuration >= 0`：时间限制（-1=不限制）
- `MaxDistance >= 0`：距离限制（-1=不限制）
- 策略返回 `MovementUpdateResult.Complete()`：主动完成
- `DestroyOnComplete = true`：完成后销毁；否则回退 `DefaultMoveMode`

`MovementCompletedEventData` 直接携带 `ElapsedTime` / `TraveledDistance`，无需读 DataKey。

## Velocity 分层合成

```text
IsMovementLocked = true  → Zero
VelocityOverride ≠ Zero  → VelocityOverride（击退/硬控）
否则                      → Velocity + VelocityImpulse（单帧冲量，用后清零）
```

## 扩展新策略

1. `MovementEnums.cs` 新增 `MoveMode`
2. `MovementParams` 新增所需 `init` 字段（附默认值）；若参数需要在同一策略内连续变化，优先复用 `ScalarDriverParams`
3. 新建策略类，私有字段存运行时状态，`[ModuleInitializer]` 注册工厂
4. 补全策略类头注释（描述 + 使用示例 + 典型用途）

## 通用标量驱动

- `ScalarDriverParams` 用于描述同一策略内部的标量参数演化，当前已接入：`OrbitRadiusScalarDriver`、`WaveAmplitudeScalarDriver`、`WaveFrequencyScalarDriver`
- 基础字段仍保留为静态/初始值，例如 `OrbitRadius`、`WaveAmplitude`、`WaveFrequency`
- `MovementParams` 中驱动字段推荐使用可空：`null` = 不启用驱动；非 `null` 时再由策略私有 `ScalarDriverState` 接管后续演化
- 已新增 `ScalarDriver/README.md`，集中说明 `ScalarDriver` 的职责边界、边界模式语义、日志上下文和接入方式
- 边界响应支持 `Clamp / PingPong / Wrap / Complete / Freeze`；`PingPong` 额外支持 `BounceDecay` 与 `StopSpeedThreshold`
- Orbit 半径演化统一由 `OrbitRadiusScalarDriver` 管理；Wave 侧则分别由 `WaveAmplitudeScalarDriver`、`WaveFrequencyScalarDriver` 管理

## 阅读顺序

1. `EntityMovementComponent说明.md`：总流程与切换规则
2. `ScalarDriver/README.md`：通用标量驱动层的职责、调用方式和边界模式语义
3. 对应策略类头注释：所需 `MovementParams` 字段
4. `VelocityResolver.cs`：速度是否会被上层覆盖
