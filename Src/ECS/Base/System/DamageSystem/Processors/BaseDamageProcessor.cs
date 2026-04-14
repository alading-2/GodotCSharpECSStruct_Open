using Godot;

/// <summary>
/// 基础伤害处理器
/// <para>初始化 BaseDamage，处理最基础的攻击力。</para>
/// </summary>
public class BaseDamageProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("BaseDamageProcessor", LogLevel.Warning);
    // 优先级 0：最先执行
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        // 如果Attacker已经死亡，而且DamageTags是Attack就跳过，因为死亡Entity不能造成攻击伤害
        // 子弹不一样，角色死亡后子弹依然造成伤害，因为子弹不是死亡的
        if (IsDeadAttackSource(info))
        {
            info.IsEnd = true;
            info.FinalDamage = 0;
            _log.Debug($"[BaseDamageProcessor] 攻击来源 {info.Attacker} 自身已死亡且伤害被标记为 Attack，伤害阻断");
            return;
        }

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

    /// <summary>
    /// 检查攻击源是否已死亡且伤害标签为攻击类型
    /// </summary>
    /// <param name="info">伤害信息</param>
    /// <returns>如果是已死亡的攻击源且为攻击伤害则返回true，否则返回false</returns>
    private static bool IsDeadAttackSource(DamageInfo info)
    {
        if ((info.Tags & DamageTags.Attack) == 0)
        {
            return false;
        }

        if (info.Attacker is not IEntity attackerEntity)
        {
            return false;
        }

        return attackerEntity.Data.Get<bool>(DataKey.IsDead);
    }
}
