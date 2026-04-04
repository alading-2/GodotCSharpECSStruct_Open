# EntityMovementComponent 说明

## 1. 组件定位

运动系统的调度器，不决定"为什么移动"，只负责把当前运动策略稳定地跑起来。

- 业务层构建 `MovementParams` 并通过事件传入，不再向 DataKey 写入运动参数
- 策略负责计算本帧位移意图，写入 `DataKey.Velocity`
- 如需将视觉朝向与位移方向解耦，策略可通过 `MovementUpdateResult` 显式返回 `FacingDirection`
- 组件持有 `MovementParams`、统计字段，执行位移、检查结束、触发事件
- 适用于 `Node2D + IEntity`、`Area2D + IEntity`、`CharacterBody2D + IEntity`

## 2. 私有状态

```csharp
private MovementParams _params;          // 本次运动输入参数（由事件传入）
private float _elapsedTime;              // 已持续时间（秒）
private float _traveledDistance;         // 已移动距离（像素）
private bool _moveCompleted;             // 完成标志（防止重复触发）
private bool _hasCollided;               // 碰撞已触发标志（CharacterBody2D 防止同次运动重复发 MovementCollision）
private Vector2 _facingDirection;        // 当前帧显式朝向意图（Zero=回退到 Velocity）
private IMovementStrategy? _currentStrategy; // 当前策略实例（每次切换新建）
```

## 3. 执行路径

```text
死亡检查 / _moveCompleted 守卫
  -> 策略 Update(entity, data, delta, elapsedTime, in _params) 写入 DataKey.Velocity
  -> 策略可选返回 FacingDirection（例如正弦波/曲线切线）
  -> 策略主动返回 Complete -> OnMoveComplete()
  -> AccumulateTravel(_elapsedTime, _traveledDistance)
  -> CheckEndConditions() 读 _params.MaxDuration / MaxDistance
  -> VelocityResolver.Resolve(data) 合成最终速度
  -> CharacterBody2D: MoveAndSlide()
       GetSlideCollisionCount() > 0 && 非默认模式 -> HandleMovementCollision()
     其他 Node2D/Area2D: GlobalPosition += velocity * delta
       （Area2D 碰撞由 OnCollisionDetected 订阅 CollisionEntered 事件处理）
  -> MovementHelper.UpdateOrientation(entity, in _params, facingDirection ?? intentVelocity, visualRoot)
```

帧率路径由策略 `UsePhysicsProcess` 决定，与节点类型无关。

## 4. 参数传递方式（新 API）

所有运动参数通过 `MovementParams` 一次性传入，不再分散写 DataKey：

```csharp
entity.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.TargetPoint, new MovementParams
    {
        Mode            = MoveMode.TargetPoint,
        TargetPoint     = new Vector2(900, 360),
        MaxDuration     = -1f,          // 可选，-1 不限制
        DestroyOnComplete = false,      // 可选
    }));
```

`MovementParams` 是 `record struct`，所有字段均为 `init` 属性，策略只读访问（`in` 参数），无法修改。

## 5. DefaultMoveMode 与临时模式

实体初始化时写入 `DataKey.DefaultMoveMode`，组件注册后自动进入该模式（用空参数构建 `MovementParams`）。

```csharp
// Entity 初始化
entity.Data.Set(DataKey.DefaultMoveMode, MoveMode.PlayerInput);
entity.Data.Set(DataKey.MoveSpeed, 240f);
entity.Data.Set(DataKey.Acceleration, 10f);
```

临时运动结束后若 `DefaultMoveMode` 有效，组件自动回退。

### 打断规则

- 当前是默认模式：允许直接切换
- 当前是临时模式且 `CanBeInterrupted = false`：拒绝新 `MovementStarted`，直到自然完成

## 6. SwitchStrategy 重置

每次切换只重置三个共享 DataKey（Velocity / VelocityOverride / VelocityImpulse）和组件私有统计字段，其余实体属性（MoveSpeed、IsMovementLocked 等）保持不变。策略运行时状态（角度、起点等）存于策略私有字段，随实例一起丢弃，无需手动清理。

## 7. 结束条件

### MovementParams 通用条件

- `MaxDuration >= 0`：累计时间到达后完成
- `MaxDistance >= 0`：累计距离到达后完成
- `DestroyOnComplete = true`：完成后销毁实体，否则发事件并回退默认模式
- `DestroyOnCollision = true`：碰撞后销毁（先发布 `MovementCollision` 事件再销毁）

