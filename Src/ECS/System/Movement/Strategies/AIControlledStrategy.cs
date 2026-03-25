using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 12】AI 决策驱动移动 - 专用于 CharacterBody2D 敌人实体
/// <para>
/// 读取 AI 行为树写入的 <c>DataKey.AIMoveDirection</c> 和 <c>DataKey.AIMoveSpeedMultiplier</c>，
/// 计算速度向量后写入 <c>DataKey.Velocity</c>。
/// 不直接修改 GlobalPosition，由 EntityMovementComponent 的 _PhysicsProcess 调用
/// VelocityResolver + MoveAndSlide 完成实际物理移动。
/// </para>
/// <para>
/// 读取参数：
/// - <c>DataKey.AIMoveDirection</c>：AI 决策的移动方向（归一化向量）
/// - <c>DataKey.AIMoveSpeedMultiplier</c>：AI 决策的速度倍率（0=停步, 1=全速）
/// - <c>DataKey.MoveSpeed</c>：实体的基础移动速度
/// </para>
/// <para>
/// 写入参数：
/// - <c>DataKey.Velocity</c>：本帧目标速度（由 PhysicsProcess 进行 VelocityResolver 合成）
/// </para>
/// <para>
/// 配套 AI 系统：所有写入 AIMoveDirection / AIMoveSpeedMultiplier 的 AI 动作节点
/// 会在 MoveMode 非 AIControlled 时自动暂停（不覆盖当前移动模式）。
/// </para>
/// </summary>
public class AIControlledStrategy : IMovementStrategy
{
    /// <summary>
    /// 注册 AI 驱动策略到全局注册表
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.AIControlled, new AIControlledStrategy());
    }

    /// <inheritdoc/>
    public float Update(IEntity entity, Data data, float delta)
    {
        Vector2 moveDirection = data.Get<Vector2>(DataKey.AIMoveDirection);
        float speedMultiplier = data.Get<float>(DataKey.AIMoveSpeedMultiplier);
        float moveSpeed = data.Get<float>(DataKey.MoveSpeed);

        Vector2 velocity = moveDirection * moveSpeed * speedMultiplier;
        data.Set(DataKey.Velocity, velocity);

        // 返回估算位移量（供 AccumulateTravel 统计，实际位移由 MoveAndSlide 决定）
        return velocity.Length() * delta;
    }
}
