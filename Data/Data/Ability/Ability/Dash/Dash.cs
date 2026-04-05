
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 冲刺技能执行器（移动系统版）
/// 
/// 触发方式：Manual（手动，玩家按键），充能系统（最多 2 层，每层 5 秒回充）
/// 目标选择：None（直接作用于施法者自身）
/// 运动模式：Charge（向当前移动方向或面朝方向高速冲刺，完成后自动回退默认模式）
/// 特效：Effect_004龙卷风（附着到施法者，跟随移动，播完自动销毁）
/// </summary>
public class DashExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(DashExecutor));

    private const float DashDuration = 0.15f;

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("Dash", new DashExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        if (caster == null || ability == null)
        {
            _log.Error("冲刺失败：施法者或技能为空");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        if (caster is not Node2D casterNode)
        {
            _log.Error("冲刺失败：施法者不是 Node2D");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 1. 获取冲刺距离
        var range = ability.Data.Get<float>(DataKey.AbilityEffectRadius);
        if (range <= 0) range = 300f;

        // 2. 确定冲刺方向：优先使用当前速度方向，否则用 VisualRoot FlipH 判断面朝方向
        var moveDir = caster.Data.Get<Vector2>(DataKey.Velocity);
        Vector2 dashDir;
        if (moveDir.LengthSquared() > 0.01f)
        {
            dashDir = moveDir.Normalized();
        }
        else
        {
            var sprite = casterNode.GetNodeOrNull<AnimatedSprite2D>("VisualRoot");
            bool facingLeft = sprite?.FlipH ?? false;
            dashDir = facingLeft ? Vector2.Left : Vector2.Right;
        }

        // 3. 通过 MovementStarted 事件触发 Charge 模式冲刺
        // 完成后 EntityMovementComponent 自动回退到 DefaultMoveMode（PlayerInput）
        var targetPos = casterNode.GlobalPosition + dashDir * range;
        caster.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.Charge,
                new MovementParams
                {
                    Mode = MoveMode.Charge,
                    TargetPoint = targetPos,
                    ActionSpeed = range / DashDuration,
                    MaxDuration = DashDuration,
                    RotateToVelocity = false,
                }
            )
        );

        // 4. 生成附加特效（附着施法者，随移动，播完自动销毁）
        var effectScene = ability.Data.Get<PackedScene>(DataKey.EffectScene);
        if (effectScene != null)
        {
            EffectTool.Spawn(casterNode.GlobalPosition, new EffectSpawnOptions(
                VisualScene: effectScene,
                Host: casterNode,
                Name: "冲刺特效"
            ));
        }

        _log.Info($"冲刺执行: 方向={dashDir}, 距离={range}, 目标={targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }
}

