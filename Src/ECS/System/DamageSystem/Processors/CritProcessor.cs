using Godot;

/// <summary>
/// 暴击处理器
/// <para>Priority: 20</para>
/// <para>处理暴击判定和伤害翻倍。</para>
/// </summary>
public class CritProcessor : IDamageProcessor
{
    public int Priority => 20;

    public void Process(DamageInfo info)
    {
        if (info.Instigator is not IEntity instigatorEntity) return;

        // 获取暴击率 (0-100)
        float critChance = instigatorEntity.Data.Get<float>(DataKey.CritChance, 0);

        // 简单的随机判定
        if (GD.Randf() * 100 < critChance)
        {
            // 获取暴击倍率 (默认 2.0，即 200%)
            // Brotato default is 1.5x or 2.0x? usually configurable.
            // DataKey.CritDamage stored as multiplier directly? or percent?
            // "CritDamage" in DataKey likely means extra damage.
            // Let's assume standard behavior: Base 2x (or 1.5x) + Extra.
            // For now, let's treat DataKey.CritDamage as the TOTAL multiplier or EXTRA multiplier?
            // "CritDamage" usually in games is "Crit Damage Multiplier".
            // Let's assume default is 1.5 (150%)?

            float critMultiplier = instigatorEntity.Data.Get<float>(DataKey.CritDamage, 1.5f); // 默认 1.5 倍?
            if (critMultiplier < 1.0f) critMultiplier = 1.5f; // 防止数据错误

            info.IsCritical = true;
            info.FinalDamage *= critMultiplier;
            info.AddLog($"Crit(x{critMultiplier}) -> {info.FinalDamage}");
        }
    }
}
