
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
        DataRegistry.Register(new DataMeta { Key = DataKey.AIState, DisplayName = "AI状态", Description = "Idle/Chasing/Attacking/Patrolling/Fleeing", Category = DataCategory_AI.Basic, Type = typeof(AIState), DefaultValue = AIState.Idle });
        DataRegistry.Register(new DataMeta { Key = DataKey.Threat, DisplayName = "威胁值", Description = "仇恨值", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 0f });
        DataRegistry.Register(new DataMeta { Key = DataKey.DetectionRange, DisplayName = "索敌范围", Description = "AI索敌半径", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 500f });
    }
}
