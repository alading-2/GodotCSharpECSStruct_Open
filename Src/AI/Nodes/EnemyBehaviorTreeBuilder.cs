using Godot;
using System.Collections.Generic;

/// <summary>
/// 敌人行为树构建器
/// <para>
/// 构建标准敌人行为树：
///
/// Selector (Root)
/// ├── Sequence (攻击序列)
/// │   ├── Condition: 有有效目标？
/// │   ├── Condition: 在攻击范围内？
/// │   ├── Condition: 攻击冷却就绪？
/// │   └── Action: 执行攻击
/// ├── Sequence (追逐序列)
/// │   ├── Condition: 有有效目标？
/// │   └── Action: 移动到目标
/// └── Action: 巡逻/待机
/// </para>
/// </summary>
public static class EnemyBehaviorTreeBuilder
{
    private static readonly Log _log = new("EnemyBT");

    /// <summary>
    /// 构建标准近战敌人行为树
    /// </summary>
    public static BehaviorNode BuildMeleeEnemyTree()
    {
        return new SelectorNode("Root")
            // === 优先级 1: 攻击 ===
            .AddChild(new SequenceNode("攻击序列")
                .AddChild(new ConditionNode("有有效目标", HasValidTarget))
                .AddChild(new ConditionNode("在攻击范围内", IsTargetInAttackRange))
                .AddChild(new ConditionNode("攻击冷却就绪", IsAttackReady))
                .AddChild(new ActionNode("执行攻击", ExecuteAttack))
            )
            // === 优先级 2: 追逐 ===
            .AddChild(new SequenceNode("追逐序列")
                .AddChild(new ConditionNode("有有效目标", HasValidTarget))
                .AddChild(new ActionNode("移动到目标", MoveToTarget))
            )
            // === 优先级 3: 巡逻 ===
            .AddChild(new ActionNode("巡逻", Patrol))
            ;
    }

    // ================= 条件节点实现 =================

    /// <summary>是否有有效目标</summary>
    private static bool HasValidTarget(AIContext ctx)
    {
        // 先尝试寻找最近的玩家
        FindNearestPlayer(ctx);

        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null || !GodotObject.IsInstanceValid(target))
        {
            ctx.Data.Remove(DataKey.TargetNode);
            return false;
        }

        // 检查目标是否已死亡
        if (target is IEntity targetEntity)
        {
            var state = targetEntity.Data.Get<string>(DataKey.LifecycleState, "");
            if (state == nameof(LifecycleState.Dead) ||
                state == nameof(LifecycleState.Dying))
            {
                ctx.Data.Remove(DataKey.TargetNode);
                return false;
            }
        }

