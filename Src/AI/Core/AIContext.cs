using Godot;

/// <summary>
/// AI 处理上下文 - 传递给行为树节点的运行时信息
/// <para>
/// 包含当前实体的所有 ECS 引用和帧数据。
/// 每帧由 AIComponent 构建并传入行为树。
/// </para>
/// <para>
/// 注意：AI运行时数据统一存储在 Entity.Data 中（通过 DataKey 常量访问），
/// 不再使用独立的 Blackboard。这遵循项目"Data 是唯一数据源"的核心理念。
/// </para>
/// </summary>
public class AIContext
{
    // ================= 实体引用 =================

    /// <summary>当前 AI 实体</summary>
    public IEntity Entity { get; set; }

    /// <summary>实体数据容器（同时作为行为树节点间共享数据的载体）</summary>
    public Data Data { get; set; }

    /// <summary>实体事件总线</summary>
    public EventBus Events { get; set; }

    /// <summary>CharacterBody2D 引用（用于设置 Velocity）</summary>
    public CharacterBody2D Body { get; set; }

    // ================= 帧数据 =================

    /// <summary>当前帧 delta 时间</summary>
    public float DeltaTime { get; set; }

}
