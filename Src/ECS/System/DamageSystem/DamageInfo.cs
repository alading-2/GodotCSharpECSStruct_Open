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
/// 用位运算最核心的原因：支持多重属性叠加，一个伤害可能同时具有多种标签。比如近战范围伤害、远程爆炸效果。
/// </summary>
[Flags]
public enum DamageTags
{
    None = 0,
    Physical = 1 << 0,       // 物理
    Magical = 1 << 1,       // 魔法
    Melee = 1 << 2,       // 近战
    Ranged = 1 << 3,      // 远程（投射物）
    Area = 1 << 4,        // 范围伤害 (AOE)
    Persistent = 1 << 5,  // 持续伤害 (DOT)
    Explosion = 1 << 6,   // 爆炸
    Engineering = 1 << 7  // 工程学
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
    /// 伤害的直接来源（可能是子弹 Area2D、陷阱 Area2D、或者近战武器 Area2D）
    /// <para>注意：此为直接攻击来源，不一定是最终归属的角色。</para>
    /// <para>统计归属查找：使用 EntityRelationshipManager.FindAncestorOfType&lt;IUnit&gt;(Attacker) 沿 PARENT 关系向上查找角色。</para>
    /// <para>例如：子弹 → 武器 → 角色，最终归属到角色进行统计。</para>
    /// </summary>
    public Node Attacker { get; set; }

    /// <summary>
    /// 受害者实体（必须实现 IUnit 接口）
    /// </summary>
    public IUnit Victim { get; set; }

    // === 数值信息 ===
    /// <summary>
    /// 伤害
    /// </summary>
    public float Damage { get; set; }

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

    /// <summary>
    /// 是否为模拟模式（预计算）
    /// <para>如果是模拟模式，HealthExecutionProcessor 将不会实际扣血，仅记录计算结果。</para>
    /// </summary>
    public bool IsSimulation { get; set; }

    // === 辅助数据 ===
    public List<string> Logs { get; } = new();

    public void AddLog(string log)
    {
#if DEBUG
        Logs.Add(log);
#endif
    }
}
