using Godot;
using System;

/// <summary>
/// 生命值组件
/// <para>
/// 遵循无状态设计：所有数据存储在 Entity.Data 中。
/// 本组件只负责逻辑处理和事件分发。
/// </para>
/// </summary>
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    // ================= IComponent 实现 =================

    private Data? _data;

    public void OnComponentRegistered(Node entity)
    {
        // 组件注册时缓存 Data 引用
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理事件
        Damaged = null;
        Died = null;
    }

    // ================= 事件 =================

    public event Action<float>? Damaged;
    public event Action? Died;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        _log.Debug($"就绪, MaxHp: {_data?.Get<float>(DataKey.MaxHp, 100f)}");
    }

    public override void _ExitTree()
    {
        // 清理事件
        Damaged = null;
        Died = null;
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// 造成伤害
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_data == null) return;

        float currentHp = _data.Get<float>(DataKey.CurrentHp);
        currentHp -= amount;
        _data.Set(DataKey.CurrentHp, currentHp);

        Damaged?.Invoke(amount);

        if (currentHp <= 0)
        {
            Died?.Invoke();
        }
    }

    /// <summary>
    /// 重置生命值（对象池复用时调用）
    /// </summary>
    public void Reset()
    {
        if (_data == null) return;

        // 从 Data 获取 MaxHp 并重置 CurrentHp
        float maxHp = _data.Get<float>(DataKey.MaxHp, 100f);
        _data.Set(DataKey.CurrentHp, maxHp);
    }
}
