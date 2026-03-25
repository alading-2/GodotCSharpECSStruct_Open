using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 7】波形前进（正弦波）
/// <para>沿基础速度方向前进，叠加正弦横向偏移。适用于蛇形飞行物或不稳定的能量波。</para>
/// <para>无积累误差方案：基于时间计算新老偏移差值，帧率抖动不影响频率和振幅。</para>
/// </summary>
public class SineWaveStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.SineWave, new SineWaveStrategy());
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        Vector2 baseVelocity = data.Get<Vector2>(DataKey.Velocity);
        if (baseVelocity.LengthSquared() < 0.001f) return 0f;

        float amplitude = data.Get<float>(DataKey.WaveAmplitude);
        float frequency = data.Get<float>(DataKey.WaveFrequency);
        float phase = data.Get<float>(DataKey.WavePhase);
        float elapsed = data.Get<float>(DataKey.MoveElapsedTime);

        // 正交基：前进方向与垂直方向
        Vector2 forward = baseVelocity.Normalized();
        Vector2 perp = new Vector2(-forward.Y, forward.X);

        // 基于时间的正弦偏移差值
        float sineNew = amplitude * Mathf.Sin(Mathf.Tau * frequency * (elapsed + delta) + phase);
        float sineOld = amplitude * Mathf.Sin(Mathf.Tau * frequency * elapsed + phase);

        Vector2 forwardDisp = forward * (baseVelocity.Length() * delta);
        Vector2 sideDisp = perp * (sineNew - sineOld);
        Vector2 totalDisp = forwardDisp + sideDisp;

        // 将合成位移转换为速度，由调度器统一执行位移
        Vector2 effectiveVelocity = totalDisp / Mathf.Max(delta, 0.001f);
        data.Set(DataKey.Velocity, effectiveVelocity);

        return totalDisp.Length();
    }
}
