using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 10】附着宿主。
/// <para>每帧将实体位置对齐到 <c>TargetNode</c>，叠加 <c>DataKey.EffectOffset</c> 偏移。宿主失效时主动完成。</para>
/// <para>通常由 <c>EffectComponent.SetupAttachment()</c> 自动触发，手动调用时：
/// <list type="bullet">
/// <item><c>TargetNode</c>（Node2D，必须）：宿主节点引用，通过 <c>MovementParams</c> 传入。</item>
/// <item><c>DestroyOnComplete</c>（bool，可选）：宿主失效后是否自动销毁实体。</item>
/// <item><c>DataKey.EffectOffset</c>：相对宿主的位置偏移，通过 Data 设置。</item>
/// </list>
/// </para>
/// <para>【典型用途】持续特效、挂载标记、附着伤害区域。</para>
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
    /// 宿主引用会在组件的 `ResetMovementState()` 中统一移除。
    /// </para>
    /// </summary>
    public void OnExit(IEntity entity, Data data)
    {
    }
}
