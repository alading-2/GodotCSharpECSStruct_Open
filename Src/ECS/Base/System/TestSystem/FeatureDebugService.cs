using Godot;
using Slime.Config.Features;
using Slime.Config.Abilities;
using ECS.Base.System.TestSystem.Core;

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

    /// <summary>测试系统临时 Modifier Feature 的命名前缀，用于构造唯一 Feature 名称。</summary>
    private const string TestModifierFeaturePrefix = "TestSystem.Modifier";

    /// <summary>
    /// 获取某个属性当前挂载的临时 Modifier 数值（直接从 Feature 实体读取）。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时直接返回 0。</param>
    /// <param name="dataKey">属性键；用于定位对应的临时 Modifier。</param>
    /// <returns>当前属性对应的临时 Modifier 数值；未找到时返回 0。</returns>
    public float GetTemporaryModifierValue(IEntity? owner, string dataKey)
    {
        if (owner == null || string.IsNullOrWhiteSpace(dataKey))
        {
            return 0f;
        }

        var feature = FindTemporaryModifierFeature(owner, dataKey);
        return feature == null ? 0f : TryReadModifierValue(feature, dataKey);
    }

    /// <summary>
    /// 通过运行时 FeatureDefinition 为指定属性施加一个临时 Modifier。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时返回失败结果。</param>
    /// <param name="dataKey">属性键；用于构造临时 Feature 名称并写入 Modifier。</param>
    /// <param name="displayName">属性显示名；用于日志和提示文本。</param>
    /// <param name="isPercentage">是否为百分比显示；仅影响返回提示文本。</param>
    /// <param name="value">临时加成数值；为 0 时视为清除。</param>
    /// <returns>执行结果，包含成功状态与提示信息。</returns>
    public TestActionResult ApplyTemporaryModifier(
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

        return SuccessResult($"已应用临时加成: {displayName} {(isPercentage ? value + "%" : value.ToString())}");
    }

    /// <summary>
    /// 清除某个属性当前挂载的临时 Modifier。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时返回失败结果。</param>
    /// <param name="dataKey">属性键；用于定位要清除的临时 Modifier。</param>
    /// <param name="displayName">属性显示名；用于日志和提示文本。</param>
    /// <returns>执行结果，包含成功状态与提示信息。</returns>
    public TestActionResult ClearTemporaryModifier(IEntity? owner, string dataKey, string displayName)
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

        return SuccessResult($"已清除临时加成: {displayName}");
    }

    /// <summary>
    /// 通过正式 Ability 授予链路，为当前实体添加一个技能 Feature。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时返回失败结果。</param>
    /// <param name="config">技能配置资源；为空时返回失败结果。</param>
    /// <param name="resourceKey">资源键；用于输出错误提示和日志定位。</param>
    /// <returns>执行结果，包含成功状态与提示信息。</returns>
    public TestActionResult GrantAbility(IEntity? owner, AbilityConfig? config, string resourceKey)
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

        var ownerName = owner.Data.Get<string>(DataKey.Name.Key);
        var abilityName = ability.Data.Get<string>(DataKey.Name.Key);
        var abilityId = ability.Data.Get<string>(DataKey.Id.Key);
        var handlerId = ability.Data.Get<string>(DataKey.FeatureHandlerId.Key);
        _log.Info($"[Feature调试] 授予技能Feature: owner={ownerName} feature={abilityName} featureId={abilityId} handler={handlerId} resourceKey={resourceKey}");
        return SuccessResult($"已添加: {abilityName}");
    }

    /// <summary>
    /// 通过正式 Feature 授予链路，为当前实体添加一个通用 Feature。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时返回失败结果。</param>
    /// <param name="definition">Feature 定义资源；为空时返回失败结果。</param>
    /// <param name="featureSource">Feature 来源标识；用于输出错误提示和日志定位。</param>
    /// <returns>执行结果，包含成功状态与提示信息。</returns>
    public TestActionResult GrantFeature(IEntity? owner, FeatureDefinition? definition, string featureSource)
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

        var ownerName = owner.Data.Get<string>(DataKey.Name.Key);
        var featureName = feature.Data.Get<string>(DataKey.Name.Key);
        var featureId = feature.Data.Get<string>(DataKey.Id.Key);
        var handlerId = feature.Data.Get<string>(DataKey.FeatureHandlerId.Key);
        _log.Info($"[Feature调试] 授予通用Feature: owner={ownerName} feature={featureName} featureId={featureId} handler={handlerId} source={featureSource}");
        return SuccessResult($"已添加: {featureName}");
    }

    /// <summary>
    /// 通过正式 Ability 移除链路，从当前实体移除一个技能 Feature。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时返回失败结果。</param>
    /// <param name="ability">要移除的技能实例；为空时返回失败结果。</param>
    /// <returns>执行结果，包含成功状态与提示信息。</returns>
    public TestActionResult RemoveAbility(IEntity? owner, AbilityEntity? ability)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (ability == null)
        {
            return Fail("未找到要移除的技能实例");
        }

        var abilityName = ability.Data.Get<string>(DataKey.Name.Key);
        var abilityId = ability.Data.Get<string>(DataKey.Id.Key);
        var removed = EntityManager.RemoveAbility(owner, ability);
        if (!removed)
        {
            _log.Warn($"[Feature调试] 移除技能Feature失败: owner={owner.Data.Get<string>(DataKey.Name.Key)} feature={abilityName} featureId={abilityId}");
            return Fail($"移除失败: {abilityName}");
        }

        _log.Info($"[Feature调试] 移除技能Feature: owner={owner.Data.Get<string>(DataKey.Name.Key)} feature={abilityName} featureId={abilityId}");
        return SuccessResult($"已移除: {abilityName}");
    }

    /// <summary>
    /// 切换某个 Feature 的启用状态。
    /// </summary>
    /// <param name="owner">当前实体所有者；为空时返回失败结果。</param>
    /// <param name="feature">要切换状态的 Feature 实例；为空时返回失败结果。</param>
    /// <param name="isEnabled">是否启用；<c>true</c> 表示启用，<c>false</c> 表示禁用。</param>
    /// <returns>执行结果，包含成功状态与提示信息。</returns>
    public TestActionResult SetFeatureEnabled(IEntity? owner, IEntity? feature, bool isEnabled)
    {
        if (owner == null)
        {
            return Fail("请先选择一个实体");
        }

        if (feature == null)
        {
            return Fail("未找到要切换的技能实例");
        }

        var featureName = feature.Data.Get<string>(DataKey.Name.Key);
        var featureId = feature.Data.Get<string>(DataKey.Id.Key);
        var handlerId = feature.Data.Get<string>(DataKey.FeatureHandlerId.Key);
        if (isEnabled)
        {
            FeatureSystem.EnableFeature(feature, owner);
            _log.Info($"[Feature调试] 启用Feature: owner={owner.Data.Get<string>(DataKey.Name.Key)} feature={featureName} featureId={featureId} handler={handlerId}");
            return SuccessResult($"已启用: {featureName}");
        }

        FeatureSystem.DisableFeature(feature, owner);
        _log.Info($"[Feature调试] 禁用Feature: owner={owner.Data.Get<string>(DataKey.Name.Key)} feature={featureName} featureId={featureId} handler={handlerId}");
        return SuccessResult($"已禁用: {featureName}");
    }

    /// <summary>
    /// 构造成功结果。
    /// </summary>
    /// <param name="message">结果提示文本。</param>
    /// <returns>成功的测试结果。</returns>
    private static TestActionResult SuccessResult(string message) => new(true, message);

    /// <summary>
    /// 构造失败结果。
    /// </summary>
    /// <param name="message">结果提示文本。</param>
    /// <returns>失败的测试结果。</returns>
    private static TestActionResult Fail(string message) => new(false, message);

    /// <summary>
    /// 构建临时 Modifier 对应的运行时 FeatureDefinition。
    /// </summary>
    /// <param name="dataKey">属性键；用于写入 Modifier 的目标字段。</param>
    /// <param name="displayName">属性显示名；用于描述文本。</param>
    /// <param name="value">Modifier 数值；写入到定义中。</param>
    /// <param name="featureName">运行时 Feature 名称；用于唯一标识。</param>
    /// <returns>构建好的运行时 FeatureDefinition。</returns>
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
    /// 构建临时 Modifier Feature 的唯一名称。
    /// </summary>
    /// <param name="dataKey">属性键；用于拼接唯一名称。</param>
    /// <returns>临时 Modifier Feature 名称。</returns>
    private static string BuildModifierFeatureName(string dataKey)
        => $"{TestModifierFeaturePrefix}.{dataKey}";

    /// <summary>
    /// 查找当前实体上已挂载的临时 Modifier Feature。
    /// </summary>
    /// <param name="owner">当前实体所有者。</param>
    /// <param name="dataKey">属性键；用于定位对应的临时 Feature。</param>
    /// <returns>匹配到的 AbilityEntity；未找到时返回 <c>null</c>。</returns>
    private static AbilityEntity? FindTemporaryModifierFeature(IEntity owner, string dataKey)
    {
        var featureName = BuildModifierFeatureName(dataKey);
        return EntityManager.GetAbilityByName(owner, featureName);
    }

    /// <summary>
    /// 从 Feature 实体中读取指定属性的 Modifier 数值。
    /// </summary>
    /// <param name="feature">Feature 实体；用于读取其 Modifier 容器。</param>
    /// <param name="dataKey">属性键；用于匹配对应的 Modifier。</param>
    /// <returns>匹配到的 Modifier 数值；未找到时返回 0。</returns>
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
}
