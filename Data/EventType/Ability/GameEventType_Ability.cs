/// <summary>
/// Ability 相关事件定义（按组件拆分，见同目录下 GameEventType_Ability_*.cs）
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        // 按职责拆分到：
        // - GameEventType_Ability_System.cs
        // - GameEventType_Ability_Execution.cs
        // - GameEventType_Ability_Cooldown.cs
        // - GameEventType_Ability_Charge.cs
        // - GameEventType_Ability_Cost.cs
    }
}

// Context 类定义已移至 AbilityContext.cs
// 包括：AbilityCanUseCheckContext, AbilityConsumeChargeContext, AbilityExecuteResult
