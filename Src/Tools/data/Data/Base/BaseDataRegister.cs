
public static class BaseDataRegister
{
    public static void Register()
    {
        // === 基础信息 ===
        DataRegistry.Register(new DataMeta { Key = DataKey.Name, DisplayName = "名称", Description = "实体的名称", Category = DataCategory.Basic, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.Level, DisplayName = "等级", Description = "实体的等级", Category = DataCategory.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1, MaxValue = 999, SupportModifiers = false });
    }
}