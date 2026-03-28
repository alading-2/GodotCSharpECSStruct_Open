using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】正弦波前进。
/// <para>沿基准前进方向推进并叠加横向正弦偏移，形成蛇形弹道。OnEnter 锁定方向和速度，防止波动分量污染后续计算。</para>
/// <para>
/// <list type="bullet">
/// <item><c>ActionSpeed</c>（float，推荐）：前进速度（像素/秒）；也可不设此值，由 DataKey.Velocity 初始长度决定。</item>
/// <item><c>WaveAmplitude</c>（float，像素）：横向振幅，默认 50。</item>
/// <item><c>WaveFrequency</c>（float，周期/秒）：波动频率，默认 2。</item>
/// <item><c>WavePhase</c>（float，度，可选）：初始相位，用于错开多发同向弹道的摆动起点（内部按需转弧度）。</item>
/// <item><c>MaxDistance / MaxDuration / DestroyOnComplete</c>（可选）：不设置 = 不限制，永久运动。</item>
/// </list>
/// </para>
/// <para>
/// 方向来源（OnEnter 时一次性采样）：使用 <c>Angle</c>（度）确定基准前进方向。
/// </para>
/// <para>
/// <code>
/// 【使用示例：蛇形子弹】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.SineWave, new MovementParams
///     {
///         Mode          = MoveMode.SineWave,
///         Angle         = -45f,     // 初始前进方向（度，0=右、90=下）
///         ActionSpeed   = 300f,    // 前进速度（推荐设置）
///         WaveAmplitude = 60f,     // 横向振幅（像素）
///         WaveFrequency = 2f,      // 波动频率（周期/秒）
///         WavePhase     = 0f,      // 可选：初始相位（度）
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
    /// <summary>
    /// 基准前进方向（单位向量），在 OnEnter 时锁定，避免波动分量污染后续计算
    /// </summary>
    private Vector2 _baseDirection;

    /// <summary>
    /// 基准前进速度（像素/秒），由 ActionSpeed 或初始 Velocity 决定
    /// </summary>
    private float _baseSpeed;

    /// <summary>
    /// 模块初始化器：在模块加载时自动将此策略注册到移动策略注册表
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.SineWave, () => new SineWaveStrategy());
    }

    /// <summary>
    /// 策略进入时的初始化处理
    /// <para>主要任务：</para>
    /// <list type="bullet">
    /// <item>锁定基准前进方向：使用 Angle（度）转换为方向向量</item>
    /// <item>确定前进速度：优先使用 ActionSpeed，备选使用初始 Velocity 长度</item>
    /// </list>
    /// </summary>
    /// <param name="entity">移动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="params">移动参数</param>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        // 获取初始速度向量（用于速度 fallback）
        Vector2 initVelocity = data.Get<Vector2>(DataKey.Velocity);

        // 初始方向由 Angle（度）确定：0=右、90=下、180=左，正值顺时针
        _baseDirection = Vector2.Right.Rotated(Mathf.DegToRad(@params.Angle));

        // 确定前进速度：优先使用 ActionSpeed，fallback 使用初始速度长度
        _baseSpeed = @params.ActionSpeed > 0.001f ? @params.ActionSpeed : initVelocity.Length();
    }

    /// <summary>
    /// 每帧更新移动状态
    /// <para>计算流程：</para>
    /// <list type="bullet">
    /// <item>计算垂直于前进方向的横向向量（用于正弦偏移）</item>
    /// <item>计算当前帧和下一帧的正弦偏移值（考虑相位和频率）</item>
    /// <item>计算前进位移和横向位移的合成向量</item>
    /// <item>更新速度向量并返回位移结果</item>
    /// </list>
    /// </summary>
    /// <param name="entity">移动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="delta">帧间隔时间</param>
    /// <param name="params">移动参数</param>
    /// <returns>移动更新结果（继续/完成）</returns>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (_baseSpeed < 0.001f) return MovementUpdateResult.Continue(); // 速度过低，跳过

        // 计算垂直于前进方向的横向向量（顺时针旋转 90 度）
        Vector2 perp = new Vector2(-_baseDirection.Y, _baseDirection.X);

        // 通过统一波形数学工具计算本帧正弦偏移增量
        float sineDelta = WaveMath.EvaluateSineDelta(
            @params.WaveAmplitude,
            @params.WaveFrequency,
            @params.ElapsedTime,
            @params.ElapsedTime + delta,
            @params.WavePhase);

        // 计算前进位移（沿基准方向）
        Vector2 forwardDisp = _baseDirection * (_baseSpeed * delta);

        // 计算横向位移（正弦偏移的变化量）
        Vector2 sideDisp = perp * sineDelta;

        // 合成总位移向量
        Vector2 totalDisp = forwardDisp + sideDisp;

        // 更新速度向量（位移 / 时间）
        data.Set(DataKey.Velocity, totalDisp / Mathf.Max(delta, 0.001f));

        // 朝向取正弦轨迹在“本帧末”的切线方向，而不是简单取位移纠偏方向
        Vector2 tangentDirection = WaveMath.EvaluateSineTangent(
            _baseDirection,
            _baseSpeed,
            @params.WaveAmplitude,
            @params.WaveFrequency,
            @params.ElapsedTime + delta,
            @params.WavePhase);

        // 返回继续移动，并告知实际位移长度
        return MovementUpdateResult.Continue(totalDisp.Length(), tangentDirection);
    }
}
