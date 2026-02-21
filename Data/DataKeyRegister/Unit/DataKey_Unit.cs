/// <summary>
/// 数据键定义 - 类型安全的数据访问
/// 使用常量而非枚举，支持 Mod 扩展
/// </summary>
public static partial class DataKey
{
    // === 基础信息 ===
    public const string Level = "Level"; // 等级
    public const string VisualScenePath = "VisualScenePath"; // 视觉场景路径
    public const string UnitRank = "UnitRank"; // 单位品阶 (Enum: UnitRank)
    // === Spawn ===
    public const string IsEnableSpawnRule = "IsEnableSpawnRule"; // 是否启用生成规则
    public const string SpawnStrategy = "SpawnStrategy"; // 生成策略
    public const string SpawnMinWave = "SpawnMinWave"; // 最小波次
    public const string SpawnMaxWave = "SpawnMaxWave"; // 最大波次
    public const string SpawnInterval = "SpawnInterval"; // 生成间隔
    public const string SpawnMaxCountPerWave = "SpawnMaxCountPerWave"; // 单波次最大数量
    public const string SingleSpawnCount = "SingleSpawnCount"; // 单次生成数量
    public const string SingleSpawnVariance = "SingleSpawnVariance"; // 单次随机波动
    public const string SpawnStartDelay = "SpawnStartDelay"; // 开始延迟
    public const string SpawnWeight = "SpawnWeight"; // 权重
    public const string HealthBarHeight = "HealthBarHeight"; // 血条高度（Y轴偏移）
    public const string IsShowHealthBar = "IsShowHealthBar"; // 是否显示血条

    // === Enemy ===
    public const string ExpReward = "ExpReward"; // 经验奖励
    public const string SpawnRule = "SpawnRule"; // 生成规则

    // === 状态标记 ===
    // 恢复
    public const string IsDisableHealthRecovery = "IsDisableHealthRecovery"; // 是否禁止生命恢复
    public const string IsDisableManaRecovery = "IsDisableManaRecovery"; // 是否禁止法力恢复

    public const string IsDead = "IsDead"; // 是否死亡
    public const string IsInvulnerable = "IsInvulnerable"; // 是否无敌（不受伤害）
    public const string IsImmune = "IsImmune"; // 是否免疫（不受控制效果）
    public const string IsStunned = "IsStunned"; // 是否眩晕
    public const string IsSilenced = "IsSilenced"; // 是否沉默
    public const string IsInvisible = "IsInvisible"; // 是否隐身
    public const string AttackState = "AttackState"; // 攻击状态 (enum: AttackState)

    // === 生命周期LifecycleComponent ===
    public const string LifecycleState = "LifecycleState"; // 生命周期状态
    public const string DeathType = "DeathType"; // 死亡类型
    public const string CanRevive = "CanRevive"; // 是否可以复活
    public const string DeathCount = "DeathCount"; // 死亡次数
    public const string MaxLifeTime = "MaxLifeTime"; // 最大生存时间

    // === FollowComponent ===
    public const string FollowSpeed = "FollowSpeed"; // 跟随速度
    public const string StopDistance = "StopDistance"; // 停止距离

    // === VelocityComponent ===
    public const string Velocity = "Velocity"; // 当前速度向量
    public const string Acceleration = "Acceleration"; // 加速度
    // === HurtboxComponent ===
    public const string InvincibilityTimer = "InvincibilityTimer"; // 无敌计时器

    // === 伤害统计（受击方） ===
    public const string TotalDamageTaken = "TotalDamageTaken"; // 累计受到的伤害
    public const string WaveDamageTaken = "WaveDamageTaken"; // 本波次受到的伤害

    // === 伤害统计（攻击方） ===
    public const string TotalDamageDealt = "TotalDamageDealt"; // 累计造成的伤害
    public const string WaveDamageDealt = "WaveDamageDealt"; // 本波次造成的伤害
    public const string HighestSingleDamage = "HighestSingleDamage"; // 单次最高伤害
    public const string TotalKills = "TotalKills"; // 累计击杀数
    public const string WaveKills = "WaveKills"; // 本波次击杀数
    public const string TotalHits = "TotalHits"; // 总命中次数/造成伤害次数
    public const string WaveHits = "WaveHits"; // 本波次命中次数/造成伤害次数
    public const string TotalCriticalHits = "TotalCriticalHits"; // 总暴击次数
    public const string WaveCriticalHits = "WaveCriticalHits"; // 本波次暴击次数
    // === RecoveryComponent ===
    public const string IsRecoverySystemRegistered = "IsRecoverySystemRegistered"; // 是否注册了恢复系统
}
