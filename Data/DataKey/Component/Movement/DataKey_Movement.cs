/// <summary>
/// 数据键定义 - 运动域（EntityMovementComponent 专用）
/// <para>
/// 复用现有键：DataKey.Velocity（速度向量）、DataKey.MoveSpeed（速度大小）
/// 新增键只描述"运动执行参数"，不涉及 AI 决策或业务语义
/// </para>
/// <para>
/// 使用规范：
/// - 配置参数（MoveMode / MoveMaxDuration 等）由 Spawn 入口或技能执行器在生成时写入
/// - 运行时状态（MoveElapsedTime / MoveTraveledDistance）由组件每帧自动更新
/// - MoveTargetNode 存储 Node2D 引用，与 AI 系统的 TargetNode 模式保持一致
/// </para>
/// </summary>
public static partial class DataKey
{

    // ========================= 速度分层合成 =========================
    // 基础速度向量（由 VelocityComponent/AI/EntityMovementComponent 写入）
    public static readonly DataMeta Velocity = DataRegistry.Register(
        new DataMeta { Key = nameof(Velocity), DisplayName = "基础速度向量", Description = "基础移动意图速度，由输入/AI/运动策略写入", Category = DataCategory_Movement.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // 强制覆盖速度（击退、控制技能期间覆盖基础速度；Zero 表示不覆盖）
    public static readonly DataMeta VelocityOverride = DataRegistry.Register(
        new DataMeta { Key = nameof(VelocityOverride), DisplayName = "强制覆盖速度", Description = "击退/控制技能期间覆盖基础速度，Zero=不覆盖", Category = DataCategory_Movement.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // 瞬时冲量（爆炸推力、弹射等，叠加到最终速度后自动清零）
    public static readonly DataMeta VelocityImpulse = DataRegistry.Register(
        new DataMeta { Key = nameof(VelocityImpulse), DisplayName = "瞬时冲量", Description = "单帧叠加冲量，用后自动清零", Category = DataCategory_Movement.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // 移动锁定标记（眩晕、冻结期间为 true，阻断所有基础移动）
    public static readonly DataMeta IsMovementLocked = DataRegistry.Register(
        new DataMeta { Key = nameof(IsMovementLocked), DisplayName = "移动锁定", Description = "眩晕/冻结期间锁定移动", Category = DataCategory_Movement.Basic, Type = typeof(bool), DefaultValue = false });

    // 加速度
    public static readonly DataMeta Acceleration = DataRegistry.Register(
        new DataMeta { Key = nameof(Acceleration), DisplayName = "加速度", Category = DataCategory_Movement.Basic, Type = typeof(float), DefaultValue = 10f });


    // ========================= 运动核心 =========================

    /// <summary>运动模式 (MoveMode enum，默认 MoveMode.None = 0)</summary>
    public static readonly DataMeta MoveMode = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveMode), DisplayName = "运动模式", Description = "运动轨迹类型：None/FixedDirection/TargetPoint/TargetEntity/OrbitPoint/OrbitEntity/Spiral/SineWave", Category = DataCategory_Movement.Basic, Type = typeof(global::MoveMode), DefaultValue = global::MoveMode.None });

    /// <summary>默认运动模式 (MoveMode enum，运动完成后自动回退到此模式，如玩家 PlayerInput、敌人 AIControlled)</summary>
    public static readonly DataMeta DefaultMoveMode = DataRegistry.Register(
        new DataMeta { Key = nameof(DefaultMoveMode), DisplayName = "默认运动模式", Description = "运动完成后自动回退的模式（由 Entity 初始化时设置）", Category = DataCategory_Movement.Basic, Type = typeof(global::MoveMode), DefaultValue = global::MoveMode.None });

    /// <summary>最大持续时间 (float，秒；-1=不限制)</summary>
    public static readonly DataMeta MoveMaxDuration = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveMaxDuration), DisplayName = "最大持续时间", Description = "运动最大持续时间（秒），-1=不限制", Category = DataCategory_Movement.Basic, Type = typeof(float), DefaultValue = -1f, MinValue = -1 });

