using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】回旋飞镖。
/// <para>去程飞向 <c>TargetPoint</c>，可选暂停，再返回起始点后完成。起始点由 OnEnter 自动记录，速度由 <c>DataKey.MoveSpeed</c>（实体属性）驱动。</para>
/// <para>
/// <list type="bullet">
/// <item><c>TargetPoint</c>（Vector2，必须）：去程目标坐标。</item>
/// <item><c>BoomerangPauseTime</c>（float，秒，可选）：到达目标后停顿时长，0 = 直接返回。</item>
/// <item><c>ReachDistance</c>（float，可选）：到达判定阈值（像素），0 = 使用默认 5px。</item>
/// <item><c>MaxDuration</c>（float，可选）：-1 = 不限制，由飞行距离 / 速度自然决定飞行时长。</item>
/// <item><c>DestroyOnComplete</c>（bool，可选）：返回起点后是否自动销毁实体。</item>
/// </list>
/// </para>
/// <para>
/// <code>
/// 【使用示例：回旋飞镖弹到目标点后回收】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Boomerang, new MovementParams
///     {
///         Mode               = MoveMode.Boomerang,
///         TargetPoint        = targetPos,    // 必须：去程目标坐标
///         BoomerangPauseTime = 0.2f,         // 可选：到达后停顿 0.2 秒再返回，0 = 立即返回
///         ReachDistance      = 20f,          // 可选：到达判定距离（像素）
///         MaxDuration        = -1f,          // -1 不限制，自然完成
///         DestroyOnComplete  = true,
///     }));
/// </code>
/// </para>
/// <para>【典型用途】回旋飞镖效果、投出后自动回收的技能、来回飞行的特效弹。</para>
/// </summary>
public class BoomerangStrategy : IMovementStrategy
{
    /// <summary>
    /// 起始位置坐标，在 OnEnter 时记录，用于返回阶段的导航
    /// </summary>
    private Vector2 _startPoint;

    /// <summary>
    /// 是否处于返回阶段：false = 去程（飞向目标点），true = 返程（返回起点）
    /// </summary>
    private bool _returning;

    /// <summary>
    /// 暂停计时器：到达目标点后的停顿倒计时，>0 时暂停移动
    /// </summary>
    private float _pauseTimer;

    /// <summary>
    /// 模块初始化器：在模块加载时自动将此策略注册到移动策略注册表
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Boomerang, () => new BoomerangStrategy());
    }

    /// <summary>
    /// 策略进入时的初始化处理
    /// <para>主要任务：</para>
    /// <list type="bullet">
    /// <item>记录当前实体位置作为起始点（用于返程导航）</item>
    /// <item>重置状态：设置为去程模式，清空暂停计时器</item>
    /// </list>
    /// </summary>
    /// <param name="entity">移动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="params">移动参数</param>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        _startPoint = entity is Node2D node ? node.GlobalPosition : Vector2.Zero;
        _returning = false; // 初始为去程模式
        _pauseTimer = 0f;   // 清空暂停计时器
    }

    /// <summary>
    /// 每帧更新移动状态
    /// <para>状态机逻辑：</para>
    /// <list type="bullet">
    /// <item>暂停状态：倒计时清零，速度置零，等待继续</item>
    /// <item>去程状态：飞向 TargetPoint，到达后切换到暂停或返程</item>
    /// <item>返程状态：飞向 StartPoint，到达后完成移动</item>
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

        // === 暂停状态处理 ===
        if (_pauseTimer > 0f)
        {
            _pauseTimer = Mathf.Max(_pauseTimer - delta, 0f); // 倒计时递减
            data.Set(DataKey.Velocity, Vector2.Zero);         // 速度置零，暂停移动
            return MovementUpdateResult.Continue();
        }

        // === 目标选择和距离计算 ===
        Vector2 target = _returning ? _startPoint : @params.TargetPoint; // 根据状态选择目标
        Vector2 toTarget = target - node.GlobalPosition;                   // 计算到目标的向量
        float dist = toTarget.Length();                                     // 计算距离
        float reach = @params.ReachDistance > 0f ? @params.ReachDistance : 5f; // 到达判定阈值

        // === 到达目标检测 ===
        if (dist <= reach)
        {
            if (!_returning)
            {
                // 去程到达目标：切换到返程状态
                _returning = true;

                // 若设置了暂停时间，进入暂停状态
                if (@params.BoomerangPauseTime > 0f)
                    _pauseTimer = @params.BoomerangPauseTime;

                data.Set(DataKey.Velocity, Vector2.Zero); // 停止移动
                return MovementUpdateResult.Continue();
            }
            else
            {
                // 返程到达起点：完成移动
                data.Set(DataKey.Velocity, toTarget / Mathf.Max(delta, 0.001f)); // 设置最终速度
                return MovementUpdateResult.Complete();
            }
        }

        // === 正常移动逻辑 ===
        // 获取实体移动速度（由属性系统管理）
        float speed = data.Get<float>(DataKey.FinalMoveSpeed);

        // 计算本帧移动距离，防止 overshoot
        float step = Mathf.Min(speed * delta, dist);

        // 设置速度向量（指向目标方向）
        data.Set(DataKey.Velocity, (toTarget / dist) * speed);

        // 返回继续移动，并告知实际位移长度
        return MovementUpdateResult.Continue(step);
    }
}
