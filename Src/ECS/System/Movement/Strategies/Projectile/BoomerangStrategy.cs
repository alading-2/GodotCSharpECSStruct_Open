using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】回旋镖（两段式弧线移动）。
/// <para>
/// 该策略模拟一个典型的回旋镖轨迹：飞出阶段（Outbound）、停顿阶段（Pause）、以及追踪返回阶段（Return）。
/// 每一段移动都基于椭圆弧（Ellipse Arc）构建，支持顺时针或逆时针旋转。
/// </para>
/// <para>
/// 核心逻辑说明：
/// 1. 三阶段状态机：
///    - **去程 (Outbound)**：从发射点沿预设弧线移向目标点。此时目标点是锁定的（Static）。
///    - **停顿 (Pause)**：到达目标点后，在原位悬停一段时间，模拟回旋镖在顶点处的速度损失。
///    - **返程 (Return)**：从当前位置回到宿主（如玩家）。此时目标点是动态追踪的（Tracking），
///      即圆弧的终点每帧随宿主移动，起点则是进入返程时的固定点。
/// 2. 匀速性质：利用 <see cref="ArcLengthLut"/> 实现弧线采样，确保沿曲线的物理线速度不因斜率变化而波动。
/// 3. 动态弧高：如果未显式配置弧高，策略会根据起终点弦长自动缩放弧高，保证在追踪移动宿主时轨迹始终自然。
/// 4. 健壮性降级：当两点距离过近无法构成有效圆弧时，自动降级为直线追逐，防止投射物卡死或跳变。
/// </para>
/// <para>
/// <code>
/// 【使用示例：飞到目标点后停顿，再沿反向弧线回到发射者】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Boomerang, new MovementParams
///     {
///         Mode = MoveMode.Boomerang,
///         TargetPoint = enemyNode.GlobalPosition,  // 设置目标点
///         TargetNode = ownerNode,                  // 设置发出回旋镖的实体，用于返程时实时跟随宿主
///         ActionSpeed = 480f,                      // 速度
///         DestroyOnComplete = true,                // 可选：返程完成后是否自动销毁实体
///         // 回旋镖参数
///         BoomerangPauseTime = 0.12f,              // 去程结束后停顿时间
///         BoomerangReturnSpeedMultiplier = 1.35f,  // 【可选】返程速度倍率
///         BoomerangArcHeight = 140f,               // 固定弧高；不配则自动估算
///         BoomerangIsClockwise = true,             // 回旋镖方向：true：顺时针，false：逆时针
///     }));
/// </code>
/// </para>
/// <para>【典型用途】回旋镖武器、飞出再回收的刀片/飞斧、需要回到施法者身边的技能投射物。</para>
/// </summary>
public class BoomerangStrategy : IMovementStrategy
{
    /// <summary>默认到达判定阈值（像素），平衡“完全重合”与“视觉到达”。</summary>
    private const float DefaultReachDistance = 8f;
    /// <summary>未配置基础速度时的兜底速度。</summary>
    private const float DefaultActionSpeed = 300f;
    /// <summary>自动估算弧高时采用的比例（弧高 = 弦长 * 0.35）。</summary>
    private const float DefaultArcHeightRatio = 0.35f;
    /// <summary>自动弧高的下限，确保短距离也有明显的曲线感。</summary>
    private const float MinArcHeight = 24f;
    /// <summary>自动弧高的上限，防止长距离下曲线跨度过大。</summary>
    private const float MaxAutoArcHeight = 220f;

    /// <summary>
    /// 弧长分布查找表（LUT）。通过将弧线等分为若干段并离散采样长度，
    /// 用于将“线性进度”映射到“曲线采样参数 t”，从而实现沿轨迹的匀速运动。
    /// </summary>
    private readonly float[] _curveLut = new float[ArcLengthLut.DefaultSegments + 1];

    /// <summary>发射时的初始全球坐标。作为返程找不到宿主时的兜底终点。</summary>
    private Vector2 _launchPoint;
    /// <summary>当前运动阶段（去程/返程）的起始位置。</summary>
    private Vector2 _phaseStartPoint;
    /// <summary>去程阶段预计算的曲线。</summary>
    private EllipseArc2D _cachedPhaseCurve;
    /// <summary>去程阶段预计算的曲线长度。</summary>
    private float _cachedPhaseCurveLength;
    /// <summary>去程锁定的目标点。在 OnEnter 时固定，不受发射过程中目标 Node 移动的影响。</summary>
    private Vector2 _outboundTargetPoint;
    /// <summary>宿主实体（通常是玩家）。用于返程阶段的实时位置跟随。</summary>
    private Node2D? _ownerEntity;
    /// <summary>标识当前是否处于返程（收回）阶段。</summary>
    private bool _returning;
    /// <summary>悬停计时器。当到达去程终点后，会在此倒计时，结束后开始返程。</summary>
    private float _pauseTimer;
    /// <summary>当前阶段在轨迹上的归一化进度 [0,1]。配合 LUT 实现匀速推进。</summary>
    private float _phaseProgress;

