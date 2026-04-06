using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 冲刺技能执行器（移动系统版）
/// 
/// 触发方式：Manual（手动，玩家按键）
/// 目标选择：None（直接作用于施法者自身）
/// 运动模式：Charge（向当前移动方向或面朝方向高速冲刺，完成后自动回退默认模式）
/// 特效：Effect_004龙卷风（附着到施法者，跟随移动，播完自动销毁）
/// </summary>
internal class DashExecutor : AbilityFeatureHandlerBase
{
    private static readonly Log _log = new(nameof(DashExecutor));

    private const float DashDuration = 0.15f;

    [ModuleInitializer]
    internal static void Initialize()
    {
        FeatureHandlerRegistry.Register(new DashExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Movement.Dash;
    public override string FeatureGroup => global::FeatureId.Ability.Groups.Movement;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

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
            var casterNode = caster as Node2D;
            var sprite = casterNode?.GetNodeOrNull<AnimatedSprite2D>("VisualRoot");
            bool facingLeft = sprite?.FlipH ?? false;
            dashDir = facingLeft ? Vector2.Left : Vector2.Right;
        }

        // 3. 通过 MovementStarted 事件触发 Charge 模式冲刺
        // 完成后 EntityMovementComponent 自动回退到 DefaultMoveMode（PlayerInput）
        var casterNode2D = caster as Node2D;
        var targetPos = (casterNode2D?.GlobalPosition ?? Vector2.Zero) + dashDir * range;
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
            EffectTool.Spawn(casterNode2D?.GlobalPosition ?? Vector2.Zero, new EffectSpawnOptions(
                VisualScene: effectScene,
                Host: caster as Node,
                Name: "冲刺特效"
            ));
        }

        _log.Info($"冲刺执行: 方向={dashDir}, 距离={range}, 目标={targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }
}

