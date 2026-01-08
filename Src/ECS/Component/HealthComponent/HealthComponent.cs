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
            _data.On(DataKey.CurrentHp, OnCurrentHpChanged);
        }
    }

    public void OnComponentUnregistered()
    {
        if (_data != null)
        {
            _data.Off(DataKey.CurrentHp, OnCurrentHpChanged);
        }

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
        _log.Debug($"就绪, MaxHp: {_data?.Get<float>(DataKey.BaseHp, 100f)}");
    }

    public override void _ExitTree()
    {
        if (_data != null)
        {
            _data.Off(DataKey.CurrentHp, OnCurrentHpChanged);
        }

        // 清理事件
        Damaged = null;
        Died = null;
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// 监听血量变化
    /// </summary>
    private void OnCurrentHpChanged(object? oldValueObj, object? newValueObj)
    {
        float oldValue = Convert.ToSingle(oldValueObj);
        float newValue = Convert.ToSingle(newValueObj);
        float delta = newValue - oldValue;

        if (delta < 0)
        {
            Damaged?.Invoke(Mathf.Abs(delta));
        }
        // else if (delta > 0) { Healed?.Invoke(delta); }

        // 死亡判定：从有血变成没血
        if (newValue <= 0 && oldValue > 0)
        {
            Died?.Invoke();
        }
    }

    /// <summary>
    /// 造成伤害
    /// </summary>
    /// <summary>
    /// 修改生命值
    /// <param name="amount">正数回血，负数扣血</param>
    /// </summary>
    public void ModifyHealth(float amount)
    {
        if (_data == null) return;

        float currentHp = _data.Get<float>(DataKey.CurrentHp);
        float maxHp = _data.Get<float>(DataKey.BaseHp, 100f);

        // 应用修改
        float newHp = currentHp + amount;

        // 限制范围
        if (newHp > maxHp) newHp = maxHp;
        if (newHp < 0) newHp = 0;

        _data.Set(DataKey.CurrentHp, newHp);
        // 事件触发移交给了 OnCurrentHpChanged
    }

    /// <summary>
    /// 重置生命值（对象池复用时调用）
    /// </summary>
    public void Reset()
    {
        if (_data == null) return;

        // 从 Data 获取 MaxHp 并重置 CurrentHp
        float maxHp = _data.Get<float>(DataKey.BaseHp, 100f);
        _data.Set(DataKey.CurrentHp, maxHp);
    }
}
