using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 7】正弦波前进。
/// <para>沿基准前进方向推进并叠加横向正弦偏移，形成蛇形弹道。OnEnter 锁定方向和速度，防止波动分量污染后续计算。</para>
/// <para>
/// <list type="bullet">
/// <item><c>ActionSpeed</c>（float，推荐）：前进速度（像素/秒）；也可不设此值，由 DataKey.Velocity 初始长度决定。</item>
/// <item><c>WaveAmplitude</c>（float，像素）：横向振幅，默认 50。</item>
/// <item><c>WaveFrequency</c>（float，周期/秒）：波动频率，默认 2。</item>
/// <item><c>WavePhase</c>（float，弧度，可选）：初始相位，用于错开多发同向弹道的摆动起点。</item>
/// <item><c>MaxDistance / MaxDuration / DestroyOnComplete</c>（可选）：不设置 = 不限制，永久运动。</item>
/// </list>
/// </para>
/// <para>
/// 方向来源（OnEnter 时一次性采样）：优先 <c>DataKey.Velocity</c> 初始方向，备选向右（Vector2.Right 兜底）。
/// </para>
/// <para>
/// <code>
/// 【使用示例：蛇形子弹】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.SineWave, new MovementParams
///     {
///         Mode          = MoveMode.SineWave,
///         ActionSpeed   = 400f,    // 前进速度（推荐设置）
///         WaveAmplitude = 60f,     // 横向振幅（像素）
///         WaveFrequency = 2f,      // 波动频率（周期/秒）
///         WavePhase     = 0f,      // 可选：初始相位（弧度）
///         MaxDistance   = 1000f,   // 最大移动距离
///         MaxDuration   = -1f,     // -1 不限制时长
///         DestroyOnComplete = true,
///     }));
/// </code>
/// </para>
/// <para>【典型用途】蛇形子弹、波浪能量束、规避预判的摆动飞行物。</para>
/// </summary>
public class SineWaveStrategy : IMovementStrategy
{
    private Vector2 _baseDirection;
    private float _baseSpeed;

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
        // 优先用 ActionSpeed（三选二推导后的结果），fallback 用初始 Velocity 长度
        _baseSpeed = @params.ActionSpeed > 0.001f ? @params.ActionSpeed : initVelocity.Length();
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (_baseSpeed < 0.001f) return MovementUpdateResult.Continue();

        Vector2 perp = new Vector2(-_baseDirection.Y, _baseDirection.X);

        float sineNew = @params.WaveAmplitude * Mathf.Sin(Mathf.Tau * @params.WaveFrequency * (@params.ElapsedTime + delta) + @params.WavePhase);
        float sineOld = @params.WaveAmplitude * Mathf.Sin(Mathf.Tau * @params.WaveFrequency * @params.ElapsedTime + @params.WavePhase);

        Vector2 forwardDisp = _baseDirection * (_baseSpeed * delta);
        Vector2 sideDisp = perp * (sineNew - sineOld);
        Vector2 totalDisp = forwardDisp + sideDisp;

        data.Set(DataKey.Velocity, totalDisp / Mathf.Max(delta, 0.001f));
        return MovementUpdateResult.Continue(totalDisp.Length());
    }
}
