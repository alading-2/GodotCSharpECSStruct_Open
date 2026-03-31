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
/// TODO MovementParams有30个参数，每个运动策略的MovementParams都有这么多参数，要不要拆分，内存优化？
public record struct MovementParams
{
    /// <summary>
    /// 带默认值的 struct 需要显式无参构造函数。
    /// </summary>
    public MovementParams() { }

    // ======== 运行时统计 ========

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
    /// <summary>
    /// 移动模式下的移动速度（像素/秒），用于 Dash 等有明确动作速度的策略。
    /// 若某些策略同时支持 <c>ActionSpeed</c> 与 <c>MaxDuration</c>（如 Boomerang），则始终以 <c>ActionSpeed</c> 优先。
    /// </summary>
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
    /// 是否实时追踪 <c>TargetNode</c>（Charge / BezierCurve 模式生效）。
    /// true = 每帧更新朝向/终点至目标当前位置，目标消失后维持最后状态。
    /// false（默认）= OnEnter 时一次性采样后锁定。
    /// </summary>
    public bool isTrackTarget { get; init; } = false;
    /// <summary>
    /// 目标节点引用，多策略复用：
    /// Charge（追踪目标）用于重复、BezierCurve、BezierCurve追踪终点、AttachToHost等
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

    // ======== 曲线通用（抛物线 / 圆弧共用）========

    /// <summary>
    /// 是否自动调整弓起方向，使弧线始终朝向屏幕上方（Y 减小方向）。
    /// 适用于 <c>Parabola</c> 和 <c>CircularArc</c> 两种模式。
    /// 启用后，无论攻击方向如何，投射物弧线均弓向上方，避免攻击左右两侧时视觉不一致。
    /// </summary>
    public bool BowWorldUp { get; init; } = false;

    // ======== 抛物线 ========

    /// <summary>
    /// 抛物线顶点高度偏移，单位像素。
    /// 正值表示向上拱起，负值表示向下下坠，0 表示退化为直线。
    /// 启用 <c>BowWorldUp</c> 时，该值的正负符号会被自动修正，仅绝对值有效。
    /// </summary>
    public float ParabolaApexHeight { get; init; } = 0f;

    // ======== 圆弧 ========

    /// <summary>
    /// 圆弧半径，单位像素。
    /// 小于等于 0 时退化为直线移动。
    /// </summary>
    public float CircularArcRadius { get; init; } = 0f;

    /// <summary>
    /// 圆弧方向。
    /// false = 逆时针短弧，true = 顺时针短弧。
    /// 启用 <c>BowWorldUp</c> 时，该值被忽略，方向由攻击方向自动决定。
    /// </summary>
    public bool CircularArcClockwise { get; init; } = false;

    // ======== 回旋镖 ========

    /// <summary>
    /// 到达去程终点后的停顿时间，单位秒。
    /// 若 Boomerang 使用 <c>MaxDuration</c> 驱动，则总时长会先扣除该停顿，再由去程/返程均分剩余飞行时间。
    /// </summary>
    public float BoomerangPauseTime { get; init; } = 0f;

    /// <summary>
    /// 返程速度倍率。
    /// 小于等于 0 时策略内部回退为 1。
    /// </summary>
    public float BoomerangReturnSpeedMultiplier { get; init; } = 1f;

    /// <summary>
    /// 回旋镖弧高，单位像素。
    /// 小于等于 0 时按当前阶段弦长自动估算。
    /// </summary>
    public float BoomerangArcHeight { get; init; } = 0f;

    /// <summary>
    /// 回旋镖方向：true：顺时针，false：逆时针。
    /// 返程会自动反向，形成镜像回收轨迹。
    /// </summary>
    public bool BoomerangIsClockwise { get; init; } = false;
}
