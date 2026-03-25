# 移动系统 (Movement System)

## 目录结构

```
Src/ECS/System/Movement/
├── IMovementStrategy.cs        # 策略接口（Update/OnEnter/OnExit）
├── MovementStrategyRegistry.cs # MoveMode → Strategy 静态映射
├── MovementHelper.cs           # 朝向旋转、到达距离等共用方法
├── VelocityResolver.cs         # Velocity 分层合成工具
├── README.md                   # 本文件
└── Strategies/
    ├── FixedDirectionStrategy.cs   # 固定方向直线飞行
    ├── TargetPointStrategy.cs      # 向目标点冲锋
    ├── TargetEntityStrategy.cs     # 追踪目标实体
    ├── OrbitPointStrategy.cs       # 围绕固定点环绕
    ├── OrbitEntityStrategy.cs      # 围绕目标实体环绕
    ├── SpiralStrategy.cs           # 螺旋收缩/扩张
    ├── SineWaveStrategy.cs         # 正弦波形前进
    ├── BezierCurveStrategy.cs      # 贝塞尔曲线运动
    ├── BoomerangStrategy.cs        # 回旋镖（去程→停顿→回程）
    └── AttachToHostStrategy.cs     # 附着跟随宿主位置
```

## 架构概览

```
业务层（技能/AI/EffectTool）
  │ 写入 Data 参数（MoveMode / MoveSpeed / MoveTargetPoint ...）
  ▼
EntityMovementComponent（策略调度器）
  │ 检测 MoveMode 变化 → OnExit(旧) / OnEnter(新)
  │ 委托 IMovementStrategy.Update() 执行位移
  │ AccumulateTravel() + CheckEndConditions()
  ▼
IMovementStrategy 实现（10 种策略，单例，[ModuleInitializer] 自注册）
  │ 读取 Data 参数，计算位移，写入 Position/Rotation
  │ 返回位移量（≥0）或 -1（运动完成）
  ▼
MovementHelper（共用工具：朝向旋转、到达距离）
```

## 10 种运动模式

| MoveMode | 策略类 | 说明 |
|----------|--------|------|
| None | — | 无运动，组件不更新 |
| FixedDirection | FixedDirectionStrategy | 沿 Velocity 方向匀速直线 |
| TargetPoint | TargetPointStrategy | 向目标点冲锋，到达后完成 |
| TargetEntity | TargetEntityStrategy | 追踪目标实体，丢失后直线继续 |
| OrbitPoint | OrbitPointStrategy | 围绕固定点圆周运动 |
| OrbitEntity | OrbitEntityStrategy | 围绕目标实体动态环绕 |
| Spiral | SpiralStrategy | 螺旋运动（半径渐变） |
| SineWave | SineWaveStrategy | 正弦波形前进 |
| BezierCurve | BezierCurveStrategy | N 阶贝塞尔曲线（支持匀速） |
| Boomerang | BoomerangStrategy | 去程→可选停顿→回程 |
| AttachToHost | AttachToHostStrategy | 附着跟随宿主位置+偏移 |

## Velocity 分层合成

`VelocityResolver.Resolve(data)` 解决多组件同时写入 Velocity 的冲突：

```
IsMovementLocked = true  → Vector2.Zero
VelocityOverride ≠ Zero  → VelocityOverride
否则                      → Velocity + VelocityImpulse
```

已接入：VelocityComponent（玩家）、EnemyMovementComponent（敌人）。

## 结束条件

- `MoveMaxDuration`：最大持续时间（-1=不限制）
- `MoveMaxDistance`：最大移动距离（-1=不限制）
- 策略返回 -1：主动触发完成（如到达目标、宿主销毁）

完成后行为由 `MoveDestroyOnComplete` 控制（true=自动销毁，false=仅标记完成）。

## 策略开发规范

### ⚠️ 策略类是单例

注册表全局共享实例，**禁止持有实例级业务状态字段**。所有运行时状态存 `Data`。

### 扩展新运动模式

1. `MovementEnums.cs` 添加 MoveMode 枚举值
2. 创建策略类实现 `IMovementStrategy`
3. 添加 `[ModuleInitializer]` 静态方法调用 `MovementStrategyRegistry.Register`
4. 如需新参数在 `DataKey_Movement.cs` 添加 DataKey

### 关联文件

- **调度器**: `Src/ECS/Component/Movement/EntityMovementComponent.cs`
- **枚举**: `Data/DataKey/Component/Movement/MovementEnums.cs`
- **DataKey**: `Data/DataKey/Component/Movement/DataKey_Movement.cs`
- **事件**: `Data/EventType/Unit/GameEventType_Unit_Movement.cs`
- **使用说明**: `Src/ECS/Component/Movement/EntityMovementComponent说明.md`
