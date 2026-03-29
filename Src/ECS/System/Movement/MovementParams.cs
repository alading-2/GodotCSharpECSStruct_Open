using Godot;

/// <summary>
/// 单次移动的完整上下文：输入参数 + 运行时统计，存储于 <see cref="EntityMovementComponent"/>，按值传给策略。
/// <para>
/// 设计原则：
/// - 输入参数（MaxDuration、TargetPoint 等）均为 <c>init</c>，切换时一次性设定，策略只读
/// - 运行时统计（<c>ElapsedTime</c>、<c>TraveledDistance</c>）为 <c>set</c>，由调度器每帧写入
/// - 策略内部状态（如 _currentAngle、_startPoint）存于策略私有字段，不放此处
/// </para>
/// <para>
/// 典型用法：
/// <code>
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
///     {
///         isTrackTarget       = true,
///         TargetNode        = enemy,
///         MaxDuration       = 5f,
///         DestroyOnComplete = true,
///     }));
/// </code>
/// </para>
/// </summary>
public record struct MovementParams
{
    /// <summary>显式无参构造函数（C# CS8983：带属性初始化值的 struct 必须声明显式构造函数）</summary>
    public MovementParams() { }

    // ======== 运行时统计（由调度器维护，策略只读） ========

    /// <summary>本次运动已持续时间（秒），由 EntityMovementComponent 每帧写入</summary>
    public float ElapsedTime { get; set; } = 0f;
    /// <summary>本次运动已移动距离（像素），由 EntityMovementComponent 每帧写入</summary>
    public float TraveledDistance { get; set; } = 0f;

    // ======== 通用参数 ========

    /// <summary>移动模式（由 EntityMovementComponent.SwitchStrategy 自动填入，调用方通常不需要手动设置）</summary>
    public MoveMode Mode { get; init; } = MoveMode.None;
    /// <summary>最大持续时间（秒），-1 = 不限制</summary>
    public float MaxDuration { get; init; } = -1f;
    /// <summary>最大移动距离（像素），-1 = 不限制</summary>
    public float MaxDistance { get; init; } = -1f;
    /// <summary>移动模式下的移动速度（像素/秒），用于 Dash 等有明确动作速度的策略</summary>
    public float ActionSpeed { get; init; } = 0f;
    /// <summary>
    /// 通用加速度参数（单位由策略解释）：
    /// Charge 使用像素/秒²，Orbit 可回退作为角加速度（弧度/秒²）。0 = 匀速。
    /// </summary>
    public float Acceleration { get; init; } = 0f;
    /// <summary>是否自动将实体旋转朝向速度方向（对无 VisualRoot 的节点生效）</summary>
    public bool RotateToVelocity { get; init; } = true;
    /// <summary>移动完成后是否自动销毁实体</summary>
    public bool DestroyOnComplete { get; init; } = false;
    /// <summary>与单位碰撞时是否自动销毁实体</summary>
    public bool DestroyOnCollision { get; init; } = false;

    // ======== 目标 / 方向 ========

    /// <summary>
    /// 目标点坐标，多策略复用：
    /// TargetPoint（目的地）/ Dash（OnEnter 采样方向）/ Boomerang（去程目标）/ BezierCurve（兼容模式终点）
    /// </summary>
    public Vector2 TargetPoint { get; init; } = Vector2.Zero;
    /// <summary>
    /// 是否实时追踪 <c>TargetNode</c>（仅 Charge 模式生效）。
    /// true = 每帧重算朝向目标方向（追踪/锁定模式），目标消失后维持最后方向。
    /// false（默认）= OnEnter 时一次性采样方向后锁定。
    /// </summary>
    public bool isTrackTarget { get; init; } = false;
    /// <summary>
    /// 目标节点引用，多策略复用：
    /// Charge（冲锋方向源 / 追踪目标）/ AttachToHost（宿主）
    /// </summary>
    public Node2D? TargetNode { get; init; } = null;
    /// <summary>
    /// 移动方向角度（度），Charge 方向备选（优先级最低）；
    ///  2D 下 0 = 向右、90 = 向下、180 = 向左，正值顺时针；策略内部按需转弧度</summary>
    public float Angle { get; init; } = 0f;
    /// <summary>到达距离阈值（像素），0 = 不启用；需要判断到达时自行设置并检查</summary>
    public float ReachDistance { get; init; } = 0f;

