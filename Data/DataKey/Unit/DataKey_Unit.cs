/// <summary>
/// 数据键定义 - 单位域
/// </summary>
public static partial class DataKey
{
    // === 基础信息 ===
    // 等级
    public static readonly DataMeta Level = DataRegistry.Register(
        new DataMeta { Key = nameof(Level), DisplayName = "等级", Description = "实体的等级", Category = DataCategory_Base.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1, MaxValue = GlobalConfig.Maxlevel });

    // 视觉场景（玩家角色、特效等）
    public const string VisualScenePath = "VisualScenePath";

    // 单位品阶
    public static readonly DataMeta UnitRank = DataRegistry.Register(
        new DataMeta { Key = nameof(UnitRank), DisplayName = "单位品阶", Description = "单位品阶", Category = DataCategory_Base.Basic, Type = typeof(UnitRank), DefaultValue = global::UnitRank.Normal });

    // 是否显示血条
    public static readonly DataMeta IsShowHealthBar = DataRegistry.Register(
        new DataMeta { Key = nameof(IsShowHealthBar), DisplayName = "是否显示血条", Description = "是否显示血条", Category = DataCategory_Base.Basic, Type = typeof(bool), DefaultValue = true });

    // === 恢复控制 ===
    // 是否禁止生命恢复
    public static readonly DataMeta IsDisableHealthRecovery = DataRegistry.Register(
        new DataMeta { Key = nameof(IsDisableHealthRecovery), DisplayName = "是否禁止生命恢复", Description = "是否禁止生命恢复", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false });

    // 是否禁止魔法恢复
    public static readonly DataMeta IsDisableManaRecovery = DataRegistry.Register(
        new DataMeta { Key = nameof(IsDisableManaRecovery), DisplayName = "是否禁止魔法恢复", Description = "是否禁止魔法恢复", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false });

    // === Spawn ===
    // 是否启用SpawnRule
    public static readonly DataMeta IsEnableSpawnRule = DataRegistry.Register(
        new DataMeta { Key = nameof(IsEnableSpawnRule), DisplayName = "是否启用SpawnRule", Category = DataCategory_Unit.Spawn, Type = typeof(bool), DefaultValue = true });

