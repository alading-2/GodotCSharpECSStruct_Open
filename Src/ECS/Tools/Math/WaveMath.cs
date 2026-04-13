using Godot;

/// <summary>
/// 波形数学工具。
/// <para>用于封装带明确语义的周期波动计算，避免将运动/波形公式散落在各个策略内部。</para>
/// <para>当前优先提供正弦波相关 API，统一遵循项目角度约定：对外相位输入使用“度”，内部再转换为弧度参与三角函数计算。</para>
/// </summary>
public static class WaveMath
{
    /// <summary>
    /// 计算标准正弦波在指定时刻的采样值。
    /// <para>标准公式：y = A × sin(2π × f × t + φ)</para>
    /// <para>其中：</para>
    /// <list type="bullet">
    /// <item><description>A：振幅 amplitude，表示最大偏移量</description></item>
    /// <item><description>f：频率 frequency，单位为“周期/秒”</description></item>
    /// <item><description>t：时间 time，单位为秒</description></item>
    /// <item><description>φ：初相位 phaseDegrees，对外使用“度”输入</description></item>
    /// </list>
    /// <para>适用场景：蛇形移动、波浪弹道、上下浮动、周期性 UI/特效位移等。</para>
    /// </summary>
    /// <param name="amplitude">振幅，决定最大偏移距离</param>
    /// <param name="frequency">频率，单位为周期/秒</param>
    /// <param name="time">采样时刻，单位为秒</param>
    /// <param name="phaseDegrees">初相位，单位为度</param>
    /// <returns>该时刻的正弦采样值</returns>
    public static float EvaluateSine(float amplitude, float frequency, float time, float phaseDegrees = 0f)
    {
        float phaseRadians = Mathf.DegToRad(phaseDegrees);
        return amplitude * Mathf.Sin(Mathf.Tau * frequency * time + phaseRadians);
    }

    /// <summary>
    /// 计算标准正弦波在指定时刻的一阶导数。
    /// <para>若 <c>y = A × sin(2π × f × t + φ)</c>，则：</para>
    /// <para><c>dy/dt = A × 2π × f × cos(2π × f × t + φ)</c></para>
    /// <para>返回值表示“横向偏移”随时间变化的瞬时速度，常用于求轨迹切线方向。</para>
    /// </summary>
    /// <param name="amplitude">振幅，决定横向偏移上限</param>
    /// <param name="frequency">频率，单位为周期/秒</param>
    /// <param name="time">采样时刻，单位为秒</param>
    /// <param name="phaseDegrees">初相位，单位为度</param>
    /// <returns>该时刻的正弦导数值（单位与 amplitude 对时间的一阶导一致）</returns>
    public static float EvaluateSineDerivative(float amplitude, float frequency, float time, float phaseDegrees = 0f)
    {
        float phaseRadians = Mathf.DegToRad(phaseDegrees);
        float angularSpeed = FrequencyToAngularSpeed(frequency);
        return amplitude * angularSpeed * Mathf.Cos(angularSpeed * time + phaseRadians);
    }

    /// <summary>
    /// 计算正弦波在两个时刻之间的偏移增量，用于SineWaveStrategy.cs。
    /// <para>等价于：EvaluateSine(toTime) - EvaluateSine(fromTime)</para>
    /// <para>适用于移动系统按“偏移差分”换算本帧横向位移的场景，可避免手动在策略里重复书写正弦公式。</para>
    /// </summary>
    /// <param name="amplitude">振幅，决定最大偏移距离</param>
    /// <param name="frequency">频率，单位为周期/秒</param>
    /// <param name="fromTime">起始时刻，单位为秒</param>
    /// <param name="toTime">结束时刻，单位为秒</param>
    /// <param name="phaseDegrees">初相位，单位为度</param>
    /// <returns>两个时刻的正弦偏移差值</returns>
    public static float EvaluateSineDelta(float amplitude, float frequency, float fromTime, float toTime, float phaseDegrees = 0f)
    {
        return EvaluateSine(amplitude, frequency, toTime, phaseDegrees)
             - EvaluateSine(amplitude, frequency, fromTime, phaseDegrees);
    }

    /// <summary>
    /// 计算正弦波轨迹在指定时刻的瞬时速度向量。
    /// <para>轨迹模型：</para>
    /// <para><c>position(t) = forward × forwardSpeed × t + side × sine(t)</c></para>
    /// <para>因此切线速度为：</para>
    /// <para><c>velocity(t) = forward × forwardSpeed + side × sine'(t)</c></para>
    /// </summary>
    /// <param name="baseDirection">基准前进方向</param>
    /// <param name="forwardSpeed">沿基准方向的前进速度（像素/秒）</param>
    /// <param name="amplitude">横向振幅</param>
    /// <param name="frequency">波形频率（周期/秒）</param>
    /// <param name="time">采样时刻（秒）</param>
    /// <param name="phaseDegrees">初相位（度）</param>
    /// <returns>该时刻的切线速度向量</returns>
    public static Vector2 EvaluateSineVelocity(
        Vector2 baseDirection,
        float forwardSpeed,
        float amplitude,
        float frequency,
        float time,
        float phaseDegrees = 0f)
    {
        if (baseDirection.LengthSquared() < 0.001f) return Vector2.Zero;

        Vector2 forward = baseDirection.Normalized();
        Vector2 side = new Vector2(-forward.Y, forward.X);
        float lateralSpeed = EvaluateSineDerivative(amplitude, frequency, time, phaseDegrees);
        return forward * forwardSpeed + side * lateralSpeed;
    }

    /// <summary>
    /// 计算正弦波轨迹在指定时刻的单位切线方向。
    /// <para>优先返回归一化后的切线速度；若瞬时速度过小，则回退到基准前进方向。</para>
    /// </summary>
    /// <param name="baseDirection">基准前进方向</param>
    /// <param name="forwardSpeed">沿基准方向的前进速度（像素/秒）</param>
    /// <param name="amplitude">横向振幅</param>
    /// <param name="frequency">波形频率（周期/秒）</param>
    /// <param name="time">采样时刻（秒）</param>
    /// <param name="phaseDegrees">初相位（度）</param>
    /// <returns>该时刻的单位切线方向</returns>
    public static Vector2 EvaluateSineTangent(
        Vector2 baseDirection,
        float forwardSpeed,
        float amplitude,
        float frequency,
        float time,
        float phaseDegrees = 0f)
    {
        Vector2 velocity = EvaluateSineVelocity(baseDirection, forwardSpeed, amplitude, frequency, time, phaseDegrees);
        if (velocity.LengthSquared() >= 0.001f) return velocity.Normalized();
        return baseDirection.LengthSquared() >= 0.001f ? baseDirection.Normalized() : Vector2.Zero;
    }

    /// <summary>
    /// 将频率（周期/秒）转换为角频率（弧度/秒）。
    /// <para>公式：ω = 2πf</para>
    /// <para>当你需要手动推导导数、角速度或与其它物理公式联动时可直接使用。</para>
    /// </summary>
    /// <param name="frequency">频率，单位为周期/秒</param>
    /// <returns>角频率，单位为弧度/秒</returns>
    public static float FrequencyToAngularSpeed(float frequency)
    {
        return Mathf.Tau * frequency;
    }
}
