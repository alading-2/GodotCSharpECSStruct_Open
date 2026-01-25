using Godot;

/// <summary>
/// UI 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class UI
    {
        // === 技能 UI ===
        /// <summary>主动技能选中切换</summary>
        public const string ActiveSkillSelected = "ui:active_skill_selected";
        /// <summary>主动技能选中切换事件数据</summary>
        public readonly record struct ActiveSkillSelectedEventData(int SlotIndex, string AbilityName);
    }
}
