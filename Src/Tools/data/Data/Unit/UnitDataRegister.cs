
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
            Path = "res://Src/Tools/Data/Data/Unit/UnitDataRegister.cs",
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
    }
}