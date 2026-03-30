using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】圆弧移动。
/// <para>
/// 在起点和终点之间构建一段指定半径的圆弧路径。支持顺时针/逆时针选择。
/// </para>
/// <para>
/// 数学原理：
/// 1. 圆心解析：给定起点 A、终点 B 和半径 R。圆心 C 必然位于 AB 的中垂线上，且与 AB 的中点距离为 d = sqrt(R^2 - (AB/2)^2)。
/// 2. 多解处理：对于给定的 A, B, R，通常存在两个可能的圆心。策略通过 <c>CircularArcClockwise</c> 参数结合向量叉积来锁定唯一的圆心。
/// 3. 退化检查：若 R &lt; AB/2，则无法构成圆弧，此时策略会自动降级为直线移动。
/// 4. 匀速性质：类似于抛物线，圆弧也使用“弧长查找表（Arc Length LUT）”来抵消三角函数参数化带来的非均匀位移，实现恒定的线速度。
/// <code>
/// 【使用示例：圆弧追踪目标（终点每帧跟随 TargetNode）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.CircularArc, new MovementParams
///     {
///         Mode = MoveMode.CircularArc,
///         MaxDuration = 2f,
///         DestroyOnComplete = true,
///         isTrackTarget = true,            // 【可选】每帧将终点更新到 TargetNode 位置
///         TargetNode = enemyNode,          // 【可选】追踪目标
///         ReachDistance = 20f,             // 【可选】到达距离阈值
///         CircularArcRadius = 220f,        // 必须：圆弧半径
///         CircularArcClockwise = true,     // 【可选】顺时针短弧
///         ActionSpeed = 420f,              // 【可选】沿圆弧前进速度
///     }));
/// </code>
/// </para>
/// <para>【典型用途】弧形飞弹、绕侧切入、带偏转轨迹的投射物。</para>
/// </summary>
public class CircularArcStrategy : IMovementStrategy
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
    private CircularArc2D _cachedCurve;
    /// <summary>静态目标下预计算的曲线长度。</summary>
    private float _cachedCurveLength;
    /// <summary>当前在路径上的进度参数 [0, 1]。</summary>
    private float _progress;
    /// <summary>是否已成功锁定初始目标。</summary>
    private bool _hasLockedTarget;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.CircularArc, () => new CircularArcStrategy());
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
    /// <item>目标解析与到达判定：同 <see cref="ParabolaStrategy"/>。</item>
    /// <item>路径有效性检查：如果半径无效或起终点重合，降级为直线移动。</item>
    /// <item>进度步进：
    ///   <description>
    ///   计算进度增量 = (速度 * delta) / 弧长。
    ///   使用归一化进度 [0, 1] 在 <see cref="CircularArc2D"/> 上进行采样。
    ///   静态模式下优先使用弧长查找表（LUT）进行采样以保证物理上的完美匀速。
    ///   </description>
    /// </item>
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

        // 3. 特殊情况：如果半径太小（无法覆盖起终点间距），退化为垂直路径上的直线运动
        if (@params.CircularArcRadius <= 0.001f)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        bool useCachedArcLength = !@params.isTrackTarget && _cachedCurve.IsValid && _cachedCurveLength > 0.001f;
        CircularArc2D curve = useCachedArcLength
            ? _cachedCurve
            : CircularArc2D.Create(_startPoint, targetPoint, @params.CircularArcRadius, @params.CircularArcClockwise);

        if (!curve.IsValid)
        {
            // 例如半径小于两点距离的一半，导致无法构建圆弧
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        float curveLength = useCachedArcLength
            ? _cachedCurveLength
            : curve.ApproximateLength(); // 通过圆心角 * 半径快速计算精确弧长

        if (curveLength <= 0.001f)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        // 计算进度增量
        float progressDelta = speed * delta / curveLength;
        float nextProgress = Mathf.Clamp(_progress + progressDelta, 0f, 1f);

        // 采样新位置：优先使用基于弧长的匀速采样
        Vector2 nextPoint = useCachedArcLength
            ? curve.EvaluateByArcProgress(nextProgress, _curveLut)
            : curve.Evaluate(nextProgress);

        Vector2 displacement = nextPoint - node.GlobalPosition;
        float displacementLength = displacement.Length();

        _progress = nextProgress;

        // 设置速度以供物理系统或同步逻辑使用
        data.Set(
            DataKey.Velocity,
            displacementLength > 0.001f
                ? displacement / Mathf.Max(delta, 0.001f)
                : Vector2.Zero);

        // 采样切线方向用于朝向同步
        Vector2 facingDirection = useCachedArcLength
            ? curve.EvaluateTangentByArcProgress(nextProgress, _curveLut)
            : curve.EvaluateTangent(nextProgress);

        return MovementUpdateResult.Continue(displacementLength, facingDirection);
    }

    private void CacheStaticCurve(MovementParams @params)
    {
        _cachedCurve = default;
        _cachedCurveLength = 0f;

        if (@params.isTrackTarget || !_hasLockedTarget || @params.CircularArcRadius <= 0.001f)
        {
            return;
        }

        _cachedCurve = CircularArc2D.Create(_startPoint, _lockedTargetPoint, @params.CircularArcRadius, @params.CircularArcClockwise);
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
