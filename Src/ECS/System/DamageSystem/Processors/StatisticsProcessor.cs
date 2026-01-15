using Godot;

/// <summary>
/// 统计处理器 - 在伤害管道末端记录统计数据
/// <para>核心职责：</para>
/// <list type="bullet">
/// <item>记录攻击者（IUnit）的总伤害、波次伤害、命中数、暴击数等统计数据</item>
/// <item>记录受击者（Victim）的波次受伤数据</item>
/// <item>更新单次最高伤害记录</item>
/// </list>
/// <para>核心机制：沿 PARENT 关系向上查找 IUnit（角色），将统计归属到角色。</para>
/// </summary>
public class StatisticsProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("StatisticsProcessor");
    /// <summary>
    /// 处理器的执行优先级。统计通常放在管道末端（优先级较低的值）。
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 处理伤害统计逻辑
    /// </summary>
    /// <param name="info">伤害上下文信息</param>
    public void Process(DamageInfo info)
    {
        // 闪避或无伤害跳过统计
        if (info.IsDodged || info.FinalDamage <= 0) return;

        // ===== 攻击者统计（归属到 IUnit）=====
        if (info.Attacker == null) return;

        var attackerUnit = EntityRelationshipManager.FindAncestorOfType<IUnit>(info.Attacker);
        if (attackerUnit == null)
        {
            _log.Error($"统计处理失败：无法找到归属的 IUnit，Attacker={info.Attacker}");
        }
        else
        {
            var data = attackerUnit.Data;

            // 累加总伤害和当前波次伤害
            data.Add(DataKey.TotalDamageDealt, info.FinalDamage);
            data.Add(DataKey.WaveDamageDealt, info.FinalDamage);

            // 累加总命中次数和当前波次命中次数
            data.Add(DataKey.TotalHits, 1);
            data.Add(DataKey.WaveHits, 1);

            // 如果触发暴击，记录暴击次数
            if (info.IsCritical)
            {
                data.Add(DataKey.TotalCriticalHits, 1);
                data.Add(DataKey.WaveCriticalHits, 1);
            }

            // 更新单次最高伤害记录
            float highest = data.Get<float>(DataKey.HighestSingleDamage);
            if (info.FinalDamage > highest)
            {
                data.Set(DataKey.HighestSingleDamage, info.FinalDamage);
            }
        }


        // ===== 受击者 (Victim) 波次伤害统计 =====
        // 注意：TotalDamageTaken 通常由 HealthComponent 内部记录
        if (info.Victim is IEntity victim)
        {
            // 记录受击者在该波次受到的总伤害
            victim.Data.Add(DataKey.WaveDamageTaken, info.FinalDamage);
        }

        // 将统计结果写入伤害日志，方便调试
        info.AddLog($"Stats: Dealt={info.FinalDamage}, Crit={info.IsCritical}");
    }
}

