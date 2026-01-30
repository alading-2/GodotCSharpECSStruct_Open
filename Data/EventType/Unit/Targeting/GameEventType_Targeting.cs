using Godot;

/// <summary>
/// 瞄准系统相关事件定义
/// 用于技能Point类型目标选择的异步流程
/// </summary>
public static partial class GameEventType
{
    public static class Targeting
    {
        // ================= 瞄准流程事件 =================

        /// <summary>
        /// 开始瞄准 - 请求显示瞄准指示器
        /// 发送者：ActiveSkillInputComponent (当Point技能被激活时)
        /// 接收者：TargetingManager
        /// </summary>
        public const string StartTargeting = "targeting:start";

        /// <summary>开始瞄准事件数据</summary>
        /// <param name="Caster">施法者实体</param>
        /// <param name="Ability">待释放的技能</param>
        /// <param name="Context">施法上下文(预填充)</param>
        /// <param name="Range">技能射程(指示器移动范围)</param>
        public readonly record struct StartTargetingEventData(
            IEntity Caster,
            AbilityEntity Ability,
            CastContext Context,
            float Range
        );

        /// <summary>
        /// 瞄准确认 - 玩家按下确认键
        /// 发送者：TargetingIndicatorComponent (当玩家按A确认时)
        /// 接收者：TargetingManager -> AbilitySystem
        /// </summary>
        public const string TargetConfirmed = "targeting:confirmed";

        /// <summary>瞄准确认事件数据</summary>
        /// <param name="TargetPosition">确认的目标位置</param>
        public readonly record struct TargetConfirmedEventData(Vector2 TargetPosition);

        /// <summary>
        /// 瞄准取消 - 玩家按下取消键
        /// 发送者：TargetingIndicatorComponent (当玩家按B取消时)
        /// 接收者：TargetingManager
        /// </summary>
        public const string TargetCancelled = "targeting:cancelled";

        /// <summary>瞄准取消事件数据</summary>
        public readonly record struct TargetCancelledEventData();

        /// <summary>
        /// 瞄准结束 - 瞄准流程完成(确认或取消后)
        /// 发送者：TargetingManager
        /// 接收者：UI系统等需要响应瞄准状态变化的模块
        /// </summary>
        public const string TargetingEnded = "targeting:ended";

        /// <summary>瞄准结束事件数据</summary>
        /// <param name="WasConfirmed">是否为确认(false表示取消)</param>
        public readonly record struct TargetingEndedEventData(bool WasConfirmed);

        // ================= 指示器更新事件 =================

        /// <summary>
        /// 指示器位置更新 - 用于UI/范围预览同步
        /// 发送者：TargetingIndicatorComponent (每帧移动时)
        /// 接收者：UI系统(可选)
        /// </summary>
        public const string IndicatorMoved = "targeting:indicator_moved";

        /// <summary>指示器移动事件数据</summary>
        public readonly record struct IndicatorMovedEventData(Vector2 Position);
    }
}
