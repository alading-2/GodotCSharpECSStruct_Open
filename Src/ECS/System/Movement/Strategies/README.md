# 移动策略系统文档

## 概述

移动策略系统是 ECS 架构中的核心组件，负责实现各种复杂的实体运动模式。每个策略都是 `IMovementStrategy` 接口的具体实现，通过 `MovementStrategyRegistry` 统一管理。

## 架构设计

### 核心接口
```csharp
public interface IMovementStrategy
{
    void OnEnter(IEntity entity, Data data, MovementParams @params);
    MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params);
}
```

### 参数系统
- **MovementParams**: 移动参数容器，包含所有策略需要的配置
- **运行时统计**: ElapsedTime, TraveledDistance（由调度器维护）
- **策略状态**: 私有字段存储（如 _baseDirection, _currentAngle）

## 策略分类

### 🌊 波动类 (Wave/)
#### SineWaveStrategy - 正弦波前进
**用途**: 蛇形子弹、波浪能量束、规避预判的摆动飞行物

**数学原理**:
```
位置(t) = 基准方向 × 前进速度 × t + 垂直方向 × 振幅 × sin(2π × 频率 × t + 相位)
```

**关键参数**:
- `ActionSpeed`: 前进速度（像素/秒）
- `WaveAmplitude`: 横向振幅基础值（像素）
- `WaveAmplitudeScalarDriver`: 振幅动态驱动（可选）
- `WaveFrequency`: 波动频率基础值（周期/秒）
- `WaveFrequencyScalarDriver`: 频率动态驱动（可选）
- `WavePhase`: 初始相位（度）

**技术特点**:
- OnEnter 锁定基准方向，防止波动分量污染
- 增量计算法：`sin(new) - sin(old)` 避免累积误差
- 垂直向量计算：`(-Y, X)` 实现顺时针90度旋转

---

### 📈 曲线类 (Curve/)
#### BezierCurveStrategy - 贝塞尔曲线移动
**用途**: 复杂弹道路径、平滑轨迹动画、精确路径控制

**数学原理**:
- **De Casteljau 算法**: 数值稳定的贝塞尔曲线求值
- **弧长参数化**: 实现真正的匀速移动
- **参数域**: t ∈ [0, 1]

**关键参数**:
- `MaxDuration`: 移动总时长（秒，**必须 > 0**，此策略不支持 -1）
- `BezierPoints`: 控制点数组（含起点和终点，推荐）；若未提供则以 `TargetPoint` 降级直线
- `BezierUniformSpeed`: 是否启用弧长参数化匀速模式

**技术特点**:
- 控制点克隆：避免污染原始数据
- 起点修正：OnEnter 时将第0个控制点设为实体当前位置
- LUT 自适应分段：`Math.Max(16, 阶数 * 8)`（三阶=24，五阶=40），替代固定 64 的过度开销

---

### 🎯 抛射物类 (Projectile/)
#### BoomerangStrategy - 回旋镖移动
**用途**: 回旋镖武器、往返巡逻、往返攻击

**状态机逻辑**:
1. **去程**: 飞向 TargetPoint
2. **暂停**: 到达后可选停顿（BoomerangPauseTime）
3. **返程**: 飞回 StartPoint
4. **完成**: 返回起点后结束

**关键参数**:
- `TargetPoint`: 目标点坐标
- `BoomerangPauseTime`: 到达后停顿时间（秒）
- `ReachDistance`: 到达判定阈值（像素）

**技术特点**:
- 起点记录：OnEnter 时记录当前位置作为返程目标
- 状态管理：_returning 标志控制去程/返程
- 防过冲：`Mathf.Min(speed * delta, dist)` 限制单帧移动

---

### ⚡ 冲锋类 (Charge/)
#### ChargeStrategy - 直线冲锋
**用途**: 突进攻击、直线冲刺、快速接近目标

**核心特性**:
- **追踪模式**: 实时更新目标方向
- **锁定模式**: OnEnter 时一次性采样方向
- **加速度支持**: 匀加速运动

---

### 🔄 环绕类 (Orbit/)
#### OrbitStrategy - 环绕运动
**用途**: 护卫卫星、环绕攻击、螺旋运动

**运动学参数**:
- **角运动**: OrbitAngularSpeed, OrbitAngularAcceleration
- **径向运动**: OrbitRadius（初始值）+ OrbitRadiusScalarDriver（速度/加速度/边界/触边策略均由此统一描述）
- **几何参数**: OrbitRadius, OrbitCenter

