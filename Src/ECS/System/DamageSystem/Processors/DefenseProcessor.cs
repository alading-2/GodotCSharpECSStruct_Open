using Godot;
using System;
/// <summary>
/// 防御处理器 (护甲/减伤)
/// <para>计算护甲减免。</para>
/// </summary>
public class DefenseProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // if (info.Type == DamageType.True) return; // 真实伤害无视护甲
        float armor = info.Victim.Data.Get<float>(DataKey.Armor);

        // 护甲减伤公式：Damage Reduction % = Armor / (Armor + Config.ArmorCoefficient)
        // 负护甲增伤公式：Damage Increase % = 1 - 1 / (1 - Armor/20) ?? 
        // 减伤/增伤比例(0.0-1.0)
        float rate = 0f;
        float originalParams = info.FinalDamage;
        if (armor >= 0)
        {
            rate = armor / (armor + Config.ArmorCoefficient);
            // 限制最大减伤? (e.g. 90%)
            if (rate > 0.9f) rate = 0.9f;
            info.FinalDamage *= 1.0f - rate;
            info.AddLog($"护甲({armor}) 减少伤害： {(int)(originalParams - info.FinalDamage)}");
        }
        else
        {
            // === 负护甲：线性增伤 (无上限) ===
            // 逻辑：每 coefficient 点负护甲，额外增加 100% 的基础伤害。
            // 公式：Multiplier = 1 + (|Armor| / coefficient)
            float coefficient = 30f;
            rate = 1 + Mathf.Abs(armor) / coefficient;
            info.FinalDamage *= 1.0f + rate;

            // 备选方案
            // 负护甲增伤公式：Damage Increase % = damage * (2 - (1 / (1 + abs(armor)/15)))
            // rate = 2 - 1 / (1 + Math.Abs(armor) / Config.ArmorCoefficient);
            // info.FinalDamage *= rate;

            info.AddLog($"护甲({armor}) 增加伤害： {(int)(originalParams - info.FinalDamage)}");
        }

    }
}
