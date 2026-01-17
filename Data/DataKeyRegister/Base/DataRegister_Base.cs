
using Godot;
using System.Runtime.CompilerServices;

public partial class DataRegister_Base : Node
{
    private static readonly Log _log = new Log("DataRegister_Base");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "DataRegister_Base",
            Path = "res://Data/DataKeyRegister/Base/DataRegister_Base.cs",
            Priority = AutoLoad.Priority.Core,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("注册基础数据...");
        // === 基础信息 ===
        // 名称
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Name,
            DisplayName = "名称",
            Description = "名称",
            Category = DataCategory_Base.Basic,
            Type = typeof(string),
            DefaultValue = ""
        });
        // 等级
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Level,
            DisplayName = "等级",
            Description = "实体的等级",
            Category = DataCategory_Base.Basic,
            Type = typeof(int),
            DefaultValue = 1,
            MinValue = 1,
            MaxValue = GlobalConfig.Maxlevel,
            SupportModifiers = false
        });
    }
}