using System.Linq;
using Godot;

/// <summary>
/// 消耗组件 - 管理技能释放时的资源消耗。
/// 
/// 支持消耗类型:
/// - Mana: 魔法值消耗 (法师技能)
/// - Energy: 能量消耗 (战士技能)
/// - Ammo: 弹药消耗 (射手技能)
/// - Health: 生命值消耗 (血魔法)
/// - None: 无消耗 (纯冷却/充能技能)
/// 
/// 遵循 Component 规范:
/// - 无状态设计,所有数据存储在 Data 中
/// - 消耗类型和数量从技能 Data 读取
/// - 资源从施法者 (Caster) Data 读取和扣除
/// </summary>
public partial class CostComponent : Node2D, IComponent
{
    private static readonly Log _log = new(nameof(CostComponent));

    // ================= 标准字段 =================
    private Data? _data;
    private IEntity? _entity;

    // ================= 属性访问 =================

    private string AbilityName => _data.Get<string>(DataKey.Name);
    private AbilityCostType CostType => _data.Get<AbilityCostType>(DataKey.AbilityCostType);
    private float CostAmount => _data.Get<float>(DataKey.AbilityCostAmount);

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;

            // 订阅事件驱动请求
            SubscribeEvents();
        }
    }

    public void OnComponentUnregistered()
    {
        _data = null;
        _entity = null;
    }

    // ================= 事件驱动 =================

    /// <summary>订阅请求事件</summary>
    private void SubscribeEvents()
    {
        if (_entity == null) return;

        // 监听请求检查可用性事件
        _entity.Events.On<GameEventType.Ability.CheckCanUseEventData>(
            GameEventType.Ability.CheckCanUse,
            OnCheckCanUse,
            (int)AbilityCheckPhase.Cost
        );

        // 监听消耗成本请求事件
        _entity.Events.On<GameEventType.Ability.ConsumeCostEventData>(
            GameEventType.Ability.ConsumeCost,
            OnConsumeCost
        );
    }

    /// <summary>响应可用性检查请求 - 检查施法者是否有足够的资源</summary>
    private void OnCheckCanUse(GameEventType.Ability.CheckCanUseEventData eventData)
    {
        // 无消耗类型,跳过检查
        if (CostType == AbilityCostType.None) return;

        // 获取施法者
        var caster = GetCaster();
        if (caster == null)
        {
            eventData.Context.SetFailed("施法者不存在");
            return;
        }

        // 检查资源是否充足
        var resourceKey = GetResourceKey(CostType);
        if (string.IsNullOrEmpty(resourceKey))
        {
            _log.Error($"未知的消耗类型: {CostType}");
            eventData.Context.SetFailed("未知的消耗类型");
            return;
        }

        var currentResource = caster.Data.Get<float>(resourceKey);
        if (currentResource < CostAmount)
        {
            var resourceName = GetResourceName(CostType);
            eventData.Context.SetFailed($"{resourceName}不足");
            _log.Debug($"技能 {AbilityName} 无法释放: {resourceName}不足 ({currentResource:F1}/{CostAmount:F1})");
        }
    }

    /// <summary>响应消耗成本请求 - 从施法者扣除资源</summary>
    private void OnConsumeCost(GameEventType.Ability.ConsumeCostEventData eventData)
    {
        // 无消耗类型,跳过
        if (CostType == AbilityCostType.None) return;

        // 获取施法者
        var caster = GetCaster();
        if (caster == null)
        {
            eventData.Context.SetFailed("施法者不存在");
            return;
        }

        // 获取资源键
        var resourceKey = GetResourceKey(CostType);
        if (string.IsNullOrEmpty(resourceKey))
        {
            _log.Error($"未知的消耗类型: {CostType}");
            eventData.Context.SetFailed("未知的消耗类型");
            return;
        }

        // 扣除资源
        caster.Data.Add(resourceKey, -CostAmount);

        // 发送消耗完成事件 (供 UI 监听)
        if (_entity is AbilityEntity abilityEntity)
        {
            _entity.Events.Emit(
                GameEventType.Ability.CostConsumed,
                new GameEventType.Ability.CostConsumedEventData(CostType, CostAmount)
            );
        }

        var resourceName = GetResourceName(CostType);
        _log.Debug($"技能 {AbilityName} 消耗: {resourceName} -{CostAmount:F1}, 剩余: {caster.Data.Get<float>(resourceKey):F1}");
    }

    // ================= 辅助方法 =================

    /// <summary>
    /// 获取施法者 (通过关系管理器)
    /// </summary>
    private IEntity? GetCaster()
    {
        if (_entity == null) return null;

        var abilityId = _entity.Data.Get<string>(DataKey.Id);
        var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        ).FirstOrDefault();

        if (string.IsNullOrEmpty(ownerId)) return null;

        return EntityManager.GetEntityById(ownerId) as IEntity;
    }

    /// <summary>
    /// 映射消耗类型到数据键
    /// </summary>
    private string GetResourceKey(AbilityCostType type)
    {
        return type switch
        {
            AbilityCostType.Mana => DataKey.CurrentMana,
            AbilityCostType.Energy => "CurrentEnergy", // TODO: 等待 Energy 系统定义
            AbilityCostType.Ammo => "CurrentAmmo",     // TODO: 等待 Ammo 系统定义
            AbilityCostType.Health => DataKey.CurrentHp,
            _ => string.Empty
        };
    }

    /// <summary>
    /// 获取资源的中文名称 (用于日志和错误提示)
    /// </summary>
    private string GetResourceName(AbilityCostType type)
    {
        return type switch
        {
            AbilityCostType.Mana => "魔法",
            AbilityCostType.Energy => "能量",
            AbilityCostType.Ammo => "弹药",
            AbilityCostType.Health => "生命值",
            _ => "未知资源"
        };
    }
}
