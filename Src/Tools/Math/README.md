# Math 数学工具层

## 定位

路径：`Src/Tools/Math/`

这一层只放可复用的纯数学能力，不处理 ECS 生命周期、事件或业务规则。当前已经明确分成三类：

- `MyMath.cs`
  - 属性倍率、冷却缩减、护甲减伤、概率判定等数值公式
- `WaveMath.cs`
  - 正弦波采样与周期运动相关公式
- `Bezier/BezierCurve.cs`
  - 通用贝塞尔曲线求值、切线、曲率、分裂与弧长参数化

## 本轮新增

### `Curves/`

- `ArcLengthLut.cs`
  - 负责累计弧长表归一化与 `progress -> t` 映射
- `EllipseArc2D.cs`
  - 由起点、终点、弧高、侧偏方向定义的二维弧线
  - 目前用于 `BoomerangStrategy`
- `Parabola2D.cs`
  - 由起点、终点、顶高定义的二维抛物线
- `CircularArc2D.cs`
  - 由起点、终点、半径、方向定义的二维圆弧

### `Geometry/`

- `Geometry2D.cs`
  - 统一提供 Circle/Ring/Box/Capsule/Cone 判定
  - 提供点到线段距离与随机采样

## 设计边界

- 数学层负责：点采样、切线采样、长度近似、弧长映射、几何判定、随机采样。
- 数学层不负责：`MovementParams`、`DataKey.Velocity`、实体过滤、目标排序、策略阶段机。
- 运行时仍统一使用 Godot `Vector2`，不引入新的运行时数学库抽象。

## 使用建议

### 曲线运动

如果需要“按路程匀速推进”，优先使用：

1. 曲线对象的 `BuildArcLengthTable(...)`
2. `EvaluateByArcProgress(...)`
3. `EvaluateTangentByArcProgress(...)`

而不是直接把公式参数 `t` 当成匀速进度。

但要注意一条运行时约束：

- 只有静态曲线适合预建 `ArcLengthLut`
- 如果终点会在运行时持续变化，不要在 `Update` 中逐帧重建 LUT
- 动态曲线优先使用 `Evaluate(...)` / `EvaluateTangent(...)` 加轻量长度估算

### 几何查询

- 纯算法请优先调 `Geometry2D`
- 兼容旧调用可继续使用 `GeometryCalculator`
- `TargetSelectorQuery`、`GeometryType` 仍属于 `TargetSelector` 领域层，不下沉到数学层

## 当前接入情况

- `BoomerangStrategy` 已切到 `EllipseArc2D`
- 新增移动模式：`MoveMode.Parabola`、`MoveMode.CircularArc`
- `TargetSelector` 相关几何算法已下沉到 `Geometry2D`
- `GeometryCalculator` 保留为兼容门面