    public bool CanBeInterrupted => false;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Boomerang, () => new BoomerangStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        // 重置所有内部状态。由于策略实例可能被池化或复用，必须确保没有上一轮的残留。
        _launchPoint = Vector2.Zero;
        _phaseStartPoint = Vector2.Zero;
        _cachedPhaseCurve = default;
        _cachedPhaseCurveLength = 0f;
        _outboundTargetPoint = @params.TargetPoint;
        _ownerEntity = null;

        if (entity is Node2D node)
        {
            _launchPoint = node.GlobalPosition;
            _phaseStartPoint = _launchPoint;

            // 优先使用显式指定的 TargetNode（需满足 IUnit 接口）作为返程目标；若未指定提供，则自动回溯查找层级宿主。
            if (@params.TargetNode != null)
            {
                _ownerEntity = @params.TargetNode;
            }
            else
            {
                _ownerEntity = EntityRelationshipManager.FindAncestorOfType<Node2D>(node);
            }
        }

        _returning = false;
        _pauseTimer = 0f;
        _phaseProgress = 0f;
        CacheOutboundCurve(@params);
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        // --- 阶段处理 1: 停顿挂起 ---
        // 发生在去程刚结束，返程尚未开始的间隙
        if (_pauseTimer > 0f)
        {
            _pauseTimer = Mathf.Max(_pauseTimer - delta, 0f);
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Continue();
        }

        // --- 阶段处理 2: 目标点解析 ---
        // 根据当前是去程还是返程，确定逻辑上的落点
        Vector2 phaseEndPoint = ResolvePhaseEndPoint();

        // 优先检查是否已经物理到达目标（通过 ReachDistance 容差）
        if (MovementHelper.HasReachedTarget(node.GlobalPosition, phaseEndPoint, @params.ReachDistance, DefaultReachDistance))
        {
            return HandlePhaseArrival(node, data, @params);
        }

