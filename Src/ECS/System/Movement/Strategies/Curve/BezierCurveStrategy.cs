using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】贝塞尔曲线移动。
/// <para>沿 N 阶贝塞尔曲线前进，由 <c>ElapsedTime / MaxDuration</c> 驱动参数 t（0→1）。OnEnter 自动将第 0 个控制点替换为当前位置。</para>
/// <para>
/// <list type="bullet">
/// <item><c>MaxDuration</c>（float，<b>必须 &gt; 0</b>）：从起点走到终点的总时长（秒），控制整体速度。此策略不支持 -1（无限制）。</item>
/// <item><c>BezierPoints</c>（Vector2[]，推荐）：完整控制点数组（含起点和终点，至少 2 点）。起点会被 OnEnter 替换为当前位置，只需填写控制点和终点即可。若未提供则以 <c>TargetPoint</c> 作终点降级为直线。</item>
/// <item><c>TargetPoint</c>（Vector2，可选）：设置后会覆盖 <c>BezierPoints</c> 的终点（最后一个控制点），可在保留曲线形状的同时动态指定落点；未提供 <c>BezierPoints</c> 时降级为直线。</item>
/// <item><c>TargetNode</c> + <c>isTrackTarget</c>（可选）：<c>isTrackTarget = true</c> 时每帧将终点更新为 <c>TargetNode</c> 的当前位置，目标消失后终点冻结在最后位置。</item>
/// <item><c>DestroyOnComplete</c>（bool，可选）：到达终点后是否自动销毁实体。</item>
/// </list>
/// </para>
/// <para>若 <c>BezierPoints</c> 为空或不足 2 点，且 <c>TargetPoint != Vector2.Zero</c>，则自动降级为当前位置 → TargetPoint 的直线移动。</para>
/// <para>
/// <code>
/// 【使用示例：曲线追踪目标（isTrackTarget，终点每帧跟随 TargetNode）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.BezierCurve, new MovementParams
///     {
///         Mode            = MoveMode.BezierCurve,
///         MaxDuration     = 2f,
///         DestroyOnComplete   = true,
///         isTrackTarget   = true,                    // 【可选】每帧将终点更新到 TargetNode 位置
///         TargetNode      = enemyNode,               // 【可选】追踪目标
///         ReachDistance = 20, // 【可选】到达距离阈值，追踪一般都要设置
///         BezierPoints    = new Vector2[]
///         {
///             Vector2.Zero,                          // [0] 占位，OnEnter 替换为起点
///             new Vector2(0f,   -200f),              // [1] 控制点（曲线弧度）
///             new Vector2(200f, -200f),              // [2] 控制点
///             Vector2.Zero,                          // [3] 占位，每帧替换为目标位置
///         },
///     }));
/// </code>
/// </para>
/// <para>【典型用途】弧形投射物、技能抛物线、沿预设动画曲线移动的特效体、曲线追踪导弹。</para>
/// </summary>
public class BezierCurveStrategy : IMovementStrategy
{
    private static readonly Log _log = new Log("BezierCurveStrategy");

    /// <summary>
    /// 最终控制点数组（包含起点和终点），OnEnter 时会将起点替换为实体当前位置
    /// </summary>
    private Vector2[] _finalPoints = System.Array.Empty<Vector2>();

    /// <summary>
    /// 模块初始化器：在模块加载时自动将此策略注册到移动策略注册表
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.BezierCurve, () => new BezierCurveStrategy());
    }

    /// <summary>
    /// 策略进入时的初始化处理
    /// <para>主要任务：</para>
    /// <list type="bullet">
    /// <item>克隆并修正控制点数组：将第 0 个控制点（起点）替换为实体当前位置</item>
    /// <item>若启用匀速模式，预计算弧长参数化查找表（LUT）</item>
    /// </list>
    /// </summary>
    /// <param name="entity">移动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="params">移动参数</param>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        // MaxDuration 必须 > 0，否则无法驱动参数 t
        if (@params.MaxDuration <= 0f)
            _log.Warn($"MaxDuration={@params.MaxDuration} 无效，必须 > 0，曲线将不会移动");

        if (@params.isTrackTarget && @params.TargetNode == null)
            _log.Warn("isTrackTarget=true 但 TargetNode 未设置，追踪将无效，终点保持初始值。");

        if (@params.BezierPoints != null && @params.BezierPoints.Length >= 2)
        {
            // ⚠️ Clone 后修改，避免污染调用方传入的共享数组
            _finalPoints = (Vector2[])@params.BezierPoints.Clone();
            _finalPoints[0] = node.GlobalPosition; // 将起点修正为当前位置
            // 若 TargetPoint 有效，覆盖终点（最后一个控制点）
            if (@params.TargetPoint != Vector2.Zero)
                _finalPoints[_finalPoints.Length - 1] = @params.TargetPoint;
        }
        else if (@params.TargetPoint != Vector2.Zero)
        {
            // 降级为直线：当前位置 → TargetPoint
            _finalPoints = new Vector2[] { node.GlobalPosition, @params.TargetPoint };
        }
        else
        {
            _finalPoints = System.Array.Empty<Vector2>();
        }

    }

    /// <summary>
    /// 每帧更新移动状态
    /// <para>计算流程：</para>
    /// <list type="bullet">
    /// <item>根据已用时间计算参数 t（0~1）</item>
    /// <item>按参数 t 直接采样曲线点与切线方向</item>
    /// <item>计算新位置并更新速度向量</item>
    /// <item>检测是否到达终点（t >= 1）</item>
    /// </list>
    /// </summary>
    /// <param name="entity">移动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="delta">帧间隔时间</param>
    /// <param name="params">移动参数</param>
    /// <returns>移动更新结果（继续/完成）</returns>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();
        if (_finalPoints.Length < 2) return MovementUpdateResult.Continue(); // 控制点不足，跳过

        float duration = @params.MaxDuration;
        if (duration <= 0f) return MovementUpdateResult.Continue(); // MaxDuration 无效（忘记设置或为 -1），跳过

        // 追踪模式：每帧将终点（最后一个控制点）更新为目标当前位置
        if (@params.isTrackTarget && @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            _finalPoints[_finalPoints.Length - 1] = @params.TargetNode.GlobalPosition;

            // 追踪模式下的 ReachDistance 提前到达判定（无隐式默认，需调用方显式设置）
            if (MovementHelper.HasReachedTarget(node.GlobalPosition, @params.TargetNode.GlobalPosition, @params.ReachDistance))
                return MovementUpdateResult.Complete();
        }

        // 计算当前参数 t（0~1），基于已用时间 + 当前帧增量的预测位置
        float t = Mathf.Clamp((@params.ElapsedTime + delta) / duration, 0f, 1f);

        // 按参数 t 直接采样曲线点和切线方向
        Vector2 newPos = BezierCurve.Evaluate(_finalPoints, t);
        Vector2 facingDirection = BezierCurve.EvaluateTangent(_finalPoints, t);

        // 计算位移向量并更新速度
        Vector2 toTarget = newPos - node.GlobalPosition;
        float displacement = toTarget.Length();

        // 避免除零，设置合理的速度向量
        data.Set(DataKey.Velocity, displacement > 0.001f ? toTarget / Mathf.Max(delta, 0.001f) : Vector2.Zero);

        // 检测是否到达终点
        if (t >= 1f) return MovementUpdateResult.Complete();
        return MovementUpdateResult.Continue(displacement, facingDirection);
    }
}
