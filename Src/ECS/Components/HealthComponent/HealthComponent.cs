using System;
using Godot;

/// <summary>
/// 生命值组件 - 管理实体的血量、伤害计算和死亡逻辑。
/// 通过 C# Event 进行组件间通信，支持对象池复用。
/// </summary>
public partial class HealthComponent : Node
{
    private static readonly Log Log = new("HealthComponent");

    // ================= Export Properties =================

    // ================= Private State =================

    /// <summary>
    /// 父实体的动态数据容器。
    /// </summary>
    private Data _data = null!;

    // ================= Runtime State =================

    /// <summary>
    /// 最大生命值。
    /// </summary>
    public float MaxHp => _data.Get<float>("MaxHp", 100f);

    /// <summary>
    /// 当前生命值。
    /// </summary>
    public float CurrentHp
    {
        get => _data.Get<float>("CurrentHp", MaxHp);
        private set => _data.Set("CurrentHp", value);
    }

    /// <summary>
    /// 是否已死亡（CurrentHp <= 0）。
    /// </summary>
    public bool IsDead => CurrentHp <= 0;

    // ================= C# Events =================

    /// <summary>
    /// 受到伤害时触发。参数: 实际伤害值。
    /// </summary>
    public event Action<float>? Damaged;

    /// <summary>
    /// 治疗时触发。参数: 实际治疗量。
    /// </summary>
    public event Action<float>? Healed;

    /// <summary>
    /// 死亡时触发（仅触发一次）。
    /// </summary>
    public event Action? Died;

    // ================= Private State =================

    /// <summary>
    /// 标记是否已触发过死亡事件，防止重复触发。
    /// </summary>
    private bool _hasDied;

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent == null)
        {
            Log.Error("HealthComponent 错误: 必须作为实体 (Node) 的子节点存在。");
            return;
        }
        _data = parent.GetData();

        // 初始化时将 CurrentHp 设为 MaxHp
        CurrentHp = MaxHp;
        _hasDied = false;
        Log.Debug($"生命组件初始化完成: 最大血量={MaxHp}, 当前血量={CurrentHp}");
    }

    public override void _ExitTree()
    {
        // 清理所有事件订阅，防止内存泄漏
        Damaged = null;
        Healed = null;
        Died = null;
        Log.Trace("生命组件退出场景树，已清理所有事件订阅。");
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 对实体造成伤害。
    /// </summary>
    /// <param name="damage">伤害值（负值或零将被忽略）。</param>
    public void TakeDamage(float damage)
    {
        // 忽略无效伤害
        if (damage <= 0)
        {
            Log.Trace($"忽略伤害: 伤害值为 {damage} (非正数)");
            return;
        }

        // 已死亡则忽略
        if (IsDead)
        {
            Log.Trace("忽略伤害: 实体已经处于死亡状态。");
            return;
        }

        // 应用伤害
        float previousHp = CurrentHp;
        CurrentHp = Math.Max(0, CurrentHp - damage);
        float actualDamage = previousHp - CurrentHp;

        Log.Debug($"受到伤害: 造成 {actualDamage} 点伤害。血量: {previousHp} -> {CurrentHp}");

        // 触发伤害事件
        Damaged?.Invoke(actualDamage);

        // 检查死亡
        if (CurrentHp <= 0 && !_hasDied)
        {
            _hasDied = true;
            Log.Info("实体已死亡。");
            Died?.Invoke();
        }
    }

    /// <summary>
    /// 治疗实体。
    /// </summary>
    /// <param name="amount">治疗量（负值或零将被忽略）。</param>
    public void Heal(float amount)
    {
        // 忽略无效治疗
        if (amount <= 0)
        {
            Log.Trace($"忽略治疗: 治疗量为 {amount} (非正数)");
            return;
        }

        // 已满血则忽略
        if (CurrentHp >= MaxHp)
        {
            Log.Trace("忽略治疗: 血量已满。");
            return;
        }

        // 应用治疗（不超过 MaxHp）
        float previousHp = CurrentHp;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        float actualHeal = CurrentHp - previousHp;

        Log.Debug($"治疗成功: 恢复 {actualHeal} 点血量。血量: {previousHp} -> {CurrentHp}");

        // 触发治疗事件
        Healed?.Invoke(actualHeal);
    }

    /// <summary>
    /// 重置组件状态（用于对象池复用）。
    /// </summary>
    public void Reset()
    {
        CurrentHp = MaxHp;
        _hasDied = false;
        Log.Debug($"生命组件已重置: 当前血量={CurrentHp}");
    }
}
