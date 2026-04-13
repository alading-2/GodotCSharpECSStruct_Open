/// <summary>
/// TestModule 的宿主上下文。
/// <para>
/// 模块只通过这个接口访问宿主提供的最小能力，避免反向依赖具体的 <see cref="TestSystem"/> 实现。
/// </para>
/// </summary>
public interface ITestModuleHost
{
    /// <summary>当前被测试面板选中的实体。</summary>
    IEntity? SelectedEntity { get; }

    /// <summary>设置当前选中的实体。</summary>
    /// <param name="entity">新的测试目标实体。</param>
    void SetSelectedEntity(IEntity? entity);

    /// <summary>刷新当前处于前台的模块。</summary>
    void RefreshCurrentModule();
}
