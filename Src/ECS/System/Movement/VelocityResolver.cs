using Godot;

/// <summary>
/// 速度分层合成器 - 将多层速度源合成为最终速度
/// <para>
/// 合成优先级（从高到低）：
/// 1. <c>IsMovementLocked</c> = true → 最终速度 = Zero（眩晕/冻结）
/// 2. <c>VelocityOverride</c> 非零 → 使用覆盖速度（击退/控制技能）
/// 3. 否则 → 使用 <c>Velocity</c> 基础速度（输入/AI/运动策略）
/// 4. 叠加 <c>VelocityImpulse</c> 瞬时冲量（爆炸推力等），用后自动清零
/// </para>
/// <para>
/// 使用方式：在 VelocityComponent / EnemyMovementComponent 的物理帧中调用
/// <c>VelocityResolver.Resolve(data)</c> 获取最终速度后应用到物理体。
/// </para>
/// </summary>
public static class VelocityResolver
{
    /// <summary>
    /// 合成最终速度向量
    /// <para>调用后会自动清零 VelocityImpulse</para>
    /// </summary>
    /// <param name="data">实体的数据容器</param>
    /// <returns>合成后的最终速度向量</returns>
    public static Vector2 Resolve(Data data)
    {
        // 1. 移动锁定检查（眩晕/冻结）
        if (data.Get<bool>(DataKey.IsMovementLocked))
        {
            ConsumeImpulse(data);
            return Vector2.Zero;
        }

        // 2. 覆盖层检查（击退/控制技能）
        Vector2 overrideVel = data.Get<Vector2>(DataKey.VelocityOverride);
        Vector2 baseVel = overrideVel.LengthSquared() > 0.001f
            ? overrideVel
            : data.Get<Vector2>(DataKey.Velocity);

        // 3. 叠加瞬时冲量
        Vector2 impulse = ConsumeImpulse(data);

        return baseVel + impulse;
    }

    /// <summary>
    /// 读取并清零 VelocityImpulse（单帧冲量只生效一次）
    /// </summary>
    private static Vector2 ConsumeImpulse(Data data)
    {
        Vector2 impulse = data.Get<Vector2>(DataKey.VelocityImpulse);
        if (impulse.LengthSquared() > 0.001f)
        {
            data.Set(DataKey.VelocityImpulse, Vector2.Zero);
        }
        return impulse;
    }
}
