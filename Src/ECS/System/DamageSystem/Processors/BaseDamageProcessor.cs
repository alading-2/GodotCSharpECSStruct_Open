using Godot;

/// <summary>
/// 基础伤害处理器
/// <para>初始化 BaseDamage，处理最基础的攻击力。</para>
/// </summary>
public class BaseDamageProcessor : IDamageProcessor
{
    // 优先级 0：最先执行
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // 基础伤害 <= 0，伤害流程结束
        if (info.BaseDamage <= 0)
        {
            info.IsEnd = true;
            info.AddLog("基础伤害 <= 0，伤害流程结束");
            return;
        }

        info.FinalDamage = info.BaseDamage;
        info.AddLog($"基础伤害: {info.BaseDamage}");
    }
}
