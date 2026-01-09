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

        float originalParams = info.FinalDamage;
        info.FinalDamage *= MyMath.CalculateArmorDamageMultiplier(armor);

        if (armor >= 0)
        {
            info.AddLog($"护甲({armor}) 减少伤害： {(int)(originalParams - info.FinalDamage)}");
        }
        else
        {
            info.AddLog($"护甲({armor}) 增加伤害： {(int)(originalParams - info.FinalDamage)}");
        }
    }
}
