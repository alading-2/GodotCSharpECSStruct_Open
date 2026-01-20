
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
        DataRegistry.Register(new DataMeta { Key = DataKey.Name, DisplayName = "名称", Description = "名称", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.Level, DisplayName = "等级", Description = "实体的等级", Category = DataCategory_Base.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1, MaxValue = GlobalConfig.Maxlevel, SupportModifiers = false });
        DataRegistry.Register(new DataMeta { Key = DataKey.Id, DisplayName = "ID", Description = "唯一标识符", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.Team, DisplayName = "阵营", Description = "0:Neutral, 1:Player, 2:Enemy", Category = DataCategory_Base.Basic, Type = typeof(Team), DefaultValue = Team.Neutral });
        DataRegistry.Register(new DataMeta { Key = DataKey.EntityType, DisplayName = "实体类型", Description = "Unit/Projectile/Structure/Item...", Category = DataCategory_Base.Basic, Type = typeof(EntityType), DefaultValue = EntityType.None });
    }
}