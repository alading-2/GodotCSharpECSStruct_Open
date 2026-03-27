using Godot;

/// <summary>
/// 瞄准指示器控制组件
/// 
/// 职责：
/// - 处理右摇杆输入移动指示器
/// - 处理确认/取消按键输入
/// - 限制指示器在施法范围内
/// </summary>
public partial class TargetingIndicatorControlComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(TargetingIndicatorControlComponent));

    // ================= IComponent 实现 =================

    private IEntity? _owner;

    // ================= 瞄准参数 =================

    /// <summary>施法者引用（用于计算距离限制）</summary>
    private IEntity? _caster;

    /// <summary>最大移动范围（技能射程）</summary>
    private float _maxRange;

    // ================= IComponent 生命周期 =================

    /// <summary>
    /// 组件注册时的回调
    /// </summary>
    /// <param name="entity">所属实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _owner = iEntity;
            _log.Debug($"TargetingIndicatorControlComponent 已注册到 {iEntity.Data.Get<string>(DataKey.Name)}");
        }
    }

    /// <summary>
    /// 组件注销时的回调
    /// </summary>
    public void OnComponentUnregistered()
    {
        _owner = null;
    }

    // ================= Godot 生命周期 =================

    /// <summary>指示器相对于施法者的偏移量</summary>
    private Vector2 _relativeOffset = Vector2.Zero;
    /// <summary>是否为初始化的第一帧</summary>
    private bool _isFirstFrame = true;

    /// <summary>
    /// 每帧更新逻辑
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public override void _Process(double delta)
    {
        // 基础检查：实体是否存在、是否为 Node2D、是否可见
        if (_owner == null) return;
        if (_owner is not Node2D node2D) return;
        if (!node2D.Visible) return;

        // 获取施法者位置（默认为零点）
        Vector2 casterPos = Vector2.Zero;
        if (_caster is Node2D casterNode)
        {
            casterPos = casterNode.GlobalPosition;
        }

        // 1. 初始化相对偏移量
        // 在第一帧计算指示器相对于施法者的初始位置，用于后续跟随逻辑
        if (_isFirstFrame)
        {
            _relativeOffset = node2D.GlobalPosition - casterPos;
            _isFirstFrame = false;
        }

        // 2. 处理移动输入 (输入改变的是相对偏移)
        var aimInput = InputManager.GetAimInput();
        if (aimInput.LengthSquared() > 0.1f)
        {
            // 获取移动速度，若未配置则使用默认值
            var moveSpeed = _owner!.Data.Get<float>(DataKey.FinalMoveSpeed);

            // 根据输入更新相对偏移量
            _relativeOffset += aimInput.Normalized() * moveSpeed * (float)delta;
        }

        // 3. 限制移动半径
        // 确保指示器不会超出施法者的最大射程范围
        if (_relativeOffset.Length() > _maxRange)
        {
            _relativeOffset = _relativeOffset.Normalized() * _maxRange;
        }

        // 4. 应用位置 (CasterPos + Offset) => 实现跟随效果
        // 这样即使施法者在移动，指示器也会相对于施法者保持位置
        node2D.GlobalPosition = casterPos + _relativeOffset;

        // 5. 处理确认/取消等按键输入
        HandleTargetingInput(node2D);
    }

    // ================= 公共方法 =================

    /// <summary>
    /// 设置瞄准参数
    /// </summary>
    /// <param name="caster">施法者实体</param>
    /// <param name="range">技能射程</param>
    public void SetTargetingParams(IEntity? caster, float range)
    {
        _caster = caster;
        _maxRange = range;
        _log.Debug($"设置瞄准参数: 射程={_maxRange}");
    }

    // ================= 内部方法 =================

    /// <summary>
    /// 处理确认/取消输入
    /// </summary>
    private void HandleTargetingInput(Node2D node2D)
    {
        // X 键确认
        if (InputManager.IsX())
        {
            GlobalEventBus.Global.Emit(
                GameEventType.Targeting.TargetConfirmed,
                new GameEventType.Targeting.TargetConfirmedEventData(node2D.GlobalPosition)
            );
        }

        // B 键取消
        if (InputManager.IsCancel())
        {
            GlobalEventBus.Global.Emit(
                GameEventType.Targeting.TargetCancelled,
                new GameEventType.Targeting.TargetCancelledEventData()
            );
        }
    }

}
