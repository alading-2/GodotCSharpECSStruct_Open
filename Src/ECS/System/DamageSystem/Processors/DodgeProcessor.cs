using Godot;

/// <summary>
/// 闪避处理器
/// <para>处理闪避逻辑，若闪避成功则伤害归零并终止后续大部分流程。</para>
/// </summary>
public class DodgeProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // 必定命中的伤害类型 (如 True Damage 是否不可闪避? Brotato 中 True Damage 似乎也不可闪避?)
        // 这里假设 True Damage 不可闪避，或者由 Tags 决定。
        // 目前简化为：所有伤害都进行闪避判定。
        float dodgeChance = info.Victim!.Data.Get<float>(DataKey.DodgeChance, 0);

        if (GD.Randf() * 100 < dodgeChance)
        {
            // 闪避成功，伤害归零，结束伤害流程
            info.IsDodged = true;
            info.IsEnd = true;
            info.FinalDamage = 0;
            info.AddLog("闪避");
        }
    }
}
