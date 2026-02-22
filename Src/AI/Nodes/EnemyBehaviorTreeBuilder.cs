using Godot;
using System.Collections.Generic;

/// <summary>
/// 敌人行为树构建器 (Enemy Behavior Tree Builder)
/// <para>
/// 这是一个静态工厂类，负责将极其轻量的基础节点组装成具有复杂逻辑层次的"行为树(Behavior Tree)实例"。
/// 它为各类特定的敌人（如：近战、远程、巡逻型等）提供标准化的 AI 大脑蓝图。
/// </para>
/// <para>
/// 解耦原则：所有 Action 节点仅通过 DataKey 和 EventBus 传达意图，
/// 不直接操作 CharacterBody2D 或 VisualRoot，物理执行由 EnemyMovementComponent 负责。
/// </para>
/// <para>
/// 构建的标准近战敌人行为树结构如下：
///
/// Selector (根节点，优先级选择，每帧从头评估)
/// ├── Sequence (最高优先级：攻击序列)
/// │   ├── Condition: 有效目标？ (感知检测)
/// │   ├── Condition: 目标在攻击范围内？ (距离判断)
/// │   ├── Condition: 攻击冷却就绪？ (频率控制)
/// │   └── Action: 执行攻击 (产生实质动作)
/// ├── Sequence (次优先级：追逐序列)
/// │   ├── Condition: 有效目标？ (感知检测)
/// │   └── Action: 移动向目标靠近 (产生实质动作/运动)
/// └── Action (最低优先级：巡逻/待机)
///     └── 漫无目的的随机移动或站立发呆 (Fallback 兜底行为)
/// </para>
/// </summary>
public static class EnemyBehaviorTreeBuilder
{
    private static readonly Log _log = new("EnemyBT");

    /// <summary>
    /// 构建并返回一个标准近战敌人的行为树实例。
    /// 
    /// 逻辑流程：
    /// 1. 尝试攻击：只有在看得到玩家、且距离够近、且技能没冷却时，才会进行攻击。
    /// 2. 尝试追击：如果不能攻击，但能看到玩家，就会努力缩小距离（走向玩家）。
    /// 3. 转入巡逻：既看不见玩家也不能攻击时，就在出生点附近随机闲逛等待。
    /// </summary>
    /// <returns>构建完成的行为树根节点</returns>
    public static BehaviorNode BuildMeleeEnemyTree()
    {
        return new SelectorNode("EnemyBehaviorNode")
            // === 优先级 1: 攻击 ===
            .Add(new SequenceNode("攻击序列")
                .Add(new ConditionNode("有有效目标", HasValidTarget))
                .Add(new ConditionNode("在攻击范围内", IsTargetInAttackRange))
                .Add(new ConditionNode("攻击冷却就绪", IsAttackReady))
                .Add(new ActionNode("执行攻击", ExecuteAttack))
            )
            // === 优先级 2: 追逐 ===
            .Add(new SequenceNode("追逐序列")
                .Add(new ConditionNode("有有效目标", HasValidTarget))
                .Add(new ActionNode("移动到目标", MoveToTarget))
            )
            // === 优先级 3: 巡逻 (兜底方案) ===
            .Add(new ActionNode("巡逻", Patrol))
            ;
    }

    // ================= 条件判定：观察与感知 =================

    /// <summary>
    /// 检查 AI 上下文中是否存在一个存活、合法的敌对目标（通常是玩家）。
    /// <para>
    /// 如果之前没目标，会尝试调用 <see cref="FindNearestPlayer"/> 去搜寻。
    /// 如果发现存储的目标已死亡或处于临终状态，会将其从 Data 字典中移除并返回查无目标。
    /// </para>
    /// </summary>
    private static bool HasValidTarget(AIContext ctx)
    {
        // 先尝试寻找最近的玩家（内部有逻辑确保不会每帧无脑重复扫描全图）
        FindNearestPlayer(ctx);

        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null || !GodotObject.IsInstanceValid(target))
        {
            ctx.Data.Remove(DataKey.TargetNode);
            return false;
        }

