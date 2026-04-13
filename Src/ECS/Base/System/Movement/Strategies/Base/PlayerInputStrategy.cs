using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】玩家输入驱动移动。
/// <para>每帧读取 <c>InputManager</c> 移动输入，结合 <c>MoveSpeed</c>/<c>Acceleration</c> 平滑插值 Velocity。通常设为玩家的 <c>DefaultMoveMode</c>，临时运动完成后自动回退。</para>
/// <para>所需 Data（实体属性，非 MovementParams）：<c>DataKey.MoveSpeed</c>（最大速度），<c>DataKey.Acceleration</c>（平滑系数）。</para>
/// <para>【典型用途】玩家常驻移动，冲刺/击退后自动恢复为本模式。</para>
/// </summary>
public class PlayerInputStrategy : IMovementStrategy
{
    /// <summary>
    /// 注册玩家输入策略到全局注册表。
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.PlayerInput, () => new PlayerInputStrategy());
    }

    /// <inheritdoc/>
    public bool UsePhysicsProcess => true;

    /// <summary>
    /// 读取输入方向，按加速度平滑插值到目标速度。
    /// </summary>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        float speed = data.Get<float>(DataKey.FinalMoveSpeed); // 最终移动速度
        float acceleration = data.Get<float>(DataKey.Acceleration); // 速度插値系数，越大响应越快

        Vector2 inputDir = InputManager.GetMoveInput(); // 输入系统给出的移动方向
        Vector2 targetVelocity = inputDir.Normalized() * speed;
        Vector2 currentVelocity = data.Get<Vector2>(DataKey.Velocity); // 上一帧基础速度，用于平滑过渡

        // Lerp 平滑加速（指数衰减公式，帧率无关）
        Vector2 newVelocity = currentVelocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-acceleration * delta));

        data.Set(DataKey.Velocity, newVelocity);

        // 返回估算位移量（供 AccumulateTravel 统计，实际位移由 MoveAndSlide 决定）
        return MovementUpdateResult.Continue(newVelocity.Length() * delta);
    }
}
