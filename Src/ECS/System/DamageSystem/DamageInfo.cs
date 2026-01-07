using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 伤害类型
/// </summary>
public enum DamageType
{
    Physical, // 物理
    Magical,  // 魔法
    True      // 真实（无视护甲）
}

/// <summary>
/// 伤害标签（位掩码），用于标记伤害属性
/// </summary>
[Flags]
public enum DamageTags
{
    None = 0,
    Melee = 1 << 0,       // 近战
    Ranged = 1 << 1,      // 远程（投射物）
    Area = 1 << 2,        // 范围伤害 (AOE)
    Persistent = 1 << 3,  // 持续伤害 (DOT)
    Explosion = 1 << 4,   // 爆炸
    Engineering = 1 << 5  // 工程学
}

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
    public IUnit Victim { get; set; }

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
    /// <summary>
    /// 是否暴击
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// 是否闪避
    /// </summary>
    public bool IsDodged { get; set; }

    /// <summary>
    /// 是否被格挡（固定减伤完全抵消）
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// 是否结束，闪避、免疫、0伤害直接结束
    /// </summary>
    public bool IsEnd { get; set; }

    // === 辅助数据 ===
    public List<string> Logs { get; } = new();

    public void AddLog(string log)
    {
#if DEBUG
        Logs.Add(log);
#endif
    }
}
