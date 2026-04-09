using System.Collections.Generic;

/// <summary>
/// 技能测试模块共享视图模型。
/// <para>
/// 这些结构只承载“要显示什么”，不承载任何 UI 控件或运行时交互逻辑。
/// </para>
/// </summary>
internal readonly record struct AbilityCatalogItemView(
    string ResourceKey,
    string DisplayName,
    string GroupPath,
    string Description,
    AbilityType AbilityType,
    AbilityTriggerMode TriggerMode,
    bool IsOwned
);

/// <summary>
/// 当前实体已拥有技能的视图模型。
/// </summary>
internal readonly record struct AbilityOwnedItemView(
    string AbilityId,
    string DisplayName,
    string GroupPath,
    string Description,
    AbilityType AbilityType,
    AbilityTriggerMode TriggerMode,
    bool IsEnabled
);

/// <summary>
/// 同一分组路径下的一组技能条目。
/// </summary>
internal readonly record struct AbilityGroupPathGroup<TItem>(
    string GroupPath,
    IReadOnlyList<TItem> Items
);

/// <summary>
/// 技能测试模块操作结果。
/// </summary>
internal readonly record struct AbilityActionResult(
    bool Success,
    string Message
);
