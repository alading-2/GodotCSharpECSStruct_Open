using Godot;

/// <summary>
/// 防御处理器 (护甲/减伤)
/// <para>Priority: 300</para>
/// <para>计算护甲减免。</para>
/// </summary>
public class DefenseProcessor : IDamageProcessor
{
    public int Priority => 300;

    public void Process(DamageInfo info)
    {
        if (info.IsDodged || info.FinalDamage <= 0) return;
        if (info.Type == DamageType.True) return; // 真实伤害无视护甲

        if (info.Victim is not IEntity victimEntity) return;

        float armor = victimEntity.Data.Get<float>(DataKey.Armor, 0);

        // Brotato Armor Formula: Damage Reduction % = Armor / (Armor + 15)
        // Negative Armor increases damage: Damage Increase % = 1 - 1 / (1 - Armor/20) ?? 
        // 简化实现：只处理正护甲，或者使用标准公式

        if (armor != 0)
        {
            float reduction = 0f;
            if (armor >= 0)
            {
                reduction = armor / (armor + 15.0f);
            }
            else
            {
                // 负护甲增加伤害，这里暂不处理复杂公式，或简单地视为易伤
                // 假设负护甲不提供减伤，反而增加受到的伤害?
                // Brotato Wiki: Negative armor increases damage taken.
                // Formula: taken = damage * (2 - (1 / (1 + abs(armor)/15))) ? No, let's look it up later.
                // For now, let's just handle positive armor.
            }

            // 限制最大减伤? (e.g. 90%)
            if (reduction > 0.9f) reduction = 0.9f;

            float originalParams = info.FinalDamage;
            info.FinalDamage *= (1.0f - reduction);

            info.AddLog($"Armor({armor}) reduced {(int)(originalParams - info.FinalDamage)}");
        }
    }
}
