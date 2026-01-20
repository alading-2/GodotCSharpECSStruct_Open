// Src/ECS/System/AbilitySystem/EntityManager.Ability.cs
// partial 扩展 EntityManager，提供 Ability 相关的增删查功能
// 放在 AbilitySystem 目录下，逻辑上属于 Ability 模块

using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// EntityManager 的 Ability 扩展
/// 
/// 职责：管理 Ability 的生命周期（增删查）
/// 注意：激活逻辑由 AbilitySystem 负责
/// </summary>
public static partial class EntityManager
{
    private static readonly Log _abilityLog = new("EntityManager.Ability");

    // ==================== Ability 管理 ====================

    /// <summary>
    /// 为单位添加技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="config">技能配置数据</param>
    /// <returns>创建的技能实体，失败返回 null</returns>
    public static AbilityEntity? AddAbility(IEntity owner, Dictionary<string, object> config)
    {
        if (owner == null)
        {
            _abilityLog.Error("无法添加技能：拥有者为空");
            return null;
        }

        // 检查技能名称
        string abilityName = config.GetValueOrDefault(DataKey.Name) as string ?? "";
        if (string.IsNullOrEmpty(abilityName))
        {
            _abilityLog.Error("无法添加技能：缺少 Name");
            return null;
        }

        // 检查是否已拥有相同技能
        var existingAbility = GetAbilityByName(owner, abilityName);
        if (existingAbility != null)
        {
            _abilityLog.Warn($"单位已拥有技能 {abilityName}");
            return existingAbility;
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

        // 获取 ID（从 Data 读取，由 EntityManager.Spawn 设置）
        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;

        // 建立关系（替代 DataKey.Owner，关系统一由 EntityRelationshipManager 管理）
        EntityRelationshipManager.AddRelationship(
            ownerId,
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // 核心逻辑连通：订阅 TryActivate 事件，由 AbilitySystem 统一处理
        ability.Events.On<GameEventType.Ability.TryActivateEventData>(
            GameEventType.Ability.TryActivate,
            AbilitySystem.HandleTryActivate
        );

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Added,
            new GameEventType.Ability.AddedEventData(ability, owner)
        );

        _abilityLog.Info($"添加技能: {abilityName} -> {ownerId}");
        return ability;
    }

    /// <summary>
    /// 从单位移除技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityName">技能名称</param>
    public static void RemoveAbility(IEntity owner, string abilityName)
    {
        if (owner == null || string.IsNullOrEmpty(abilityName)) return;

        var ability = GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _abilityLog.Warn($"单位不拥有技能 {abilityName}");
            return;
        }

        // 获取 ID（从 Data 读取）
        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;

        // 移除关系
        EntityRelationshipManager.RemoveRelationship(
            ownerId,
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // 销毁技能实体（自动处理对象池归还）
        Destroy(ability);

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Removed,
            new GameEventType.Ability.RemovedEventData(abilityName, owner)
        );

        _abilityLog.Info($"移除技能: {abilityName} <- {ownerId}");
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
}
