using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】抛物线移动。
/// <para>
/// 该策略由起点、终点和顶高（Apex Height）通过数学公式构建单段抛物线路径。
/// 支持静态目标点或动态追踪 <c>TargetNode</c>。
/// 数学原理：
/// 1. 建立局部坐标系：将起点映射为 (0,0)，终点映射为 (L, 0)，其中 L 是起点到终点的欧氏距离。
/// 2. 在局部系下，抛物线方程为 y = ax^2 + bx + c。由于过 (0,0) 则 c=0。
/// 3. 通过顶点高度 H（在 L/2 处达到）和终点 (L,0) 求解系数 a, b。
/// 4. 匀速性质：默认的参数化（基于 x 线性增加）在斜率较大处会导致物理速度变快。本策略使用“弧长查找表（Arc Length LUT）”
///    将进度参数 [0, 1] 映射为均匀的路径长度进度，从而实现真正的匀速运动。
/// <code>
/// 【使用示例：抛物线追踪目标（终点每帧跟随 TargetNode）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Parabola, new MovementParams
///     {
///         Mode = MoveMode.Parabola,
///         MaxDuration = 2f,
///         DestroyOnComplete = true,
///         isTrackTarget = true,            // 【可选】每帧将终点更新到 TargetNode 位置
///         TargetNode = enemyNode,          // 【可选】追踪目标
///         ReachDistance = 20f,             // 【可选】到达距离阈值
///         ParabolaApexHeight = 180f,       // 必须：抛物线顶高
///         ActionSpeed = 420f,              // 【可选】沿曲线前进速度
///     }));
/// </code>
/// </para>
/// <para>【典型用途】投掷物、跳斩位移、带拱形弹道的技能特效。</para>
/// </summary>
public class ParabolaStrategy : IMovementStrategy
{
    /// <summary>默认运动速度（当 Params 未指定时）。</summary>
    private const float DefaultActionSpeed = 300f;

    /// <summary>弧长查找表缓冲区，用于实现匀速曲线运动。</summary>
    private readonly float[] _curveLut = new float[ArcLengthLut.DefaultSegments + 1];

