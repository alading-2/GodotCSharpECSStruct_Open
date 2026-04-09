using Godot;
using Slime.Config.Features;
using Slime.Config.Abilities;
using System.Collections.Generic;

/// <summary>
/// Feature 调试服务。
/// <para>
/// 负责把 TestSystem 的调试操作转发到正式运行时链路，避免测试系统重复实现 Feature 生命周期。
/// </para>
/// <para>
/// 当前先承接 Ability 子域的授予、移除、启用与禁用；后续其它 Feature 子域也应继续扩展到这里。
/// </para>
/// </summary>
internal sealed class FeatureDebugService
{
    private static readonly Log _log = new(nameof(FeatureDebugService));
    private const string TestModifierFeaturePrefix = "TestSystem.Modifier";
    private readonly System.Collections.Generic.Dictionary<string, float> _temporaryModifierValues = new();

    /// <summary>
    /// Feature 调试操作结果。
    /// </summary>
    internal readonly record struct ActionResult(
        bool Success,
        string Message
    );

    /// <summary>
    /// 获取某个属性当前挂载的临时 Modifier 数值。
    /// </summary>
    public float GetTemporaryModifierValue(IEntity? owner, string dataKey)
    {
        if (owner == null || string.IsNullOrWhiteSpace(dataKey))
        {
            return 0f;
        }

        var cacheKey = BuildModifierCacheKey(owner, dataKey);
        if (_temporaryModifierValues.TryGetValue(cacheKey, out var cachedValue))
        {
            return cachedValue;
        }

        var feature = FindTemporaryModifierFeature(owner, dataKey);
        if (feature == null)
        {
            return 0f;
        }

        var value = TryReadModifierValue(feature, dataKey);
        _temporaryModifierValues[cacheKey] = value;
        return value;
    }

    /// <summary>
    /// 通过运行时 FeatureDefinition 为指定属性施加一个临时 Modifier。
    /// </summary>
    public ActionResult ApplyTemporaryModifier(
        IEntity? owner,
        string dataKey,
        string displayName,
        bool isPercentage,
        float value)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (string.IsNullOrWhiteSpace(dataKey))
        {
            return Fail("缺少属性键，无法应用临时Modifier");
        }

        if (Mathf.IsZeroApprox(value))
        {
            return ClearTemporaryModifier(owner, dataKey, displayName);
        }

        var existingFeature = FindTemporaryModifierFeature(owner, dataKey);
        if (existingFeature != null)
        {
            EntityManager.RemoveAbility(owner, existingFeature);
        }

        var featureName = BuildModifierFeatureName(dataKey);
        var definition = BuildTemporaryModifierDefinition(dataKey, displayName, value, featureName);
        var result = GrantFeature(owner, definition, dataKey);
        if (!result.Success)
        {
            return result;
        }

