/// <summary>
/// Ability 充能相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>充能恢复</summary>
        public const string ChargeRestored = "ability:charge_restored";
        /// <summary>充能恢复事件数据</summary>
        public readonly record struct ChargeRestoredEventData(int CurrentCharges, int MaxCharges);

        /// <summary>
        /// 使用技能消耗充能事件。
        /// 发送者：AbilitySystem (技能激活时)
        /// 接收者：ChargeComponent
        /// </summary>
        public const string ConsumeCharge = "ability:consume_charge";
        /// <summary>消耗充能事件数据</summary>
        public readonly record struct ConsumeChargeEventData(EventContext Context);

        /// <summary>
        /// 请求增加充能事件。
        /// 发送者：道具系统、Buff 等外部逻辑
        /// 接收者：ChargeComponent
        /// </summary>
        public const string AddCharge = "ability:add_charge";
        /// <summary>增加充能事件数据</summary>
        public readonly record struct AddChargeEventData(int Amount);
    }
}
