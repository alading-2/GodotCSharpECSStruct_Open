using Godot;

/// <summary>
/// 受伤增幅处理器
/// <para>Priority: 310</para>
/// <para>处理“受到的伤害增加/减少 %” (Damage Taken Multiplier)。</para>
/// </summary>
public class DamageTakenAmplificationProcessor : IDamageProcessor
{
    public int Priority => 310;

    public void Process(DamageInfo info)
    {
        if (info.IsDodged || info.FinalDamage <= 0) return;
        if (info.Victim is not IEntity victimEntity) return;

        // 默认为 1.0 (100%)
        // 如果 < 1.0 表示减伤，> 1.0 表示易伤
        float multiplier = victimEntity.Data.Get<float>(DataKey.DamageTakenMultiplier, 1.0f);

        if (multiplier != 1.0f)
        {
            info.FinalDamage *= multiplier;
            info.AddLog($"TakenAmp({multiplier:F2}) -> {info.FinalDamage}");
        }
    }
}
