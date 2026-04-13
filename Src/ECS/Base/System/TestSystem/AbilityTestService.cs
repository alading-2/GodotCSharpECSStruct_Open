using Godot;
using Slime.Config.Abilities;
using System;
using System.Collections.Generic;

/// <summary>
/// 技能测试模块的数据与操作服务。
/// <para>
/// 负责技能配置目录缓存、分类解析、当前实体技能视图构建，以及增删启停等操作。
/// UI 只消费这里返回的视图模型，不直接关心资源扫描和业务细节。
/// </para>
/// </summary>
internal sealed class AbilityTestService
{
    /// <summary>技能分组兜底路径：当配置与运行时数据都无法提供分组时使用。</summary>
    private const string DefaultGroupPath = "技能.未分类";

    /// <summary>Feature 调试服务，用于复用正式链路执行授予、移除与启停。</summary>
    private readonly FeatureDebugService _featureDebugService = new();

    /// <summary>缓存全部技能配置（ResourceKey → AbilityConfig）。</summary>
    private readonly Dictionary<string, AbilityConfig> _configByKey = new(StringComparer.Ordinal);

    /// <summary>缓存技能库顺序，供左侧分类树稳定展示。</summary>
    private readonly List<AbilityConfigEntry> _catalogEntries = new();

    /// <summary>记录分组路径的首次出现顺序，避免界面每次刷新都乱序。</summary>
    private readonly Dictionary<string, int> _groupPathOrder = new(StringComparer.Ordinal);

    /// <summary>
    /// 技能配置缓存项。
    /// <para>
    /// 包含原始配置、资源键和解析后的分类信息，便于 UI 层直接消费。
    /// </para>
    /// </summary>
    private sealed record AbilityConfigEntry(
        string ResourceKey,
        string AbilityName,
        string DisplayName,
        string GroupPath,
        string Description,
        AbilityType AbilityType,
        AbilityTriggerMode TriggerMode
    );

    /// <summary>
    /// 初始化技能测试服务并预热技能目录缓存。
    /// </summary>
    public AbilityTestService()
    {
        LoadAllAbilityConfigs();
    }

    /// <summary>
    /// 为当前实体添加一个技能实例。
    /// </summary>
    /// <param name="owner">技能拥有者实体，为空时返回失败结果。</param>
    /// <param name="resourceKey">技能资源键（ResourceKey）。</param>
    /// <returns>添加操作结果，包含成功标记与提示文本。</returns>
    public AbilityActionResult AddAbility(IEntity? owner, string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return Fail("技能资源键不能为空");
        }

        if (!_configByKey.TryGetValue(resourceKey, out var config))
        {
            return Fail($"未找到技能配置: {resourceKey}");
        }

