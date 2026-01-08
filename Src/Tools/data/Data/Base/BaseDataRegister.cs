
using Godot;
using System.Runtime.CompilerServices;

public partial class BaseDataRegister : Node
{
    private static readonly Log _log = new Log("BaseDataRegister");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "BaseDataRegister",
            Path = "res://Src/Tools/Data/Data/Base/BaseDataRegister.cs",
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
            Description = "实体的名称",
            Category = DataCategory.Basic,
            Type = typeof(string),
            DefaultValue = ""
        });
        // 等级
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Level,
            DisplayName = "等级",
            Description = "实体的等级",
            Category = DataCategory.Basic,
            Type = typeof(int),
            DefaultValue = 1,
            MinValue = 1,
            MaxValue = Config.Maxlevel,
            SupportModifiers = false
        });
    }
}