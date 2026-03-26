using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 7】正弦波前进。
/// <para>沿基准前进方向推进并叠加横向正弦偏移，形成蛇形弹道。OnEnter 锁定方向，防止波动分量污染后续计算。</para>
/// <para>先写初始速度（决定前进方向和速度大小），再触发事件：
/// <list type="bullet">
/// <item><c>DataKey.Velocity</c>（Vector2，必须）：初始速度，OnEnter 时记录为基准前进方向。</item>
/// <item><c>WaveAmplitude</c>（float，像素）：横向振幅。</item>
/// <item><c>WaveFrequency</c>（float，周期/秒）：波动频率。</item>
/// <item><c>WavePhase</c>（float，弧度，可选）：初始相位，用于错开多发同向弹道的摆动起点。</item>
/// <item><c>MaxDistance / MaxDuration / DestroyOnComplete</c>（可选）</item>
/// </list>
/// </para>
/// <para>【典型用途】蛇形子弹、波浪能量束、规避预判的摆动飞行物。</para>
/// </summary>
public class SineWaveStrategy : IMovementStrategy
{
    private Vector2 _baseDirection;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.SineWave, () => new SineWaveStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        Vector2 initVelocity = data.Get<Vector2>(DataKey.Velocity);
        _baseDirection = initVelocity.LengthSquared() > 0.001f
            ? initVelocity.Normalized()
            : Vector2.Right;
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        float baseSpeed = data.Get<Vector2>(DataKey.Velocity).Length();
        if (baseSpeed < 0.001f) return MovementUpdateResult.Continue();

        Vector2 perp = new Vector2(-_baseDirection.Y, _baseDirection.X);

        float sineNew = @params.WaveAmplitude * Mathf.Sin(Mathf.Tau * @params.WaveFrequency * (@params.ElapsedTime + delta) + @params.WavePhase);
        float sineOld = @params.WaveAmplitude * Mathf.Sin(Mathf.Tau * @params.WaveFrequency * @params.ElapsedTime + @params.WavePhase);

        Vector2 forwardDisp = _baseDirection * (baseSpeed * delta);
        Vector2 sideDisp = perp * (sineNew - sineOld);
        Vector2 totalDisp = forwardDisp + sideDisp;

        data.Set(DataKey.Velocity, totalDisp / Mathf.Max(delta, 0.001f));
        return MovementUpdateResult.Continue(totalDisp.Length());
    }
}
