# ScalarDriver 说明

## 1. 这是干什么的

`ScalarDriver` 是移动系统里的**通用标量驱动层**。

它不直接移动实体，而是负责驱动“某个会随时间连续变化的标量参数”，例如：

- `OrbitRadius`：环绕半径
- `WaveAmplitude`：正弦波振幅
- `WaveFrequency`：正弦波频率

这些参数在具体策略内部，往往都需要相似能力：

- 初始值覆盖
- 速度 / 加速度
- 最小值 / 最大值
- 到边界后的响应策略
- 完成 / 冻结 / 自定义边界处理

`ScalarDriver` 把这套公共逻辑统一封装，避免每个策略都手写一份“参数随时间变化 + 越界处理”的重复代码。

---

## 2. 组成结构

当前目录下核心代码文件：

- `ScalarMotion.cs`

虽然文件名目前还是 `ScalarMotion.cs`，但其中实际对外类型已经统一为：

- `ScalarDriver`
- `ScalarDriverParams`
- `ScalarDriverState`
- `ScalarDriverStepResult`
- `ScalarBoundaryResponse`
- `ScalarBoundMode`
- `ScalarBoundaryHandler`

---

## 3. 职责边界

### `ScalarDriver` 负责什么

- 根据 `ScalarDriverParams` 推进单个标量的运行态
- 处理速度、加速度、边界检测
- 处理 `Clamp / PingPong / Wrap / Complete / Freeze / Custom`
- 生成带上下文的日志（`MoveMode + 参数名`）

### `ScalarDriver` 不负责什么

- 不直接改实体位置
- 不决定整体移动模式何时切换
- 不保存跨策略共享状态
- 不写入 `Data`

也就是说：

- **策略**决定“这个标量是什么、如何用于轨迹公式”
- **ScalarDriver**只负责“这个标量本帧应该变成多少”

---

## 4. 运行模型

### 配置层：`ScalarDriverParams`

描述一个标量如何演化：

- `InitialValue`
- `Velocity`
- `Acceleration`
- `Min / Max`
- `MinResponse / MaxResponse`
- `Enabled`

在 `MovementParams` 中，推荐将驱动字段声明为可空：

- `null` = 不启用驱动，保持基础值常量
- 非 `null` = 启用驱动，由策略实例持有运行态

例如：

- `OrbitRadiusScalarDriver`
- `WaveAmplitudeScalarDriver`
- `WaveFrequencyScalarDriver`

### 运行态：`ScalarDriverState`

由具体策略实例私有持有：

- `Value`
- `Velocity`
- `IsFrozen`
- `IsCompleted`

### 单帧结果：`ScalarDriverStepResult`

`Step()` 每帧返回：

- `Value`
- `ValueDelta`
- `Velocity`
- `HitMin`
- `HitMax`
- `IsFrozen`
- `IsCompleted`

其中 `ValueDelta` 很重要。

例如 Orbit 会把：

- `本帧半径变化量 ValueDelta`

换算成：

- `本帧径向速度`

从而参与切线朝向计算。

---

## 5. 调用方式

典型流程：

1. 策略从 `MovementParams` 读取基础值和 `ScalarDriverParams?`
2. `OnEnter()` 时创建 `ScalarDriverState`
3. `Update()` 时调用 `ScalarDriver.Step()`
4. 将返回的 `Value` / `ValueDelta` 应用于当前策略公式

示例：

```csharp
_radiusScalarDriver = @params.OrbitRadiusScalarDriver;
_radiusState = _radiusScalarDriver.HasValue
    ? ScalarDriver.CreateState(@params.OrbitRadius, _radiusScalarDriver.Value)
    : new ScalarDriverState { Value = @params.OrbitRadius, Velocity = 0f };

if (_radiusScalarDriver.HasValue)
{
    ScalarDriverParams radiusMotion = _radiusScalarDriver.Value;
    ScalarDriverStepResult radiusStep = ScalarDriver.Step(
        ref _radiusState,
        radiusMotion,
        delta,
        @params.Mode,
        nameof(MovementParams.OrbitRadiusScalarDriver));
}
```

---

## 6. 日志定位

`ScalarDriver.Step()` 要求调用方传入：

- `MoveMode mode`
- `string paramName`

这样在日志里可以明确定位：

- 是哪个移动模式
- 是哪个参数驱动过程出问题

例如：

- `[Orbit/OrbitRadiusScalarDriver]`
- `[SineWave/WaveAmplitudeScalarDriver]`
- `[SineWave/WaveFrequencyScalarDriver]`

这比只打印 `ScalarDriver` 更容易排查配置问题。

---

## 7. 边界模式语义

### `Clamp`

到边界后钳位，速度归零。
加速度仍然有效，下一帧可能重新向范围内运动。

### `Freeze`

到边界后钳位并冻结。
后续 `Step()` 不再推进，除非外部手动解除冻结。

### `Complete`

到边界后钳位并标记完成。
之后该驱动不可恢复，通常用于单程结束语义。

### `PingPong`

到边界后镜像反弹，并按 `BounceDecay` 衰减速度。
当反弹后速度低于 `StopSpeedThreshold` 时，会冻结在边界。

### `Wrap`

从一端溢出后从另一端重新进入。
适合“循环参数”语义；对半径类参数虽然可能造成瞬时跳变，但在某些玩法里仍然合法。

### `Custom`

交给调用方传入的 `ScalarBoundaryHandler` 自定义处理。
若未传入处理函数，会记录警告并退化为 `Clamp`。

---

## 8. 当前接入点

目前移动系统中已接入：

- `OrbitStrategy` → `OrbitRadiusScalarDriver`
- `SineWaveStrategy` → `WaveAmplitudeScalarDriver`
- `SineWaveStrategy` → `WaveFrequencyScalarDriver`

后续若新增策略，只要它内部存在“连续变化的单个标量参数”，优先考虑复用 `ScalarDriver`，不要重复造轮子。

---

## 9. 使用约束

- `ScalarDriverState` 由**策略实例私有持有**，不要放进 `MovementParams`
- `ScalarDriver` 仅用于**策略内部公式参数**，不要拿来保存跨系统业务状态
- `Min / Max` 的“不限制”语义统一用 `-1`
- 若 `Min > Max`，`Step()` 会报错并跳过本帧更新
- 若单帧速度过大导致两轮边界处理后仍超界，会打 `Warn` 并强制钳位

---

## 10. 阅读顺序

建议按以下顺序阅读：

1. `Src/ECS/System/Movement/MovementParams.cs`
2. `Src/ECS/System/Movement/ScalarDriver/ScalarMotion.cs`
3. `Src/ECS/System/Movement/Strategies/Orbit/OrbitStrategy.cs`
4. `Src/ECS/System/Movement/Strategies/Wave/SineWaveStrategy.cs`
5. `Src/ECS/System/Movement/Strategies/README.md`
