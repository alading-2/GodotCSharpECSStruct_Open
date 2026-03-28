using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】贝塞尔曲线移动。
/// <para>沿 N 阶贝塞尔曲线前进，由 <c>ElapsedTime / BezierDuration</c> 驱动参数 t。OnEnter 自动将第 0 个控制点替换为当前位置。</para>
/// <para>
/// <list type="bullet">
/// <item><c>BezierPoints</c>（Vector2[]，必须）：完整控制点数组（含起点和终点，至少 2 点）。起点会被 OnEnter 替换为当前位置，只需填写控制点和终点即可。</item>
/// <item><c>BezierDuration</c>（float，必须）：从起点走到终点的总时长（秒），控制整体速度。</item>
/// <item><c>BezierUniformSpeed</c>（bool，可选）：true = 弧长参数化匀速移动，false（默认）= 非匀速（参数 t 线性）。</item>
/// <item><c>MaxDuration</c>（float，可选）：-1 = 不限制；若设置比 BezierDuration 小，则会提前结束。通常不需要，直接依赖 BezierDuration 完成。</item>
/// <item><c>DestroyOnComplete</c>（bool，可选）：到达终点后是否自动销毁实体。</item>
/// </list>
/// </para>
/// <para>若 <c>BezierPoints</c> 为空则降级为直线（以 <c>TargetPoint</c> 作终点）。</para>
/// <para>
/// <code>
/// 【使用示例：抛物线弹道（三阶贝塞尔）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.BezierCurve, new MovementParams
///     {
///         Mode            = MoveMode.BezierCurve,
///         BezierPoints    = new Vector2[]            // 起点会被 OnEnter 自动替换为当前位置
///         {
///             Vector2.Zero,                          // [0] 占位，OnEnter 自动替换
///             new Vector2(200f, -300f),              // [1] 控制点（决定弧高）
///             new Vector2(400f, -300f),              // [2] 控制点
///             new Vector2(600f,  100f),              // [3] 终点
///         },
///         BezierDuration      = 1.5f,               // 总飞行时长（秒）
///         BezierUniformSpeed  = false,               // 可选：true = 匀速，false = 非匀速
///         DestroyOnComplete   = true,
///     }));
/// </code>
/// </para>
/// <para>【典型用途】弧形投射物、技能抛物线、沿预设动画曲线移动的特效体。</para>
/// </summary>
public class BezierCurveStrategy : IMovementStrategy
{
    /// <summary>
    /// 最终控制点数组（包含起点和终点），OnEnter 时会将起点替换为实体当前位置
    /// </summary>
    private Vector2[] _finalPoints = System.Array.Empty<Vector2>();

    /// <summary>
    /// 弧长参数化查找表（LUT），用于匀速移动时的弧长到参数 t 的映射
    /// </summary>
    private float[]? _lengthLut;

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

        if (@params.BezierPoints != null && @params.BezierPoints.Length >= 2)
        {
            // ⚠️ Clone 后修改，避免污染调用方传入的共享数组
            _finalPoints = (Vector2[])@params.BezierPoints.Clone();
            _finalPoints[0] = node.GlobalPosition; // 将起点修正为当前位置
        }
        else
        {
            _finalPoints = System.Array.Empty<Vector2>();
        }

        // 重置查找表
        _lengthLut = null;

        // 若启用匀速模式且控制点有效，预计算弧长查找表
        if (@params.BezierUniformSpeed && _finalPoints.Length >= 2)
        {
            _lengthLut = BezierCurve.BuildLengthTable(_finalPoints, 64); // 64 段采样精度
        }
    }

    /// <summary>
    /// 每帧更新移动状态
    /// <para>计算流程：</para>
    /// <list type="bullet">
    /// <item>根据已用时间计算参数 t（0~1）</item>
    /// <item>根据匀速模式选择求值方法：弧长参数化 或 直接参数求值</item>
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

        float duration = @params.BezierDuration;
        if (duration <= 0f) return MovementUpdateResult.Continue(); // 无效时长，跳过

        // 计算当前参数 t（0~1），基于已用时间 + 当前帧增量的预测位置
        float t = Mathf.Clamp((@params.ElapsedTime + delta) / duration, 0f, 1f);

        // 根据匀速模式选择求值方法
        Vector2 newPos = (@params.BezierUniformSpeed && _lengthLut != null)
            ? BezierCurve.EvaluateUniform(_finalPoints, t, _lengthLut) // 匀速：弧长参数化
            : BezierCurve.Evaluate(_finalPoints, t);                    // 非匀速：直接参数求值

        // 朝向使用曲线在同一参数点的切线，而不是当前位置到采样点的纠偏向量
        Vector2 facingDirection = (@params.BezierUniformSpeed && _lengthLut != null)
            ? BezierCurve.EvaluateUniformTangent(_finalPoints, t, _lengthLut)
            : BezierCurve.EvaluateTangent(_finalPoints, t);

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
