using Godot;

/// <summary>
/// 基础伤害处理器
/// <para>Priority: 0</para>
/// <para>初始化 BaseDamage，处理最基础的攻击力。</para>
/// </summary>
public class BaseDamageProcessor : IDamageProcessor
{
    // 优先级 0：最先执行
    public int Priority => 0;

    public void Process(DamageInfo info)
    {
        if (info.BaseDamage <= 0)
        {
            // 尝试从 Attacker 获取 Damage 属性 (如果未在构造 DamageInfo 时指定)
            // 目前假设调用者通常会填好 BaseDamage，或者是从子弹的 Data 中读取
            // 这里作为一个兜底或初始化逻辑

            // 如果 Attacker 是 IEntity，尝试读取 Damage
            if (info.Attacker is IEntity entity)
            {
                // info.BaseDamage = entity.Data.Get<float>(DataKey.Damage, 0);
                // 注意：通常远程伤害由子弹决定，近战由人物决定。
                // 这里不做过多假设，只记录日志。
            }
        }

        info.FinalDamage = info.BaseDamage;
        info.AddLog($"Base: {info.BaseDamage}");
    }
}
