/// <summary>
/// Unit 成长相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>
        /// 单位等级提升
        /// </summary>
        public const string LevelUp = "unit:level_up";
        /// <summary>等级提升事件数据</summary>
        public readonly record struct LevelUpEventData(IEntity Entity, int OldLevel, int NewLevel);
    }
}
