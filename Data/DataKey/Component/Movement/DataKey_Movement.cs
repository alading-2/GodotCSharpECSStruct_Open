/// <summary>
/// 数据键定义 - 运动域（跨系统共享属性）
/// <para>
/// 此文件只保留真正需要跨系统共享的 DataKey：
/// - VelocityResolver 读写的速度分层合成键
/// - 状态效果系统写入的移动锁定键
/// - AI/输入系统持续写入的方向键
/// - 外部系统观察当前运动模式的键
/// </para>
/// <para>
/// 每次运动的输入参数（目标点、最大距离/时长、波形参数等）已迁移到 <see cref="MovementParams"/>，
/// 通过 <see cref="GameEventType.Unit.MovementStartedEventData"/> 传入，存于 EntityMovementComponent。
/// 策略运行时内部状态（角度、起点、LUT 等）存于各策略类的私有字段。
/// </para>
/// </summary>
public static partial class DataKey
{
    // ========================= 速度分层合成 =========================

    /// <summary>基础速度向量（由输入/AI/运动策略写入，VelocityResolver 合成时读取）</summary>
    public static readonly DataMeta Velocity = DataRegistry.Register(
        new DataMeta { Key = nameof(Velocity), DisplayName = "基础速度向量", Description = "基础移动意图速度，由输入/AI/运动策略写入", Category = DataCategory_Movement.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>强制覆盖速度（击退/控制技能期间覆盖基础速度；Zero=不覆盖）</summary>
    public static readonly DataMeta VelocityOverride = DataRegistry.Register(
        new DataMeta { Key = nameof(VelocityOverride), DisplayName = "强制覆盖速度", Description = "击退/控制技能期间覆盖基础速度，Zero=不覆盖", Category = DataCategory_Movement.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>瞬时冲量（单帧叠加冲量，用后自动清零）</summary>
    public static readonly DataMeta VelocityImpulse = DataRegistry.Register(
        new DataMeta { Key = nameof(VelocityImpulse), DisplayName = "瞬时冲量", Description = "单帧叠加冲量，用后自动清零", Category = DataCategory_Movement.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    /// <summary>移动锁定标记（眩晕/冻结期间为 true，由状态效果系统写入）</summary>
    public static readonly DataMeta IsMovementLocked = DataRegistry.Register(
        new DataMeta { Key = nameof(IsMovementLocked), DisplayName = "移动锁定", Description = "眩晕/冻结期间锁定移动", Category = DataCategory_Movement.Basic, Type = typeof(bool), DefaultValue = false });

    /// <summary>加速度（PlayerInputStrategy 读取，由实体属性系统管理）</summary>
    public static readonly DataMeta Acceleration = DataRegistry.Register(
        new DataMeta { Key = nameof(Acceleration), DisplayName = "加速度", Category = DataCategory_Movement.Basic, Type = typeof(float), DefaultValue = 10f });

    // ========================= 运动模式（外部观察用）=========================

    /// <summary>当前运动模式（由 EntityMovementComponent 写入，AI/外部系统可读取）</summary>
    public static readonly DataMeta MoveMode = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveMode), DisplayName = "运动模式", Description = "当前运动轨迹类型，由 EntityMovementComponent 维护", Category = DataCategory_Movement.Basic, Type = typeof(global::MoveMode), DefaultValue = global::MoveMode.None });

    /// <summary>默认运动模式（Entity 初始化时设置，运动完成后自动回退到此模式）</summary>
    public static readonly DataMeta DefaultMoveMode = DataRegistry.Register(
        new DataMeta { Key = nameof(DefaultMoveMode), DisplayName = "默认运动模式", Description = "运动完成后自动回退的模式（由 Entity 初始化时设置）", Category = DataCategory_Movement.Basic, Type = typeof(global::MoveMode), DefaultValue = global::MoveMode.None });
}
