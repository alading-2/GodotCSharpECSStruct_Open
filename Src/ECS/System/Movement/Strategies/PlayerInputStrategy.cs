using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 11】玩家输入驱动移动 - 专用于 CharacterBody2D 玩家实体
/// <para>
/// 读取 InputManager 输入，经 Lerp 平滑加速后写入 <c>DataKey.Velocity</c>。
/// 不直接修改 GlobalPosition，由 EntityMovementComponent 的 _PhysicsProcess 调用
/// VelocityResolver + MoveAndSlide 完成实际物理移动。
/// </para>
/// <para>
/// 读取参数：
/// - <c>DataKey.MoveSpeed</c>：最大移动速度
/// - <c>DataKey.Acceleration</c>：加速度因子（值越大越快到达目标速度）
/// - <c>DataKey.Velocity</c>：上一帧速度（用于 Lerp 平滑）
/// </para>
/// <para>
/// 写入参数：
/// - <c>DataKey.Velocity</c>：本帧目标速度（由 PhysicsProcess 进行 VelocityResolver 合成）
/// </para>
/// </summary>
public class PlayerInputStrategy : IMovementStrategy
{
    /// <summary>
    /// 注册玩家输入策略到全局注册表
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.PlayerInput, new PlayerInputStrategy());
    }

    /// <inheritdoc/>
    public float Update(IEntity entity, Data data, float delta)
    {
        float speed = data.Get<float>(DataKey.MoveSpeed);
        float acceleration = data.Get<float>(DataKey.Acceleration);

        Vector2 inputDir = InputManager.GetMoveInput();
        Vector2 targetVelocity = inputDir.Normalized() * speed;
        Vector2 currentVelocity = data.Get<Vector2>(DataKey.Velocity);

        // Lerp 平滑加速（指数衰减公式，帧率无关）
        Vector2 newVelocity = currentVelocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-acceleration * delta));

        data.Set(DataKey.Velocity, newVelocity);

        // 返回估算位移量（供 AccumulateTravel 统计，实际位移由 MoveAndSlide 决定）
        return newVelocity.Length() * delta;
    }
}
