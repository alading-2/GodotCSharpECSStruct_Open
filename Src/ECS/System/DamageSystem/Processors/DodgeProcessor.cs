using Godot;

/// <summary>
/// 闪避处理器
/// <para>处理闪避逻辑，若闪避成功则伤害归零并终止后续大部分流程。</para>
/// </summary>
public class DodgeProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("DodgeProcessor");
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // 检查 Victim 有效性（主循环已处理 IsEnd）
        if (info.Victim == null) return;

        // 真实伤害不可闪避
        if (info.Type == DamageType.True)
        {
            return;
        }

        float dodgeChance = info.Victim!.Data.Get<float>(DataKey.DodgeChance);

        if (dodgeChance > 0 && GD.Randf() * 100 < dodgeChance)
        {
            // 闪避成功，伤害归零，结束伤害流程
            info.IsDodged = true;
            info.IsEnd = true;
            info.FinalDamage = 0;
            info.AddLog("闪避");
        }
    }
}
