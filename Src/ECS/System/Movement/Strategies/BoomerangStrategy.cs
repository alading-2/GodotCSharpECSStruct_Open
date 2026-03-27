using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 9】回旋飞镖。
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
    private Vector2 _startPoint;
    private bool _returning;
    private float _pauseTimer;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Boomerang, () => new BoomerangStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        _startPoint = entity is Node2D node ? node.GlobalPosition : Vector2.Zero;
        _returning = false;
        _pauseTimer = 0f;
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        if (_pauseTimer > 0f)
        {
            _pauseTimer = Mathf.Max(_pauseTimer - delta, 0f);
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Continue();
        }

        Vector2 target = _returning ? _startPoint : @params.TargetPoint;
        Vector2 toTarget = target - node.GlobalPosition;
        float dist = toTarget.Length();
        float reach = @params.ReachDistance > 0f ? @params.ReachDistance : 5f;

        if (dist <= reach)
        {
            if (!_returning)
            {
                _returning = true;
                if (@params.BoomerangPauseTime > 0f)
                    _pauseTimer = @params.BoomerangPauseTime;
                data.Set(DataKey.Velocity, Vector2.Zero);
                return MovementUpdateResult.Continue();
            }
            else
            {
                data.Set(DataKey.Velocity, toTarget / Mathf.Max(delta, 0.001f));
                return MovementUpdateResult.Complete();
            }
        }

        float speed = data.Get<float>(DataKey.FinalMoveSpeed); // 实体属性，由属性系统管理
        float step = Mathf.Min(speed * delta, dist);
        data.Set(DataKey.Velocity, (toTarget / dist) * speed);
        return MovementUpdateResult.Continue(step);
    }
}
