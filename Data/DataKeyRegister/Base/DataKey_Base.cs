using System.Runtime.CompilerServices;

/// <summary>
/// 数据键定义 - 基础域
/// </summary>
public static partial class DataKey
{
    [ModuleInitializer]
    internal static void EnsureDataKeyInit()
    {
        _ = Name;
    }

    // 名称
    public static readonly DataMeta Name = DataRegistry.Register(
        new DataMeta { Key = nameof(Name), DisplayName = "名称", Description = "名称", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });

    // 描述
    public static readonly DataMeta Description = DataRegistry.Register(
        new DataMeta { Key = nameof(Description), DisplayName = "描述", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });

    // ID
    public static readonly DataMeta Id = DataRegistry.Register(
        new DataMeta { Key = nameof(Id), DisplayName = "ID", Description = "唯一标识符", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = "" });

    // 阵营
    public static readonly DataMeta Team = DataRegistry.Register(
        new DataMeta { Key = nameof(Team), DisplayName = "阵营", Description = "0:Neutral, 1:Player, 2:Enemy", Category = DataCategory_Base.Basic, Type = typeof(global::Team), DefaultValue = global::Team.Neutral });

    // 实体类型
    public static readonly DataMeta EntityType = DataRegistry.Register(
        new DataMeta { Key = nameof(EntityType), DisplayName = "实体类型", Description = "实体类型", Category = DataCategory_Base.Basic, Type = typeof(global::EntityType), DefaultValue = global::EntityType.None });
}
