/// <summary>
/// TestSystem 模块运行态。
/// </summary>
internal enum TestModuleRunState
{
    /// <summary>模块已初始化，但尚未进入前台运行。</summary>
    Initialized,

    /// <summary>模块当前处于前台激活态。</summary>
    Active,

    /// <summary>模块已注册但未激活。</summary>
    Inactive,

    /// <summary>模块因宿主面板隐藏而暂停。</summary>
    Suspended
}
