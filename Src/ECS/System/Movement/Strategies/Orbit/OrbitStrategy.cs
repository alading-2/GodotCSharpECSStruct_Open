using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】环绕（固定点 / 目标实体，含螺旋参数化）。
/// <para>
/// 圆心优先级：<c>TargetNode</c>（每帧实时跟随实体位置）&gt; <c>OrbitCenter</c>（固定世界坐标）。
/// 目标失效时原地暂停（不主动完成）。
/// </para>
/// <para>
/// 【角速度三选二】提供以下任意两个，自动推算第三个：
/// <c>OrbitAngularSpeed</c>（角速度）/ <c>OrbitTotalAngle</c>（总角度）/ <c>MaxDuration</c>（时间）
/// </para>
/// <para>
/// 【螺旋参数】通过 Orbit 参数即可表达螺旋（不再需要独立策略）：
/// <c>OrbitRadialSpeed</c>（径向速度）+ <c>OrbitRadialMin</c>/<c>OrbitRadialMax</c>（半径边界）。
/// </para>
/// <para>
/// <code>
/// 【使用示例 1：固定点匀速环绕，转 3 圈后完成】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///     {
///         Mode              = MoveMode.Orbit,
///         OrbitCenter       = new Vector2(400f, 300f),    // 中心点
///         OrbitRadius       = 300f,   // 半径
///         OrbitTotalAngle   = 360 * 3f,   // 总环绕角度/距离
///         MaxDuration       = 3f, // 最大持续时间
///         // OrbitAngularSpeed = 180,   // 角速度（可选）
///         // OrbitAngularAcceleration = 0f, // 角加速度（度/秒²，可选）
///         // IsOrbitClockwise  = false,  // 逆时针（可选），默认逆时针
///         // OrbitInitAngle    = 0f,     // 初始角度（可选），不设置从entity的位置推导
///         DestroyOnComplete = true,   // 完成后销毁
///         // 
///         // OrbitRadialSpeed = 100f,    // 径向速度（可选），正数向外，负数向内
///         // OrbitRadialMin = 100f,      // 最小半径（可选）
///         // OrbitRadialMax = 500f,      // 最大半径（可选）
///     }));
///
/// 【使用示例 2：3 颗卫星均匀分布，围绕目标实体环绕】
/// for (int i = 0; i < 3; i++)
///     entity.Events.Emit(GameEventType.Unit.MovementStarted,
///         new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///         {
///             Mode              = MoveMode.Orbit,
///             TargetNode        = bossNode,
///             OrbitRadius       = 300f,
///             OrbitInitAngle    = 120f * i,  // 0° / 120° / 240° 均匀分布
///             OrbitAngularSpeed = 180f,
///         DestroyOnComplete = true,   // 完成后销毁
///        }));
///
/// 【使用示例 3：从慢到快加速环绕（初始静止，逐渐加速）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///     {
///         Mode                    = MoveMode.Orbit,
///         OrbitCenter             = centerPos,
///         OrbitRadius             = 300f,
///         OrbitAngularSpeed       = 0f,              // 初始角速度为 0
///         OrbitAngularAcceleration = 60f, // 逐渐加速（度/秒²）
///         MaxDuration             = 6f,
///     }));
///
/// 【使用示例 4：由外向内螺旋收束（使用 Orbit 模式）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///     {
///         Mode              = MoveMode.Orbit,
///         OrbitCenter       = centerPos,
///         OrbitRadius       = 200f,
///         OrbitAngularSpeed = 180f,
///         OrbitRadialSpeed  = -60f,
///         OrbitRadialMin    = 0f,
///     }));
/// 【使用示例 5：由内向外螺旋扩散（使用 Orbit 模式）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///     {
///         Mode              = MoveMode.Orbit,
///         OrbitCenter       = centerPos,
///         OrbitRadius       = 20f,
///         OrbitAngularSpeed = 144f,
///         OrbitRadialSpeed  = 40f,
///         OrbitRadialMax    = 200f,
///         OrbitTotalAngle   = 720f,
///     }));
/// </code>
/// </para>
/// <para>【典型用途】护卫卫星、弹幕圆环、Boss 护盾、围绕目标的散射弹、螺旋收束/扩散轨迹。</para>
/// </summary>
public class OrbitStrategy : IMovementStrategy
{
    /// <summary>
    /// 当前所处的极角，相对于中心点的角度。OnEnter 时从 <c>OrbitInitAngle</c> 或实体位置初始化，此后每帧累加推进。
    /// 不从位置反推的原因见 <see cref="MovementHelper.OrbitStep"/> 注释。
    /// </summary>
    private float _currentAngle;

