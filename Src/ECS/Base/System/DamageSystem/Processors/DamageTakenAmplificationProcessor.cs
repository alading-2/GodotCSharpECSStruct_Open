using Godot;

/// <summary>
/// 受伤增幅处理器
/// <para>处理“受到的伤害增加/减少 %” (Damage Taken Multiplier)。</para>
/// </summary>
public class DamageTakenAmplificationProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("DamageTakenAmplificationProcessor");
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.Victim is not IEntity victimEntity) return;

        // 默认为 1.0 (100%)
        // 如果 < 1.0 表示减伤，> 1.0 表示易伤
        float multiplier = victimEntity.Data.Get<float>(DataKey.DamageTakenMultiplier, 1.0f);
        _log.Debug($"[DamageTakenAmplificationProcessor] multiplier={multiplier}, FinalDamage前={info.FinalDamage}");

        if (multiplier != 1.0f)
        {
            info.FinalDamage *= multiplier;
            info.AddLog($"受到伤害增幅({multiplier:F2}) -> {info.FinalDamage}");
            _log.Debug($"[DamageTakenAmplificationProcessor] 应用后 FinalDamage={info.FinalDamage}");
        }
    }
}
