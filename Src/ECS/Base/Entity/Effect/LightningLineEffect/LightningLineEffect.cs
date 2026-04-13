using Godot;

/// <summary>
/// 链式闪电专用的连线视觉特效
/// 
/// 【设计说明】：
/// 1. 这是一个极其轻量级的 Entity（直接继承 Line2D 提升性能），不按照常规 ECS 拆分 Component。
/// 2. 虽然实现了 IEntity 接口（为了能受 EntityManager 生命周期管理），但故意【不使用】Data 和 EventBus 驱动逻辑。
/// 3. 直接通过暴露的方法 (PlayChain) 或 Export 参数进行驱动，避免数据总线带来的额外性能开销。
/// 4. 不要复杂化：对于这种纯视觉、高频生成且生命周期极短的特效，直接操作更高效。
/// </summary>
public partial class LightningLineEffect : Line2D, IPoolable, ILineEffect, IEntity
{
    // ================= IEntity 极简实现 =================
    // 仅为了满足接口约束，内部逻辑不强依赖它们
    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public LightningLineEffect()
    {
        Data = new Data(this);
    }

    private Tween? _tween;

    public override void _Ready()
    {
        // 以防在 tscn 里忘记设置，这里提供一个基础保底配置
        // if (Width == 10f) Width = 15.0f; // Godot默认是10，改粗一些
        if (DefaultColor == Colors.White) DefaultColor = new Color(0.4f, 0.8f, 1.0f, 1.0f); // rgb(102,204,255)

        ZIndex = 10; // 确保闪电画在实体上层
    }

    /// <summary>
    /// 从对象池取出时初始化状态
    /// </summary>
    public void OnPoolAcquire()
    {
        Visible = true;
        var mod = Modulate;
        mod.A = 1.0f;
        Modulate = mod;
    }

    /// <summary>
    /// 回收进对象池时清理状态
    /// </summary>
    public void OnPoolRelease()
    {
        Visible = false;
        if (_tween != null && _tween.IsValid())
        {
            _tween.Kill();
            _tween = null;
        }
    }

    /// <summary>
    /// 在两点之间播放闪电连线动画
    /// </summary>
    /// <param name="fromPos">起点世界坐标</param>
    /// <param name="toPos">终点世界坐标</param>
    public void PlayChain(Vector2 fromPos, Vector2 toPos)
    {
        // 1. 设置连线两端点（Line2D 的 Points 是局部坐标，需要将世界坐标转换为局部）
        Points = new Vector2[] { ToLocal(fromPos), ToLocal(toPos) };

        // 2. 停止上一个未完成的动画
        if (_tween != null && _tween.IsValid())
        {
            _tween.Kill();
        }

        // 3. 创建动画：设定淡出消失效果（加长留存时间方便看清）
        _tween = CreateTween();

        // 闪电瞬间明亮，然后随时间渐隐（使用 Linear 或者 EaseIn，这样能在前期保持可见度）
        _tween.TweenProperty(this, "modulate:a", 0.0f, 0.35f)
              .SetTrans(Tween.TransitionType.Linear)
              .SetEase(Tween.EaseType.InOut);

        // 额外加一个宽度变细的动画，增加动态感。假设它原宽 1.0，我们缩小到 0.2
        _tween.Parallel().TweenProperty(this, "width", 3.0f, 0.35f)
              .SetTrans(Tween.TransitionType.Sine)
              .SetEase(Tween.EaseType.Out);

        // 4. 动画结束后，将自身归还到专属对象池中
        _tween.TweenCallback(Callable.From(() =>
        {
            // 恢复初始线宽，避免下次从对象池拿出来时还是极细的
            Width = 15.0f;
            ObjectPoolManager.ReturnToPool(this);
        }));
    }
}
