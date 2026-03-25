using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 9】回旋镖运动
/// <para>
/// 分两阶段：
/// 1. 去程：从 BoomerangStartPoint 飞向 MoveTargetPoint（可选停顿 BoomerangPauseTime 秒）
/// 2. 回程：从 MoveTargetPoint 飞回 BoomerangStartPoint，到达后完成
/// </para>
/// <para>使用 MoveSpeed 控制飞行速度。BoomerangReturning 标记当前阶段。</para>
/// </summary>
public class BoomerangStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Boomerang, new BoomerangStrategy());
    }

    public void OnEnter(IEntity entity, Data data)
    {
        if (entity is Node2D node)
        {
            data.Set(DataKey.BoomerangStartPoint, node.GlobalPosition);
        }
        data.Set(DataKey.BoomerangReturning, false);
        data.Set(DataKey.BoomerangPauseTimer, 0f);
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        if (entity is not Node2D node) return 0f;

        bool returning = data.Get<bool>(DataKey.BoomerangReturning);

        // 停顿阶段检查
        float pauseTimer = data.Get<float>(DataKey.BoomerangPauseTimer);
        if (pauseTimer > 0f)
        {
            pauseTimer -= delta;
            data.Set(DataKey.BoomerangPauseTimer, Mathf.Max(pauseTimer, 0f));
            data.Set(DataKey.Velocity, Vector2.Zero);
            return 0f; // 停顿中不移动
        }

        // 确定当前目标
        Vector2 target = returning
            ? data.Get<Vector2>(DataKey.BoomerangStartPoint)
            : data.Get<Vector2>(DataKey.MoveTargetPoint);

        Vector2 toTarget = target - node.GlobalPosition;
        float dist = toTarget.Length();
        float reach = MovementHelper.GetReachDistance(data);

        if (dist <= reach)
        {
            if (!returning)
            {
                // 去程到达，切换为回程（可选停顿）
                data.Set(DataKey.BoomerangReturning, true);
                float pauseTime = data.Get<float>(DataKey.BoomerangPauseTime);
                if (pauseTime > 0f)
                {
                    data.Set(DataKey.BoomerangPauseTimer, pauseTime);
                }
                data.Set(DataKey.Velocity, Vector2.Zero);
                return 0f;
            }
            else
            {
                // 回程到达，写入精确速度由调度器执行最后一步
                data.Set(DataKey.Velocity, toTarget / Mathf.Max(delta, 0.001f));
                return -1f;
            }
        }

        float speed = data.Get<float>(DataKey.MoveSpeed);
        Vector2 dir = toTarget / dist;
        float step = Mathf.Min(speed * delta, dist);

        Vector2 velocity = dir * speed;
        data.Set(DataKey.Velocity, velocity);

        // 位移由调度器统一执行
        return step;
    }

    public void OnExit(IEntity entity, Data data)
    {
        data.Set(DataKey.BoomerangReturning, false);
        data.Set(DataKey.BoomerangPauseTimer, 0f);
    }
}
