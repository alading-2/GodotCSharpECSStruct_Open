using Godot;

namespace Slime.Test;

/// <summary>
/// EntityMovementComponent 单场景测试
/// 默认演示 Charge 模式（目标点冲锋），并验证 -1 表示不限制
/// </summary>
public partial class MovementComponentTestScene : Node2D
{
    private static readonly Log _log = new(nameof(MovementComponentTestScene));

    private MovementTestEntity? _entity;
    private EntityMovementComponent? _movement;

    public override void _Ready()
    {
        _entity = GetNode<MovementTestEntity>("MovementTestEntity");
        _movement = _entity.GetNode<EntityMovementComponent>("EntityMovementComponent");

        _movement.OnComponentRegistered(_entity);

        setupMovementData();
        bindEvents();

        _log.Info("MovementComponentTestScene Ready");
    }

    public override void _ExitTree()
    {
        _movement?.OnComponentUnregistered();
        base._ExitTree();
    }

    private void setupMovementData()
    {
        if (_entity == null) return;

        _entity.Position = new Vector2(120, 360);

        _entity.Data.Set(DataKey.DefaultMoveMode, MoveMode.Charge);

        // 通过 MovementParams 传入本次运动所有输入参数，通过事件触发策略切换
        _entity.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
            {
                TargetPoint = new Vector2(900, 360),
                ActionSpeed = 240f,
                ReachDistance = 8f,
            }));
    }

    private void bindEvents()
    {
        if (_entity == null) return;

        _entity.Events.On<GameEventType.Unit.MovementCompletedEventData>(
            GameEventType.Unit.MovementCompleted,
            onMovementCompleted);
    }

    private void onMovementCompleted(GameEventType.Unit.MovementCompletedEventData data)
    {
        // 统计数据直接从事件携带的字段读取，不再轮询 DataKey
        _log.Info($"运动完成 Mode={data.Mode}, Elapsed={data.ElapsedTime:F2}s, Distance={data.TraveledDistance:F1}");
    }
}