    // ======== 环绕（Orbit）========

    /// <summary>圆心坐标（固定点模式；设置 <c>TargetNode</c> 时以实体实时位置为主）</summary>
    public Vector2 OrbitCenter { get; init; } = Vector2.Zero;
    /// <summary>
    /// 初始角度（度），用于设置环绕Entity的初始位置；<c>null</c> = 从实体当前位置到圆心的方向自动推导，避免第一帧跳变。
    /// 0 = 右方，90 = 下方（Godot Y 向下），180 = 左方。
    /// 策略内部按需转弧度。
    /// </summary>
    public float? OrbitInitAngle { get; init; } = null;
    /// <summary>初始环绕半径（像素）</summary>
    public float OrbitRadius { get; init; } = 0f;
    /// <summary>
    /// 环绕半径通用驱动器。
    /// <c>null</c> = 不启用驱动，半径保持 <c>OrbitRadius</c> 常量不变。
    /// <c>InitialValue = null</c> 时继承 <c>OrbitRadius</c>。
    /// </summary>
    public ScalarDriverParams? OrbitRadiusScalarDriver { get; init; } = null;
    /// <summary>
    /// 初始角速度（度/秒）。
    /// 三选二推导：<c>OrbitAngularSpeed &gt; 0</c> 直接用；否则从 <c>OrbitTotalAngle / MaxDuration</c> 推算。
    /// 策略内部按需转弧度/秒。
    /// </summary>
    public float OrbitAngularSpeed { get; init; } = 0f;
    /// <summary>角加速度（度/秒²），0 = 匀速，正值加速，负值减速（减至 0 停转）；策略内部按需转弧度/秒²</summary>
    public float OrbitAngularAcceleration { get; init; } = 0f;
    /// <summary>
    /// 总环绕角度（度），-1 = 不限制。
    /// <c>360f * N</c> = 恰好 N 圈后完成。配合 <c>MaxDuration</c> 可三选二推算 <c>OrbitAngularSpeed</c>。
    /// 策略内部按需转弧度。
    /// </summary>
    public float OrbitTotalAngle { get; init; } = -1f;
    /// <summary>是否顺时针旋转（默认 true = 顺时针）</summary>
    public bool IsOrbitClockwise { get; init; } = true;

    // ======== 波形 ========

    /// <summary>横向振幅（像素，SineWave 模式）</summary>
    public float WaveAmplitude { get; init; } = 50f;
    /// <summary>
    /// 波形振幅通用驱动器。
    /// <c>null</c> = 不启用驱动，振幅保持 <c>WaveAmplitude</c> 常量不变。
    /// <c>InitialValue = null</c> 时继承 <c>WaveAmplitude</c>。
    /// </summary>
    public ScalarDriverParams? WaveAmplitudeScalarDriver { get; init; } = null;
    /// <summary>波形频率（周期/秒，SineWave 模式）</summary>
    public float WaveFrequency { get; init; } = 2f;
    /// <summary>
    /// 波形频率通用驱动器。
    /// <c>null</c> = 不启用驱动，频率保持 <c>WaveFrequency</c> 常量不变。
    /// <c>InitialValue = null</c> 时继承 <c>WaveFrequency</c>。
    /// </summary>
    public ScalarDriverParams? WaveFrequencyScalarDriver { get; init; } = null;
    /// <summary>初始相位偏移（度，SineWave 模式，用于错开多发同向波形弹的起始摆动；策略内部按需转弧度）</summary>
    public float WavePhase { get; init; } = 0f;

    // ======== 贝塞尔曲线 ========

    /// <summary>
    /// 完整控制点数组（含起点和终点，BezierCurve 模式）。
    /// 长度 = 阶数 + 1：2 点线性、3 点二次、4 点三阶（经典）、5+ 高阶。
    /// 第 0 个点会在 OnEnter 时自动替换为实体当前位置。
    /// </summary>
    public Vector2[]? BezierPoints { get; init; } = null;
    /// <summary>从起点走到终点的总时长（秒）</summary>
    public float BezierDuration { get; init; } = 1f;
    /// <summary>是否使用弧长参数化实现匀速移动</summary>
    public bool BezierUniformSpeed { get; init; } = false;

    // ======== 回旋镖 ========

    /// <summary>到达目标点后的停顿时间（秒），0 = 不停顿直接返回</summary>
    public float BoomerangPauseTime { get; init; } = 0f;
}
