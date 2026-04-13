using Godot;

/// <summary>
/// AI 处理上下文 - 传递给行为树节点的运行时信息
/// <para>
/// 解耦原则：AIContext 不持有 CharacterBody2D 引用。
/// AI 通过 DataKey（如 AIMoveDirection / AIMoveSpeedMultiplier）表达意图，
/// 由 EntityMovementComponent 在 MoveMode.AIControlled 下执行实际移动。
/// 需要位置信息时，通过 Entity（作为 Node2D）的 GlobalPosition 获取。
/// </para>
/// </summary>
public class AIContext
{
    /// <summary>当前 AI 实体</summary>
    public IEntity Entity { get; set; }
}

