/// <summary>
/// 运动模式枚举，定义 `EntityMovementComponent` 当前支持的所有运动策略。
/// <para>
/// 这些模式只是“如何移动”的描述，不包含业务语义。业务侧通过先写 DataKey，再发送 `MovementStarted` 事件来切换模式。
/// </para>
/// </summary>
public enum MoveMode
{
    /// <summary>
    /// 无运动。
    /// <para>
    /// 组件不会执行任何策略更新，常用于没有默认移动逻辑的实体。
    /// </para>
    /// </summary>
    None = 0,

    /// <summary>
    /// 冲锋（统一冲刺 / 追点 / 追踪）。
    /// <para>
    /// 方向优先级：TargetNode > TargetPoint > Angle。
    /// 速度推导（三选二）：ActionSpeed+MaxDuration | ActionSpeed+MaxDistance | MaxDistance+MaxDuration。
    /// </para>
    /// </summary>
    Charge = 1,

    /// <summary>
    /// 环绕（固定点 / 目标实体）。
    /// <para>
    /// 圆心优先级：TargetNode（每帧实时跟随）> OrbitCenter（固定世界坐标）。
    /// 不设置 TargetNode → 固定点环绕；设置 TargetNode → 实体跟随环绕，目标失效时原地暂停。
    /// </para>
    /// </summary>
    Orbit = 2,

    /// <summary>
    /// 正弦波前进。
    /// <para>
    /// 沿基础前进方向移动，同时叠加横向波动位移。
    /// </para>
    /// </summary>
    SineWave = 3,

    /// <summary>
    /// 贝塞尔曲线移动。
    /// <para>
    /// 沿控制点定义的曲线前进，可选开启匀速参数化。
    /// </para>
    /// </summary>
    BezierCurve = 4,

    /// <summary>
    /// 回旋镖运动。
    /// <para>
    /// 去程飞向目标点，可选停顿，再自动回到起点。
    /// </para>
    /// </summary>
    Boomerang = 5,

    /// <summary>
    /// 附着跟随。
    /// <para>
    /// 持续对齐到宿主节点及偏移量，宿主失效时完成。
    /// </para>
    /// </summary>
    AttachToHost = 6,

    /// <summary>
    /// 玩家输入驱动移动。
    /// <para>
    /// 适合作为玩家默认模式，固定帧率下读取输入并平滑写回速度。
    /// </para>
    /// </summary>
    PlayerInput = 7,

    /// <summary>
    /// AI 决策驱动移动。
    /// <para>
    /// 适合作为敌人默认模式，固定帧率下读取 AI 写入的方向和速度倍率。
    /// </para>
    /// </summary>
    AIControlled = 8,


}
