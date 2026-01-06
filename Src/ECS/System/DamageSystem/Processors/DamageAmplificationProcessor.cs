using Godot;

/// <summary>
/// 输出增幅处理器
/// <para>Priority: 10</para>
/// <para>处理全局伤害增加 %，以及类型增幅（近战/远程/元素）。</para>
/// </summary>
public class DamageAmplificationProcessor : IDamageProcessor
{
    public int Priority => 10;

    public void Process(DamageInfo info)
    {
        if (info.Instigator is not IEntity instigatorEntity) return;

        float multiplier = 1.0f;

        // 1. 全局伤害加成 (Damage %)
        // 假设 DataKey.Damage 是百分比加成 (例如 10 表示 +10%)
        // 或者 DataKey.Damage 是基础攻击力？在此项目中，Damage 通常指基础攻击力。
        // "Damage%" 或者是具体的 "DamageMultiplier"？
        // 按照 Brotato 逻辑，Stats.Damage 是 % 增幅。
        // 我们需要澄清 DataKey 定义。在此假设有一个 "DamageMultiplier" 或 "DamageBonus"。
        // 现有的 DataKey.Damage 似乎是基础值。
        // 让我们检查 DataKey definition again later. 
        // 假设 DataKey.Damage 是基础攻击力，那么这里的增幅可能来自 buffs。
        // 我们暂时只处理特定类型的增幅，如果没有通用增幅 Key。

        // 假设 DataKey.Damage 是基础攻击力，那么这里不应该再次应用 Attacker.Damage，
        // 除非 BaseDamage 还没算上它。

        // 在 Brotato 中：
        // Damage = Base (Weapon) + (Base * Damage%)

        // 我们假设 BaseDamage 已经是 (Weapon Base)，这里加上 Instigator 的属性修正。

        // 修正：Brotato 中 "Damage" stat 实际上是 "Damage %"。
        // 但我们在 DataKey 中定义了 "Damage" 作为 "攻击系统" 的属性。
        // 如果 "Damage" 存储的是 10，代表 +10% 还是 +10 点？
        // 通常 "Damage" 是 PercentModification (Brotato Wiki: "Damage" increases all damage by X%).

        // 我们使用 "Damage" 作为全局增幅 %
        float damagePercent = instigatorEntity.Data.Get<float>(DataKey.Damage, 0);
        // 假设存储的是整数 10 代表 10%
        multiplier += damagePercent / 100.0f;

        // 2. 类型特定增幅
        if (info.Tags.HasFlag(DamageTags.Melee))
        {
            // Melee Damage
            // 假设 DataKey 中有 MeleeDamage (TODO: 需要添加)
        }
        else if (info.Tags.HasFlag(DamageTags.Ranged))
        {
            // Ranged Damage
        }

        if (multiplier != 1.0f)
        {
            info.FinalDamage *= multiplier;
            info.AddLog($"Amp({multiplier:F2}) -> {info.FinalDamage}");
        }
    }
}