    // 生成策略
    public static readonly DataMeta SpawnStrategy = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnStrategy), DisplayName = "生成策略", Category = DataCategory_Unit.Spawn, Type = typeof(SpawnPositionStrategy), DefaultValue = SpawnPositionStrategy.Rectangle });

    // 最小波次
    public static readonly DataMeta SpawnMinWave = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnMinWave), DisplayName = "最小波次", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 0 });

    // 最大波次
    public static readonly DataMeta SpawnMaxWave = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnMaxWave), DisplayName = "最大波次", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = -1 });

    // 生成间隔
    public static readonly DataMeta SpawnInterval = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnInterval), DisplayName = "生成间隔", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 1.0f });

    // 单波最大数
    public static readonly DataMeta SpawnMaxCountPerWave = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnMaxCountPerWave), DisplayName = "单波最大数", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = -1 });

    // 单次数量
    public static readonly DataMeta SingleSpawnCount = DataRegistry.Register(
        new DataMeta { Key = nameof(SingleSpawnCount), DisplayName = "单次数量", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 1 });

    // 数量波动
    public static readonly DataMeta SingleSpawnVariance = DataRegistry.Register(
        new DataMeta { Key = nameof(SingleSpawnVariance), DisplayName = "数量波动", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 0 });

    // 开始延迟
    public static readonly DataMeta SpawnStartDelay = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnStartDelay), DisplayName = "开始延迟", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 0f });

    // 生成权重
    public static readonly DataMeta SpawnWeight = DataRegistry.Register(
        new DataMeta { Key = nameof(SpawnWeight), DisplayName = "生成权重", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 10 });

    // 血条显示高度
    public static readonly DataMeta HealthBarHeight = DataRegistry.Register(
        new DataMeta { Key = nameof(HealthBarHeight), DisplayName = "血条显示高度", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 100f });

    // === Enemy ===
    // 击杀经验奖励
    public static readonly DataMeta ExpReward = DataRegistry.Register(
        new DataMeta { Key = nameof(ExpReward), DisplayName = "击杀经验奖励", Category = DataCategory_Base.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 0 });

    // === 状态标记 ===
    // 是否死亡
    public static readonly DataMeta IsDead = DataRegistry.Register(
        new DataMeta { Key = nameof(IsDead), DisplayName = "是否死亡", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });

    // 是否无敌
    public static readonly DataMeta IsInvulnerable = DataRegistry.Register(
        new DataMeta { Key = nameof(IsInvulnerable), DisplayName = "是否无敌", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });

    // 是否免疫
    public static readonly DataMeta IsImmune = DataRegistry.Register(
        new DataMeta { Key = nameof(IsImmune), DisplayName = "是否免疫", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });

    // 是否眩晕
    public static readonly DataMeta IsStunned = DataRegistry.Register(
        new DataMeta { Key = nameof(IsStunned), DisplayName = "是否眩晕", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });

    // 是否隐身
    public static readonly DataMeta IsInvisible = DataRegistry.Register(
        new DataMeta { Key = nameof(IsInvisible), DisplayName = "是否隐身", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });

    // 攻击状态
    public static readonly DataMeta AttackState = DataRegistry.Register(
        new DataMeta { Key = nameof(AttackState), DisplayName = "攻击状态", Category = DataCategory_Unit.State, Type = typeof(global::AttackState), DefaultValue = global::AttackState.Idle });

    // === 生命周期 ===
    // 生命周期状态
    public static readonly DataMeta LifecycleState = DataRegistry.Register(
        new DataMeta { Key = nameof(LifecycleState), DisplayName = "生命周期状态", Category = DataCategory_Unit.State, Type = typeof(LifecycleState), DefaultValue = global::LifecycleState.Alive });

    // 死亡类型
    public static readonly DataMeta DeathType = DataRegistry.Register(
        new DataMeta { Key = nameof(DeathType), DisplayName = "死亡类型", Category = DataCategory_Unit.State, Type = typeof(DeathType), DefaultValue = global::DeathType.Normal });

    // 是否可复活
    public static readonly DataMeta CanRevive = DataRegistry.Register(
        new DataMeta { Key = nameof(CanRevive), DisplayName = "是否可复活", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });

    // 死亡次数
    public static readonly DataMeta DeathCount = DataRegistry.Register(
        new DataMeta { Key = nameof(DeathCount), DisplayName = "死亡次数", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    // 最大生存时间
    public static readonly DataMeta MaxLifeTime = DataRegistry.Register(
        new DataMeta { Key = nameof(MaxLifeTime), DisplayName = "最大生存时间", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = -1f });

    // === FollowComponent ===
    // 跟随速度
    public static readonly DataMeta FollowSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(FollowSpeed), DisplayName = "跟随速度", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 100f });

    // 停止距离
    public static readonly DataMeta StopDistance = DataRegistry.Register(
        new DataMeta { Key = nameof(StopDistance), DisplayName = "停止距离", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 200f });

    // === HurtboxComponent ===
    // 无敌计时器
    public static readonly DataMeta InvincibilityTimer = DataRegistry.Register(
        new DataMeta { Key = nameof(InvincibilityTimer), DisplayName = "无敌计时器", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });

    // === 伤害统计（运行时，保留const） ===
    public const string TotalDamageTaken = "TotalDamageTaken";
    public const string WaveDamageTaken = "WaveDamageTaken";
    public const string TotalDamageDealt = "TotalDamageDealt";
    public const string WaveDamageDealt = "WaveDamageDealt";
    public const string HighestSingleDamage = "HighestSingleDamage";
    public const string TotalKills = "TotalKills";
    public const string WaveKills = "WaveKills";
    public const string TotalHits = "TotalHits";
    public const string WaveHits = "WaveHits";
    public const string TotalCriticalHits = "TotalCriticalHits";
    public const string WaveCriticalHits = "WaveCriticalHits";

    // === 其他运行时（保留const） ===
    public const string IsRecoverySystemRegistered = "IsRecoverySystemRegistered";
    public const string AvailableAnimations = "AvailableAnimations";
}