    /// <summary>当前角速度（度/秒）。OnEnter 时三选二推算，此后每帧按角加速度更新。</summary>
    private float _currentAngularSpeed;

    /// <summary>当前环绕半径（像素）。OnEnter 时取 OrbitRadius，此后按 OrbitRadialSpeed 变化，受 OrbitRadialMin/OrbitRadialMax 限制。</summary>
    private float _currentRadius;

    /// <summary>已累计的环绕角度（度），用于 OrbitTotalAngle 终止检测。</summary>
    private float _traveledAngle;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Orbit, () => new OrbitStrategy());
    }

    /// <inheritdoc/>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        // 角速度三选二推导
        _currentAngularSpeed = MovementHelper.ResolveAngularSpeed(@params);

        // 初始化半径和已走角度
        _currentRadius = @params.OrbitRadius;
        _traveledAngle = 0f;

        // 初始化极角（中心点到环绕entity的角度）：优先使用 OrbitInitAngle；否则从实体当前位置推导（避免第一帧跳变）
        Vector2 center = MovementHelper.ResolveOrbitCenter(@params) ?? node.GlobalPosition;
        if (@params.OrbitInitAngle.HasValue)
        {
            _currentAngle = @params.OrbitInitAngle.Value;
        }
        else
        {
            Vector2 toSelf = node.GlobalPosition - center;
            _currentAngle = toSelf.LengthSquared() > 0.001f ? Mathf.RadToDeg(toSelf.Angle()) : 0f;
        }
    }

    /// <inheritdoc/>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        Vector2? center = MovementHelper.ResolveOrbitCenter(@params);
        if (center == null) return MovementUpdateResult.Continue(); // TargetNode 失效：暂停但不主动完成

        // 角加速度：速度只降到 0，不反转（方向由 OrbitClockwise 控制）
        if (!Mathf.IsZeroApprox(@params.OrbitAngularAcceleration))
            _currentAngularSpeed = Mathf.Max(0f, _currentAngularSpeed + @params.OrbitAngularAcceleration * delta);

        float actualRadialSpeed = 0f;

        // 径向速度：向外扩大或向内收缩，受 OrbitRadialMin（内限）/ OrbitRadialMax（外限）双向限制
        if (!Mathf.IsZeroApprox(@params.OrbitRadialSpeed))
        {
            float previousRadius = _currentRadius;
            _currentRadius += @params.OrbitRadialSpeed * delta;
            if (@params.OrbitRadialMin >= 0f)
                _currentRadius = Mathf.Max(_currentRadius, @params.OrbitRadialMin);
            if (@params.OrbitRadialMax >= 0f)
                _currentRadius = Mathf.Min(_currentRadius, @params.OrbitRadialMax);
            _currentRadius = Mathf.Max(0f, _currentRadius); // 半径不能为负
            actualRadialSpeed = (_currentRadius - previousRadius) / Mathf.Max(delta, 0.001f);
        }

        // 核心轨道计算
        var result = MovementHelper.OrbitStep(
            node, data, @params,
            center.Value, _currentRadius, _currentAngularSpeed, actualRadialSpeed,
            ref _currentAngle, delta);

        // 总角度终止检测（OrbitTotalAngle）
        _traveledAngle += _currentAngularSpeed * delta;
        if (@params.OrbitTotalAngle >= 0f && _traveledAngle >= @params.OrbitTotalAngle)
            return MovementUpdateResult.Complete();

        return result;
    }

}
