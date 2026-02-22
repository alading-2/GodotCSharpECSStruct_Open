/// <summary>
/// Unit 动画相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>动画播放完毕（结果事件：UnitAnimationComponent -> 其他组件）</summary>
        public const string AnimationFinished = "unit:animation_finished";
        /// <summary>动画播放完毕事件数据（携带动画名，供监听者判断是哪个动画）</summary>
        public readonly record struct AnimationFinishedEventData(string AnimName);

        /// <summary>请求停止当前动画立即回 idle（命令事件：外部 -> UnitAnimationComponent）</summary>
        public const string StopAnimationRequested = "unit:stop_animation_requested";
        /// <summary>请求停止动画事件数据</summary>
        public readonly record struct StopAnimationRequestedEventData();

        /// <summary>请求播放动画（命令事件：外部 -> UnitAnimationComponent）</summary>
        public const string PlayAnimationRequested = "unit:play_animation_requested";
        /// <summary>请求播放动画事件数据</summary>
        public readonly record struct PlayAnimationRequestedEventData(
            string AnimName,
            bool ForceRestart = false,
            float Duration = -1f
        );
    }
}
