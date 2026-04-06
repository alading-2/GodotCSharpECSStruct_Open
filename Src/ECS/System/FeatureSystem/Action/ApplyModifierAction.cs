/// <summary>
/// 施加修改器动作 - 将一个 DataModifier 添加到目标实体的 Data 上
///
/// 来源标记为 FeatureInstance.FeatureEntity，支持后续按来源批量回滚。
/// 通常用于 IFeatureHandler.OnGranted 中，实现属性加成效果。
/// </summary>
public class ApplyModifierAction : IFeatureAction
{
    /// <summary>目标数据键名（如 "AttackDamage"）</summary>
    public string DataKeyName { get; set; } = "";

    /// <summary>修改器类型</summary>
    public ModifierType Type { get; set; } = ModifierType.Additive;

    /// <summary>修改值</summary>
    public float Value { get; set; } = 0f;

    /// <summary>优先级（数值越小越先生效）</summary>
    public int Priority { get; set; } = 0;

    /// <summary>是否作用于宿主（true）还是 Feature 实体本身（false）</summary>
    public bool TargetOwner { get; set; } = true;

    public void Execute(FeatureContext ctx)
    {
        if (string.IsNullOrEmpty(DataKeyName)) return;

        var target = TargetOwner ? ctx.Owner : ctx.Feature as IEntity;
        if (target == null) return;

        var modifier = new DataModifier(
            type: Type,
            value: Value,
            priority: Priority,
            source: ctx.Feature
        );
        target.Data.AddModifier(DataKeyName, modifier);
    }
}
