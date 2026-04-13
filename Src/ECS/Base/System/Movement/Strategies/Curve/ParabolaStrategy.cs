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
/// 4. 进度推进：每帧根据 speed * delta / curveLength 计算进度增量，直接按参数 t 采样曲线点。
/// <code>
/// 【使用示例：抛物线追踪目标（终点每帧跟随 TargetNode）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Parabola, new MovementParams
///     {
///         Mode = MoveMode.Parabola,
///         MaxDuration = 2f,                // 最大持续时间
///         DestroyOnComplete = true,        // 结束销毁
///         isTrackTarget = true,            // 每帧将终点更新到 TargetNode 位置
///         TargetNode = enemyNode,          // 追踪目标
///         ReachDistance = 20f,             // 到达距离阈值
///         ParabolaApexHeight = 180f,       // 必须：抛物线顶高
///         // 抛物线
///         BowWorldUp = true,               // 弧弧始终朝向屏幕上方，适用于攻击投射物
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

        // 静态目标下优先使用 OnEnter 时预计算的缓存曲线，避免每帧重建
        bool useCachedCurve = !@params.isTrackTarget && _cachedCurve.IsValid && _cachedCurveLength > 0.001f;

        // 构建或获取当前曲线实例
        Parabola2D curve = useCachedCurve
            ? _cachedCurve
            : Parabola2D.Create(_startPoint, targetPoint, ResolveEffectiveApexHeight(_startPoint, targetPoint, @params));

        if (!curve.IsValid)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        // 获取路径总长度用于计算进度增量
        float curveLength = useCachedCurve
            ? _cachedCurveLength
            : curve.ApproximateLength();

        if (curveLength <= 0.001f)
        {
            return UpdateLinear(node, data, delta, targetPoint, speed);
        }

        // 4. 计算进度增量。
        // ds = speed * delta (当前帧应走过的弧长)
        // dProgress = ds / curveLength (对应的参数 t 增量)
        float progressDelta = speed * delta / curveLength;
        float nextProgress = Mathf.Clamp(_progress + progressDelta, 0f, 1f);

        // 5. 按参数 t 直接采样曲线点
        Vector2 nextPoint = curve.Evaluate(nextProgress);

        // 计算本帧位移（相对于 Node2D 的移动）
        Vector2 displacement = nextPoint - node.GlobalPosition;
        float displacementLength = displacement.Length();

        _progress = nextProgress;

        // 6. 设置速度以供物理系统或同步逻辑使用。
        data.Set(
            DataKey.Velocity,
            displacementLength > 0.001f
                ? displacement / Mathf.Max(delta, 0.001f)
                : Vector2.Zero);

        // 采样切线方向，用于自动转向逻辑
        Vector2 facingDirection = curve.EvaluateTangent(nextProgress);

        return MovementUpdateResult.Continue(displacementLength, facingDirection);
    }

    /// <summary>
    /// 预计算并缓存静态抛物线路径。
    /// <para>当移动目标点在 OnEnter 时已确定且不再随帧变化时，预先构建曲线并测量其弧长，减少 Update 循环中的计算开销。</para>
    /// </summary>
    private void CacheStaticCurve(MovementParams @params)
    {
        // 初始重置缓存状态
        _cachedCurve = default;
        _cachedCurveLength = 0f;

        // 如果开启了实时追踪（终点动态变化）、尚未锁定任何目标、或顶点高度过小（路径退化为直线），则无需缓存静态曲线
        if (@params.isTrackTarget || !_hasLockedTarget || Mathf.Abs(@params.ParabolaApexHeight) <= 0.001f)
        {
            return;
        }

        // 尝试构建静态抛物线实例
        _cachedCurve = Parabola2D.Create(_startPoint, _lockedTargetPoint, ResolveEffectiveApexHeight(_startPoint, _lockedTargetPoint, @params));
        
        // 构建失败（如起点终点重合）则放弃缓存
        if (!_cachedCurve.IsValid)
        {
            _cachedCurve = default;
            return;
        }

        // 预计算曲线近似长度，用于进度步进计算
        _cachedCurveLength = _cachedCurve.ApproximateLength();
        
        // 长度过小时路径无效，清理缓存
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
    /// 根据 <c>BowWorldUp</c> 计算实际使用的顶点高度。
    /// BowWorldUp 开启时自动修正符号使弓起朝向屏幕上方：向右攻击取负高度，向左攻击取正高度。
    /// </summary>
    private static float ResolveEffectiveApexHeight(Vector2 start, Vector2 target, MovementParams @params)
    {
        if (!@params.BowWorldUp) return @params.ParabolaApexHeight;
        float h = Mathf.Abs(@params.ParabolaApexHeight);
        return (target - start).X > 0f ? -h : h;
    }

    /// <summary>
    /// 当路径退化为直线（如顶点高度为 0 或曲线构建失败）时的匀速补间更新。
    /// </summary>
    /// <param name="node">执行移动的 Node2D 节点。</param>
    /// <param name="data">用于存储 Velocity 等物理状态的数据容器。</param>
    /// <param name="delta">帧时长。</param>
    /// <param name="targetPoint">直线运动的目标终点。</param>
    /// <param name="speed">线速度。</param>
    private static MovementUpdateResult UpdateLinear(
        Node2D node,
        Data data,
        float delta,
        Vector2 targetPoint,
        float speed)
    {
        // 计算当前位置指向目标的位移矢量
        Vector2 toTarget = targetPoint - node.GlobalPosition;
        float distance = toTarget.Length();

        // 距离判定：若已在目标点附近，直接标记完成并停滞速度
        if (distance <= 0.001f)
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Complete();
        }

        // 计算本帧预期步长，并确保不会因浮点误差越过目标点
        float step = Mathf.Min(speed * delta, distance);
        Vector2 direction = toTarget / distance;

        // 根据本帧实际步长反推物理瞬时速度（Velocity = Displacement / Time）
        // 这样做可以确保当物体因距离过近而减速刹车时，速度状态能被正确同步
        data.Set(DataKey.Velocity, direction * (step / Mathf.Max(delta, 0.001f)));

        // 返回包含位移量和朝向的更新结果，通知外部系统（如动画或旋转）
        return MovementUpdateResult.Continue(step, direction);
    }
}
