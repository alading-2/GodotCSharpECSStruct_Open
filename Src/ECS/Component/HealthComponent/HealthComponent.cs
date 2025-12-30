using Godot;
using System;

/// <summary>
/// 生命值组件。
/// <para>
/// 遵循无状态设计：所有状态数据 (CurrentHp, MaxHp) 均存储在 AttributeComponent 中。
/// 本组件只负责逻辑处理和事件分发。
/// </para>
/// </summary>
public partial class HealthComponent : Node
{
    private static readonly Log _log = new("HealthComponent");

    // 缓存引用
    private Node _entity;
    private AttributeComponent _dataComp;

    // 事件
    public event Action<float> Damaged;
    public event Action Died;

    public override void _Ready()
    {
        _entity = GetParent();
        if (_entity == null)
        {
            _log.Error("HealthComponent 必须挂载在实体节点下");
            return;
        }

        // 懒加载获取 AttributeComponent
        _dataComp = _entity.GetComponent<AttributeComponent>(ECSIndex.Component.AttributeComponent);
    }

    /// <summary>
    /// 造成伤害。
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_dataComp == null) _dataComp = _entity.GetComponent<AttributeComponent>(ECSIndex.Component.AttributeComponent);
        if (_dataComp == null) return;

        var data = _entity.GetData(); // 直接用 NodeExtensions 获取 Data

        float currentHp = data.Get<float>("CurrentHp");
        currentHp -= amount;
        data.Set("CurrentHp", currentHp);

        Damaged?.Invoke(amount);

        if (currentHp <= 0)
        {
            Died?.Invoke();
        }
    }

    /// <summary>
    /// 重置逻辑。
    /// 确保 CurrentHp 被重置为 MaxHp。
    /// </summary>
    public void Reset()
    {
        var data = _entity.GetData();

        // 从 AttributeComponent 获取最终的 MaxHp
        float maxHp = 0;
        if (_dataComp != null)
        {
            maxHp = _dataComp.Get("MaxHp", 10f); // 假设默认 10 HP
        }
        else
        {
            // 如果没有属性组件，尝试从 Data 获取（备选）
            maxHp = data.Get<float>("MaxHp", 10f);
        }

        data.Set("CurrentHp", maxHp);
    }
}
