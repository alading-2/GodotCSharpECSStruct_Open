using Godot;

/// <summary>
/// 闪避处理器
/// <para>Priority: 100</para>
/// <para>处理闪避逻辑，若闪避成功则伤害归零并终止后续大部分流程。</para>
/// </summary>
public class DodgeProcessor : IDamageProcessor
{
    public int Priority => 100;

    public void Process(DamageInfo info)
    {
        if (info.Victim is not IEntity victimEntity) return;

        // 必定命中的伤害类型 (如 True Damage 是否不可闪避? Brotato 中 True Damage 似乎也不可闪避?)
        // 这里假设 True Damage 不可闪避，或者由 Tags 决定。
        // 目前简化为：所有伤害都进行闪避判定。

        float dodgeChance = victimEntity.Data.Get<float>(DataKey.DodgeChance, 0);
        // Cap dodge rate (e.g. 60%)?
        if (dodgeChance > 60f) dodgeChance = 60f;

        if (GD.Randf() * 100 < dodgeChance)
        {
            info.IsDodged = true;
            info.FinalDamage = 0;
            info.AddLog("DODGED!");

            // 下游处理器需检查 IsDodged
        }
    }
}