        // 进一步验证该目标对象是否仍具备生命体征
        // Reviving 状态也视为无效目标，避免敌人在玩家复活期间守尸
        if (target is IEntity targetEntity)
        {
            var state = targetEntity.Data.Get<LifecycleState>(DataKey.LifecycleState);
            if (state == LifecycleState.Dead ||
                state == LifecycleState.Reviving)
            {
                ctx.Data.Remove(DataKey.TargetNode);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查目标是否已经进入到本实体的攻击有效打击范围内。
    /// 取决于配置表中名为 <c>AttackRange</c> 的数值（默认 50f）。
    /// </summary>
    private static bool IsTargetInAttackRange(AIContext ctx)
    {
        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return false;

        // 通过 Entity（作为 Node2D）获取位置，不依赖 Body
        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return false;

        float attackRange = ctx.Data.Get<float>(DataKey.AttackRange, 50f);
        float distance = selfNode.GlobalPosition.DistanceTo(target.GlobalPosition);

        return distance <= attackRange;
    }

    /// <summary>
    /// 读取本体数据校验攻击组件是否处于可以发起新攻击的闲置状态(Idle)。
    /// </summary>
    private static bool IsAttackReady(AIContext ctx)
    {
        var state = ctx.Data.Get<AttackState>(DataKey.AttackState, AttackState.Idle);
        return state == AttackState.Idle; // 正在前摇(WindUp)、后摇(Recovery)或冷却期时不认为是Ready
    }

    // ================= 动作执行：通过 DataKey 和事件传达意图 =================

    /// <summary>
    /// 执行动作：发号施令，通过 EventBus 抛出事件请求组件系统执行真实的攻击。
    /// 同时通过 DataKey 停止移动并设置面朝目标的方向。
    /// </summary>
    private static NodeState ExecuteAttack(AIContext ctx)
    {
        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        var attackState = ctx.Data.Get<AttackState>(DataKey.AttackState);

        // 攻击期间：面向目标但停止移动
        Vector2 faceDir = (target.GlobalPosition - selfNode.GlobalPosition).Normalized();
        ctx.Data.Set(DataKey.AIMoveDirection, faceDir);         // 朝向更新
        ctx.Data.Set(DataKey.AIMoveSpeedMultiplier, 0f);        // 停止移动

        if (attackState != AttackState.Idle)
        {
            if (attackState == AttackState.WindUp)
            {
                // 前摇动画执行中：保持阻塞，等待打出这一刀
                return NodeState.Running;
            }
            // Recovery / 追加CD冷却期：此次攻击已经完成，
            // 返回 Failure 让攻击序列失败，Selector 可转入追逐序列
            return NodeState.Failure;
        }

        // --- 至此必定是真正开始新的单次攻击 ---
        // 通过发射统一规范事件，分离 AI 决策层 与 技能/武器逻辑系统 的直接耦合
        ctx.Events?.Emit(GameEventType.Attack.Requested,
            new GameEventType.Attack.RequestedEventData(target));

        ctx.Data.Set(DataKey.AIState, AIState.Attacking);

        // 刚派发完事件，攻击动作将在未来几帧异步执行，故此报告 Running
        return NodeState.Running;
    }

    /// <summary>
    /// 执行动作：通过 DataKey 设置移动意图，由 EnemyMovementComponent 执行实际物理移动。
    /// </summary>
    private static NodeState MoveToTarget(AIContext ctx)
    {
        var target = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        // 1. 计算到目标的方向向量
        Vector2 direction = (target.GlobalPosition - selfNode.GlobalPosition).Normalized();

        // 2. 通过 DataKey 设置移动意图（EnemyMovementComponent 负责物理执行和朝向翻转）
        ctx.Data.Set(DataKey.AIMoveDirection, direction);
        ctx.Data.Set(DataKey.AIMoveSpeedMultiplier, 1.0f);

        // 3. 更新 AI 状态标记
        ctx.Data.Set(DataKey.AIState, AIState.Chasing);

        return NodeState.Running;
    }

    /// <summary>
    /// 执行动作：在出生地附近一定半径内漫流巡视和稍微停顿交替出现的兜底复合行为。
    /// 包含 "前往某点 -> 到点后发呆停留 -> 发呆结束选新点 -> 前往" 的完整子状态机。
    /// </summary>
    private static NodeState Patrol(AIContext ctx)
    {
        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        // 获取巡逻游荡的边界和中心（初始刷新坐标）
        float patrolRadius = ctx.Data.Get<float>(DataKey.PatrolRadius, 100f);
        Vector2 spawnPos = ctx.Data.Get<Vector2>(DataKey.SpawnPosition, selfNode.GlobalPosition);

        // 提取现有的巡逻目的地
        Vector2 patrolTarget = ctx.Data.Get<Vector2>(DataKey.PatrolTargetPoint, Vector2.Zero);

        // 判断当前是不是已经走到了目的地（或者尚未分配目的地）
        bool needNewTarget = patrolTarget == Vector2.Zero ||
            selfNode.GlobalPosition.DistanceTo(patrolTarget) < 10f;

        if (needNewTarget)
        {
            // === 到达指定点：进入停更等待阶段 ===
            float waitTime = ctx.Data.Get<float>(DataKey.PatrolWaitTime, 1.5f);
            float currentWait = ctx.Data.Get<float>(DataKey.PatrolWaitTimer, 0f);

            // 计时还没满（发呆时间充裕）
            if (currentWait < waitTime)
            {
                ctx.Data.Set(DataKey.PatrolWaitTimer, currentWait + ctx.DeltaTime);

                // 待着别动
                ctx.Data.Set(DataKey.AIMoveDirection, Vector2.Zero);
                ctx.Data.Set(DataKey.AIState, AIState.Idle);
                return NodeState.Running;
            }

            // 发呆时间结束：选一个新的目的地
            ctx.Data.Set(DataKey.PatrolWaitTimer, 0f);

            // 使用实体哈希码作为随机种子偏移，确保每个敌人生成不同的巡逻点
            ulong entityHash = (ulong)selfNode.GetInstanceId();
            ulong timeSeed = (ulong)(Time.GetTicksMsec());
            ulong seed = entityHash ^ timeSeed;

            var rng = new RandomNumberGenerator();
            rng.Seed = seed;

            // 极坐标随机撒点（角度与距离），映射于 SpawnPos 附近
            float angle = rng.RandfRange(0, Mathf.Tau);
            float dist = rng.RandfRange(patrolRadius * 0.3f, patrolRadius); // 至少移动一点距离

            patrolTarget = spawnPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            ctx.Data.Set(DataKey.PatrolTargetPoint, patrolTarget);
        }

        // === 尚未到达指定点：通过 DataKey 设置移动意图 ===
        Vector2 direction = (patrolTarget - selfNode.GlobalPosition).Normalized();

        // 巡逻属于非战备状态，移动速度做半衰减以显得随意自然
        ctx.Data.Set(DataKey.AIMoveDirection, direction);
        ctx.Data.Set(DataKey.AIMoveSpeedMultiplier, 0.5f);

        ctx.Data.Set(DataKey.AIState, AIState.Patrolling);

        return NodeState.Running;
    }

    // ================= 底层基础支撑方法 =================

    /// <summary>
    /// 调用全局目标选择工具 <see cref="TargetSelector"/> 去探测范围内的高优敌手目标。
    /// 有仇恨锁定的机制设计，一旦锁定除非距离太远丢失，否则一般不换目标。
    /// </summary>
    private static void FindNearestPlayer(AIContext ctx)
    {
        var selfNode = ctx.Entity as Node2D;

        // 1. 如果已经有一个存活目标，检查它是否超出了视野极限导致仇恨脱离
        var existing = ctx.Data.Get<Node2D>(DataKey.TargetNode);
        if (existing != null && GodotObject.IsInstanceValid(existing))
        {
            if (selfNode != null)
            {
                float loseDist = ctx.Data.Get<float>(DataKey.LoseTargetRange, 500f);
                if (selfNode.GlobalPosition.DistanceTo(existing.GlobalPosition) > loseDist)
                {
                    // 猎物逃脱太远，放弃追踪
                    ctx.Data.Remove(DataKey.TargetNode);
                    return;
                }
            }
            // 目标健在且尚未追丢，维持仇恨目标。
            return;
        }

        // 2. 当前没有追踪人物，使用项目提供的统一雷达查询工具找最近距离的敌人（玩家）
        if (selfNode == null) return;

        float detectionRange = ctx.Data.Get<float>(DataKey.DetectionRange, 300f);

        var targets = TargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = AbilityTargetGeometry.Circle,
            Origin = selfNode.GlobalPosition,
            Range = detectionRange,
            CenterEntity = ctx.Entity,
            TeamFilter = AbilityTargetTeamFilter.Enemy, // 从当前AI眼里看过去的"敌人"即为玩家阵容
            Sorting = AbilityTargetSorting.Nearest,
            MaxTargets = 1
        });

        // 取命中集合第一名并缓存关联之。
        if (targets != null && targets.Count > 0)
        {
            var target = targets[0];
            if (target is Node2D node2D)
            {
                ctx.Data.Set(DataKey.TargetNode, node2D);
                _log.Debug($"发现新目标开始锁定: {node2D.Name}");
            }
        }
    }
}
