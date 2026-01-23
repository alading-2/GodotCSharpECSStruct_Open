using Godot;

/// <summary>
/// UI基类
/// 封装Bind/Unbind通用逻辑，实现IBindableUI和IPoolable接口
/// 
/// 核心职责：
/// 1. 管理UI与Entity的绑定关系
/// 2. 自动处理对象池生命周期
/// 3. 提供虚方法供子类扩展
/// 
/// 使用方式：
/// <code>
/// public partial class MyUI : UIBase
/// {
///     protected override void OnBind()
///     {
///         // 订阅事件
///         _entity.Events.On<float>(GameEventType.Data.PropertyChanged, OnDataChanged);
///     }
///     
///     protected override void OnUnbind()
///     {
///         // 取消订阅（可选，EventBus会自动清理）
///     }
/// }
/// </code>
/// </summary>
public abstract partial class UIBase : Control, IBindableUI, IPoolable
{
    private static readonly Log _log = new Log("UIBase");

    /// <summary>当前绑定的实体</summary>
    protected IEntity? _entity;

    // ============================================================
    // IBindableUI 实现
    // ============================================================

    /// <summary>
    /// 绑定实体
    /// </summary>
    public void Bind(IEntity entity)
    {
        if (_entity != null)
        {
            _log.Warn($"[{Name}] 已绑定实体 {_entity}，将先解绑");
            Unbind();
        }

        _entity = entity;
        OnBind();
    }

    /// <summary>
    /// 解绑实体
    /// </summary>
    public void Unbind()
    {
        if (_entity == null)
        {
            return;
        }

        OnUnbind();
        _entity = null;
    }

    /// <summary>
    /// 获取当前绑定的实体
    /// </summary>
    public IEntity? GetBoundEntity() => _entity;

    // ============================================================
    // 虚方法 - 供子类重写
    // ============================================================

    /// <summary>
    /// 绑定成功回调
    /// 子类在此处订阅事件、初始化UI状态
    /// </summary>
    protected virtual void OnBind() { }

    /// <summary>
    /// 解绑回调
    /// 子类在此处取消订阅（可选，EventBus会自动清理）
    /// </summary>
    protected virtual void OnUnbind() { }

    // ============================================================
    // IPoolable 实现
    // ============================================================

    /// <summary>
    /// 从对象池取出时调用
    /// </summary>
    public virtual void OnPoolAcquire()
    {
        // 子类可重写，添加额外逻辑
    }

    /// <summary>
    /// 归还对象池时调用
    /// </summary>
    public virtual void OnPoolRelease()
    {
        // 归还时自动解绑
        Unbind();
    }

    /// <summary>
    /// 重置UI状态
    /// </summary>
    public virtual void OnPoolReset()
    {
        // 子类可重写，重置UI元素
    }
}
