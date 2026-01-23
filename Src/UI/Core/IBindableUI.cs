using Godot;

/// <summary>
/// 可绑定UI接口
/// 规范UI组件与Entity的绑定/解绑生命周期
/// 
/// 设计理念：
/// - UI不是Component，而是Entity的观察者（Observer）
/// - 通过Bind模式将UI与Entity关联
/// - 监听Entity.Events实现响应式更新
/// </summary>
public interface IBindableUI
{
    /// <summary>
    /// 绑定实体
    /// 订阅实体的事件，开始响应数据变化
    /// </summary>
    /// <param name="entity">要绑定的实体</param>
    void Bind(IEntity entity);

    /// <summary>
    /// 解绑实体
    /// 取消所有事件订阅，停止响应数据变化
    /// </summary>
    void Unbind();

    /// <summary>
    /// 获取当前绑定的实体
    /// </summary>
    /// <returns>绑定的实体，若未绑定则返回null</returns>
    IEntity? GetBoundEntity();
}
