using Godot;

/// <summary>
/// 技能实体 - 每个技能都是独立的实体
/// 
/// 设计理念:
/// - 技能是 Entity，实现 IEntity 接口
/// - 业务逻辑归 Component (Cooldown, Trigger, Charge 等)
/// - 效果执行归 AbilityEffect 执行器
/// </summary>
public partial class AbilityEntity : Node, IEntity
{
    private static readonly Log _log = new("AbilityEntity");

    // ================= IEntity 实现 =================

    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();
    public string EntityId { get; private set; } = string.Empty;

    // ================= 构造函数 =================

    public AbilityEntity()
    {
        Data = new Data(this);
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        EntityId = GetInstanceId().ToString();
        _log.Debug($"技能实体就绪: {Data.Get<string>(DataKey.Name)}");
    }

    // ================= 便捷属性 =================

    /// <summary>技能ID</summary>
    public string AbilityId => Data.Get<string>(DataKey.Name);

    /// <summary>技能名称</summary>
    public string AbilityName => Data.Get<string>(DataKey.Name);

    /// <summary>技能类型</summary>
    public AbilityType Type => (AbilityType)Data.Get<int>(DataKey.AbilityType);

    /// <summary>触发模式</summary>
    public AbilityTriggerMode TriggerMode => (AbilityTriggerMode)Data.Get<int>(DataKey.AbilityTriggerMode);

    /// <summary>技能是否启用</summary>
    public bool IsEnabled => Data.Get<bool>(DataKey.AbilityEnabled);

    /// <summary>技能是否正在执行</summary>
    public bool IsActive => Data.Get<bool>(DataKey.AbilityIsActive);

    /// <summary>获取技能拥有者</summary>
    public IEntity? GetOwner()
    {
        return Data.Get<IEntity>(DataKey.AbilityOwner);
    }
}
