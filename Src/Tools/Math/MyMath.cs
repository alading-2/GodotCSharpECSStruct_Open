

using Godot;
using System;

public static class MyMath
{
    /// <summary>
    /// 属性加成计算 finalValue = baseVal * (1 + rate / 100)
    /// </summary>
    /// <param name="baseVal">基础值</param>
    /// <param name="rate">加成比例</param>
    /// <returns>计算结果</returns>
    public static float AttributeBonusCalculation(float baseVal, float rate)
    {
        return baseVal * (1 + rate / 100);
    }

    /// <summary>
    /// 计算最终冷却时间（应用缩减）
    /// FinalTime = BaseTime * (1 - reduction / 100)
    /// </summary>
    /// <param name="baseTime">基础时间</param>
    /// <param name="reduction">缩减百分比 (0-100)</param>
    /// <returns>计算后的最终时间</returns>
    public static float CalculateFinalCooldownTime(float baseTime, float reduction)
    {
        return baseTime * (1f - reduction / 100f);
    }

    /// <summary>
    /// 护甲/魔抗减伤计算
    /// 返回的是受到伤害的倍率 (1.0 = 100% 伤害, 0.5 = 50% 伤害)
    /// <param name="armor">护甲值</param>
    /// <returns>伤害倍率: 1.5 = 150% 伤害, 0.5 = 50% 伤害</returns>
    /// </summary>
    public static float CalculateArmorDamageMultiplier(float armor)
    {
        if (armor >= 0)
        {
            // 护甲减伤公式：Damage Reduction % = Armor / (Armor + Config.ArmorCoefficient)
            float reductionRate = armor / (armor + GlobalConfig.ArmorCoefficient);
            // 限制最大减伤
            reductionRate = Mathf.Clamp(reductionRate, 0f, GlobalConfig.MaxArmorReduction / 100f);
            return 1.0f - reductionRate;
        }
        else
        {
            // === 负护甲：线性增伤 (无上限) ===
            // 逻辑：每 coefficient 点负护甲，额外增加 100% 的基础伤害。
            // 公式：Multiplier = 1 + (|Armor| / coefficient)
            // 原逻辑：rate = 1 + Abs(armor)/30; Final *= 1 + rate;
            // 这意味着 Multiplier = 1 + (1 + |armor|/30) = 2 + |armor|/30.
            float coefficient = 30f;
            float rate = 1 + Mathf.Abs(armor) / coefficient;
            return 1.0f + rate;

            // 备选方案
            // 负护甲增伤公式：Damage Increase % = damage * (2 - (1 / (1 + abs(armor)/15)))
            // float rate = 2 - 1 / (1 + Mathf.Abs(armor) / Config.ArmorCoefficient);
            // return rate;
        }
    }

    /// <summary>
    /// 检查概率是否触发
    /// </summary>
    /// <param name="chance">触发概率 (0-100)</param>
    /// <returns>是否触发</returns>
    public static bool CheckProbability(float chance)
    {
        return GD.Randf() * 100f < chance;
    }
}