using Godot;
using System.Collections.Generic;

namespace Slime.Test;

/// <summary>
/// EntityMovementComponent 场景测试。
/// <para>
/// 依次验证 Charge、Parabola、CircularArc、Boomerang 四种模式的完整流程。
/// </para>
/// </summary>
public partial class MovementComponentTestScene : Node2D
{
    private sealed record MovementScenario(
        string Name,
        Vector2 StartPosition,
        Vector2 ExpectedEndPosition,
        float EndTolerance,
        bool ExpectRotation,
        MovementParams Params);

    private static readonly Log _log = new(nameof(MovementComponentTestScene));

    private readonly List<MovementScenario> _scenarios = new();

    private MovementTestEntity? _entity;
    private EntityMovementComponent? _movement;
    private int _currentScenarioIndex = -1;
    private bool _currentScenarioSawRotation;

    public override void _Ready()
    {
        _entity = GetNode<MovementTestEntity>("MovementTestEntity");
        _movement = _entity.GetNode<EntityMovementComponent>("EntityMovementComponent");

        _movement.OnComponentRegistered(_entity);
        SetupMovementData();
        BindEvents();
        BuildScenarios();

        CallDeferred(nameof(StartNextScenario));
        _log.Info("MovementComponentTestScene Ready");
    }

    public override void _Process(double delta)
    {
        if (_entity == null || _currentScenarioIndex < 0 || _currentScenarioIndex >= _scenarios.Count) return;

        if (Mathf.Abs(_entity.RotationDegrees) > 1f)
        {
            _currentScenarioSawRotation = true;
        }
    }

    public override void _ExitTree()
    {
        _movement?.OnComponentUnregistered();
        base._ExitTree();
    }

    private void SetupMovementData()
    {
        if (_entity == null) return;

        _entity.GlobalPosition = new Vector2(120f, 360f);
        _entity.Data.Set(DataKey.DefaultMoveMode, MoveMode.None);
    }

    private void BuildScenarios()
    {
        _scenarios.Clear();

        _scenarios.Add(new MovementScenario(
            "Charge",
            new Vector2(120f, 360f),
            new Vector2(320f, 360f),
            16f,
            false,
            new MovementParams
            {
                Mode = MoveMode.Charge,
                TargetPoint = new Vector2(320f, 360f),
                ActionSpeed = 240f,
                ReachDistance = 8f,
                RotateToVelocity = true,
            }));

        _scenarios.Add(new MovementScenario(
            "Parabola",
            new Vector2(120f, 360f),
            new Vector2(420f, 360f),
            16f,
            true,
            new MovementParams
            {
                Mode = MoveMode.Parabola,
                TargetPoint = new Vector2(420f, 360f),
                ActionSpeed = 260f,
                ReachDistance = 8f,
                RotateToVelocity = true,
                ParabolaApexHeight = 120f,
            }));

        _scenarios.Add(new MovementScenario(
            "CircularArc",
            new Vector2(120f, 360f),
            new Vector2(420f, 360f),
            16f,
            true,
            new MovementParams
            {
                Mode = MoveMode.CircularArc,
                TargetPoint = new Vector2(420f, 360f),
                ActionSpeed = 260f,
                ReachDistance = 8f,
                RotateToVelocity = true,
                CircularArcRadius = 220f,
                CircularArcClockwise = true,
            }));

        _scenarios.Add(new MovementScenario(
            "Boomerang",
            new Vector2(120f, 360f),
            new Vector2(120f, 360f),
            18f,
            true,
            new MovementParams
            {
                Mode = MoveMode.Boomerang,
                TargetPoint = new Vector2(360f, 360f),
                ActionSpeed = 260f,
                ReachDistance = 12f,
                RotateToVelocity = true,
                BoomerangPauseTime = 0.05f,
                BoomerangReturnSpeedMultiplier = 1.2f,
                BoomerangArcHeight = 100f,
                BoomerangIsClockwise = true,
            }));
    }

    private void BindEvents()
    {
        if (_entity == null) return;

        _entity.Events.On<GameEventType.Unit.MovementCompletedEventData>(
            GameEventType.Unit.MovementCompleted,
            OnMovementCompleted);
    }

    private void StartNextScenario()
    {
        if (_entity == null) return;

        _currentScenarioIndex++;
        if (_currentScenarioIndex >= _scenarios.Count)
        {
            _log.Info("MovementComponentTestScene 全部场景测试完成");
            GetTree().Quit();
            return;
        }

        MovementScenario scenario = _scenarios[_currentScenarioIndex];
        _entity.GlobalPosition = scenario.StartPosition;
        _entity.RotationDegrees = 0f;
        _currentScenarioSawRotation = false;

        _log.Info($"开始测试运动模式: {scenario.Name}");
        _entity.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(scenario.Params.Mode, scenario.Params));
    }

    private void OnMovementCompleted(GameEventType.Unit.MovementCompletedEventData data)
    {
        if (_entity == null) return;
        if (_currentScenarioIndex < 0 || _currentScenarioIndex >= _scenarios.Count) return;

        MovementScenario scenario = _scenarios[_currentScenarioIndex];
        float endDistance = _entity.GlobalPosition.DistanceTo(scenario.ExpectedEndPosition);
        bool endPass = endDistance <= scenario.EndTolerance;
        if (endPass)
        {
            _log.Info($"[通过] {scenario.Name} 终点校验，distance={endDistance:F2}");
        }
        else
        {
            _log.Error($"[失败] {scenario.Name} 终点校验，distance={endDistance:F2} expected={scenario.ExpectedEndPosition} actual={_entity.GlobalPosition}");
        }

        if (scenario.ExpectRotation)
        {
            if (_currentScenarioSawRotation)
            {
                _log.Info($"[通过] {scenario.Name} 旋转跟随已生效");
            }
            else
            {
                _log.Error($"[失败] {scenario.Name} 未观察到 RotateToVelocity 带来的旋转变化");
            }
        }

        _log.Info($"运动完成 Mode={data.Mode}, Elapsed={data.ElapsedTime:F2}s, Distance={data.TraveledDistance:F1}");
        CallDeferred(nameof(StartNextScenario));
    }
}
