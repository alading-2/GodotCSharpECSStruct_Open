
using Godot;
using System.Runtime.CompilerServices;

public partial class DataRegister_Unit : Node
{
    private static readonly Log _log = new Log("DataRegister_Unit");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "DataRegister_Unit",
            Path = "res://Data/DataKeyRegister/Unit/DataRegister_Unit.cs",
            Priority = AutoLoad.Priority.Core,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("注册Unit数据...");
        // 等级
        DataRegistry.Register(new DataMeta { Key = DataKey.Level, DisplayName = "等级", Description = "实体的等级", Category = DataCategory_Base.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1, MaxValue = GlobalConfig.Maxlevel, SupportModifiers = false });
        // 单位品阶UnitRank
        DataRegistry.Register(new DataMeta { Key = DataKey.UnitRank, DisplayName = "单位品阶", Description = "单位品阶", Category = DataCategory_Base.Basic, Type = typeof(UnitRank), DefaultValue = UnitRank.Normal });

        // DisableHealthRecovery
        DataRegistry.Register(new DataMeta { Key = DataKey.IsDisableHealthRecovery, DisplayName = "是否禁止生命恢复", Description = "是否禁止生命恢复", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false });
        // DisableManaRecovery
        DataRegistry.Register(new DataMeta { Key = DataKey.IsDisableManaRecovery, DisplayName = "是否禁止魔法恢复", Description = "是否禁止魔法恢复", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false });


        // ================ Spawn ================
        // 是否启用SpawnRule
        DataRegistry.Register(new DataMeta { Key = DataKey.IsEnableSpawnRule, DisplayName = "是否启用SpawnRule", Category = DataCategory_Unit.Spawn, Type = typeof(bool), DefaultValue = false });
        // SpawnStrategy
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnStrategy, DisplayName = "生成策略", Category = DataCategory_Unit.Spawn, Type = typeof(SpawnPositionStrategy), DefaultValue = SpawnPositionStrategy.Rectangle });
        // SpawnMinWave
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnMinWave, DisplayName = "最小波次", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 0 });
        // SpawnMaxWave
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnMaxWave, DisplayName = "最大波次", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = -1 });
        // SpawnInterval
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnInterval, DisplayName = "生成间隔", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 1.0f });
        // SpawnMaxCountPerWave
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnMaxCountPerWave, DisplayName = "单波最大数", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = -1 });
        // SingleSpawnCount
        DataRegistry.Register(new DataMeta { Key = DataKey.SingleSpawnCount, DisplayName = "单次数量", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 1 });
        // SingleSpawnVariance
        DataRegistry.Register(new DataMeta { Key = DataKey.SingleSpawnVariance, DisplayName = "数量波动", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 0 });
        // SpawnStartDelay
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnStartDelay, DisplayName = "开始延迟", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 0f });
        // SpawnWeight
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnWeight, DisplayName = "生成权重", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 10 });

        // ================ 状态标记 ================
        // 是否死亡
        DataRegistry.Register(new DataMeta { Key = DataKey.IsDead, DisplayName = "是否死亡", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });
        // 是否无敌
        DataRegistry.Register(new DataMeta { Key = DataKey.IsInvulnerable, DisplayName = "是否无敌", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });
        // 是否免疫
        DataRegistry.Register(new DataMeta { Key = DataKey.IsImmune, DisplayName = "是否免疫", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });
        // 是否眩晕
        DataRegistry.Register(new DataMeta { Key = DataKey.IsStunned, DisplayName = "是否眩晕", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });
        // 是否隐身
        DataRegistry.Register(new DataMeta { Key = DataKey.IsInvisible, DisplayName = "是否隐身", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });
        // ================ LifecycleComponent ================
        // 生命周期状态
        DataRegistry.Register(new DataMeta { Key = DataKey.LifecycleState, DisplayName = "生命周期状态", Category = DataCategory_Unit.State, Type = typeof(LifecycleState), DefaultValue = LifecycleState.Alive });
        // 死亡类型
        DataRegistry.Register(new DataMeta { Key = DataKey.DeathType, DisplayName = "死亡类型", Category = DataCategory_Unit.State, Type = typeof(DeathType), DefaultValue = DeathType.Normal });
        // 是否可复活
        DataRegistry.Register(new DataMeta { Key = DataKey.CanRevive, DisplayName = "是否可复活", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false });
        // 死亡次数
        DataRegistry.Register(new DataMeta { Key = DataKey.DeathCount, DisplayName = "死亡次数", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });
        // 最大生存时间
        DataRegistry.Register(new DataMeta { Key = DataKey.MaxLifeTime, DisplayName = "最大生存时间", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = -1f });

        // ================ FollowComponent ================
        // 跟随速度
        DataRegistry.Register(new DataMeta { Key = DataKey.FollowSpeed, DisplayName = "跟随速度", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 100f });
        // 停止距离
        DataRegistry.Register(new DataMeta { Key = DataKey.StopDistance, DisplayName = "停止距离", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 200f });
        // ================ VelocityComponent ================
        // 当前速度向量
        DataRegistry.Register(new DataMeta { Key = DataKey.Velocity, DisplayName = "当前速度向量", Category = DataCategory_Unit.Movement, Type = typeof(Vector2), DefaultValue = Vector2.Zero });
        // 加速度
        DataRegistry.Register(new DataMeta { Key = DataKey.Acceleration, DisplayName = "加速度", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 10f });
        // ================ HurtboxComponent ================
        // 无敌计时器
        DataRegistry.Register(new DataMeta { Key = DataKey.InvincibilityTimer, DisplayName = "无敌计时器", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });
    }
}