        return true;
    }

    /// <summary>是否在攻击范围内</summary>
    private static bool IsTargetInAttackRange(AIContext ctx)
    {
        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null || ctx.Body == null) return false;

        float attackRange = ctx.Data.Get<float>(DataKey.AttackRange, 50f);
        float distance = ctx.Body.GlobalPosition.DistanceTo(target.GlobalPosition);

        return distance <= attackRange;
    }

    /// <summary>攻击冷却是否就绪</summary>
    private static bool IsAttackReady(AIContext ctx)
    {
        var state = ctx.Data.Get<AttackState>(DataKey.AttackState, AttackState.Idle);
        return state == AttackState.Idle;
    }

    // ================= 动作节点实现 =================

    /// <summary>执行攻击</summary>
    private static NodeState ExecuteAttack(AIContext ctx)
    {
        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null || ctx.Body == null) return NodeState.Failure;

        var attackState = ctx.Data.Get<AttackState>(DataKey.AttackState, AttackState.Idle);

        if (attackState != AttackState.Idle)
        {
            // 正在攻击中（WindUp 或 Recovery），保持在当前节点
            return NodeState.Running;
        }

        // 触发攻击请求事件
        ctx.Events?.Emit(GameEventType.Attack.Requested,
            new GameEventType.Attack.RequestedEventData(target));

        // 面向目标
        FaceTarget(ctx, target);

        ctx.Data.Set(DataKey.AIState, AIState.Attacking);
        return NodeState.Running;
    }

    /// <summary>移动到目标</summary>
    private static NodeState MoveToTarget(AIContext ctx)
    {
        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null || ctx.Body == null) return NodeState.Failure;

        // 计算方向
        Vector2 direction = (target.GlobalPosition - ctx.Body.GlobalPosition).Normalized();

        // 获取移动速度
        float moveSpeed = ctx.Data.Get<float>(DataKey.MoveSpeed, 100f);

        // 设置速度
        ctx.Body.Velocity = direction * moveSpeed;
        ctx.Body.MoveAndSlide();

        // 面向目标
        FaceTarget(ctx, target);

        // 更新 AI 状态
        ctx.Data.Set(DataKey.AIState, AIState.Chasing);

        return NodeState.Running;
    }

    /// <summary>巡逻行为</summary>
    private static NodeState Patrol(AIContext ctx)
    {
        if (ctx.Body == null) return NodeState.Failure;

        // 获取巡逻参数
        float patrolRadius = ctx.Data.Get<float>(DataKey.PatrolRadius, 100f);
        Vector2 spawnPos = ctx.Data.Get<Vector2>(DataKey.SpawnPosition, ctx.Body.GlobalPosition);

        // 检查是否有巡逻目标点
        Vector2 patrolTarget = ctx.Data.Get<Vector2>(DataKey.PatrolTargetPoint, Vector2.Zero);

        // 是否需要新的巡逻目标点
        bool needNewTarget = patrolTarget == Vector2.Zero ||
            ctx.Body.GlobalPosition.DistanceTo(patrolTarget) < 10f;

        if (needNewTarget)
        {
            // 巡逻等待
            float waitTime = ctx.Data.Get<float>(DataKey.PatrolWaitTime, 1.5f);
            float currentWait = ctx.Data.Get<float>(DataKey.PatrolWaitTimer, 0f);

            if (currentWait < waitTime)
            {
                ctx.Data.Set(DataKey.PatrolWaitTimer, currentWait + ctx.DeltaTime);

                // 等待时停止移动
                ctx.Body.Velocity = Vector2.Zero;
                ctx.Data.Set(DataKey.AIState, AIState.Idle);
                return NodeState.Running;
            }

            // 等待结束，生成新巡逻点
            ctx.Data.Set(DataKey.PatrolWaitTimer, 0f);
            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float dist = (float)GD.RandRange(patrolRadius * 0.3f, patrolRadius);
            patrolTarget = spawnPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            ctx.Data.Set(DataKey.PatrolTargetPoint, patrolTarget);
        }

        // 向巡逻点移动
        Vector2 direction = (patrolTarget - ctx.Body.GlobalPosition).Normalized();
        float moveSpeed = ctx.Data.Get<float>(DataKey.MoveSpeed, 100f);

        // 巡逻时速度减半
        ctx.Body.Velocity = direction * moveSpeed * 0.5f;
        ctx.Body.MoveAndSlide();

        // 面向移动方向
        FaceDirection(ctx, direction);

        ctx.Data.Set(DataKey.AIState, AIState.Patrolling);

        return NodeState.Running;
    }

    // ================= 辅助方法 =================

    /// <summary>查找最近的玩家实体</summary>
    private static void FindNearestPlayer(AIContext ctx)
    {
        // 已有有效目标则跳过
        var existing = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (existing != null && GodotObject.IsInstanceValid(existing))
        {
            // 检查是否超出丢失范围
            if (ctx.Body != null)
            {
                float loseDist = ctx.Data.Get<float>(DataKey.LoseTargetRange, 500f);
                if (ctx.Body.GlobalPosition.DistanceTo(existing.GlobalPosition) > loseDist)
                {
                    ctx.Data.Remove(DataKey.TargetNode);
                    return;
                }
            }
            return;
        }

        // 使用 TargetSelector 查找最近的敌方目标
        if (ctx.Body == null) return;

        float detectionRange = ctx.Data.Get<float>(DataKey.DetectionRange, 300f);

        var targets = TargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = AbilityTargetGeometry.Circle,
            Origin = ctx.Body.GlobalPosition,
            Range = detectionRange,
            CenterEntity = ctx.Entity,
            TeamFilter = AbilityTargetTeamFilter.Enemy,
            Sorting = AbilityTargetSorting.Nearest,
            MaxTargets = 1
        });

        if (targets != null && targets.Count > 0)
        {
            var target = targets[0];
            if (target is Node2D node2D)
            {
                ctx.Data.Set(DataKey.TargetNode, node2D);
                _log.Debug($"发现目标: {node2D.Name}");
            }
        }
    }

    /// <summary>面向目标（翻转 Sprite）</summary>
    private static void FaceTarget(AIContext ctx, Node2D target)
    {
        if (ctx.Body == null || target == null) return;
        Vector2 direction = target.GlobalPosition - ctx.Body.GlobalPosition;
        FaceDirection(ctx, direction);
    }

    /// <summary>
    /// 面向方向（翻转 Sprite）
    /// TODO: 后续应改为通过事件驱动，由 UnitAnimationComponent 处理朝向
    /// </summary>
    private static void FaceDirection(AIContext ctx, Vector2 direction)
    {
        if (ctx.Body == null) return;
        if (direction.LengthSquared() < 0.001f) return;

        var visualRoot = ctx.Body.GetNodeOrNull<Node2D>("VisualRoot");
        if (visualRoot != null)
        {
            // 正值朝右，负值朝左
            visualRoot.Scale = new Vector2(
                direction.X >= 0 ? Mathf.Abs(visualRoot.Scale.X) : -Mathf.Abs(visualRoot.Scale.X),
                visualRoot.Scale.Y
            );
        }
    }
}