### 策略主动完成

返回 `MovementUpdateResult.Complete()` 即可，组件统一处理后续清理与回退。

### 完成事件

`MovementCompletedEventData` 直接携带统计数据，无需读 DataKey：

```csharp
entity.Events.On<GameEventType.Unit.MovementCompletedEventData>(
    GameEventType.Unit.MovementCompleted,
    evt => _log.Info($"Mode={evt.Mode} Elapsed={evt.ElapsedTime:F2}s Dist={evt.TraveledDistance:F1}px"));
```

## 8. Velocity 分层合成

策略写的 `Velocity` 先经过 `VelocityResolver` 再执行位移：

```text
IsMovementLocked = true  → Zero
VelocityOverride ≠ Zero  → VelocityOverride
否则                      → Velocity + VelocityImpulse（用后清零）
```

## 9. 朝向语义（2026-03 更新）

- `Velocity`：表示“本帧要如何移动”，服务于位移执行与速度分层合成
- `FacingDirection`：表示“本帧应该朝哪看”，服务于视觉翻面与旋转
- 默认情况下若策略未显式提供朝向，组件仍回退到 `Velocity` 方向

这能解决曲线路径中的语义混淆：

- 直线/追踪/输入移动：通常 `FacingDirection == Velocity方向`
- `SineWaveStrategy`：`Velocity` 是本帧去往下一采样点的纠偏速度，`FacingDirection` 应取正弦轨迹切线
- `BezierCurveStrategy`：`FacingDirection` 取曲线参数点的一阶导数切线，而非当前位置到采样点的纠偏向量
- `OrbitStrategy`：`FacingDirection` 取切向速度与径向速度的解析合成方向，螺旋时也能正确朝向
- 其它曲线路径若后续加入，只要视觉朝向不应直接取 `Velocity`，也应复用同一机制

## 10. 碰撞处理（OnCollision）

### 触发路径

| 实体类型 | 触发方式 | 特点 |
|--------|---------|------|
| `Area2D` | `CollisionComponent` 发出 `CollisionEntered` → `EntityMovementComponent.OnCollisionDetected` | 每次进入均触发 |
| `CharacterBody2D` | `ApplyMovement` 中 `MoveAndSlide` 后检测 `GetSlideCollisionCount()` | 同一次运动内只触发一次（`_hasCollided` 防重） |

> **仅在非默认运动模式下响应**（`curMode != defaultMode`），避免 AI/Player 常驻移动产生噪声事件。

### 事件与销毁

```text
HandleMovementCollision(target)
  -> 发布 GameEventType.Unit.MovementCollision（Mode/Target）
  -> 若 DestroyOnCollision=true -> OnMoveComplete(byCollision=true)
       -> 发布 MovementCompleted
       -> EntityManager.Destroy
```

其中：

- `Target` 是命中的 `Node2D?`
- 如果业务需要目标实体，应从 `evt.Target` 回溯宿主 `IEntity`

### 典型用法：发射子弹

```csharp
// 1. 发射前订阅碰撞事件（在技能/武器组件中）
bullet.Events.On<GameEventType.Unit.MovementCollisionEventData>(
    GameEventType.Unit.MovementCollision, OnBulletHit);

// 2. 启动运动（子弹自己的初始化代码）
bullet.Events.Emit(GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.FixedDirection, new MovementParams
    {
        MaxDistance        = 800f,
        DestroyOnCollision = true,
    }));

// 3. 命中处理
private void OnBulletHit(GameEventType.Unit.MovementCollisionEventData evt)
{
    var target = EntityManager.ResolveOwningIEntity(evt.Target);
    if (target == null) return;
    DamageService.Instance.Process(new DamageInfo { ... });
}
```

## 11. 扩展新运动模式

1. `MovementEnums.cs` 新增枚举值
2. `MovementParams` 新增所需 `init` 字段（附默认值）
3. 新建策略类，私有字段存运行时状态，`[ModuleInitializer]` 注册工厂到 `MovementStrategyRegistry`
4. 如视觉朝向不应直接取 `Velocity`，通过 `MovementUpdateResult.Continue(distance, facingDirection)` 显式返回朝向
5. 补全策略类头注释

## 12. 测试场景

`Src/Test/SingleTest/ECS/Movement/MovementComponentTestScene.tscn`
