

/// <summary>
/// AI 运行时状态枚举
/// </summary>
public enum AIState
{
    /// <summary> 待机 </summary>
    Idle = 0,
    /// <summary> 追逐 </summary>
    Chasing = 1,
    /// <summary> 攻击 </summary>
    Attacking = 2,
    /// <summary> 巡逻 </summary>
    Patrolling = 3,
    /// <summary> 逃跑 </summary>
    Fleeing = 4
}


/// <summary>
/// 数据键定义 - AI 相关
/// <para>
/// 分为以下几类：
/// 1. AI 行为状态 - 运行时状态标记
/// 2. AI 感知参数 - 索敌、视野等
/// 3. AI 攻击参数 - 攻击间隔、攻击距离等
/// 4. AI 移动参数 - 巡逻、追逐等
/// 5. AI 黑板数据 - 行为树运行时共享数据
/// </para>
/// <para>
/// 注意：移动速度使用 DataKey.MoveSpeed (DataKey_Attribute)
///       跟随速度使用 DataKey.FollowSpeed (DataKey_Unit)
///       攻击力使用   DataKey.BaseAttack / FinalAttack (DataKey_Attribute)
/// </para>
/// </summary>

public static partial class DataKey
{
    // ========== AI 行为状态 ==========

    /// <summary>AI状态（Idle/Chasing/Attacking/Patrolling/Fleeing）</summary>
    public const string AIState = "AIState";

    /// <summary>威胁值（仇恨值，用于多目标优先级排序）</summary>
    public const string Threat = "Threat";

    /// <summary>当前目标节点引用（Node2D，AI运行时临时数据）</summary>
    public const string TargetNode = "TargetNode";

    /// <summary>AI 是否启用（bool，可用于暂停 AI 逻辑）</summary>
    public const string AIEnabled = "AIEnabled";

    // ========== AI 感知参数 ==========

    /// <summary>索敌范围（圆形检测半径）</summary>
    public const string DetectionRange = "DetectionRange";

    /// <summary>丢失目标范围（超出此范围后放弃追逐，通常 > DetectionRange）</summary>
    public const string LoseTargetRange = "LoseTargetRange";

    // ========== AI 移动参数 ==========

    /// <summary>巡逻半径（以出生点为中心的随机巡逻范围）</summary>
    public const string PatrolRadius = "PatrolRadius";

    /// <summary>巡逻等待时间（秒，到达巡逻点后等待多久再移动）</summary>
    public const string PatrolWaitTime = "PatrolWaitTime";

    // ========== AI 黑板数据（运行时使用，不建议外部设置） ==========

    /// <summary>出生位置（用于巡逻计算基准点）</summary>
    public const string SpawnPosition = "SpawnPosition";

    /// <summary>当前巡逻目标点</summary>
    public const string PatrolTargetPoint = "PatrolTargetPoint";

    /// <summary>巡逻等待计时器（AI行为树运行时数据）</summary>
    public const string PatrolWaitTimer = "PatrolWaitTimer";

    /// <summary>攻击动画名称（可选配置，默认使用 Anim.Attack1）</summary>
    public const string AttackAnimName = "AttackAnimName";
}