    /// <summary>最大移动距离 (float，像素；-1=不限制)</summary>
    public static readonly DataMeta MoveMaxDistance = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveMaxDistance), DisplayName = "最大移动距离", Description = "运动最大移动距离（像素），-1=不限制", Category = DataCategory_Movement.Basic, Type = typeof(float), DefaultValue = -1f, MinValue = -1 });

    /// <summary>已运动时间 (float，秒，运行时累计，由组件写入)</summary>
    public static readonly DataMeta MoveElapsedTime = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveElapsedTime), DisplayName = "已运动时间", Description = "已运动时间（秒），运行时累计", Category = DataCategory_Movement.Basic, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

    /// <summary>已移动距离 (float，像素，运行时累计，由组件写入)</summary>
    public static readonly DataMeta MoveTraveledDistance = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveTraveledDistance), DisplayName = "已移动距离", Description = "已移动距离（像素），运行时累计", Category = DataCategory_Movement.Basic, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

    /// <summary>运动是否已完成 (bool，由组件在满足结束条件时写入)</summary>
    public static readonly DataMeta MoveCompleted = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveCompleted), DisplayName = "运动完成", Description = "运动是否已完成", Category = DataCategory_Movement.Basic, Type = typeof(bool), DefaultValue = false });

    /// <summary>运动完成后是否自动销毁实体 (bool，默认 false)</summary>
    public static readonly DataMeta MoveDestroyOnComplete = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveDestroyOnComplete), DisplayName = "完成后销毁", Description = "运动完成后是否自动销毁实体", Category = DataCategory_Movement.Basic, Type = typeof(bool), DefaultValue = false });

    // ========================= 目标相关 =========================

    /// <summary>目标点 (Vector2；TargetPoint 模式)</summary>
    public static readonly DataMeta MoveTargetPoint = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveTargetPoint), DisplayName = "目标点", Description = "目标点坐标（TargetPoint 模式）", Category = DataCategory_Movement.Target, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // MoveTargetNode 是 Node2D 引用，不走 DataRegistry 类型约束
    // 存储 Node2D 引用，与 AI 系统的 DataKey.TargetNode 模式保持一致
    public const string MoveTargetNode = "MoveTargetNode";

    /// <summary>到达距离阈值 (float，像素；<=0 时使用默认值 5f)</summary>
    public static readonly DataMeta MoveReachDistance = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveReachDistance), DisplayName = "到达距离", Description = "到达距离阈值（像素），<=0 时使用默认值 5f", Category = DataCategory_Movement.Target, Type = typeof(float), DefaultValue = 5f, MinValue = 0 });

    // ========================= 朝向 =========================

    /// <summary>是否自动将实体旋转朝向速度方向 (bool，默认 false)</summary>
    public static readonly DataMeta RotateToVelocity = DataRegistry.Register(
        new DataMeta { Key = nameof(RotateToVelocity), DisplayName = "自动朝向速度", Description = "是否自动将实体旋转朝向速度方向", Category = DataCategory_Movement.Basic, Type = typeof(bool), DefaultValue = false });

    // ========================= 环绕 / 螺旋 =========================

    /// <summary>环绕圆心 (Vector2；OrbitPoint / Spiral 模式)</summary>
    public static readonly DataMeta OrbitCenterPoint = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitCenterPoint), DisplayName = "环绕圆心", Description = "环绕圆心坐标（OrbitPoint/Spiral 模式）", Category = DataCategory_Movement.Orbit, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>环绕半径 (float，像素)</summary>
    public static readonly DataMeta OrbitRadius = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitRadius), DisplayName = "环绕半径", Description = "环绕半径（像素）", Category = DataCategory_Movement.Orbit, Type = typeof(float), DefaultValue = 100f, MinValue = 0 });

    /// <summary>螺旋目标半径 (float；Spiral 模式，半径渐变至此值后停止变化)</summary>
    public static readonly DataMeta OrbitTargetRadius = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitTargetRadius), DisplayName = "螺旋目标半径", Description = "螺旋目标半径（Spiral 模式）", Category = DataCategory_Movement.Orbit, Type = typeof(float), DefaultValue = 50f, MinValue = 0 });

    /// <summary>当前环绕角度 (float，弧度，由组件每帧更新)</summary>
    public static readonly DataMeta OrbitAngle = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitAngle), DisplayName = "当前环绕角度", Description = "当前环绕角度（弧度），运行时累计", Category = DataCategory_Movement.Orbit, Type = typeof(float), DefaultValue = 0f });

    /// <summary>角速度 (float，弧度/秒)</summary>
    public static readonly DataMeta OrbitAngularSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitAngularSpeed), DisplayName = "角速度", Description = "角速度（弧度/秒）", Category = DataCategory_Movement.Orbit, Type = typeof(float), DefaultValue = 3.14159f, MinValue = 0 });

    /// <summary>径向变化速度 (float，像素/秒；Spiral 模式；<=0 时使用默认值 50f)</summary>
    public static readonly DataMeta OrbitRadialSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitRadialSpeed), DisplayName = "径向变化速度", Description = "径向变化速度（像素/秒，Spiral 模式）", Category = DataCategory_Movement.Orbit, Type = typeof(float), DefaultValue = 50f, MinValue = 0 });

    /// <summary>是否顺时针旋转 (bool，默认 false = 逆时针)</summary>
    public static readonly DataMeta OrbitClockwise = DataRegistry.Register(
        new DataMeta { Key = nameof(OrbitClockwise), DisplayName = "顺时针旋转", Description = "是否顺时针旋转（默认 false = 逆时针）", Category = DataCategory_Movement.Orbit, Type = typeof(bool), DefaultValue = false });

    // ========================= 波形 =========================

    /// <summary>横向振幅 (float，像素；SineWave 模式)</summary>
    public static readonly DataMeta WaveAmplitude = DataRegistry.Register(
        new DataMeta { Key = nameof(WaveAmplitude), DisplayName = "波形振幅", Description = "横向振幅（像素，SineWave 模式）", Category = DataCategory_Movement.Wave, Type = typeof(float), DefaultValue = 50f, MinValue = 0 });

    /// <summary>波形频率 (float，周期/秒；SineWave 模式)</summary>
    public static readonly DataMeta WaveFrequency = DataRegistry.Register(
        new DataMeta { Key = nameof(WaveFrequency), DisplayName = "波形频率", Description = "波形频率（周期/秒，SineWave 模式）", Category = DataCategory_Movement.Wave, Type = typeof(float), DefaultValue = 2f, MinValue = 0 });

    /// <summary>初始相位偏移 (float，弧度；SineWave 模式，用于错开多个同向弹道)</summary>
    public static readonly DataMeta WavePhase = DataRegistry.Register(
        new DataMeta { Key = nameof(WavePhase), DisplayName = "初始相位", Description = "初始相位偏移（弧度，SineWave 模式）", Category = DataCategory_Movement.Wave, Type = typeof(float), DefaultValue = 0f });

    // ========================= 贝塞尔曲线 =========================

    /// <summary>
    /// 贝塞尔控制点数组 (Vector2[]；BezierCurve 模式，支持任意阶)
    /// <para>
    /// 完整控制点序列（含起点和终点），长度 = 阶数 + 1：
    /// - 2 点 = 线性，3 点 = 二次，4 点 = 三阶（经典），5+ 点 = 高阶
    /// - 若未设置，策略会从 BezierControlPoint1/2 + StartPoint/TargetPoint 构建三阶兼容数组
    /// </para>
    /// </summary>
    public static readonly DataMeta BezierControlPoints = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierControlPoints), DisplayName = "贝塞尔控制点数组", Description = "完整控制点序列（含起点终点），支持任意阶贝塞尔", Category = DataCategory_Movement.Bezier, Type = typeof(Godot.Vector2[]) });

    /// <summary>贝塞尔控制点 1 (Vector2；三阶快捷方式，向后兼容)</summary>
    public static readonly DataMeta BezierControlPoint1 = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierControlPoint1), DisplayName = "贝塞尔控制点1", Description = "三阶贝塞尔的第一个控制点（向后兼容快捷方式）", Category = DataCategory_Movement.Bezier, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>贝塞尔控制点 2 (Vector2；三阶快捷方式，向后兼容)</summary>
    public static readonly DataMeta BezierControlPoint2 = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierControlPoint2), DisplayName = "贝塞尔控制点2", Description = "三阶贝塞尔的第二个控制点（向后兼容快捷方式）", Category = DataCategory_Movement.Bezier, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>贝塞尔起始点 (Vector2；OnEnter 时自动记录实体当前位置)</summary>
    public static readonly DataMeta BezierStartPoint = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierStartPoint), DisplayName = "贝塞尔起始点", Description = "贝塞尔曲线起点（自动记录）", Category = DataCategory_Movement.Bezier, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>贝塞尔总时长 (float，秒；从起点到终点的运动时间)</summary>
    public static readonly DataMeta BezierDuration = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierDuration), DisplayName = "贝塞尔总时长", Description = "贝塞尔曲线运动总时长（秒）", Category = DataCategory_Movement.Bezier, Type = typeof(float), DefaultValue = 1f, MinValue = 0.01f });

    /// <summary>是否使用弧长参数化实现匀速运动 (bool，默认 false)</summary>
    public static readonly DataMeta BezierUniformSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierUniformSpeed), DisplayName = "贝塞尔匀速", Description = "是否使用弧长参数化实现匀速运动", Category = DataCategory_Movement.Bezier, Type = typeof(bool), DefaultValue = false });

    /// <summary>弧长参数化查找表 (float[]；运行时由策略自动生成，外部不需要设置)</summary>
    public static readonly DataMeta BezierLengthLut = DataRegistry.Register(
        new DataMeta { Key = nameof(BezierLengthLut), DisplayName = "贝塞尔弧长LUT", Description = "弧长参数化查找表（运行时自动生成）", Category = DataCategory_Movement.Bezier, Type = typeof(float[]) });

    // ========================= 回旋镖 =========================

    /// <summary>回旋镖起始点 (Vector2；OnEnter 时自动记录实体当前位置)</summary>
    public static readonly DataMeta BoomerangStartPoint = DataRegistry.Register(
        new DataMeta { Key = nameof(BoomerangStartPoint), DisplayName = "回旋镖起始点", Description = "回旋镖起点（自动记录）", Category = DataCategory_Movement.Boomerang, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>回旋镖是否处于返回阶段 (bool，由策略自动切换)</summary>
    public static readonly DataMeta BoomerangReturning = DataRegistry.Register(
        new DataMeta { Key = nameof(BoomerangReturning), DisplayName = "回旋镖返回中", Description = "是否正在返回起点", Category = DataCategory_Movement.Boomerang, Type = typeof(bool), DefaultValue = false });

    /// <summary>回旋镖在目标点的停顿时间 (float，秒；0=不停顿)</summary>
    public static readonly DataMeta BoomerangPauseTime = DataRegistry.Register(
        new DataMeta { Key = nameof(BoomerangPauseTime), DisplayName = "回旋镖停顿时间", Description = "到达目标点后的停顿时间（秒），0=不停顿", Category = DataCategory_Movement.Boomerang, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

    /// <summary>回旋镖停顿计时器 (float，运行时由策略写入)</summary>
    public static readonly DataMeta BoomerangPauseTimer = DataRegistry.Register(
        new DataMeta { Key = nameof(BoomerangPauseTimer), DisplayName = "回旋镖停顿计时器", Description = "回旋镖停顿剩余时间", Category = DataCategory_Movement.Boomerang, Type = typeof(float), DefaultValue = 0f });
}
