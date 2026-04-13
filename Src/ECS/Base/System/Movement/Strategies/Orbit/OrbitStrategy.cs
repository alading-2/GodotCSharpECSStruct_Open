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
/// 【螺旋参数】通过 <c>OrbitRadiusScalarDriver</c> 即可表达螺旋（不再需要独立策略）。
/// <c>OrbitRadiusScalarDriver = null</c> 时不驱动半径，保持 <c>OrbitRadius</c> 常量。
/// </para>
/// <para>
/// <code>
/// 【使用示例 1：固定点匀速环绕，转 3 圈后完成】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///     {
///         Mode              = MoveMode.Orbit,
///         OrbitCenter       = new Vector2(400f, 300f),    // 中心点
///         OrbitTotalAngle   = 360 * 3f,   // 总环绕角度/距离
///         MaxDuration       = 3f, // 最大持续时间
///         DestroyOnComplete = true,   // 完成后销毁
///         OrbitRadius       = 300f,   // 半径
///         [可选]OrbitRadiusScalarDriver = new ScalarDriverParams
///         {
///             Velocity = 100f,
///             Min = 100f,
///             Max = 500f,
///             MaxResponse = new ScalarBoundaryResponse
///             {
///                 Mode = ScalarBoundMode.PingPong,
///                 BounceDecay = 0.75f,
///             },
///         },
///         [可选]OrbitAngularSpeed = 180f,   // 角速度（可选）
///         [可选]OrbitAngularAcceleration = 0f, // 角加速度（度/秒²，可选）
///         [可选]IsOrbitClockwise  = false,  // 逆时针（可选），默认逆时针
///         [可选]OrbitInitAngle    = 0f,     // 初始角度（可选），不设置从entity的位置推导
///     }));
///
/// 【使用示例 2：3 颗卫星均匀分布，围绕目标实体环绕】
/// for (int i = 0; i < 3; i++)
///     entity.Events.Emit(GameEventType.Unit.MovementStarted,
///         new GameEventType.Unit.MovementStartedEventData(MoveMode.Orbit, new MovementParams
///         {
///             Mode              = MoveMode.Orbit, // 环绕模式
///             DestroyOnComplete = true,   // 完成后销毁
///             TargetNode        = bossNode, // 环绕中心目标实体
///             OrbitRadius       = 300f, // 半径
///             OrbitInitAngle    = 120f * i,  // 0° / 120° / 240° 均匀分布
///             OrbitAngularSpeed = 180f, // 角速度
///        }));
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

    /// <summary>环绕半径运行态。OnEnter 时由 <c>OrbitRadius</c> 或 <c>OrbitRadiusScalarDriver.InitialValue</c> 初始化。</summary>
    private ScalarDriverState _radiusState;

    /// <summary>本次环绕半径驱动配置。<c>null</c> = 不驱动，半径保持常量。</summary>
    private ScalarDriverParams? _radiusScalarDriver;

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
        _radiusScalarDriver = @params.OrbitRadiusScalarDriver;
        _radiusState = _radiusScalarDriver.HasValue
            ? ScalarDriver.CreateState(@params.OrbitRadius, _radiusScalarDriver.Value)
            : new ScalarDriverState { Value = @params.OrbitRadius, Velocity = 0f };
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

        ScalarDriverStepResult radiusStep = default;
        if (_radiusScalarDriver.HasValue)
        {
            ScalarDriverParams radiusMotion = _radiusScalarDriver.Value;
            radiusStep = ScalarDriver.Step(ref _radiusState, radiusMotion, delta,
                @params.Mode, nameof(MovementParams.OrbitRadiusScalarDriver));
        }

        // 轨道半径始终约束为 >= 0，避免负半径导致轨道点翻转
        _radiusState.Value = Mathf.Max(0f, _radiusState.Value);

        // 将“本帧半径变化量”换算为“本帧径向速度（像素/秒）”。
        // - ValueDelta = newRadius - oldRadius，保留符号：外扩为正、内收为负。
        // - OrbitStep 需要径向速度来计算螺旋轨迹切线朝向；仅有当前半径值（半径永远是正）无法表达这一帧是向内还是向外。
        // - 未配置半径驱动时 radiusStep 为 default，ValueDelta = 0，表示本帧无径向变化。
        float actualRadialSpeed = radiusStep.ValueDelta / Mathf.Max(delta, 0.001f);

        // 核心轨道计算
        var result = MovementHelper.OrbitStep(
            node, data, @params,
            center.Value, _radiusState.Value, _currentAngularSpeed, actualRadialSpeed,
            ref _currentAngle, delta);

        // 仅当启用了半径驱动且驱动器触发 Complete 时，提前结束 Orbit。
        if (_radiusScalarDriver.HasValue && radiusStep.IsCompleted)
            return MovementUpdateResult.Complete();

        // 总角度终止检测（OrbitTotalAngle）
        _traveledAngle += _currentAngularSpeed * delta;
        if (@params.OrbitTotalAngle >= 0f && _traveledAngle >= @params.OrbitTotalAngle)
            return MovementUpdateResult.Complete();

        return result;
    }

}
