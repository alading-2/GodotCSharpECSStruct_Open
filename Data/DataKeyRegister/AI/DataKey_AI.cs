

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
    // TargetNode 是 Node2D 引用，不走 DataRegistry 类型约束
    public const string TargetNode = "TargetNode";

    // ========== AI 行为状态 ==========
    // AI状态
    public static readonly DataMeta AIState = DataRegistry.Register(
        new DataMeta { Key = nameof(AIState), DisplayName = "AI状态", Description = "Idle/Chasing/Attacking/Patrolling/Fleeing", Category = DataCategory_AI.Basic, Type = typeof(global::AIState), DefaultValue = global::AIState.Idle });

    // 威胁值
    public static readonly DataMeta Threat = DataRegistry.Register(
        new DataMeta { Key = nameof(Threat), DisplayName = "威胁值", Description = "仇恨值", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 0f });

    // AI是否启用
    public static readonly DataMeta AIEnabled = DataRegistry.Register(
        new DataMeta { Key = nameof(AIEnabled), DisplayName = "AI是否启用", Description = "可用于暂停 AI 逻辑", Category = DataCategory_AI.Basic, Type = typeof(bool), DefaultValue = false });

    // ========== AI 感知参数 ==========
    // 索敌范围
    public static readonly DataMeta DetectionRange = DataRegistry.Register(
        new DataMeta { Key = nameof(DetectionRange), DisplayName = "索敌范围", Description = "圆形检测半径", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 500f, MinValue = 0 });

    // 丢失目标范围
    public static readonly DataMeta LoseTargetRange = DataRegistry.Register(
        new DataMeta { Key = nameof(LoseTargetRange), DisplayName = "丢失目标范围", Description = "超出此范围后放弃追逐", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 800f, MinValue = 0 });

    // ========== AI 移动参数 ==========
    // 巡逻半径
    public static readonly DataMeta PatrolRadius = DataRegistry.Register(
        new DataMeta { Key = nameof(PatrolRadius), DisplayName = "巡逻半径", Description = "以出生点为中心的随机巡逻范围", Category = DataCategory_AI.Basic, Type = typeof(float), DefaultValue = 500f, MinValue = 0 });

    // 巡逻等待时间
    public static readonly DataMeta PatrolWaitTime = DataRegistry.Register(
        new DataMeta { Key = nameof(PatrolWaitTime), DisplayName = "巡逻等待时间", Description = "到达巡逻点后等待多久再移动", Category = DataCategory_AI.Basic, Type = typeof(float), DefaultValue = 2f, MinValue = 0 });

    // ========== AI 黑板数据 ==========
    // 出生位置
    public static readonly DataMeta SpawnPosition = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnPosition), DisplayName = "出生位置", Description = "用于巡逻计算基准点", Category = DataCategory_AI.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // 巡逻目标点
    public static readonly DataMeta PatrolTargetPoint = DataRegistry.Register(
        new DataMeta { Key = nameof(PatrolTargetPoint), DisplayName = "巡逻目标点", Description = "当前巡逻目标点", Category = DataCategory_AI.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // 巡逻等待完成
    public static readonly DataMeta PatrolWaitDone = DataRegistry.Register(
        new DataMeta { Key = nameof(PatrolWaitDone), DisplayName = "巡逻等待完成", Description = "TimerManager回调写入的完成标记", Category = DataCategory_AI.Basic, Type = typeof(bool), DefaultValue = false });

    // ========== AI 移动意图 ==========
    // AI请求移动方向
    public static readonly DataMeta AIMoveDirection = DataRegistry.Register(
        new DataMeta { Key = nameof(AIMoveDirection), DisplayName = "AI请求移动方向", Description = "请求的移动方向（归一化），Zero表示停止", Category = DataCategory_AI.Basic, Type = typeof(Godot.Vector2), DefaultValue = Godot.Vector2.Zero });

    // AI移动速度倍率
    public static readonly DataMeta AIMoveSpeedMultiplier = DataRegistry.Register(
        new DataMeta { Key = nameof(AIMoveSpeedMultiplier), DisplayName = "AI移动速度倍率", Description = "请求的移动速度倍率（默认1.0）", Category = DataCategory_AI.Basic, Type = typeof(float), DefaultValue = 1.0f, MinValue = 0 });
}
