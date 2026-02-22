/// <summary>
/// Ability 消耗相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>
        /// 请求消耗成本 (魔法/能量/生命值等)。
        /// 发送者：AbilitySystem (技能激活时)
        /// 接收者：CostComponent
        /// </summary>
        public const string ConsumeCost = "ability:consume_cost";
        /// <summary>消耗成本请求事件数据</summary>
        public readonly record struct ConsumeCostEventData(EventContext Context);

        /// <summary>
        /// 成本消耗完成事件 (供 UI 监听)。
        /// 发送者：CostComponent
        /// 接收者：UI、统计系统等
        /// </summary>
        public const string CostConsumed = "ability:cost_consumed";
        /// <summary>成本消耗完成事件数据</summary>
        public readonly record struct CostConsumedEventData(AbilityCostType CostType, float Amount);
    }
}
