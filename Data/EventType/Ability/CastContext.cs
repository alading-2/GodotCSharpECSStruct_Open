using System.Collections.Generic;

/// <summary>
/// 施法上下文 - 携带施法过程中的所有必要信息
/// 
/// 用途：
/// - 由 TriggerComponent 创建并传递给 AbilitySystem
/// - 由 AbilitySystem 传递给 IAbilityExecutor.Execute()
/// 
/// 设计原则：
/// - 替代 _TriggerEventData 存入 Data 的做法
/// - 一次性上下文，不存入 Entity.Data
/// </summary>
public class CastContext
{
    /// <summary>施法者实体</summary>
    public IEntity? Caster { get; set; }

    /// <summary>技能实体</summary>
    public AbilityEntity? Ability { get; set; }

    /// <summary>
    /// 请求的目标列表
    /// - 手动指定时由 TriggerComponent 或输入系统填充
    /// - 自动选择时由 AbilitySystem 填充
    /// </summary>
    public List<IEntity>? Targets { get; set; }

    /// <summary>
    /// 请求的目标位置 (点施法技能)
    /// </summary>
    public Godot.Vector2? TargetPosition { get; set; }

    /// <summary>
    /// 触发源事件数据 (事件触发技能时携带)
    /// 例如：OnDamaged 触发时，携带 DamageEventData
    /// </summary>
    public object? SourceEventData { get; set; }

    /// <summary>
    /// 是否已预选目标
    /// true = 已由外部指定目标，AbilitySystem 跳过自动选取
    /// </summary>
    public bool HasPreselectedTargets => Targets != null && Targets.Count > 0;

    /// <summary>
    /// 是否已预选位置
    /// </summary>
    public bool HasPreselectedPosition => TargetPosition.HasValue;
}

