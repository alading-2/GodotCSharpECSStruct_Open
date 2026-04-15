using System.Collections.Generic;

/// <summary>
/// 技能测试模块共享视图模型。
/// <para>
/// 这些结构只承载“要显示什么”，不承载任何 UI 控件或运行时交互逻辑。
/// </para>
/// </summary>
/// <param name="ResourceKey">技能配置资源键（用于添加技能）。</param>
/// <param name="DisplayName">技能显示名称。</param>
/// <param name="FeatureGroupId">技能分组 ID，直接来自 AbilityConfig.FeatureGroupId。</param>
/// <param name="Description">技能描述文本（用于提示信息）。</param>
/// <param name="AbilityType">技能类型（主动/被动/武器等）。</param>
/// <param name="TriggerMode">触发模式（主动/自动等）。</param>
/// <param name="IsOwned">当前实体是否已拥有该技能。</param>
internal readonly record struct AbilityCatalogItemView(
    string ResourceKey,
    string DisplayName,
    string FeatureGroupId,
    string Description,
    AbilityType AbilityType,
    AbilityTriggerMode TriggerMode,
    bool IsOwned
);

/// <summary>
/// 当前实体已拥有技能的视图模型。
/// </summary>
/// <param name="AbilityId">运行时技能实例 Id（用于移除/启停）。</param>
/// <param name="DisplayName">技能显示名称。</param>
/// <param name="FeatureGroupId">技能分组 ID，直接来自运行时 DataKey.AbilityFeatureGroup。</param>
/// <param name="Description">技能描述文本（用于提示信息）。</param>
/// <param name="AbilityType">技能类型（主动/被动/武器等）。</param>
/// <param name="TriggerMode">触发模式（主动/自动等）。</param>
/// <param name="IsEnabled">运行时是否启用。</param>
internal readonly record struct AbilityOwnedItemView(
    string AbilityId,
    string DisplayName,
    string FeatureGroupId,
    string Description,
    AbilityType AbilityType,
    AbilityTriggerMode TriggerMode,
    bool IsEnabled
);

/// <summary>
/// 同一 FeatureGroupId 下的一组技能条目。
/// </summary>
/// <param name="FeatureGroupId">技能分组 ID。</param>
/// <param name="Items">属于该分组的条目列表。</param>
internal readonly record struct AbilityFeatureGroup<TItem>(
    string FeatureGroupId,
    IReadOnlyList<TItem> Items
);
