/// <summary>
/// 运动模式枚举 - 定义 EntityMovementComponent 支持的全部轨迹类型
/// <para>
/// 第一版（v1）支持 6 种基础模式，覆盖绝大多数游戏运动需求：
/// 直线冲锋 / 目标点 / 追踪 / 环绕 / 螺旋 / 波形
/// </para>
/// <para>
/// 扩展预留：Boomerang（回旋镖）、MultiPhase（多段运动）、
/// PredictiveHoming（预测追踪）等待第二版实现
/// </para>
/// </summary>
public enum MoveMode
{
    /// <summary>无运动，组件停止更新位置</summary>
    None = 0,

    /// <summary>固定方向冲锋：沿 DataKey.Velocity 方向匀速直线运动</summary>
    FixedDirection = 1,

    /// <summary>目标点冲锋：向 DataKey.MoveTargetPoint 直线运动，到达后完成</summary>
    TargetPoint = 2,

    /// <summary>追踪目标实体：持续追向 DataKey.MoveTargetNode，到达后完成；目标丢失则直线继续</summary>
    TargetEntity = 3,

    /// <summary>围绕中心点环绕：以 DataKey.OrbitCenterPoint 为圆心做圆周运动</summary>
    OrbitPoint = 4,

    /// <summary>围绕目标实体环绕：圆心实时跟随 DataKey.MoveTargetNode 位置</summary>
    OrbitEntity = 5,

    /// <summary>螺旋收缩/扩张：环绕模式 + DataKey.OrbitRadius 向 OrbitTargetRadius 渐变</summary>
    Spiral = 6,

    /// <summary>波形前进：沿 DataKey.Velocity 方向前进，叠加正弦横向偏移</summary>
    SineWave = 7,

    /// <summary>贝塞尔曲线：沿三次贝塞尔曲线 (P0→P1→P2→P3) 运动</summary>
    BezierCurve = 8,

    /// <summary>回旋镖：飞向目标点后自动返回起点</summary>
    Boomerang = 9,

    /// <summary>附着跟随：每帧跟随 MoveTargetNode 位置 + EffectOffset 偏移，宿主无效时运动完成</summary>
    AttachToHost = 10,

    /// <summary>玩家输入驱动：读取 InputManager 输入，Lerp 平滑加速，写入 Velocity 由物理帧执行（仅 CharacterBody2D）</summary>
    PlayerInput = 11,

    /// <summary>AI 决策驱动：读取 AIMoveDirection + AIMoveSpeedMultiplier，写入 Velocity 由物理帧执行（仅 CharacterBody2D）</summary>
    AIControlled = 12,
}