**数学概念**:
- **向心加速度**: a_c = ω² × r
- **线速度**: v = ω × r
- **螺旋运动**: 角运动 + 径向运动的合成

---

### 🎮 控制类 (Base/)
#### AIControlledStrategy - AI控制
#### PlayerInputStrategy - 玩家输入
#### AttachToHostStrategy - 附着宿主

---

## 数学与物理基础

### 坐标系统
- **Godot 2D**: 左上角原点，X向右为正，Y向下为正
- **角度系统**: 0°向右，90°向下，正值顺时针
- **角度转换**: `弧度 = 角度 × π / 180`

### 向量运算
```csharp
// 单位向量
direction = vector.Normalized();

// 垂直向量（顺时针90度）
perp = new Vector2(-direction.Y, direction.X);

// 位移与速度
velocity = displacement / time;
displacement = velocity * time;
```

### 正弦波动
```csharp
// 标准正弦函数
y = A × sin(2π × f × t + φ)

// 增量计算（避免累积误差）
deltaY = sin(t + Δt) - sin(t)
```

### 数值稳定性
- **防除零**: `Mathf.Max(delta, 0.001f)`
- **向量长度检查**: `LengthSquared()` 避免开方
- **阈值判断**: `> 0.001f` 处理浮点精度

## 使用指南

### 基本用法
```csharp
entity.Events.Emit(GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.SineWave, new MovementParams
    {
        Mode = MoveMode.SineWave,
        ActionSpeed = 400f,
        WaveAmplitude = 60f,
        WaveFrequency = 2f,
        MaxDistance = 1000f,
        DestroyOnComplete = true,
    }));
```

### 策略选择
| 场景 | 推荐策略 | 关键参数 |
|------|----------|----------|
| 蛇形子弹 | SineWave | WaveAmplitude, WaveFrequency |
| 复杂路径 | BezierCurve | BezierPoints, MaxDuration（必须 > 0） |
| 往返攻击 | Boomerang | TargetPoint, BoomerangPauseTime |
| 突进攻击 | Charge | ActionSpeed, Acceleration |
| 护卫卫星 | Orbit | OrbitRadius, OrbitAngularSpeed |

### 性能考虑
- **OnEnter**: 一次性初始化，避免每帧重复计算
- **增量计算**: 使用差分而非积分，减少累积误差
- **对象复用**: 策略实例复用，避免频繁 GC
- **数值优化**: 使用 `LengthSquared()` 避免不必要的开方运算

## 扩展开发

### 新增策略步骤
1. 实现 `IMovementStrategy` 接口
2. 添加 `[ModuleInitializer]` 静态注册方法
3. 在 `MovementParams` 中添加所需参数
4. 更新 `MoveMode` 枚举
5. 编写单元测试

### 代码规范
- 使用中文注释
- 遵循现有命名约定
- 添加详细的 XML 文档
- 处理边界情况和数值稳定性

## 调试与优化

### 常见问题
1. **方向跳变**: 检查 OnEnter 是否正确锁定基准方向
2. **累积误差**: 确保使用增量计算而非积分
3. **数值不稳定**: 添加阈值检查和防除零保护
4. **性能问题**: 避免在 Update 中进行重量级计算

### 调试工具
- **可视化轨迹**: 绘制移动路径
- **参数监控**: 实时显示关键参数
- **性能分析**: 监控帧率和 GC 压力

---

*本文档随代码更新持续维护，最后更新时间: 2026-03-28*

### 通用标量驱动（ScalarDriver）

适用于“同一运动策略内部，某个标量参数会持续变化”的场景，例如：

- Orbit 半径在区间内往返
- Wave 振幅逐渐变大或逐渐收敛
- Wave 频率触边后反向并按 `BounceDecay` 衰减

推荐模式：保留基础值字段，再挂载对应 `ScalarDriverParams?`。

- `null` = 不启用驱动，保持基础值常量不变；非 `null` 时再由策略实例私有持有 `ScalarDriverState`。
- 更完整的职责说明、日志上下文和边界模式语义见 `../ScalarDriver/README.md`。

```csharp
new MovementParams
{
    Mode = MoveMode.SineWave,
    WaveAmplitude = 20f,
    WaveAmplitudeScalarDriver = new ScalarDriverParams
    {
        Enabled = true,
        Velocity = 40f,
        Min = 20f,
        Max = 80f,
        MinResponse = new ScalarBoundaryResponse { Mode = ScalarBoundMode.PingPong },
        MaxResponse = new ScalarBoundaryResponse
        {
            Mode = ScalarBoundMode.PingPong,
            BounceDecay = 0.8f,
        },
    },
}
```
