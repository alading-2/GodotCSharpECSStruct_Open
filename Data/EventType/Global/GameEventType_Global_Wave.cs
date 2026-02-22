/// <summary>
/// Global 波次相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>波次开始</summary>
        public const string WaveStarted = "global:wave_started";
        /// <summary>波次开始事件数据</summary>
        public readonly record struct WaveStartedEventData(int WaveIndex);

        /// <summary>波次完成</summary>
        public const string WaveCompleted = "global:wave_completed";
        /// <summary>波次完成事件数据</summary>
        public readonly record struct WaveCompletedEventData(int WaveIndex);
    }
}
