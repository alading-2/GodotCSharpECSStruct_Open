using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 伤害上下文
/// <para>承载单次伤害的所有信息，贯穿整个处理管道。</para>
/// </summary>
public class DamageInfo
{
    // === 基础信息 ===
    /// <summary>
    /// 唯一 ID，用于追踪
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// 伤害的直接来源（可能是子弹、陷阱 Area2D、或者近战武器 Area2D）
    /// </summary>
    public Node Attacker { get; set; }

    /// <summary>
    /// 伤害的始作俑者（真正的凶手，用于统计伤害）
    /// <para>如果 Attacker 是 Unit，则 Instigator == Attacker</para>
    /// <para>如果 Attacker 是子弹，则 Instigator == Attacker.Owner</para>
    /// </summary>
    public IUnit? Instigator { get; set; }

    /// <summary>
    /// 受击者实体
    /// </summary>
    public Node Victim { get; set; }

    // === 数值信息 ===
    /// <summary>
    /// 原始面板伤害
    /// </summary>
    public float BaseDamage { get; set; }

    /// <summary>
    /// 最终结算伤害
    /// </summary>
    public float FinalDamage { get; set; }

    // === 标签与类型 ===
    public DamageType Type { get; set; }
    public DamageTags Tags { get; set; }

    // === 状态标记 ===
    public bool IsCritical { get; set; }
    public bool IsDodged { get; set; }
    public bool IsBlocked { get; set; } // 固定减伤完全抵消

    // === 辅助数据 ===
    public List<string> Logs { get; } = new();

    public void AddLog(string log)
    {
#if DEBUG
        Logs.Add(log);
#endif
    }
}
