using Godot;

/// <summary>
/// 通用标量参数边界模式。
/// </summary>
public enum ScalarBoundMode
{
    /// <summary>无限制，允许值超出范围</summary>
    None = 0,
    /// <summary>
    /// 钳位停止。到达边界时速度归零，停留在边界。
    /// <br/>注意：加速度仍然有效；若加速度方向朝向范围内，下一帧速度可重新积累并自然离开边界（物理弹性行为）。
    /// <br/>适用：速度上限、转角限制等"软边界"，或"到达边界后在外力驱动下自动反向"的场景。
    /// </summary>
    Clamp = 1,
    /// <summary>反弹模式（乒乓）。速度方向反转，支持衰减系数和停机阈值。</summary>
    PingPong = 2,
    /// <summary>环绕模式。从一端溢出后从另一端重新进入（如半径从100-0，再从100-0）。</summary>
    Wrap = 3,
    /// <summary>
    /// 到达即完成。钳位并标记 IsCompleted，后续 Step 不再工作且不可恢复。
    /// <br/>区别于 Freeze（可外部解冻）：Complete 是单程运动的最终终点，通常触发销毁或状态切换。
    /// <br/>适用：螺旋线到最大半径、振幅渐增到目标值等"单次到达终点"场景。
    /// </summary>
    Complete = 4,
    /// <summary>
    /// 冻结等待。钳位并标记 IsFrozen，后续 Step 完全停止（加速度也忽略）。需外部将 IsFrozen 置 false 才能恢复。
    /// <br/>区别于 Complete（不可恢复）：Freeze 是临时暂停，可被外部事件解除。
    /// <br/>区别于 Clamp（加速度仍有效）：Freeze 在解冻前完全冻住一切更新。
    /// <br/>适用：充能到满后等待玩家操作、分阶段运动的"阶段间暂停"。
    /// </summary>
    Freeze = 5,
    /// <summary>
    /// 自定义处理。调用外部传入的边界处理委托，实现任意自定义逻辑。
    /// <br/>配置方式：将 MinResponse.Mode 或 MaxResponse.Mode 设为 Custom，
    /// 并在调用 Step 时传入对应的 onMinBoundary / onMaxBoundary 委托。
    /// </summary>
    Custom = 6,
}

/// <summary>
/// 单侧边界响应配置。
/// </summary>
public record struct ScalarBoundaryResponse
{
    /// <summary>显式无参构造函数（C# CS8983：带属性初始化值的 struct 必须声明显式构造函数）</summary>
    public ScalarBoundaryResponse() { }

    /// <summary>边界响应模式</summary>
    public ScalarBoundMode Mode { get; init; } = ScalarBoundMode.Clamp;

    /// <summary>
    /// 反弹衰减系数，仅在 <c>PingPong</c> 模式下生效。
    /// 1 = 不衰减；0.5 = 每次触边后速度减半。
    /// </summary>
    public float BounceDecay { get; init; } = 1f;

    /// <summary>
    /// 反弹后的停机阈值；当新速度绝对值小于该值时，参数将停在边界并冻结。
    /// </summary>
    public float StopSpeedThreshold { get; init; } = 0.01f;
}

/// <summary>
/// 自定义边界处理委托。<see cref="ScalarBoundMode.Custom"/> 触发时由 Step 调用。
/// </summary>
/// <param name="boundary">触发的边界值（Min 或 Max）。</param>
/// <param name="overshoot">越界量（始终为正数）。</param>
/// <param name="value">当前标量值（ref，可直接修改为期望值）。</param>
/// <param name="velocity">当前速度（ref，可直接修改）。</param>
public delegate void ScalarBoundaryHandler(float boundary, float overshoot, ref float value, ref float velocity);

/// <summary>
/// 通用标量参数演化配置。
/// <para>
/// 典型用途：Orbit 半径、Wave 振幅、Wave 频率等在同一运动策略内部随时间变化的标量参数。
/// </para>
/// </summary>
public record struct ScalarDriverParams
{
    /// <summary>显式无参构造函数（C# CS8983：带属性初始化值的 struct 必须声明显式构造函数）</summary>
    public ScalarDriverParams() { }

