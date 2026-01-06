using Godot;

/// <summary>
/// 统计处理器
/// <para>Priority: 700</para>
/// <para>记录总伤害统计。</para>
/// </summary>
public class StatisticsProcessor : IDamageProcessor
{
    public int Priority => 700;

    public void Process(DamageInfo info)
    {
        if (info.FinalDamage <= 0) return;

        // 记录 Instigator 造成的总伤害
        if (info.Instigator is IEntity instigatorEntity)
        {
            // 假设有一个统计用的 DataKey，或者专门的 StatisticsSystem
            // 简单处理：累加到一个特定的 DataKey "TotalDamageDealt"
            // instigatorEntity.Data.Add("TotalDamageDealt", info.FinalDamage);
        }

        // 记录 Log
        // _log.Info($"Damage Processed: {info.BaseDamage} -> {info.FinalDamage}");
    }
}
