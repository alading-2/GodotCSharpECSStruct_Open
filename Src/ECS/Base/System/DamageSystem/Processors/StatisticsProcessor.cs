using Godot;

/// <summary>
/// 统计处理器 - 在伤害管道末端记录统计数据
/// <para>核心职责：</para>
/// <list type="bullet">
/// <item>遍历攻击链（Attacker → 武器 → 角色），为 IUnit 和 IWeapon 累加统计</item>
/// <item>记录受害者（Victim）的波次受伤数据</item>
/// </list>
/// <para>核心机制：沿 PARENT 关系遍历攻击链，只统计 IUnit 或 IWeapon 类型的实体。</para>
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
        if (info.Attacker == null) return;

        // ===== 攻击链统计（遍历 IUnit 和 IWeapon）=====
        var ancestorChain = EntityRelationshipManager.GetAncestorChain(info.Attacker);
        bool foundAnyTarget = false;

        foreach (var entity in ancestorChain)
        {
            // 只统计 IUnit（角色）或 IWeapon（武器）
            if (entity is IUnit or IWeapon)
            {
                foundAnyTarget = true;
                RecordDamageStats(entity.Data, info);
            }
        }

        if (!foundAnyTarget)
        {
            _log.Warn($"统计处理：攻击链上未找到 IUnit 或 IWeapon，Attacker={info.Attacker}");
        }

        // ===== 受害者 (Victim) 波次伤害统计 =====
        // 注意：TotalDamageTaken 通常由 HealthComponent 内部记录
        if (info.Victim is IEntity victim)
        {
            // 记录受害者在该波次受到的总伤害
            victim.Data.Add(DataKey.WaveDamageTaken, info.FinalDamage);
        }

        // 将统计结果写入伤害日志，方便调试
        info.AddLog($"统计: 造成伤害={info.FinalDamage}, 是否暴击={info.IsCritical}");
    }

    /// <summary>
    /// 记录伤害统计到指定 Data
    /// </summary>
    /// <param name="data">目标数据容器</param>
    /// <param name="info">伤害信息</param>
    private void RecordDamageStats(Data data, DamageInfo info)
    {
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
}

