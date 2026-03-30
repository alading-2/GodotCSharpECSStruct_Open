---
name: tools
description: 使用 TimerManager、ObjectPool、TargetSelector、ResourceManagement 等工具层能力时使用。适用于定时器、对象池、范围查询、随机落点、资源加载与工具层框架说明同步。
---

# tools skill

## TimerManager

禁止直接 `new Timer()` 或 `GetTree().CreateTimer()`。
统一使用 `TimerManager`。

## ObjectPool

高频对象禁止手写 `new + QueueFree()`。
统一通过 `EntityManager.Spawn(... UsingObjectPool=true ...)` 与 `EntityManager.Destroy(...)` 使用对象池。

## TargetSelector / Geometry2D

### 目标选择层

优先使用：

- `EntityTargetSelector`
- `PositionTargetSelector`
- `TargetSelectorQuery`

适用场景：

- 查找范围内敌人
- 按阵营/类型过滤
- 按距离/血量排序
- 在几何范围内批量生成随机点

### 纯几何层

如果只需要算法，不需要目标选择协议，优先使用：

- `Geometry2D`

当前已提供：

- Circle / Ring / Box / Capsule / Cone 判定
- 点到线段距离
- 圆 / 环 / 盒 / 扇形 / AABB / HollowBox 随机采样

### 兼容层

- `GeometryCalculator` 仍然保留
- 老代码可以继续调用
- 新代码优先直接调 `Geometry2D`

## Math 曲线工具

当前已接入的通用曲线工具：

- `EllipseArc2D`
- `Parabola2D`
- `CircularArc2D`
- `BezierCurve`
- `ArcLengthLut`

适用场景：

- 轨迹采样
- 切线朝向
- 弧长近似
- 匀速曲线推进

性能约束：

- `ArcLengthLut` 只适合静态曲线的预计算
- 不要在逐帧更新里为追踪目标反复重建 LUT
- 动态曲线优先 `Evaluate(...)` / `EvaluateTangent(...)`，再配合轻量长度估算推进

不要把曲线公式继续手写回运动策略内部。
优先抽到 `Src/Tools/Math/Curves/` 再由策略调用。

## ResourceManagement

禁止 `GD.Load<T>("res://...")` 或硬编码路径加载。
统一使用 `ResourceManagement.Load(...)`。