    /// <summary>禁用态配置（通常仅用于显式覆盖默认启用行为）</summary>
    public static ScalarDriverParams Disabled => new() { Enabled = false };

    /// <summary>
    /// 是否启用该标量驱动；默认 <c>true</c>。
    /// 对应 MovementParams 中驱动字段为 <c>null</c> 时，策略层不会调用驱动。
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// 初始值；null = 继承对应基础字段（如 <c>OrbitRadius</c> / <c>WaveAmplitude</c> / <c>WaveFrequency</c>）。
    /// </summary>
    public float? InitialValue { get; init; } = null;

    /// <summary>值变化速度，表示每秒变化量</summary>
    public float Velocity { get; init; } = 0f;

    /// <summary>值变化加速度，表示每秒速度变化量</summary>
    public float Acceleration { get; init; } = 0f;

    /// <summary>最小值，-1 = 不限制</summary>
    public float Min { get; init; } = -1f;

    /// <summary>最大值，-1 = 不限制</summary>
    public float Max { get; init; } = -1f;

    /// <summary>到达最小边界时的响应</summary>
    public ScalarBoundaryResponse MinResponse { get; init; } = new();

    /// <summary>到达最大边界时的响应</summary>
    public ScalarBoundaryResponse MaxResponse { get; init; } = new();
}

/// <summary>
/// 通用标量参数运行时状态，由具体策略实例私有持有。
/// </summary>
public struct ScalarDriverState
{
    /// <summary>值：径向速度、振幅、频率</summary>
    public float Value;

    /// <summary>值的变化速度</summary>
    public float Velocity;

    /// <summary>是否冻结</summary>
    public bool IsFrozen;

    /// <summary>是否完成</summary>
    public bool IsCompleted;
}

/// <summary>
/// 单帧标量参数步进结果快照。
/// </summary>
public readonly struct ScalarDriverStepResult
{
    /// <summary>更新后的当前值</summary>
    public float Value { get; }
    /// <summary>本帧产生的值变化量，比如这一帧中半径从5.1变成4.8，ValueDelta = 0.3</summary>
    public float ValueDelta { get; }
    /// <summary>更新后的当前速度</summary>
    public float Velocity { get; }
    /// <summary>是否触碰了最小边界</summary>
    public bool HitMin { get; }
    /// <summary>是否触碰了最大边界</summary>
    public bool HitMax { get; }
    /// <summary>当前是否处于冻结状态</summary>
    public bool IsFrozen { get; }
    /// <summary>当前是否处于完成状态</summary>
    public bool IsCompleted { get; }

    public ScalarDriverStepResult(
        float value,
        float valueDelta,
        float velocity,
        bool hitMin,
        bool hitMax,
        bool isFrozen,
        bool isCompleted)
    {
        Value = value;
        ValueDelta = valueDelta;
        Velocity = velocity;
        HitMin = hitMin;
        HitMax = hitMax;
        IsFrozen = isFrozen;
        IsCompleted = isCompleted;
    }
}

/// <summary>
/// 通用标量驱动工具。
/// <para>
/// 用于驱动随时间演化的标量值（如半径、频率、波幅等），支持加速度、边界限制及多种边界响应模式。
/// </para>
/// </summary>
public static class ScalarDriver
{
    private static readonly Log Log = new Log("ScalarDriver");

    /// <summary>
    /// 使用基础值和运动配置初始化运行时状态。
    /// </summary>
    /// <param name="baseValue">如果配置未指定初始值，则使用的默认基础值。</param>
    /// <param name="motion">标量运动配置参数。</param>
    /// <returns>初始化后的运行时状态对象。</returns>
    public static ScalarDriverState CreateState(float baseValue, ScalarDriverParams motion)
    {
        return new ScalarDriverState
        {
            // 优先使用配置中的初始值，否则继承基础值
            Value = motion.InitialValue ?? baseValue,
            Velocity = motion.Velocity,
            IsFrozen = false,
            IsCompleted = false,
        };
    }

