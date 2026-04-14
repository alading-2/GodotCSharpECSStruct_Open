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
public abstract partial class TestModuleBase : VBoxContainer, ITestModule
{
    /// <summary>统一模块上下文，提供宿主、选中实体与刷新调度。</summary>
    private ITestModuleContext _moduleContext = null!;

    /// <summary>当前挂载的 TestSystem 上下文，供子模块访问统一的测试入口与公共工具。</summary>
    protected TestSystem testSystem = null!;

    /// <summary>当前被 TestSystem 选中的实体，子模块刷新时直接读取即可。</summary>
    protected IEntity? selectedEntity;

    /// <summary>当前模块是否已经请求过宿主调度刷新。</summary>
    private bool _refreshRequested;

    /// <summary>当前模块运行态。</summary>
    internal TestModuleRunState ModuleState { get; private set; }

    /// <summary>当前模块是否处于激活态；只有激活模块才允许持有高频订阅与刷新。</summary>
    protected bool IsModuleActive => ModuleState == TestModuleRunState.Active;

    /// <summary>当前模块是否允许执行刷新逻辑。</summary>
    protected bool CanRefresh => IsModuleActive && IsVisibleInTree();

    /// <summary>模块定义信息。</summary>
    internal abstract TestModuleDefinition Definition { get; }

    /// <summary>模块根节点，供宿主统一显示和隐藏。</summary>
    Control ITestModule.ModuleRoot => this;

    /// <summary>
    /// 子模块初始化入口。
    /// <para>
    /// 基类统一负责保存 TestSystem 引用、隐藏模块节点，并设置为可扩展布局；
    /// 子类只需要在调用 base 后继续构建自己的 UI 即可。
    /// </para>
    /// </summary>
    internal virtual void Initialize(ITestModuleContext context)
    {
        _moduleContext = context;
        testSystem = context.Host;
        selectedEntity = context.SelectedEntity;
        Visible = false;
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        ModuleState = TestModuleRunState.Initialized;
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
    internal void ActivateModule()
    {
        if (ModuleState == TestModuleRunState.Active)
        {
            return;
        }

        var previousState = ModuleState;
        ModuleState = TestModuleRunState.Active;
        if (previousState == TestModuleRunState.Suspended)
        {
            OnResumed();
            return;
        }

        OnActivated();
    }

    internal void DeactivateModule()
    {
        if (ModuleState == TestModuleRunState.Inactive)
        {
            return;
        }

        CancelScheduledRefresh();
        var previousState = ModuleState;
        ModuleState = TestModuleRunState.Inactive;
        if (previousState == TestModuleRunState.Active)
        {
            OnDeactivated();
        }
    }

    internal void SuspendModule()
    {
        if (ModuleState != TestModuleRunState.Active)
        {
            return;
        }

        CancelScheduledRefresh();
        ModuleState = TestModuleRunState.Suspended;
        OnSuspended();
    }

    internal virtual void OnActivated()
    {
    }

    /// <summary>模块离开当前页时回调，可在这里释放订阅或暂停刷新。</summary>
    internal virtual void OnDeactivated()
    {
    }

    /// <summary>模块因宿主面板隐藏而进入暂停态时回调。</summary>
    internal virtual void OnSuspended()
    {
        OnDeactivated();
    }

    /// <summary>模块从暂停态恢复到前台时回调。</summary>
    internal virtual void OnResumed()
    {
        OnActivated();
    }

    /// <summary>外部请求刷新模块 UI 时回调，子类负责把当前实体状态重新渲染到界面。</summary>
    internal virtual void Refresh()
    {
        RequestScheduledRefresh();
    }

    /// <summary>
    /// 请求宿主在帧末统一刷新当前模块。
    /// </summary>
    protected void RequestScheduledRefresh()
    {
        if (_refreshRequested)
        {
            return;
        }

        _refreshRequested = true;
        _moduleContext.RefreshScheduler.Request(this);
    }

    /// <summary>
    /// 冲刷当前模块的待刷新请求。
    /// </summary>
    internal void FlushScheduledRefreshInternal()
    {
        if (!_refreshRequested)
        {
            return;
        }

        _refreshRequested = false;
        if (!CanRefresh)
        {
            return;
        }

        FlushScheduledRefresh();
    }

    /// <summary>
    /// 取消当前模块挂起的刷新请求。
    /// </summary>
    internal void CancelScheduledRefresh()
    {
        _refreshRequested = false;
        _moduleContext.RefreshScheduler.Cancel(this);
    }

    /// <summary>
    /// 由宿主统一调度的实际刷新入口。
    /// </summary>
    protected virtual void FlushScheduledRefresh()
    {
    }

    TestModuleDefinition ITestModule.Definition => Definition;

    void ITestModule.Initialize(ITestModuleContext context)
    {
        Initialize(context);
    }

    void ITestModule.OnSelectedEntityChanged(IEntity? entity)
    {
        OnSelectedEntityChanged(entity);
    }

    void ITestModule.ActivateModule()
    {
        ActivateModule();
    }

    void ITestModule.DeactivateModule()
    {
        DeactivateModule();
    }

    void ITestModule.SuspendModule()
    {
        SuspendModule();
    }

    void ITestModule.Refresh()
    {
        Refresh();
    }

    void ITestModule.FlushScheduledRefreshInternal()
    {
        FlushScheduledRefreshInternal();
    }

    void ITestModule.CancelScheduledRefresh()
    {
        CancelScheduledRefresh();
    }
}
