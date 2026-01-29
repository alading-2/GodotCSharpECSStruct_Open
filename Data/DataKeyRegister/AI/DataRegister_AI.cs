
using Godot;
using System.Runtime.CompilerServices;

public static class DataRegister_AI
{
    private static readonly Log _log = new Log("DataRegister_AI");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(DataRegister_AI),
            InitAction = Init,
            Priority = AutoLoad.Priority.Core
        });
    }

    public static void Init()
    {
        _log.Info("注册AI数据...");
        // === AI ===
        DataRegistry.Register(new DataMeta { Key = DataKey.AIState, DisplayName = "AI状态", Description = "Idle/Chasing/Attacking/Fleeing", Category = DataCategory_AI.Basic, Type = typeof(int), DefaultValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.Threat, DisplayName = "威胁值", Description = "仇恨值", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 0f });
        DataRegistry.Register(new DataMeta { Key = DataKey.Target, DisplayName = "当前目标", Description = "目标的EntityId", Category = DataCategory_AI.Combat, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.DetectionRange, DisplayName = "索敌范围", Description = "AI索敌半径", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 500f });
        DataRegistry.Register(new DataMeta { Key = DataKey.AttackRange, DisplayName = "攻击范围", Description = "AI攻击半径", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 100f });
    }
}
