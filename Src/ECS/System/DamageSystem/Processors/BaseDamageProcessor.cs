using Godot;

/// <summary>
/// 基础伤害处理器
/// <para>初始化 BaseDamage，处理最基础的攻击力。</para>
/// </summary>
public class BaseDamageProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("BaseDamageProcessor");
    // 优先级 0：最先执行
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // 1. 无效对象检查
        if (info.Victim == null)
        {
            info.IsEnd = true;
            return;
        }

        var data = info.Victim.Data;

        // 2. 状态前置检查 (相当于原来的 PreDamageCheckProcessor)
        // 死亡检测
        if (data.Get<bool>(DataKey.IsDead))
        {
            info.IsEnd = true;
            info.FinalDamage = 0;
            _log.Debug($"[BaseDamageProcessor] 目标 {info.Victim} 已死亡(IsDead=true)，伤害阻断");
            return;
        }

        // 无敌检测
        if (data.Get<bool>(DataKey.IsInvulnerable))
        {
            info.IsEnd = true;
            info.FinalDamage = 0;
            _log.Debug($"目标 {info.Victim} 处于无敌状态，伤害无效");
            return;
        }

        // 3. 基础伤害初始化
        // 基础伤害 <= 0，伤害流程结束
        if (info.Damage <= 0)
        {
            info.IsEnd = true;
            info.FinalDamage = 0;
            info.AddLog("基础伤害 <= 0，伤害流程结束");
            _log.Debug($"[BaseDamageProcessor] Damage={info.Damage} <= 0，伤害阻断");
            return;
        }

        info.FinalDamage = info.Damage;
        info.AddLog($"基础伤害: {info.Damage}");
        _log.Debug($"[BaseDamageProcessor] 基础伤害初始化: FinalDamage={info.FinalDamage}");
    }
}
