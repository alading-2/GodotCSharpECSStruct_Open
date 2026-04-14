using Godot;

/// <summary>
/// TestSystem 测试模块协议。
/// <para>
/// 宿主只依赖模块协议，不依赖具体模块实现细节。
/// </para>
/// </summary>
internal interface ITestModule
{
    /// <summary>模块定义信息。</summary>
    TestModuleDefinition Definition { get; }

    /// <summary>模块根节点。</summary>
    Control ModuleRoot { get; }

    /// <summary>初始化模块。</summary>
    void Initialize(ITestModuleContext context);

    /// <summary>选中实体变化。</summary>
    void OnSelectedEntityChanged(IEntity? entity);

    /// <summary>激活模块。</summary>
    void ActivateModule();

    /// <summary>停用模块。</summary>
    void DeactivateModule();

    /// <summary>暂停模块。</summary>
    void SuspendModule();

    /// <summary>请求刷新模块。</summary>
    void Refresh();

    /// <summary>冲刷模块挂起的刷新请求。</summary>
    void FlushScheduledRefreshInternal();

    /// <summary>取消挂起的刷新请求。</summary>
    void CancelScheduledRefresh();
}
