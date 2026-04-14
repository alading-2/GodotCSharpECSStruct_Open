/// <summary>
/// TestSystem 模块定义。
/// <para>
/// 统一描述模块 Id、显示名与排序，避免宿主依赖场景挂载顺序。
/// </para>
/// </summary>
internal readonly record struct TestModuleDefinition(
    string Id, // 稳定模块 Id
    string DisplayName, // 下拉显示名称
    int SortOrder = 0 // 模块排序权重，越小越靠前
);
