using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 技能系统 - 管理技能生命周期和执行
/// 
/// 职责:
/// - 添加/移除技能
/// - 激活技能
/// - 就绪检查
/// - 协调技能间互斥
/// </summary>
public partial class AbilitySystem : Node
{
    private static readonly Log _log = new("AbilitySystem");

    // ================= AutoLoad 注册 =================

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "AbilitySystem",
            Path = "res://Src/ECS/System/AbilitySystem/AbilitySystem.cs",
            Priority = AutoLoad.Priority.System
        });
    }

    // ================= 单例 =================

    public static AbilitySystem Instance { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            _log.Warn("检测到重复的 AbilitySystem 实例");
            QueueFree();
            return;
        }
        Instance = this;
        _log.Info("技能系统初始化完成");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ================= 技能管理 =================

    /// <summary>
    /// 添加技能到单位
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="config">技能配置</param>
    /// <returns>创建的技能实体</returns>
    public AbilityEntity? AddAbility(IEntity owner, Dictionary<string, object> config)
    {
        if (owner == null)
        {
            _log.Error("无法添加技能：拥有者为空");
            return null;
        }

        // 检查是否已拥有相同技能
        string abilityId = config.TryGetValue(DataKey.Name, out var idVal) ? idVal?.ToString() ?? "" : "";
        if (string.IsNullOrEmpty(abilityId))
        {
            _log.Error("无法添加技能：缺少 AbilityId");
            return null;
        }

        var existingAbility = GetAbilityById(owner, abilityId);
        if (existingAbility != null)
        {
            _log.Warn($"单位已拥有技能 {abilityId}");
            return existingAbility;
        }

        // 创建技能实体
        var ability = EntityManager.Spawn<AbilityEntity>(new EntitySpawnConfig
        {
            Config = config,
            UsingObjectPool = false
        });

        if (ability == null)
        {
            _log.Error($"创建技能实体失败: {abilityId}");
            return null;
        }

        // 设置拥有者
        ability.Data.Set(DataKey.AbilityOwner, owner);

        // 建立关系
        EntityRelationshipManager.AddRelationship(
            owner.EntityId,
            ability.EntityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // 添加为拥有者的子节点
        if (owner is Node ownerNode)
        {
            ownerNode.AddChild(ability);
        }

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Added,
            new GameEventType.Ability.AddedEventData(ability, owner)
        );

        _log.Info($"添加技能: {abilityId} -> {owner.EntityId}");
        return ability;
    }

    /// <summary>
    /// 从单位移除技能
    /// </summary>
    public void RemoveAbility(IEntity owner, string abilityId)
    {
        if (owner == null || string.IsNullOrEmpty(abilityId)) return;

        var ability = GetAbilityById(owner, abilityId);
        if (ability == null)
        {
            _log.Warn($"单位不拥有技能 {abilityId}");
            return;
        }

        // 移除关系
        EntityRelationshipManager.RemoveRelationship(
            owner.EntityId,
            ability.EntityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // 销毁技能实体
        EntityManager.Destroy(ability);

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Removed,
            new GameEventType.Ability.RemovedEventData(abilityId, owner)
        );

        _log.Info($"移除技能: {abilityId} <- {owner.EntityId}");
    }

    // ================= 技能激活 =================

    /// <summary>
    /// 尝试激活技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityId">技能ID</param>
    /// <returns>是否成功激活</returns>
    public bool TryActivateAbility(IEntity owner, string abilityId)
    {
        var ability = GetAbilityById(owner, abilityId);
        if (ability == null)
        {
            _log.Debug($"技能不存在: {abilityId}");
            return false;
        }

        return TryActivateAbility(ability);
    }

    /// <summary>
    /// 尝试激活技能
    /// </summary>
    public bool TryActivateAbility(AbilityEntity ability)
    {
        if (ability == null) return false;

        // 就绪检查
        if (!CanUseAbility(ability))
        {
            return false;
        }

        // 获取组件
        var cooldown = EntityManager.GetComponent<CooldownComponent>(ability);
        var charge = EntityManager.GetComponent<ChargeComponent>(ability);
        var trigger = EntityManager.GetComponent<TriggerComponent>(ability);

        // 消耗充能 (仅主动技能)
        if (ability.Type == AbilityType.Active && charge != null)
        {
            if (!charge.ConsumeCharge())
            {
                return false;
            }
        }

        // 启动冷却
        cooldown?.StartCooldown();

        // 标记为执行中
        ability.Data.Set(DataKey.AbilityIsActive, true);

        // 选择目标
        var targets = SelectTargets(ability);

        // 发送激活事件
        ability.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(ability, targets)
        );

        var owner = ability.GetOwner();
        owner?.Events.Emit(
            GameEventType.Ability.Activated,
            new GameEventType.Ability.ActivatedEventData(ability, targets)
        );

        // 执行效果
        ExecuteAbilityEffects(ability, targets);

        // 标记执行完成
        ability.Data.Set(DataKey.AbilityIsActive, false);

        _log.Debug($"激活技能: {ability.AbilityId}");
        return true;
    }

    // ================= 就绪检查 =================

    /// <summary>
    /// 检查技能是否可用
    /// </summary>
    public bool CanUseAbility(AbilityEntity ability)
    {
        if (ability == null) return false;

        // 检查启用状态
        if (!ability.IsEnabled)
        {
            _log.Debug($"技能 {ability.AbilityId} 未启用");
            return false;
        }

        // 检查是否正在执行
        if (ability.IsActive)
        {
            _log.Debug($"技能 {ability.AbilityId} 正在执行中");
            return false;
        }

        // 检查冷却
        var cooldown = EntityManager.GetComponent<CooldownComponent>(ability);
        if (cooldown != null && !cooldown.IsReady())
        {
            _log.Debug($"技能 {ability.AbilityId} 正在冷却");
            return false;
        }

        // 检查充能 (仅主动技能)
        if (ability.Type == AbilityType.Active)
        {
            var charge = EntityManager.GetComponent<ChargeComponent>(ability);
            if (charge != null && !charge.HasCharge())
            {
                _log.Debug($"技能 {ability.AbilityId} 充能不足");
                return false;
            }
        }

        // TODO: 检查资源消耗 (CostComponent)

        return true;
    }

    // ================= 技能查询 =================

    /// <summary>
    /// 获取单位的所有技能
    /// </summary>
    public List<AbilityEntity> GetAbilities(IEntity owner)
    {
        var abilities = new List<AbilityEntity>();
        if (owner == null) return abilities;

        var abilityIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
            owner.EntityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        foreach (var abilityId in abilityIds)
        {
            var entity = EntityManager.GetEntityById(abilityId);
            if (entity is AbilityEntity ability)
            {
                abilities.Add(ability);
            }
        }

        return abilities;
    }

    /// <summary>
    /// 根据ID获取技能
    /// </summary>
    public AbilityEntity? GetAbilityById(IEntity owner, string abilityId)
    {
        var abilities = GetAbilities(owner);
        foreach (var ability in abilities)
        {
            if (ability.AbilityId == abilityId)
            {
                return ability;
            }
        }
        return null;
    }

    // ================= 目标选择 =================

    private List<IEntity> SelectTargets(AbilityEntity ability)
    {
        var targets = new List<IEntity>();

        var targetType = (AbilityTargetType)ability.Data.Get<int>(DataKey.AbilityTargetType);
        var owner = ability.GetOwner();

        switch (targetType)
        {
            case AbilityTargetType.Self:
                if (owner != null) targets.Add(owner);
                break;

            case AbilityTargetType.EventSource:
                // 从触发事件数据中获取
                var eventData = ability.Data.Get<object>("_TriggerEventData");
                // TODO: 解析事件数据获取攻击者
                break;

            case AbilityTargetType.SingleEnemy:
            case AbilityTargetType.AllEnemies:
            case AbilityTargetType.AreaOfEffect:
            case AbilityTargetType.RandomEnemy:
                // TODO: 由 TargetingComponent 处理
                break;

            default:
                break;
        }

        return targets;
    }

    // ================= 效果执行 =================

    private void ExecuteAbilityEffects(AbilityEntity ability, List<IEntity> targets)
    {
        // TODO: 调用 AbilityEffect 执行器
        // 暂时直接发送执行完成事件

        var result = new AbilityExecuteResult
        {
            Success = true,
            TargetsHit = targets.Count
        };

        ability.Events.Emit(
            GameEventType.Ability.Executed,
            new GameEventType.Ability.ExecutedEventData(ability, result)
        );
    }
}
