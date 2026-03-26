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
    /// 方向优先级：TargetNode > TargetPoint > Angle > 当前 Velocity 方向。
    /// 速度推导（三选二）：ActionSpeed+MaxDuration | ActionSpeed+MaxDistance | MaxDistance+MaxDuration。
    /// </para>
    /// </summary>
    Charge = 1,

    /// <summary>
    /// 围绕固定点环绕。
    /// <para>
    /// 读取圆心、半径和角速度，持续做圆周运动。
    /// </para>
    /// </summary>
    OrbitPoint = 4,

    /// <summary>
    /// 围绕目标实体环绕。
    /// <para>
    /// 每帧同步目标实体位置为圆心，再复用固定圆心环绕逻辑。
    /// </para>
    /// </summary>
    OrbitEntity = 5,

    /// <summary>
    /// 螺旋运动。
    /// <para>
    /// 在环绕的基础上逐渐改变半径，直到靠近目标半径后继续环绕。
    /// </para>
    /// </summary>
    Spiral = 6,

    /// <summary>
    /// 正弦波前进。
    /// <para>
    /// 沿基础前进方向移动，同时叠加横向波动位移。
    /// </para>
    /// </summary>
    SineWave = 7,

    /// <summary>
    /// 贝塞尔曲线移动。
    /// <para>
    /// 沿控制点定义的曲线前进，可选开启匀速参数化。
    /// </para>
    /// </summary>
    BezierCurve = 8,

    /// <summary>
    /// 回旋镖运动。
    /// <para>
    /// 去程飞向目标点，可选停顿，再自动回到起点。
    /// </para>
    /// </summary>
    Boomerang = 9,

    /// <summary>
    /// 附着跟随。
    /// <para>
    /// 持续对齐到宿主节点及偏移量，宿主失效时完成。
    /// </para>
    /// </summary>
    AttachToHost = 10,

    /// <summary>
    /// 玩家输入驱动移动。
    /// <para>
    /// 适合作为玩家默认模式，固定帧率下读取输入并平滑写回速度。
    /// </para>
    /// </summary>
    PlayerInput = 11,

    /// <summary>
    /// AI 决策驱动移动。
    /// <para>
    /// 适合作为敌人默认模式，固定帧率下读取 AI 写入的方向和速度倍率。
    /// </para>
    /// </summary>
    AIControlled = 12,


}