    /// <summary>进入状态时的起始坐标。</summary>
    private Vector2 _startPoint;
    /// <summary>锁定的目标点坐标（非追踪模式下使用）。</summary>
    private Vector2 _lockedTargetPoint;
    /// <summary>静态目标下预计算的曲线。</summary>
    private Parabola2D _cachedCurve;
    /// <summary>静态目标下预计算的曲线长度。</summary>
    private float _cachedCurveLength;
    /// <summary>当前在路径上的进度参数 [0, 1]。</summary>
    private float _progress;
    /// <summary>是否已成功锁定初始目标。</summary>
    private bool _hasLockedTarget;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Parabola, () => new ParabolaStrategy());
    }

    /// <summary>
    /// 进入移动状态时的初始化逻辑。
    /// </summary>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        _startPoint = entity is Node2D node ? node.GlobalPosition : Vector2.Zero;
        _progress = 0f;
        // 尝试解析并锁定初始目标点
        _hasLockedTarget = TryResolveTargetPoint(entity as Node2D, @params, false, out _lockedTargetPoint);
        CacheStaticCurve(@params);
    }

    /// <summary>
    /// 每帧更新逻辑。
    /// <para>核心流程：</para>
    /// <list type="number">
    /// <item>确定目标点：根据追踪开关或锁定点确定本帧理论上的落点。</item>
    /// <item>到达判定：检查当前位置与目标的距离是否小于容差。</item>
    /// <item>路径计算：
    ///   <description>
    ///   如果是实时追踪模式，每帧根据当前位置和目标位置构建新的抛物线并估算长度。
    ///   如果是静态模式，使用 OnEnter 时预计算的缓存数据（包含 LUT 以保证匀速）。
    ///   </description>
    /// </item>
    /// <item>步进与求值：根据线速度计算进度增量，采样路径点并计算位移矢量。</item>
    /// </list>
    /// </summary>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        // 1. 解析当前目标（如果开启 isTrackTarget 且有 TargetNode，则目标会随之移动）
        if (!TryResolveTargetPoint(node, @params, true, out Vector2 targetPoint))
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Continue();
        }

        // 2. 距离检查：是否到达目标
        if (MovementHelper.HasReachedTarget(node.GlobalPosition, targetPoint, @params.ReachDistance))
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Complete();
        }

        float speed = @params.ActionSpeed > 0f ? @params.ActionSpeed : DefaultActionSpeed;
        if (speed <= 0.001f)
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Continue();
        }

        // 3. 特殊情况：如果顶高极小，路径退化为起点到终点的直线
        // 这通常发生在配置错误或动态计算出的高度趋近 0 时
        if (Mathf.Abs(@params.ParabolaApexHeight) <= 0.001f)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        // 判定是否使用缓存数据（静态路径下为了性能和匀速表现，优先使用缓存）
        bool useCachedArcLength = !@params.isTrackTarget && _cachedCurve.IsValid && _cachedCurveLength > 0.001f;

        // 构建或获取当前曲线实例
        Parabola2D curve = useCachedArcLength
            ? _cachedCurve
            : Parabola2D.Create(_startPoint, targetPoint, @params.ParabolaApexHeight);

        if (!curve.IsValid)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        // 获取路径总长度用于计算进度增量
        float curveLength = useCachedArcLength
            ? _cachedCurveLength
            : curve.ApproximateLength(); // 非缓存模式下通过数值估算（步进采样）获取长度

        if (curveLength <= 0.001f)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        // 4. 计算进度增量。
        // ds = speed * delta (当前帧应走过的弧长)
        // dProgress = ds / curveLength (对应的参数 t 增量)
        float progressDelta = speed * delta / curveLength;
        float nextProgress = Mathf.Clamp(_progress + progressDelta, 0f, 1f);

        // 5. 采样新位置
        // 如果有 LUT，使用基于弧长参数化的更精准采样以实现完美匀速
        Vector2 nextPoint = useCachedArcLength
            ? curve.EvaluateByArcProgress(nextProgress, _curveLut)
            : curve.Evaluate(nextProgress);

        // 计算本帧位移（相对于 Node2D 的移动）
        Vector2 displacement = nextPoint - node.GlobalPosition;
        float displacementLength = displacement.Length();

        _progress = nextProgress;

        // 6. 设置速度以供物理系统或同步逻辑使用。
        // 注意：这里推导的速度是“瞬时速度”，确保在跨帧插值时表现平滑。
        data.Set(
            DataKey.Velocity,
            displacementLength > 0.001f
                ? displacement / Mathf.Max(delta, 0.001f)
                : Vector2.Zero);

        // 采样切线方向，用于自动转向逻辑
        Vector2 facingDirection = useCachedArcLength
            ? curve.EvaluateTangentByArcProgress(nextProgress, _curveLut)
            : curve.EvaluateTangent(nextProgress);

        return MovementUpdateResult.Continue(displacementLength, facingDirection);
    }

    private void CacheStaticCurve(MovementParams @params)
    {
        _cachedCurve = default;
        _cachedCurveLength = 0f;

        if (@params.isTrackTarget || !_hasLockedTarget || Mathf.Abs(@params.ParabolaApexHeight) <= 0.001f)
        {
            return;
        }

        _cachedCurve = Parabola2D.Create(_startPoint, _lockedTargetPoint, @params.ParabolaApexHeight);
        if (!_cachedCurve.IsValid)
        {
            _cachedCurve = default;
            return;
        }

        _cachedCurveLength = _cachedCurve.BuildArcLengthTable(_curveLut);
        if (_cachedCurveLength <= 0.001f)
        {
            _cachedCurve = default;
            _cachedCurveLength = 0f;
        }
    }

    /// <summary>
    /// 解析当前应该移动到的目标坐标点。
    /// </summary>
    /// <param name="node">当前移动节点。</param>
    /// <param name="params">移动参数配置。</param>
    /// <param name="allowTracking">是否允许在本帧进行目标追踪逻辑。</param>
    /// <param name="targetPoint">解析出的目标坐标输出。</param>
    private bool TryResolveTargetPoint(Node2D? node, MovementParams @params, bool allowTracking, out Vector2 targetPoint)
    {
        // 1. 如果开启了实时追踪，优先使用节点当前位置
        if (allowTracking && @params.isTrackTarget && @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            targetPoint = @params.TargetNode.GlobalPosition;
            return true;
        }

        // 2. 如果已经锁定了初始目标点（追踪关闭或 TargetNode 已失效），使用它
        if (_hasLockedTarget)
        {
            targetPoint = _lockedTargetPoint;
            return true;
        }

        // 3. Fallback: 尝试一次性获取 TargetNode 位置
        if (@params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            targetPoint = @params.TargetNode.GlobalPosition;
            return true;
        }

        // 4. Fallback: 使用配置里的静态坐标
        if (node != null && @params.TargetPoint != Vector2.Zero)
        {
            targetPoint = @params.TargetPoint;
            return true;
        }

        targetPoint = Vector2.Zero;
        return false;
    }

    /// <summary>
    /// 退化后的匀速直线运动更新。
    /// </summary>
    private static MovementUpdateResult UpdateLinear(
        Node2D node,
        Data data,
        float delta,
        Vector2 targetPoint,
        float speed)
    {
        Vector2 toTarget = targetPoint - node.GlobalPosition;
        float distance = toTarget.Length();
        if (distance <= 0.001f)
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Complete();
        }

        float step = Mathf.Min(speed * delta, distance);
        Vector2 direction = toTarget / distance;

        // 步长除以 delta 得到物理瞬时速度
        data.Set(DataKey.Velocity, direction * (step / Mathf.Max(delta, 0.001f)));
        return MovementUpdateResult.Continue(step, direction);
    }
}
