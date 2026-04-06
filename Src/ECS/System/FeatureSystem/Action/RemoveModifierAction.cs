/// <summary>
/// 回滚修改器动作 - 移除由当前 Feature 施加的所有修改器
///
/// 按 Source == FeatureEntity 批量移除，与 ApplyModifierAction 对称。
/// 通常用于 IFeatureHandler.OnRemoved 中。
/// </summary>
public class RemoveModifierAction : IFeatureAction
{
    /// <summary>是否作用于宿主（true）还是 Feature 实体本身（false）</summary>
    public bool TargetOwner { get; set; } = true;

    public void Execute(FeatureContext ctx)
    {
        if (ctx.Feature == null) return;

        var target = TargetOwner ? ctx.Owner : ctx.Feature as IEntity;
        target?.Data.RemoveModifiersBySource(ctx.Feature);
    }
}