        _temporaryModifierValues[BuildModifierCacheKey(owner, dataKey)] = value;
        return SuccessResult($"已应用临时加成: {displayName} {(isPercentage ? value + "%" : value.ToString())}");
    }

    /// <summary>
    /// 清除某个属性当前挂载的临时 Modifier。
    /// </summary>
    public ActionResult ClearTemporaryModifier(IEntity? owner, string dataKey, string displayName)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (string.IsNullOrWhiteSpace(dataKey))
        {
            return Fail("缺少属性键，无法清除临时Modifier");
        }

        var existingFeature = FindTemporaryModifierFeature(owner, dataKey);
        if (existingFeature != null)
        {
            EntityManager.RemoveAbility(owner, existingFeature);
        }

        _temporaryModifierValues.Remove(BuildModifierCacheKey(owner, dataKey));
        return SuccessResult($"已清除临时加成: {displayName}");
    }

    /// <summary>
    /// 通过正式 Ability 授予链路，为当前实体添加一个技能 Feature。
    /// </summary>
    public ActionResult GrantAbility(IEntity? owner, AbilityConfig? config, string resourceKey)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (config == null)
        {
            return Fail($"未找到技能配置: {resourceKey}");
        }

        var ability = EntityManager.AddAbility(owner, config);
        if (ability == null)
        {
            return Fail($"添加失败: {config.Name}");
        }

        var ownerName = owner.Data.Get<string>(nameof(DataKey.Name));
        var abilityName = ability.Data.Get<string>(nameof(DataKey.Name));
        var abilityId = ability.Data.Get<string>(nameof(DataKey.Id));
        var handlerId = ability.Data.Get<string>(nameof(DataKey.FeatureHandlerId));
        _log.Info($"[Feature调试] 授予技能Feature: owner={ownerName} feature={abilityName} featureId={abilityId} handler={handlerId} resourceKey={resourceKey}");
        return SuccessResult($"已添加: {abilityName}");
    }

    /// <summary>
    /// 通过正式 Feature 授予链路，为当前实体添加一个通用 Feature。
    /// </summary>
    public ActionResult GrantFeature(IEntity? owner, FeatureDefinition? definition, string featureSource)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (definition == null)
        {
            return Fail($"未找到Feature定义: {featureSource}");
        }

        var feature = EntityManager.AddAbility(owner, definition);
        if (feature == null)
        {
            return Fail($"添加Feature失败: {definition.Name}");
        }

        var ownerName = owner.Data.Get<string>(nameof(DataKey.Name));
        var featureName = feature.Data.Get<string>(nameof(DataKey.Name));
        var featureId = feature.Data.Get<string>(nameof(DataKey.Id));
        var handlerId = feature.Data.Get<string>(nameof(DataKey.FeatureHandlerId));
        _log.Info($"[Feature调试] 授予通用Feature: owner={ownerName} feature={featureName} featureId={featureId} handler={handlerId} source={featureSource}");
        return SuccessResult($"已添加: {featureName}");
    }

    /// <summary>
    /// 通过正式 Ability 移除链路，从当前实体移除一个技能 Feature。
    /// </summary>
    public ActionResult RemoveAbility(IEntity? owner, AbilityEntity? ability)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (ability == null)
        {
            return Fail("未找到要移除的技能实例");
        }

        var abilityName = ability.Data.Get<string>(nameof(DataKey.Name));
        var abilityId = ability.Data.Get<string>(nameof(DataKey.Id));
        var removed = EntityManager.RemoveAbility(owner, ability);
        if (!removed)
        {
            _log.Warn($"[Feature调试] 移除技能Feature失败: owner={owner.Data.Get<string>(nameof(DataKey.Name))} feature={abilityName} featureId={abilityId}");
            return Fail($"移除失败: {abilityName}");
        }

        _log.Info($"[Feature调试] 移除技能Feature: owner={owner.Data.Get<string>(nameof(DataKey.Name))} feature={abilityName} featureId={abilityId}");
        return SuccessResult($"已移除: {abilityName}");
    }

    /// <summary>
    /// 切换某个 Feature 的启用状态。
    /// </summary>
    public ActionResult SetFeatureEnabled(IEntity? owner, IEntity? feature, bool isEnabled)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (feature == null)
        {
            return Fail("未找到要切换的技能实例");
        }

        var featureName = feature.Data.Get<string>(nameof(DataKey.Name));
        var featureId = feature.Data.Get<string>(nameof(DataKey.Id));
        var handlerId = feature.Data.Get<string>(nameof(DataKey.FeatureHandlerId));
        if (isEnabled)
        {
            FeatureSystem.EnableFeature(feature, owner);
            _log.Info($"[Feature调试] 启用Feature: owner={owner.Data.Get<string>(nameof(DataKey.Name))} feature={featureName} featureId={featureId} handler={handlerId}");
            return SuccessResult($"已启用: {featureName}");
        }

        FeatureSystem.DisableFeature(feature, owner);
        _log.Info($"[Feature调试] 禁用Feature: owner={owner.Data.Get<string>(nameof(DataKey.Name))} feature={featureName} featureId={featureId} handler={handlerId}");
        return SuccessResult($"已禁用: {featureName}");
    }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    private static ActionResult SuccessResult(string message)
    {
        return new ActionResult(true, message);
    }

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    private static ActionResult Fail(string message)
    {
        return new ActionResult(false, message);
    }

    /// <summary>
    /// 构建临时 Modifier 对应的运行时 FeatureDefinition。
    /// </summary>
    private static FeatureDefinition BuildTemporaryModifierDefinition(
        string dataKey,
        string displayName,
        float value,
        string featureName)
    {
        return new FeatureDefinition
        {
            Name = featureName,
            FeatureHandlerId = featureName,
            Description = $"TestSystem 临时属性加成：{displayName}",
            Category = "TestSystem",
            EntityType = EntityType.Ability,
            Enabled = true,
            Modifiers = new Godot.Collections.Array<FeatureModifierEntry>
            {
                new FeatureModifierEntry
                {
                    DataKeyName = dataKey,
                    ModifierType = ModifierType.Additive,
                    Value = value,
                    Priority = 0
                }
            }
        };
    }

    /// <summary>
    /// 生成临时 Modifier Feature 的唯一名称。
    /// </summary>
    private static string BuildModifierFeatureName(string dataKey)
    {
        return $"{TestModifierFeaturePrefix}.{dataKey}";
    }

    /// <summary>
    /// 查找某个属性当前挂载的临时 Modifier Feature。
    /// </summary>
    private static AbilityEntity? FindTemporaryModifierFeature(IEntity owner, string dataKey)
    {
        var featureName = BuildModifierFeatureName(dataKey);
        return EntityManager.GetAbilityByName(owner, featureName);
    }

    /// <summary>
    /// 从临时 Modifier Feature 中读取当前修改值。
    /// </summary>
    private static float TryReadModifierValue(IEntity feature, string dataKey)
    {
        var raw = feature.Data.Get<object>(DataKey.FeatureModifiers);
        if (raw is not Godot.Collections.Array<FeatureModifierEntry> modifiers || modifiers.Count == 0)
        {
            return 0f;
        }

        foreach (var modifier in modifiers)
        {
            if (modifier != null && modifier.DataKeyName == dataKey)
            {
                return modifier.Value;
            }
        }

        return 0f;
    }

    /// <summary>
    /// 构建临时 Modifier 缓存键。
    /// </summary>
    private static string BuildModifierCacheKey(IEntity owner, string dataKey)
    {
        var ownerId = owner.Data.Get<string>(nameof(DataKey.Id));
        return $"{ownerId}:{dataKey}";
    }
}
