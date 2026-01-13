
using Godot;
using System.Runtime.CompilerServices;

public partial class UnitDataRegister : Node
{
    private static readonly Log _log = new Log("UnitDataRegister");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "UnitDataRegister",
            Path = "res://Data/DataKeyRegister/Unit/UnitDataRegister.cs",
            Priority = AutoLoad.Priority.Core,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("注册Unit数据...");

        // DisableHealthRecovery
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.IsDisableHealthRecovery,
            DisplayName = "是否禁止生命恢复",
            Description = "是否禁止生命恢复",
            Category = UnitCategory.Recovery,
            Type = typeof(bool),
            DefaultValue = false
        });

        // DisableManaRecovery
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.IsDisableManaRecovery,
            DisplayName = "是否禁止魔法恢复",
            Description = "是否禁止魔法恢复",
            Category = UnitCategory.Recovery,
            Type = typeof(bool),
            DefaultValue = false
        });

        // === 状态标记 ===
        // 是否死亡
        DataRegistry.Register(new DataMeta { Key = DataKey.IsDead, DisplayName = "是否死亡", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // 是否无敌
        DataRegistry.Register(new DataMeta { Key = DataKey.IsInvulnerable, DisplayName = "是否无敌", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // 是否免疫
        DataRegistry.Register(new DataMeta { Key = DataKey.IsImmune, DisplayName = "是否免疫", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // 是否眩晕
        DataRegistry.Register(new DataMeta { Key = DataKey.IsStunned, DisplayName = "是否眩晕", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // 是否沉默
        DataRegistry.Register(new DataMeta { Key = DataKey.IsSilenced, DisplayName = "是否沉默", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // 是否隐身
        DataRegistry.Register(new DataMeta { Key = DataKey.IsInvisible, DisplayName = "是否隐身", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // === LifecycleComponent ===
        // 生命周期状态
        DataRegistry.Register(new DataMeta { Key = DataKey.LifecycleState, DisplayName = "生命周期状态", Category = UnitCategory.State, Type = typeof(LifecycleState), DefaultValue = LifecycleState.Alive });
        // 死亡类型
        DataRegistry.Register(new DataMeta { Key = DataKey.DeathType, DisplayName = "死亡类型", Category = UnitCategory.State, Type = typeof(DeathType), DefaultValue = DeathType.Normal });
        // 是否可复活
        DataRegistry.Register(new DataMeta { Key = DataKey.CanRevive, DisplayName = "是否可复活", Category = UnitCategory.State, Type = typeof(bool), DefaultValue = false });
        // 死亡次数
        DataRegistry.Register(new DataMeta { Key = DataKey.DeathCount, DisplayName = "死亡次数", Category = UnitCategory.State, Type = typeof(int), DefaultValue = 0 });
        // 最大生存时间
        DataRegistry.Register(new DataMeta { Key = DataKey.MaxLifeTime, DisplayName = "最大生存时间", Category = UnitCategory.State, Type = typeof(float), DefaultValue = -1f });
        // === VelocityComponent ===
        // 当前速度向量
        DataRegistry.Register(new DataMeta { Key = DataKey.Velocity, DisplayName = "当前速度向量", Category = UnitCategory.Movement, Type = typeof(Vector2), DefaultValue = Vector2.Zero });
        // === HurtboxComponent ===
        // 无敌计时器
        DataRegistry.Register(new DataMeta { Key = DataKey.InvincibilityTimer, DisplayName = "无敌计时器", Category = UnitCategory.State, Type = typeof(float), DefaultValue = 0f });
    }
}