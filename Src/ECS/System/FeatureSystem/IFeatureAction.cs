/// <summary>
/// Feature 动作接口 - 表示一次激活时可执行的最小业务单元
///
/// 职责边界：
/// - 消费 FeatureContext，执行一种具体效果
/// - 调用现有系统（DamageService / TargetSelector / EventBus 等）
/// - 不自己管理宿主生命周期
/// - 不重做冷却、目标选择等已有基础设施
///
/// 典型实现：ApplyModifierAction / RemoveModifierAction / EmitEventAction
///
/// 使用方式：
/// - 在 IFeatureHandler.OnGranted/OnActivated 中构造并调用
/// - 或通过 FeatureSystem.ExecuteActions 批量执行
/// </summary>
public interface IFeatureAction
{
    /// <summary>执行该动作</summary>
    void Execute(FeatureContext ctx);
}
