using Godot;

/// <summary>
/// TestSystem 运行时测试模块的统一基类。
/// <para>
/// 负责承接 TestSystem 注入的运行上下文，并为各个测试模块提供统一的生命周期入口：
/// 初始化、选中实体变化、模块激活/失活、刷新界面。
/// </para>
/// <para>
/// 具体模块只需要关注自己的 UI 构建和测试逻辑，不需要重复处理测试面板接入流程。
/// </para>
/// </summary>
public abstract partial class TestModuleBase : VBoxContainer
{
    /// <summary>当前挂载的 TestSystem 上下文，供子模块访问统一的测试入口与公共工具。</summary>
    protected TestSystem testSystem = null!;

    /// <summary>当前被 TestSystem 选中的实体，子模块刷新时直接读取即可。</summary>
    protected IEntity? selectedEntity;

    /// <summary>模块在下拉列表中显示的名称。</summary>
    internal abstract string DisplayName { get; }

    /// <summary>
    /// 子模块初始化入口。
    /// <para>
    /// 基类统一负责保存 TestSystem 引用、隐藏模块节点，并设置为可扩展布局；
    /// 子类只需要在调用 base 后继续构建自己的 UI 即可。
    /// </para>
    /// </summary>
    internal virtual void Initialize(TestSystem system)
    {
        testSystem = system;
        Visible = false;
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    /// <summary>
    /// 当 TestSystem 切换当前选中实体时回调。
    /// 子类通常会在这里重置监听、缓存新实体并刷新界面。
    /// </summary>
    internal virtual void OnSelectedEntityChanged(IEntity? entity)
    {
        selectedEntity = entity;
    }

    /// <summary>模块被切换为当前页时回调，可在这里恢复订阅或执行一次性准备工作。</summary>
    internal virtual void OnActivated()
    {
    }

    /// <summary>模块离开当前页时回调，可在这里释放订阅或暂停刷新。</summary>
    internal virtual void OnDeactivated()
    {
    }

    /// <summary>外部请求刷新模块 UI 时回调，子类负责把当前实体状态重新渲染到界面。</summary>
    internal virtual void Refresh()
    {
    }
}
