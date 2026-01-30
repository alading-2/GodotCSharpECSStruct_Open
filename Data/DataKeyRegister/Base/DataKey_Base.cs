/// <summary>
/// 数据键定义 - 类型安全的数据访问
/// 使用常量而非枚举，支持 Mod 扩展
/// </summary>
public static partial class DataKey
{
    // === 基础信息 ===
    public const string Name = "Name"; // 名称
    /// <summary>描述</summary>
    public const string Description = "Description";
    public const string Id = "Id"; // ID
    public const string Team = "Team"; // 阵营 (Enum: Team)
    public const string EntityType = "EntityType"; // 实体类型 (Enum: EntityType)
}