    /// <summary>
    /// 推进标量参数的运行时状态（单帧步进）。
    /// <para>
    /// 核心逻辑：应用加速度 -> 计算新值 -> 两轮顺序边界处理（先 Min 后 Max）-> 兜底安全检查。
    /// </para>
    /// </summary>
    /// <param name="state">当前运行时状态（ref，将被更新）。</param>
    /// <param name="motion">标量驱动配置。</param>
    /// <param name="delta">帧间隔时间（秒）。</param>
    /// <param name="mode">调用方的移动模式（用于日志定位，如 MoveMode.Orbit）。</param>
    /// <param name="paramName">被驱动的参数名（建议用 nameof，如 nameof(MovementParams.OrbitRadiusScalarDriver)）。</param>
    /// <param name="onMinBoundary">
    /// 最小值边界的自定义处理委托（仅在 MinResponse.Mode == Custom 时调用）。
    /// </param>
    /// <param name="onMaxBoundary">
    /// 最大值边界的自定义处理委托（仅在 MaxResponse.Mode == Custom 时调用）。
    /// </param>
    /// <returns>包含详细步进信息的快照记录。</returns>
    public static ScalarDriverStepResult Step(
        ref ScalarDriverState state,
        in ScalarDriverParams motion,
        float delta,
        MoveMode mode,
        string paramName,
        ScalarBoundaryHandler? onMinBoundary = null,
        ScalarBoundaryHandler? onMaxBoundary = null)
    {
        float previousValue = state.Value;

        // 预检：未启用、已完成、已冻结或无效 delta 时直接返回当前状态
        if (!motion.Enabled || state.IsCompleted || state.IsFrozen || delta <= 0f)
        {
            return new ScalarDriverStepResult(
                state.Value,
                0f,
                state.Velocity,
                false,
                false,
                state.IsFrozen,
                state.IsCompleted);
        }

        bool hasMin = motion.Min >= 0f;
        bool hasMax = motion.Max >= 0f;

        string ctx = $"[{mode}/{paramName}]";

        // Min > Max 非法配置：直接报错并跳过本帧，不做任何修复
        if (hasMin && hasMax && motion.Min > motion.Max)
        {
            Log.Error($"{ctx} 非法配置：Min({motion.Min}) > Max({motion.Max})，跳过本帧更新。");
            return new ScalarDriverStepResult(
                state.Value, 0f, state.Velocity, false, false, state.IsFrozen, state.IsCompleted);
        }

        // v = v0 + a*dt，x = x0 + v*dt
        float velocity = state.Velocity + motion.Acceleration * delta;
        float nextValue = state.Value + velocity * delta;

        bool hitMin = false;
        bool hitMax = false;
        bool isFrozen = false;
        bool isCompleted = false;

        // 第一轮：处理最小值越界
        if (hasMin && nextValue < motion.Min)
        {
            hitMin = true;
            ApplyBoundary(motion.Min, motion.Max, motion.Min,
                ref nextValue, ref velocity,
                motion.MinResponse, isMinBoundary: true,
                ref isFrozen, ref isCompleted, onMinBoundary, ctx);
        }

        // 第二轮：处理最大值越界（PingPong 从 min 反弹后可能落在 max 外）
        if (hasMax && nextValue > motion.Max)
        {
            hitMax = true;
            ApplyBoundary(motion.Min, motion.Max, motion.Max,
                ref nextValue, ref velocity,
                motion.MaxResponse, isMinBoundary: false,
                ref isFrozen, ref isCompleted, onMaxBoundary, ctx);
        }

        // 兜底安全检查：两轮处理后仍超界，说明单帧位移超过范围宽度（配置的速度过大）
        // 这是配置问题，打警告提示，强制钳位
        if (motion.MinResponse.Mode != ScalarBoundMode.None && hasMin && nextValue < motion.Min)
        {
            Log.Warn($"{ctx} 两轮边界处理后仍小于 Min({motion.Min})，强制钳位。请检查 Velocity 配置（单帧位移超过范围宽度）。");
            nextValue = motion.Min;
            velocity = 0f;
        }
        else if (motion.MaxResponse.Mode != ScalarBoundMode.None && hasMax && nextValue > motion.Max)
        {
            Log.Warn($"{ctx} 两轮边界处理后仍大于 Max({motion.Max})，强制钳位。请检查 Velocity 配置（单帧位移超过范围宽度）。");
            nextValue = motion.Max;
            velocity = 0f;
        }

        // 更新运行时状态
        state.Value = nextValue;
        state.Velocity = velocity;
        state.IsFrozen = isFrozen;
        state.IsCompleted = isCompleted;

        return new ScalarDriverStepResult(
            nextValue,
            nextValue - previousValue,
            velocity,
            hitMin,
            hitMax,
            isFrozen,
            isCompleted);
    }

