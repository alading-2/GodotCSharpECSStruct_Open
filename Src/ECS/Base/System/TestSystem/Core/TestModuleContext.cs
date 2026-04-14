/// <summary>
/// TestSystem 模块上下文。
/// <para>
/// 统一向模块注入宿主、选中实体上下文与刷新调度器。
/// </para>
/// </summary>
internal sealed class TestModuleContext
    : ITestModuleContext
{
    public TestModuleContext(
        TestSystem host, // 宿主系统
        TestSelectionContext selection, // 选中实体上下文
        TestRefreshScheduler refreshScheduler // 统一刷新调度器
    )
    {
        Host = host;
        Selection = selection;
        RefreshScheduler = refreshScheduler;
    }

    /// <summary>宿主 TestSystem。</summary>
    public TestSystem Host { get; }

    /// <summary>统一选中实体上下文。</summary>
    public TestSelectionContext Selection { get; }

    /// <summary>统一刷新调度器。</summary>
    public TestRefreshScheduler RefreshScheduler { get; }

    /// <summary>当前被测试面板选中的实体。</summary>
    public IEntity? SelectedEntity => Selection.SelectedEntity;
}
