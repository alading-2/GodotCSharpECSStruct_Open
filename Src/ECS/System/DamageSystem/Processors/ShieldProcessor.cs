using Godot;

/// <summary>
/// 护盾处理器
/// <para>优先于护甲生效，承受原始伤害。</para>
/// </summary>
public class ShieldProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // TODO 护盾系统做完再写
        // if (info.IsDodged || info.FinalDamage <= 0) return;
        // if (info.Victim is not IEntity victimEntity) return;

        // float shield = victimEntity.Data.Get<float>(DataKey.Shield, 0);

        // if (shield > 0)
        // {
        //     float damageToAbsorb = info.FinalDamage;

        //     if (shield >= damageToAbsorb)
        //     {
        //         // 护盾足够抵消所有伤害
        //         shield -= damageToAbsorb;
        //         info.AddLog($"Shield Absorb: {damageToAbsorb} (Remaining: {shield})");
        //         info.FinalDamage = 0;
        //     }
        //     else
        //     {
        //         // 护盾破裂，扣除护盾值，剩余伤害穿透
        //         info.FinalDamage -= shield;
        //         info.AddLog($"Shield Broken! Absorbed: {shield}, Overflow: {info.FinalDamage}");
        //         shield = 0;
        //     }

        //     // 更新护盾值
        //     victimEntity.Data.Set(DataKey.Shield, shield);
        // }
    }
}
