/// <summary>
/// 可池化对象接口
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// [初始化] 对象被取出时调用 (Spawn/Get)
    /// 相当于 _Ready() 或 Init()，用于设置初始状态（位置、数值等）
    /// </summary>
    void OnPoolAcquire() { }

    /// <summary>
    /// [清理] 对象被归还时调用 (Release)
    /// 相当于 QueueFree() 前的清理，用于停止特效、移除引用、解绑事件等
    /// </summary>
    void OnPoolRelease() { }

    /// <summary>
    /// [重置] 当归还池时重置数据重置
    /// 在 OnPoolRelease 之后调用，专门用于将数据恢复为默认值（如 HP=Max, Score=0）
    /// </summary>
    void OnPoolReset() { }
}