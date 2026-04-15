// Src/ECS/System/AbilitySystem/EntityManager_Ability.cs
// partial 扩展 EntityManager，提供 Ability 相关的增删查功能
// 放在 AbilitySystem 目录下，逻辑上属于 Ability 模块

using System.Collections.Generic;
using System.Linq;
using Godot;
using Slime.Config.Abilities;

/// <summary>
/// EntityManager 的 Ability 扩展
/// 
/// 职责：管理 Ability 的生命周期（增删查）
/// 注意：激活逻辑由 AbilitySystem 负责
/// </summary>
public static partial class EntityManager
{
    private static readonly Log _abilityLog = new("EntityManager_Ability", LogLevel.Warning);

    // ==================== Ability 管理 ====================

    /// <summary>
    /// 为单位添加技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="config">技能配置资源</param>
    /// <returns>创建的技能实体，失败返回 null</returns>
    public static AbilityEntity? AddAbility(IEntity owner, Resource config)
    {
        if (owner == null)
        {
            _abilityLog.Error("无法添加技能：拥有者为空");
            return null;
        }

        // 尝试通过反射获取 Name
        string abilityName = "";
        var prop = config.GetType().GetProperty(DataKey.Name);
        if (prop != null)
        {
            abilityName = prop.GetValue(config) as string ?? "";
        }

        if (string.IsNullOrEmpty(abilityName))
        {
            _abilityLog.Error("无法添加技能：Resource 缺少 Name 属性");
            return null;
        }

        // 检查是否已拥有相同技能
        var existingAbility = GetAbilityByName(owner, abilityName);
        if (existingAbility != null)
        {
            _abilityLog.Warn($"单位已拥有技能 {abilityName}");
            return existingAbility;
        }

        if (config is AbilityConfig abilityConfig && !ValidateAbilityHandlerConfig(abilityConfig, abilityName))
        {
            return null;
        }

        // 创建技能实体
        AbilityEntity? ability;
        ability = Spawn<AbilityEntity>(new EntitySpawnConfig
        {
            Config = config,
            UsingObjectPool = true,
            PoolName = ObjectPoolNames.AbilityPool
        });

        if (ability == null)
        {
            _abilityLog.Error($"创建技能实体失败: {abilityName}");
            return null;
        }

        var handlerId = ability.Data.Get<string>(DataKey.FeatureHandlerId);
        if (config is AbilityConfig && string.IsNullOrWhiteSpace(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：AbilityConfig.FeatureHandlerId 为空");
            Destroy(ability);
            return null;
        }

        if (config is AbilityConfig && !FeatureHandlerRegistry.HasHandler(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：未注册 FeatureHandlerId='{handlerId}'");
            Destroy(ability);
            return null;
        }

        // 获取 ID（从 Data 读取，由 EntityManager.Spawn 设置）
        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;

        // 建立关系（替代 DataKey.Owner，关系统一由 EntityRelationshipManager 管理）
        EntityRelationshipManager.AddRelationship(
            ownerId,
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // 核心逻辑连通：订阅 TryTrigger 事件，由 AbilitySystem 统一处理
        ability.Events.On<GameEventType.Ability.TryTriggerEventData>(
            GameEventType.Ability.TryTrigger,
            AbilitySystem.HandleTryTrigger
        );

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Added,
            new GameEventType.Ability.AddedEventData(ability, owner)
        );

        // Feature 生命周期钩子：Granted
        FeatureSystem.OnFeatureGranted(ability, owner);

        _abilityLog.Info($"添加技能: {abilityName} -> {ownerId}");
        return ability;
    }

    /// <summary>
    /// 校验 AbilityConfig 是否显式绑定了可用的 FeatureHandler。
    /// </summary>
    /// <param name="config">技能配置资源。</param>
    /// <param name="abilityName">技能名称，用于日志定位。</param>
    /// <returns>配置有效返回 true，否则返回 false。</returns>
    private static bool ValidateAbilityHandlerConfig(AbilityConfig config, string abilityName)
    {
        var handlerId = config.FeatureHandlerId;
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：AbilityConfig.FeatureHandlerId 为空");
            return false;
        }

        if (!FeatureHandlerRegistry.HasHandler(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：未注册 FeatureHandlerId='{handlerId}'");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 从单位移除技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityName">技能名称</param>
    public static bool RemoveAbility(IEntity owner, string abilityName)
    {
        if (owner == null || string.IsNullOrEmpty(abilityName)) return false;

        var ability = GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _abilityLog.Warn($"单位不拥有技能 {abilityName}");
            return false;
        }

        return RemoveAbility(owner, ability);
    }

    /// <summary>
    /// 从单位移除指定技能实例。
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="ability">要移除的技能实例</param>
    /// <returns>成功移除返回 true，否则返回 false</returns>
    public static bool RemoveAbility(IEntity owner, AbilityEntity ability)
    {
        if (owner == null || ability == null) return false;

        // 获取 ID（从 Data 读取）
        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityName = ability.Data.Get<string>(DataKey.Name) ?? string.Empty;

        // 移除关系
        EntityRelationshipManager.RemoveRelationship(
            ownerId,
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // Feature 生命周期钩子：Removed（在 Destroy 之前，使 handler 还能访问 feature.Data）
        FeatureSystem.OnFeatureRemoved(ability, owner);

        // 销毁技能实体（自动处理对象池归还）
        Destroy(ability);

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Removed,
            new GameEventType.Ability.RemovedEventData(abilityName, abilityId, owner)
        );

        _abilityLog.Info($"移除技能实例: {abilityName} ({abilityId}) <- {ownerId}");
        return true;
    }

    // ==================== Ability 查询 ====================

    /// <summary>
    /// 获取单位的所有技能
    /// </summary>
    public static List<AbilityEntity> GetAbilities(IEntity owner)
    {
        var abilities = new List<AbilityEntity>();
        if (owner == null) return abilities;

        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
            ownerId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        foreach (var abilityId in abilityIds)
        {
            var entity = GetEntityById(abilityId);
            if (entity is AbilityEntity ability)
            {
                abilities.Add(ability);
            }
        }

        return abilities;
    }

    /// <summary>
    /// 获取单位的所有手动触发主动技能（供输入组件和UI共用）
    /// 过滤规则：TriggerMode 包含 Manual 且 AbilityType 不是 Passive
    /// </summary>
    public static List<AbilityEntity> GetManualAbilities(IEntity owner)
    {
        var abilities = GetAbilities(owner);
        var result = new List<AbilityEntity>();
        foreach (var a in abilities)
        {
            var mode = (AbilityTriggerMode)a.Data.Get<int>(DataKey.AbilityTriggerMode);
            var type = (AbilityType)a.Data.Get<int>(DataKey.AbilityType);
            if (type != AbilityType.Passive && mode.HasFlag(AbilityTriggerMode.Manual))
            {
                result.Add(a);
            }
        }
        return result;
    }

    /// <summary>
    /// 根据名称获取技能
    /// </summary>
    public static AbilityEntity? GetAbilityByName(IEntity owner, string abilityName)
    {
        var abilities = GetAbilities(owner);
        foreach (var ability in abilities)
        {
            // 从 Data 读取技能名称（不使用便捷属性）
            var name = ability.Data.Get<string>(DataKey.Name);
            if (name == abilityName)
            {
                return ability;
            }
        }
        return null;
    }

    /// <summary>
    /// 根据运行时实例 ID 获取技能。
    /// </summary>
    public static AbilityEntity? GetAbilityById(IEntity owner, string abilityId)
    {
        if (owner == null || string.IsNullOrWhiteSpace(abilityId))
        {
            return null;
        }

        var abilities = GetAbilities(owner);
        foreach (var ability in abilities)
        {
            var currentId = ability.Data.Get<string>(DataKey.Id);
            if (currentId == abilityId)
            {
                return ability;
            }
        }

        return null;
    }
}
