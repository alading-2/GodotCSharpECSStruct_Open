using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】附着宿主。
/// <para>每帧将实体位置对齐到 <c>TargetNode</c>，叠加 <c>DataKey.EffectOffset</c> 偏移。宿主失效时主动完成。</para>
/// <para>
/// <list type="bullet">
/// <item><c>TargetNode</c>（Node2D，必须）：宿主节点引用。</item>
/// <item><c>DestroyOnComplete</c>（bool，可选）：宿主失效后是否自动销毁实体。</item>
/// <item><c>MaxDuration</c>（float，可选）：最大附着时长（秒），-1 = 不限制，永久附着直到宿主失效。</item>
/// <item><c>DataKey.EffectOffset</c>：相对宿主的位置偏移，通过 Data 设置（非 MovementParams）。</item>
/// </list>
/// </para>
/// <para>
/// <code>
/// 【使用示例：持续附着到目标实体】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.AttachToHost, new MovementParams
///     {
///         Mode              = MoveMode.AttachToHost,
///         TargetNode        = hostNode,    // 必须：宿主节点引用
///         MaxDuration       = -1f,         // -1 不限制，永久附着
///         DestroyOnComplete = true,        // 移动结束后自动销毁
///     }));
/// </code>
/// </para>
/// <para>【典型用途】持续特效、挂载标记、附着伤害区域。通常由 EffectComponent.SetupAttachment() 自动触发。</para>
/// </summary>
public class AttachToHostStrategy : IMovementStrategy
{
    /// <summary>
    /// 注册附着跟随策略到全局注册表。
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.AttachToHost, () => new AttachToHostStrategy());
    }

    /// <summary>
    /// 每帧根据宿主最新位置重新计算跟随速度。
    /// </summary>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D selfNode) return MovementUpdateResult.Complete();

        if (@params.TargetNode == null || !GodotObject.IsInstanceValid(@params.TargetNode))
            return MovementUpdateResult.Complete();

        var offset = data.Get<Vector2>(DataKey.EffectOffset); // Effect 系统概念，仍从 Data 读
        Vector2 toTarget = @params.TargetNode.GlobalPosition + offset - selfNode.GlobalPosition;
        data.Set(DataKey.Velocity, toTarget / Mathf.Max(delta, 0.001f));

        return MovementUpdateResult.Continue(); // 位置对齐不计入 TraveledDistance
    }

    /// <summary>
    /// 退出时没有额外实例状态需要清理。
    /// <para>
    /// 宿主引用存储于 <c>MovementParams.TargetNode</c>，随 <c>SwitchStrategy</c> 替换 <c>_params</c> 时自然失效。
    /// </para>
    /// </summary>
    public void OnExit(IEntity entity, Data data)
    {
    }
}