    /// <summary>
    /// 根据配置的响应模式处理边界碰撞逻辑。
    /// </summary>
    /// <param name="min">配置的整体最小值限制。</param>
    /// <param name="max">配置的整体最大值限制。</param>
    /// <param name="boundary">当前触发碰撞的具体边界值（min 或 max）。</param>
    /// <param name="value">当前待更新的标量值（ref）。</param>
    /// <param name="velocity">当前速度（ref）。</param>
    /// <param name="response">对应的边界响应配置。</param>
    /// <param name="isMinBoundary">是否为最小边界碰撞。</param>
    /// <param name="isFrozen">是否触发冻结状态（ref）。</param>
    /// <param name="isCompleted">是否触发完成状态（ref）。</param>
    /// <param name="customHandler">Custom 模式下的自定义处理委托（可为 null）。</param>
    /// <param name="ctx">日志上下文前缀（格式：[Mode/ParamName]），由 Step 构造后传入。</param>
    private static void ApplyBoundary(
        float min,
        float max,
        float boundary,
        ref float value,
        ref float velocity,
        in ScalarBoundaryResponse response,
        bool isMinBoundary,
        ref bool isFrozen,
        ref bool isCompleted,
        ScalarBoundaryHandler? customHandler,
        string ctx)
    {
        float overshoot = isMinBoundary ? boundary - value : value - boundary;

        switch (response.Mode)
        {
            case ScalarBoundMode.None:
                // 不做任何响应，允许值超出范围
                return;

            case ScalarBoundMode.Clamp:
                value = boundary;
                velocity = 0f;
                return;

            case ScalarBoundMode.Freeze:
                // 钳位并冻结状态，除非通过外部手段解除 IsFrozen，否则后续 Step 将不再工作
                value = boundary;
                velocity = 0f;
                isFrozen = true;
                return;

            case ScalarBoundMode.Complete:
                // 标记为已完成。常用于单次运动，后续不再更新
                value = boundary;
                velocity = 0f;
                isCompleted = true;
                return;

            case ScalarBoundMode.Wrap:
                // 循环环绕模式。从一端溢出后，从另一端重新进入（例如 半径100-0再从100-0）
                if (min >= 0f && max >= 0f && max > min)
                    value = isMinBoundary ? max - overshoot : min + overshoot;
                else
                    value = boundary;
                return;

            case ScalarBoundMode.PingPong:
                // 镜像反弹模式。计算反弹后的位置，并根据衰减系数反转速度
                value = isMinBoundary ? boundary + overshoot : boundary - overshoot;
                velocity = -velocity * Mathf.Clamp(response.BounceDecay, 0f, 1f);

                // 如果反弹后速度低于停机阈值，则直接停留在边界并冻结
                if (Mathf.Abs(velocity) < response.StopSpeedThreshold)
                {
                    value = boundary;
                    velocity = 0f;
                    isFrozen = true;
                }
                return;

            case ScalarBoundMode.Custom:
                if (customHandler != null)
                    customHandler(boundary, overshoot, ref value, ref velocity);
                else
                {
                    Log.Warn($"{ctx} Custom 模式未传入处理委托，退化为 Clamp。");
                    value = boundary;

                    velocity = 0f;
                }
                return;

            default:
                value = boundary;
                velocity = 0f;
                return;
        }
    }
}
