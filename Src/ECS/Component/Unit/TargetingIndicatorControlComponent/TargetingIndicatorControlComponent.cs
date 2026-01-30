using Godot;

/// <summary>
/// 瞄准指示器控制组件
/// 
/// 职责：
/// - 处理右摇杆输入移动指示器
/// - 处理确认/取消按键输入
/// - 限制指示器在施法范围内
/// - 发送瞄准相关事件
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
    private float _maxRange = 200f;

    // ================= IComponent 生命周期 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _owner = iEntity;
            _log.Debug($"TargetingIndicatorControlComponent 已注册到 {iEntity.Data.Get<string>(DataKey.Name)}");
        }
    }

    public void OnComponentUnregistered()
    {
        _owner = null;
    }

    // ================= Godot 生命周期 =================

    private Vector2 _relativeOffset = Vector2.Zero;
    private bool _isFirstFrame = true;

    public override void _Process(double delta)
    {
        if (_owner == null) return;
        if (_owner is not Node2D node2D) return;
        if (!node2D.Visible) return;

        // 获取施法者位置
        Vector2 casterPos = Vector2.Zero;
        if (_caster is Node2D casterNode)
        {
            casterPos = casterNode.GlobalPosition;
        }

        // 1. 初始化或获取当前相对偏移
        if (_isFirstFrame)
        {
            _relativeOffset = node2D.GlobalPosition - casterPos;
            _isFirstFrame = false;
        }

        // 2. 处理输入 (输入改变的是相对偏移)
        var aimInput = InputManager.GetAimInput();
        if (aimInput.LengthSquared() > 0.1f)
        {
            var moveSpeed = _owner!.Data.Get<float>(DataKey.MoveSpeed);
            if (moveSpeed <= 0) moveSpeed = 400f;

            _relativeOffset += aimInput.Normalized() * moveSpeed * (float)delta;
        }

        // 3. 限制半径
        if (_relativeOffset.Length() > _maxRange)
        {
            _relativeOffset = _relativeOffset.Normalized() * _maxRange;
        }

        // 4. 应用位置 (CasterPos + Offset) => 实现跟随效果
        node2D.GlobalPosition = casterPos + _relativeOffset;

        // 发送位置更新事件
        if (aimInput.LengthSquared() > 0.1f)
        {
            GlobalEventBus.Global.Emit(
                 GameEventType.Targeting.IndicatorMoved,
                 new GameEventType.Targeting.IndicatorMovedEventData(node2D.GlobalPosition)
            );
        }

        // 处理确认/取消输入
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
    /// 处理右摇杆移动输入
    /// </summary>
    private void HandleMovement(Node2D node2D, float delta)
    {
        // 获取瞄准输入（右摇杆）
        var aimInput = InputManager.GetAimInput();

        if (aimInput.LengthSquared() > 0.1f)
        {
            var moveSpeed = _owner!.Data.Get<float>(DataKey.MoveSpeed);
            if (moveSpeed <= 0) moveSpeed = 400f;

            // 移动指示器
            node2D.GlobalPosition += aimInput.Normalized() * moveSpeed * delta;

            // 发送位置更新事件
            GlobalEventBus.Global.Emit(
                GameEventType.Targeting.IndicatorMoved,
                new GameEventType.Targeting.IndicatorMovedEventData(node2D.GlobalPosition)
            );
        }
    }

    /// <summary>
    /// 处理确认/取消输入
    /// </summary>
    private void HandleTargetingInput(Node2D node2D)
    {
        // A 键确认
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

    /// <summary>
    /// 限制指示器在施法范围内
    /// </summary>
    private void ClampToRange(Node2D node2D)
    {
        if (_caster == null) return;

        Vector2 casterPos = Vector2.Zero;
        if (_caster is Node2D casterNode)
        {
            casterPos = casterNode.GlobalPosition;
        }

        // 核心修改: 保持相对于施法者的偏移，而不是世界坐标
        // 这样当施法者移动时，指示器也会跟随移动
        var currentOffset = node2D.GlobalPosition - casterPos;

        // 如果超出范围，钳制偏移量
        if (currentOffset.Length() > _maxRange)
        {
            currentOffset = currentOffset.Normalized() * _maxRange;
        }

        // 重新应用位置 (这将修正世界坐标，使其保持在范围内且跟随玩家)
        // 注意：这一步其实主要是为了Range Clamp，但结合 _Process 中的逻辑，
        // 我们应该在 HandleMovement 中就应用 "相对移动" 的概念，或者在这里强制修正。
        // 为了最平滑的效果，我们应该每一帧都基于 "上一帧的相对位置 + 输入" 来更新。

        // 但由于 HandleMovement 是直接改 GlobalPosition，所以这里只需要确保它不跑出圈即可。
        // 等等，如果仅仅是 Clamp，当玩家移动时，GlobalPosition 没变，RelativePosition 变了。
        // 所以 Clamp 会把它拉回来。这会产生 "拖拽" 效果，而不是 "跟随"。

        // 要实现 "跟随" (Relative Position 保持不变)，我们需要手动更新 GlobalPosition
        // 但这会和 HandleMovement 冲突。

        // 正确做法：
        // 1. 记录上一帧的 CasterPos (或者不记录，直接用偏移量)
        // 实际上，TargetingIndicatorEntity 是独立的。
        // 如果想让它 "挂在" 玩家身上，最简单是 Move 它的 GlobalPosition += CasterDelta。
        // 或者：GlobalPosition = CasterPos + RelativeOffset。

        // 让我们在 _Process 里改。
        node2D.GlobalPosition = casterPos + currentOffset;
    }
}