        var result = _featureDebugService.GrantAbility(owner, config, resourceKey);
        return new AbilityActionResult(result.Success, result.Message);
    }

    /// <summary>
    /// 移除指定技能实例。
    /// </summary>
    /// <param name="owner">技能拥有者实体，为空时返回失败结果。</param>
    /// <param name="abilityId">运行时技能实例 Id。</param>
    /// <returns>移除操作结果，包含成功标记与提示文本。</returns>
    public AbilityActionResult RemoveAbility(IEntity? owner, string abilityId)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return Fail("技能实例 Id 不能为空");
        }

        var ability = FindOwnedAbility(owner, abilityId);
        var result = _featureDebugService.RemoveAbility(owner, ability);
        return new AbilityActionResult(result.Success, result.Message);
    }

    /// <summary>
    /// 切换指定技能实例的启用状态。
    /// </summary>
    /// <param name="owner">技能拥有者实体，为空时返回失败结果。</param>
    /// <param name="abilityId">运行时技能实例 Id。</param>
    /// <param name="isEnabled">目标启用状态，true 为启用，false 为禁用。</param>
    /// <returns>启停操作结果，包含成功标记与提示文本。</returns>
    public AbilityActionResult SetAbilityEnabled(IEntity? owner, string abilityId, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return Fail("技能实例 Id 不能为空");
        }

        var ability = FindOwnedAbility(owner, abilityId);
        var result = _featureDebugService.SetFeatureEnabled(owner, ability, isEnabled);
        return new AbilityActionResult(result.Success, result.Message);
    }

    /// <summary>
    /// 按技能实例 Id 查询当前实体的技能视图。
    /// </summary>
    /// <param name="owner">技能拥有者实体。</param>
    /// <param name="abilityId">运行时技能实例 Id。</param>
    /// <param name="itemView">输出的技能视图数据。</param>
    /// <returns>存在对应技能实例时返回 true，否则返回 false。</returns>
    public bool TryGetOwnedItem(IEntity? owner, string abilityId, out AbilityOwnedItemView itemView)
    {
        itemView = default;
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return false;
        }

        var ability = FindOwnedAbility(owner, abilityId);
        if (ability == null)
        {
            return false;
        }

        itemView = CreateOwnedItemView(ability);
        return true;
    }

    /// <summary>
    /// 获取技能库视图，并根据当前实体标记“已拥有”状态。
    /// </summary>
    /// <param name="owner">当前选中的实体，可为空。</param>
    /// <returns>按分组路径组织后的技能库视图集合。</returns>
    public IReadOnlyList<AbilityGroupPathGroup<AbilityCatalogItemView>> GetCatalogGroups(IEntity? owner)
    {
        var ownedNames = new HashSet<string>(StringComparer.Ordinal);
        if (owner != null)
        {
            foreach (var ability in EntityManager.GetAbilities(owner))
            {
                var abilityName = ability.Data.Get<string>(nameof(DataKey.Name));
                if (!string.IsNullOrWhiteSpace(abilityName))
                {
                    ownedNames.Add(abilityName);
                }
            }
        }

        var views = new List<AbilityCatalogItemView>(_catalogEntries.Count);
        foreach (var entry in _catalogEntries)
        {
            views.Add(new AbilityCatalogItemView(
                entry.ResourceKey,
                entry.DisplayName,
                entry.GroupPath,
                entry.Description,
                entry.AbilityType,
                entry.TriggerMode,
                ownedNames.Contains(entry.AbilityName)
            ));
        }

        return BuildGroupPathGroups(
            views,
            static item => item.GroupPath,
            static item => item.DisplayName
        );
    }

    /// <summary>
    /// 获取当前实体已拥有技能的分类视图。
    /// </summary>
    /// <param name="owner">当前选中的实体，可为空。</param>
    /// <returns>按分组路径组织后的已拥有技能视图集合。</returns>
    public IReadOnlyList<AbilityGroupPathGroup<AbilityOwnedItemView>> GetOwnedGroups(IEntity? owner)
    {
        var views = new List<AbilityOwnedItemView>();
        if (owner == null)
        {
            return Array.Empty<AbilityGroupPathGroup<AbilityOwnedItemView>>();
        }

        foreach (var ability in EntityManager.GetAbilities(owner))
        {
            views.Add(CreateOwnedItemView(ability));
        }

        return BuildGroupPathGroups(
            views,
            static item => item.GroupPath,
            static item => item.DisplayName
        );
    }

    /// <summary>
    /// 加载全部技能配置，并提前计算展示所需的分类信息。
    /// </summary>
    private void LoadAllAbilityConfigs()
    {
        _configByKey.Clear();
        _catalogEntries.Clear();
        _groupPathOrder.Clear();

        if (!ResourcePaths.Resources.TryGetValue(ResourceCategory.DataAbility, out var entries))
        {
            return;
        }

        foreach (var (resourceKey, resourceData) in entries)
        {
            var config = ResourceManagement.Load<AbilityConfig>(resourceKey, ResourceCategory.DataAbility);
            if (config == null)
            {
                continue;
            }

            var displayName = string.IsNullOrWhiteSpace(config.Name) ? resourceKey : config.Name!;
            var groupPath = ResolveGroupPath(config, resourceData.Path);
            var description = string.IsNullOrWhiteSpace(config.Description)
                ? "暂无描述"
                : config.Description!;

            _configByKey[resourceKey] = config;
            _catalogEntries.Add(new AbilityConfigEntry(
                resourceKey,
                displayName,
                displayName,
                groupPath,
                description,
                config.AbilityType,
                config.AbilityTriggerMode
            ));

            RegisterGroupPathOrder(groupPath);
        }

        _catalogEntries.Sort((left, right) =>
        {
            var groupPathCompare = CompareGroupPath(left.GroupPath, right.GroupPath);
            if (groupPathCompare != 0)
            {
                return groupPathCompare;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// 构建当前技能实例的视图模型。
    /// </summary>
    private AbilityOwnedItemView CreateOwnedItemView(AbilityEntity ability)
    {
        var abilityName = ability.Data.Get<string>(DataKey.Name);
        var groupPath = ResolveGroupPath(ability);
        var description = ability.Data.Get<string>(DataKey.Description);
        var abilityId = ability.Data.Get<string>(DataKey.Id);
        var isEnabled = ability.Data.Get<bool>(DataKey.FeatureEnabled);
        var abilityType = (AbilityType)ability.Data.Get<int>(DataKey.AbilityType);
        var triggerMode = (AbilityTriggerMode)ability.Data.Get<int>(DataKey.AbilityTriggerMode);

        RegisterGroupPathOrder(groupPath);

        return new AbilityOwnedItemView(
            abilityId,
            abilityName,
            groupPath,
            string.IsNullOrWhiteSpace(description) ? "暂无描述" : description,
            abilityType,
            triggerMode,
            isEnabled
        );
    }

    /// <summary>
    /// 按运行时技能实例 Id 查找技能实体。
    /// </summary>
    private static AbilityEntity? FindOwnedAbility(IEntity? owner, string abilityId)
    {
        if (owner == null || string.IsNullOrWhiteSpace(abilityId))
        {
            return null;
        }

        foreach (var ability in EntityManager.GetAbilities(owner))
        {
            var currentAbilityId = ability.Data.Get<string>(DataKey.Id);
            if (string.Equals(currentAbilityId, abilityId, StringComparison.Ordinal))
            {
                return ability;
            }
        }

        return null;
    }

    /// <summary>
    /// 解析技能展示分组路径。
    /// <para>
    /// 统一使用 FeatureGroupId；若旧资源尚未补齐，则按资源路径 / 技能类型兜底。
    /// </para>
    /// </summary>
    private static string ResolveGroupPath(AbilityConfig config, string resourcePath)
    {
        if (!string.IsNullOrWhiteSpace(config.FeatureGroupId))
        {
            return config.FeatureGroupId.Trim();
        }

        if (resourcePath.Contains("/Movement/", StringComparison.Ordinal))
        {
            return FeatureId.Ability.Groups.Movement;
        }

        return config.AbilityType switch
        {
            AbilityType.Passive => FeatureId.Ability.Groups.Passive,
            AbilityType.Weapon => "技能.武器",
            _ => DefaultGroupPath
        };
    }

    /// <summary>
    /// 从运行时技能 Data 中解析展示分组路径。
    /// </summary>
    private static string ResolveGroupPath(AbilityEntity ability)
    {
        var featureGroup = ability.Data.Get<string>(DataKey.AbilityFeatureGroup);
        if (!string.IsNullOrWhiteSpace(featureGroup))
        {
            return featureGroup.Trim();
        }

        var abilityType = (AbilityType)ability.Data.Get<int>(DataKey.AbilityType);
        return abilityType switch
        {
            AbilityType.Passive => FeatureId.Ability.Groups.Passive,
            AbilityType.Weapon => "技能.武器",
            _ => DefaultGroupPath
        };
    }

    /// <summary>
    /// 根据分组路径与名称构建稳定的分组结果。
    /// </summary>
    private IReadOnlyList<AbilityGroupPathGroup<TItem>> BuildGroupPathGroups<TItem>(
        List<TItem> items,
        Func<TItem, string> groupPathSelector,
        Func<TItem, string> nameSelector)
    {
        items.Sort((left, right) =>
        {
            var leftGroupPath = groupPathSelector(left);
            var rightGroupPath = groupPathSelector(right);
            var groupPathCompare = CompareGroupPath(leftGroupPath, rightGroupPath);
            if (groupPathCompare != 0)
            {
                return groupPathCompare;
            }

            return string.Compare(nameSelector(left), nameSelector(right), StringComparison.Ordinal);
        });

        var groups = new List<AbilityGroupPathGroup<TItem>>();
        string? currentGroupPath = null;
        List<TItem>? currentItems = null;

        foreach (var item in items)
        {
            var groupPath = groupPathSelector(item);
            if (!string.Equals(currentGroupPath, groupPath, StringComparison.Ordinal))
            {
                currentItems = new List<TItem>();
                groups.Add(new AbilityGroupPathGroup<TItem>(groupPath, currentItems));
                currentGroupPath = groupPath;
            }

            currentItems!.Add(item);
        }

        return groups;
    }

    /// <summary>
    /// 记录分组路径首次出现顺序。
    /// </summary>
    private void RegisterGroupPathOrder(string groupPath)
    {
        if (_groupPathOrder.ContainsKey(groupPath))
        {
            return;
        }

        _groupPathOrder[groupPath] = _groupPathOrder.Count;
    }

    /// <summary>
    /// 比较两个分组路径的稳定顺序。
    /// </summary>
    private int CompareGroupPath(string left, string right)
    {
        var leftOrder = _groupPathOrder.TryGetValue(left, out var existingLeftOrder)
            ? existingLeftOrder
            : int.MaxValue;
        var rightOrder = _groupPathOrder.TryGetValue(right, out var existingRightOrder)
            ? existingRightOrder
            : int.MaxValue;

        if (leftOrder != rightOrder)
        {
            return leftOrder.CompareTo(rightOrder);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="message">返回给 UI 的提示文本。</param>
    /// <returns>成功状态的操作结果。</returns>
    private static AbilityActionResult Success(string message) => new(true, message);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="message">返回给 UI 的提示文本。</param>
    /// <returns>失败状态的操作结果。</returns>
    private static AbilityActionResult Fail(string message) => new(false, message);
}
