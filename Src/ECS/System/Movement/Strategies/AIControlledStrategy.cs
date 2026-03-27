using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 12】AI 决策驱动移动。
/// <para>不做寻路/索敌，只消费 AI 层写入的方向与倍率。通常设为敌人/NPC 的 <c>DefaultMoveMode</c>，临时策略完成后自动回退。</para>
/// <para>AI 每帧持续写入 Data（非 MovementParams）：
/// <list type="bullet">
/// <item> 使用：在Entity中设置Data.Set(DataKey.DefaultMoveMode, MoveMode.AIControlled);
/// <item><c>DataKey.AIMoveDirection</c>（Vector2）：归一化移动方向，零向量 = 停步。</item>
/// <item><c>DataKey.AIMoveSpeedMultiplier</c>（float）：速度倍率，0=停，1=满速。</item>
/// <item><c>DataKey.FinalMoveSpeed</c>（float）：实体最终移动速度上限（属性系统计算后的结果）。</item>
/// </list>
/// </para>
/// <para>【典型用途】敌人追击、巡逻、游荡、逃跑等 AI 持续写方向的常驻模式。</para>
/// </summary>
public class AIControlledStrategy : IMovementStrategy
{
    /// <summary>
    /// 注册 AI 驱动策略到全局注册表。
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.AIControlled, () => new AIControlledStrategy());
    }

    /// <inheritdoc/>
    public bool UsePhysicsProcess => true;

    /// <summary>
    /// 读取 AI 层给出的方向与倍率，换算为基础移动速度。
    /// </summary>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        Vector2 moveDirection = data.Get<Vector2>(DataKey.AIMoveDirection); // AI请求移动方向
        float speedMultiplier = data.Get<float>(DataKey.AIMoveSpeedMultiplier); // AI移动速度倍率
        float moveSpeed = data.Get<float>(DataKey.FinalMoveSpeed); // 最终移动速度

        Vector2 velocity = moveDirection * moveSpeed * speedMultiplier;
        data.Set(DataKey.Velocity, velocity);

        // 返回估算位移量（供 AccumulateTravel 统计，实际位移由 MoveAndSlide 决定）
        return MovementUpdateResult.Continue(velocity.Length() * delta);
    }
}
