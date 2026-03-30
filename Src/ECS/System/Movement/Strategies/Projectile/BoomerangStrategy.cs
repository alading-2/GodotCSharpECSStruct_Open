using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】回旋飞镖。
/// <para>
/// 去程飞向 <c>TargetPoint</c>，可选暂停，再返回持有者（<c>TargetNode</c>）的<b>当前</b>位置后完成。
/// 返程目标每帧实时更新，确保飞镖始终追回持有者的最新位置，而非发射时的旧坐标。
/// </para>
/// <para>
/// 速度完全由 <c>ActionSpeed</c>（像素/秒）驱动，与实体的 <c>FinalMoveSpeed</c> 属性无关。
/// 不允许被外部运动事件打断（<c>CanBeInterrupted = false</c>）。
/// </para>
/// <para>
/// <list type="bullet">
/// <item><c>TargetPoint</c>（Vector2，必须）：去程目标坐标。</item>
/// <item><c>ActionSpeed</c>（float，必须）：飞行速度（像素/秒），决定去程和返程基准速度。</item>
/// <item><c>TargetNode</c>（Node2D，强烈推荐）：持有者节点引用（通常为玩家），返程时实时追踪其当前位置；
///   未设置或节点已离开场景树时，回退到发射时记录的起始坐标。</item>
/// <item><c>BoomerangPauseTime</c>（float，秒，可选）：到达目标后停顿时长，0 = 直接返回。</item>
/// <item><c>BoomerangReturnSpeedMultiplier</c>（float，可选）：返程速度倍率，1 = 同速，>1 = 加速返回。</item>
/// <item><c>ReachDistance</c>（float，可选）：到达判定阈值（像素），0 = 使用默认 8px。</item>
/// <item><c>DestroyOnComplete</c>（bool，可选）：返回持有者后是否自动销毁实体。</item>
/// </list>
/// </para>
/// <para>
/// <code>
/// 【使用示例】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Boomerang, new MovementParams
///     {
///         Mode                           = MoveMode.Boomerang,
///         TargetPoint                    = targetPos,   // 必须：去程目标坐标
///         TargetNode                     = player,      // 强烈推荐：返程动态追踪持有者
///         ActionSpeed                    = 600f,        // 必须：飞行速度（像素/秒）
///         BoomerangPauseTime             = 0.1f,        // 可选：到达后停顿
///         BoomerangReturnSpeedMultiplier = 1.5f,        // 可选：返程 1.5 倍速
///         ReachDistance                  = 20f,         // 可选：到达判定距离
///         DestroyOnComplete              = true,        // 返回后销毁
///     }));
/// </code>
/// </para>
/// <para>【典型用途】回旋飞镖效果、投出后自动回收的技能、来回飞行的特效弹。</para>
/// </summary>
public class BoomerangStrategy : IMovementStrategy
{
    /// <summary>发射时记录的起始坐标，当 TargetNode 不可用时作为返程备用目标</summary>
    private Vector2 _startPoint;

    /// <summary>持有者 Entity（IUnit），在 OnEnter 时通过 EntityRelationshipManager 查找并缓存</summary>
    private IUnit? _ownerEntity;

    /// <summary>当前阶段：false = 去程（飞向 TargetPoint），true = 返程（追回持有者）</summary>
    private bool _returning;

    /// <summary>暂停倒计时（秒），>0 时速度清零，等待再出发</summary>
    private float _pauseTimer;

    /// <summary>回旋镖一旦发出不允许被外部事件打断，必须完整走完去程 + 返程</summary>
    public bool CanBeInterrupted => false;

    /// <summary>模块初始化器：自动注册回旋镖策略到移动策略注册表。</summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Boomerang, () => new BoomerangStrategy());
    }

    /// <inheritdoc/>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        _startPoint = Vector2.Zero;
        _ownerEntity = null;
        if (entity is Node2D node)
        {
            _startPoint = node.GlobalPosition;
            _ownerEntity = EntityRelationshipManager.FindAncestorOfType<IUnit>(node);
        }
        _returning = false;
        _pauseTimer = 0f;
    }

    /// <inheritdoc/>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        // === 暂停阶段 ===
        if (_pauseTimer > 0f)
        {
            _pauseTimer = Mathf.Max(_pauseTimer - delta, 0f);
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Continue();
        }

        // === 确定本帧目标 ===
        Vector2 targetPoint;    // 目标位置
        // 是否返程
        if (_returning)
        {
            // 返程：每帧实时追踪持有者当前位置；不可用时回退到发射起始点
            targetPoint = (_ownerEntity is Node2D ownerNode && ownerNode.IsInsideTree())
                ? ownerNode.GlobalPosition
                : _startPoint;
        }
        else
        {
            targetPoint = @params.TargetPoint;
        }

        Vector2 toTarget = targetPoint - node.GlobalPosition;
        float dist = toTarget.Length();

        // === 到达检测 ===
        if (MovementHelper.HasReachedTarget(node.GlobalPosition, targetPoint, @params.ReachDistance, 8f))
        {
            if (!_returning)
            {
                // 去程抵达：切换到返程，可选停顿
                _returning = true;
                if (@params.BoomerangPauseTime > 0f)
                    _pauseTimer = @params.BoomerangPauseTime;

                data.Set(DataKey.Velocity, Vector2.Zero);
                return MovementUpdateResult.Continue();
            }
            else
            {
                // 返程抵达持有者：完成
                data.Set(DataKey.Velocity, Vector2.Zero);
                return MovementUpdateResult.Complete();
            }
        }

        // === 飞行阶段 ===
        // 速度来自 ActionSpeed 参数（可通过 MaxDistance+MaxDuration 推导），与实体属性无关
        float speed = @params.ActionSpeed > 0f ? @params.ActionSpeed : 300f;
        if (_returning)
        {
            float mult = @params.BoomerangReturnSpeedMultiplier > 0f
                ? @params.BoomerangReturnSpeedMultiplier : 1f;
            speed *= mult;
        }

        // 防过冲：单帧位移不超过剩余距离
        float step = Mathf.Min(speed * delta, dist);
        Vector2 dir = toTarget / dist;

        data.Set(DataKey.Velocity, dir * speed);

        // 显式传入切线方向，让实体始终面向飞行方向
        return MovementUpdateResult.Continue(step, dir);
    }
}
