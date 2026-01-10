using Godot;

/// <summary>
/// 恢复组件 - 标记单位拥有恢复能力
///
/// 核心职责：
/// - 标记"此单位拥有恢复能力"
/// - 向 RecoverySystem 注册/注销
/// - 监听恢复属性变化，动态管理注册状态
///
/// 设计原则：
/// - Component + System 分离架构
/// - 恢复逻辑由 RecoverySystem 统一处理
/// - 智能注册：只有恢复属性 > 0 时才注册
/// </summary>
public partial class RecoveryComponent : Node, IComponent
{
    private static readonly Log _log = new("RecoveryComponent");

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;

            // 监听恢复属性变化
            _entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
                GameEventType.Data.PropertyChanged,
                OnDataPropertyChanged
            );
        }

        // 检查是否需要注册
        TryRegister();
    }

    public void OnComponentUnregistered()
    {
        // 注销恢复
        TryUnregister();

        _entity = null;
        _data = null;
    }

    /// <summary>
    /// 重置组件状态（Entity归还对象池时调用）
    /// </summary>
    public void OnComponentReset()
    {
        TryUnregister();
    }

    // ================= 注册管理 =================

    /// <summary>
    /// 尝试向 RecoverySystem 注册
    /// </summary>
    private void TryRegister()
    {
        if (_entity == null || _data == null || _data.Get<bool>(DataKey.IsRecoverySystemRegistered))
            return;

        // 检查是否有任何恢复属性 > 0
        if (!ShouldRegister())
            return;

        RecoverySystem.Instance?.Register(_entity);
        _data.Set(DataKey.IsRecoverySystemRegistered, true);
        _log.Debug($"已注册到 RecoverySystem: {(_entity as Node)?.Name}");
    }

    /// <summary>
    /// 尝试从 RecoverySystem 注销
    /// </summary>
    private void TryUnregister()
    {
        if (_entity == null || _data == null || !_data.Get<bool>(DataKey.IsRecoverySystemRegistered))
            return;

        RecoverySystem.Instance?.Unregister(_entity);
        _data.Set(DataKey.IsRecoverySystemRegistered, false);
        _log.Debug($"已从 RecoverySystem 注销: {(_entity as Node)?.Name}");
    }

    /// <summary>
    /// 判断是否应该注册到恢复系统
    /// </summary>
    private bool ShouldRegister()
    {
        if (_data == null) return false;

        float hpRegen = _data.Get<float>(DataKey.FinalHpRegen);
        float manaRegen = _data.Get<float>(DataKey.FinalManaRegen);

        return hpRegen > 0 || manaRegen > 0;
    }

    // ================= 事件监听 =================

    /// <summary>
    /// 监听数据属性变化
    /// </summary>
    private void OnDataPropertyChanged(GameEventType.Data.PropertyChangedEventData evt)
    {
        // 只关心恢复属性的变化
        if (evt.Key != DataKey.FinalHpRegen && evt.Key != DataKey.FinalManaRegen)
            return;

        // 动态调整注册状态
        if (ShouldRegister())
        {
            TryRegister();
        }
        else
        {
            TryUnregister();
        }
    }

}