        // --- 阶段处理 3: 运动轨道参数计算 ---
        float speed = ResolveCurrentSpeed(@params);
        if (speed <= 0.001f)
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Continue();
        }

        // 计算当前阶段的弦长与弧高（可能是动态的，随宿主位置实时变化）。
        float chordLength = _phaseStartPoint.DistanceTo(phaseEndPoint);
        float arcHeight = ResolveArcHeight(chordLength, @params);

        // 决定弯曲方向。返程时取反，使内外弧线形成左右对称的椭圆感路径。
        bool clockwise = _returning ? !@params.BoomerangIsClockwise : @params.BoomerangIsClockwise;

        // 缓存策略：去程是静态点，可以使用 OnEnter 时预算的缓存；返程因为目标实时移动，必须实时构建
        bool useCachedArcLength = !_returning && _cachedPhaseCurve.IsValid && _cachedPhaseCurveLength > 0.001f;

        EllipseArc2D curve = useCachedArcLength
            ? _cachedPhaseCurve
            : EllipseArc2D.Create(_phaseStartPoint, phaseEndPoint, arcHeight, clockwise);

        // 稳定性退化处理：如果弧线构造失败（由于目标太近）或高度太低（退化为直线），回退到线性追逐，防止物体卡死。
        if (!curve.IsValid || arcHeight <= 0.001f)
        {
            return UpdateLinear(node, data, delta, @params, phaseEndPoint, speed);
        }

        float curveLength = useCachedArcLength
            ? _cachedPhaseCurveLength
            : curve.ApproximateLength(); // 虽然是实时构建，但 EllipseArc2D.ApproximateLength 求值极快

        if (curveLength <= 0.001f)
        {
            return UpdateLinear(node, data, delta, @params, phaseEndPoint, speed);
        }

        // 计算这一帧应该推进的进度比例。
        // speed * delta 是本帧移动的物理长度，除以总曲线长度得到归一化步长。
        float progressDelta = speed * delta / curveLength;
        float nextProgress = Mathf.Clamp(_phaseProgress + progressDelta, 0f, 1f);

        Vector2 nextPoint = useCachedArcLength
            ? curve.EvaluateByArcProgress(nextProgress, _curveLut)
            : curve.Evaluate(nextProgress);

        Vector2 displacement = nextPoint - node.GlobalPosition;
        float displacementLength = displacement.Length();

        _phaseProgress = nextProgress;

        // 根据位移计算当前物理速度，写入 Data 供移动系统消费。
        data.Set(
            DataKey.Velocity,
            displacementLength > 0.001f
                ? displacement / Mathf.Max(delta, 0.001f)
                : Vector2.Zero);

        // 采样切线，用于处理投射物的旋转朝向
        Vector2 facingDirection = useCachedArcLength
            ? curve.EvaluateTangentByArcProgress(nextProgress, _curveLut)
            : curve.EvaluateTangent(nextProgress);

        return MovementUpdateResult.Continue(displacementLength, facingDirection);
    }

    /// <summary>
    /// 直线退化更新逻辑。
    /// <para>
    /// 当椭圆弧工具无法创建有效曲线（如距离太近）或弧高过小时调用。
    /// 确保在极端条件下，回旋镖仍能以直线平衡地飞向目标，保证逻辑闭环。
    /// </para>
    /// </summary>
    private MovementUpdateResult UpdateLinear(
        Node2D node,
        Data data,
        float delta,
        MovementParams @params,
        Vector2 phaseEndPoint,
        float speed)
    {
        // 计算目标朝向向量与剩余距离。
        Vector2 toTarget = phaseEndPoint - node.GlobalPosition;
        float distance = toTarget.Length();

        // 数值溢出保护：若已极度接近目标，停止位移并触发阶段切换。
        if (distance <= 0.001f)
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return HandlePhaseArrival(node, data, @params);
        }

        // 步长限制：确保本帧位移不会超过剩余总距离（防止过冲/抖动）。
        float step = Mathf.Min(speed * delta, distance);
        Vector2 direction = toTarget / distance;

        // 速度转换：将计算出的“期望位移”转换为 Velocity，交由 EntityMovementComponent 执行最终移动。
        data.Set(DataKey.Velocity, direction * (step / Mathf.Max(delta, 0.001f)));

        // 返回本帧产生的位移大小与朝向。
        return MovementUpdateResult.Continue(step, direction);
    }

    /// <summary>
    /// 处理阶段到达与切换。
    /// <para>
    /// 当去程结束时，启动停顿计时器并标记返程，同时锁定当前位置作为返程起点。
    /// 当返程结束时，彻底标记移动任务完成。
    /// </para>
    /// </summary>
    private MovementUpdateResult HandlePhaseArrival(Node2D node, Data data, MovementParams @params)
    {
        data.Set(DataKey.Velocity, Vector2.Zero);

        if (!_returning)
        {
            // 去程阶段结束：切换为返程标识。
            // 此时记录当前位置为返程曲线的新起点，并根据配置开启停顿计时。
            _returning = true;
            _pauseTimer = Mathf.Max(@params.BoomerangPauseTime, 0f);
            _phaseStartPoint = node.GlobalPosition;
            _phaseProgress = 0f;
            _cachedPhaseCurve = default;
            _cachedPhaseCurveLength = 0f;
            // 继续执行，因为切换阶段后下一帧可能直接进入悬停或开始回飞。
            return MovementUpdateResult.Continue();
        }

        // 返程阶段结束：整个回旋镖运动流完成。
        return MovementUpdateResult.Complete();
    }

    /// <summary>
    /// 确定当前阶段的终点位置。
    /// </summary>
    private Vector2 ResolvePhaseEndPoint()
    {
        // 去程终点已固定在 _outboundTargetPoint。
        if (!_returning) return _outboundTargetPoint;

        // 返程终点尝试实时锁定宿主（发射者）位置。
        // 如果宿主已经销毁或不在树中，回退到最初的发射点，避免物体飞向 (0,0)。
        return (_ownerEntity as Node2D) is { } ownerNode && ownerNode.IsInsideTree()
            ? ownerNode.GlobalPosition
            : _launchPoint;
    }

    /// <summary>
    /// 计算本帧的移动速度。
    /// </summary>
    private float ResolveCurrentSpeed(MovementParams @params)
    {
        float speed = @params.ActionSpeed > 0f ? @params.ActionSpeed : DefaultActionSpeed;
        if (!_returning) return speed;

        // 返程支持特定的速度倍率，通常为了“回收感”会设置得比去程更快。
        float multiplier = @params.BoomerangReturnSpeedMultiplier > 0f
            ? @params.BoomerangReturnSpeedMultiplier
            : 1f;
        return speed * multiplier;
    }

    /// <summary>
    /// 根据当前弦长计算弧高。
    /// </summary>
    private float ResolveArcHeight(float chordLength, MovementParams @params)
    {
        // 优先使用显式配置。
        if (@params.BoomerangArcHeight > 0f)
        {
            return @params.BoomerangArcHeight;
        }

        // 默认算法：弧高与当前弦长成正比（在限制范围内）。
        // 这样即使宿主在移动，回旋路径也会根据剩余距离自动收放。
        float autoArcHeight = chordLength * DefaultArcHeightRatio;
        return Mathf.Clamp(autoArcHeight, MinArcHeight, MaxAutoArcHeight);
    }

    private void CacheOutboundCurve(MovementParams @params)
    {
        float chordLength = _phaseStartPoint.DistanceTo(_outboundTargetPoint);
        float arcHeight = ResolveArcHeight(chordLength, @params);
        if (arcHeight <= 0.001f)
        {
            _cachedPhaseCurve = default;
            _cachedPhaseCurveLength = 0f;
            return;
        }

        _cachedPhaseCurve = EllipseArc2D.Create(_phaseStartPoint, _outboundTargetPoint, arcHeight, @params.BoomerangIsClockwise);
        if (!_cachedPhaseCurve.IsValid)
        {
            _cachedPhaseCurve = default;
            _cachedPhaseCurveLength = 0f;
            return;
        }

        _cachedPhaseCurveLength = _cachedPhaseCurve.BuildArcLengthTable(_curveLut);
        if (_cachedPhaseCurveLength <= 0.001f)
        {
            _cachedPhaseCurve = default;
            _cachedPhaseCurveLength = 0f;
        }
    }
}
