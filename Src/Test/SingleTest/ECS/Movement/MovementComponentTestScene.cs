using Godot;

namespace Slime.Test;

/// <summary>
/// EntityMovementComponent 单场景测试
/// 默认演示 TargetPoint 模式，并验证 -1 表示不限制
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

        _entity.Data.Set(DataKey.MoveMode, MoveMode.TargetPoint);
        _entity.Data.Set(DataKey.MoveSpeed, 240f);
        _entity.Data.Set(DataKey.Velocity, Vector2.Right * 240f);

        _entity.Data.Set(DataKey.MoveTargetPoint, new Vector2(900, 360));
        _entity.Data.Set(DataKey.MoveReachDistance, 8f);

        _entity.Data.Set(DataKey.MoveMaxDuration, -1f);
        _entity.Data.Set(DataKey.MoveMaxDistance, -1f);

        _entity.Data.Set(DataKey.RotateToVelocity, true);
        _entity.Data.Set(DataKey.MoveCompleted, false);
        _entity.Data.Set(DataKey.MoveDestroyOnComplete, false);
        _entity.Data.Set(DataKey.MoveElapsedTime, 0f);
        _entity.Data.Set(DataKey.MoveTraveledDistance, 0f);
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
        if (_entity == null) return;

        float elapsed = _entity.Data.Get<float>(DataKey.MoveElapsedTime);
        float distance = _entity.Data.Get<float>(DataKey.MoveTraveledDistance);
        _log.Info($"运动完成 Mode={data.Mode}, Elapsed={elapsed:F2}s, Distance={distance:F1}");
    }
}
