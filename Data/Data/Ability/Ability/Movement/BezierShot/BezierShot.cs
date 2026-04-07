using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 贝塞尔曲线弹技能执行器 - 验证 BezierCurve 运动模式
/// 向最近敌人发射沿二次贝塞尔曲线飞行的投射物（弓形抛射）
/// </summary>
internal class BezierShotExecutor : AbilityFeatureHandlerBase
{
    private static readonly Log _log = new(nameof(BezierShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new BezierShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.BezierShot;
    public override string FeatureGroup => global::FeatureId.Ability.Groups.Projectile;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;
        if (caster == null || ability == null || caster is not Node2D casterNode)
            return new AbilityExecutedResult { TargetsHit = 0 };

        var damage = ability.Data.Get<float>(DataKey.AbilityDamage)
                   * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f;

        // 查找最近敌人作为终点
        var targetPos = GetNearestEnemyPos(caster, casterNode);
        var startPos = casterNode.GlobalPosition;
        var midPoint = (startPos + targetPos) / 2f;
        var controlPoint = midPoint + new Vector2(0f, -180f);
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            startPos,
            new ProjectileSpawnOptions(projectileScene, "BezierShotProjectile"));
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        float cachedDamage = damage;
        IEntity cachedCaster = caster;

        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision,
            (evt) => OnHit(evt, cachedCaster, cachedDamage));

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.BezierCurve,
                new MovementParams
                {
                    Mode = MoveMode.BezierCurve,
                    BezierPoints = new Vector2[] { startPos, controlPoint, targetPos },
                    ActionSpeed = 420f,
                    DestroyOnComplete = true,
                    DestroyOnCollision = true,
                    RotateToVelocity = true,
                }
            )
        );

        _log.Info($"贝塞尔弹: 起={startPos}, 终={targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private static Vector2 GetNearestEnemyPos(IEntity caster, Node2D casterNode)
    {
        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = casterNode.GlobalPosition,
            Range = 600f,
            CenterEntity = caster,
            TeamFilter = AbilityTargetTeamFilter.Enemy,
            Sorting = TargetSorting.Nearest,
            MaxTargets = 1
        };
        var targets = EntityTargetSelector.Query(query);
        if (targets.Count > 0 && targets[0] is Node2D t)
            return t.GlobalPosition;
        return casterNode.GlobalPosition + new Vector2(400f, 0f);
    }

    private static void OnHit(GameEventType.Unit.MovementCollisionEventData evt, IEntity caster, float damage)
    {
        if (evt.Target is IUnit victim)
        {
            DamageService.Instance.Process(new DamageInfo
            {
                Attacker = caster as Godot.Node,
                Victim = victim,
                Damage = damage,
                Type = DamageType.Physical,
                Tags = DamageTags.Ability
            });
        }
    }
}
