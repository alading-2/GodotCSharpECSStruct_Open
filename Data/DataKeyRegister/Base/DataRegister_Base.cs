
using Godot;
using System.Runtime.CompilerServices;

public static class DataRegister_Base
{
    private static readonly Log _log = new Log("DataRegister_Base");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(DataRegister_Base),
            InitAction = Init,
            Priority = AutoLoad.Priority.Core
        });
    }

    public static void Init()
    {
        _log.Info("注册基础数据...");
        // === 基础信息 ===
        DataRegistry.Register(new DataMeta { Key = DataKey.Name, DisplayName = "名称", Description = "名称", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });
        // 描述
        DataRegistry.Register(new DataMeta { Key = DataKey.Description, DisplayName = "描述", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });
        // ID
        DataRegistry.Register(new DataMeta { Key = DataKey.Id, DisplayName = "ID", Description = "唯一标识符", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });
        // 阵营
        DataRegistry.Register(new DataMeta { Key = DataKey.Team, DisplayName = "阵营", Description = "0:Neutral, 1:Player, 2:Enemy", Category = DataCategory_Base.Basic, Type = typeof(Team), DefaultValue = Team.Neutral });
        // 实体类型
        DataRegistry.Register(new DataMeta { Key = DataKey.EntityType, DisplayName = "实体类型", Description = "Unit/Projectile/Structure/Item...", Category = DataCategory_Base.Basic, Type = typeof(EntityType), DefaultValue = EntityType.None });
    }
}