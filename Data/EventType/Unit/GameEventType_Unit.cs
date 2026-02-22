/// <summary>
/// Unit 相关事件定义（按组件拆分，见同目录下 GameEventType_Unit_*.cs）
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        // 按职责拆分到：
        // - GameEventType_Unit_Core.cs
        // - GameEventType_Unit_Health.cs
        // - GameEventType_Unit_Lifecycle.cs
        // - GameEventType_Unit_Animation.cs
        // - GameEventType_Unit_Progression.cs
    }
}
