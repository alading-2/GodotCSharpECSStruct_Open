
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

        // ========== AI 行为状态 ==========
        DataRegistry.Register(new DataMeta { Key = DataKey.AIState, DisplayName = "AI状态", Description = "Idle/Chasing/Attacking/Patrolling/Fleeing", Category = DataCategory_AI.Basic, Type = typeof(AIState), DefaultValue = AIState.Idle });
        DataRegistry.Register(new DataMeta { Key = DataKey.Threat, DisplayName = "威胁值", Description = "仇恨值", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 0f });
        DataRegistry.Register(new DataMeta { Key = DataKey.AIEnabled, DisplayName = "AI是否启用", Description = "可用于暂停 AI 逻辑", Category = DataCategory_AI.Basic, Type = typeof(bool), DefaultValue = false });

        // ========== AI 感知参数 ==========
        DataRegistry.Register(new DataMeta { Key = DataKey.DetectionRange, DisplayName = "索敌范围", Description = "圆形检测半径", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 500f });
        DataRegistry.Register(new DataMeta { Key = DataKey.LoseTargetRange, DisplayName = "丢失目标范围", Description = "超出此范围后放弃追逐", Category = DataCategory_AI.Combat, Type = typeof(float), DefaultValue = 800f });

        // ========== AI 移动参数 ==========
        DataRegistry.Register(new DataMeta { Key = DataKey.PatrolRadius, DisplayName = "巡逻半径", Description = "以出生点为中心的随机巡逻范围", Category = DataCategory_AI.Basic, Type = typeof(float), DefaultValue = 500f });
        DataRegistry.Register(new DataMeta { Key = DataKey.PatrolWaitTime, DisplayName = "巡逻等待时间", Description = "到达巡逻点后等待多久再移动", Category = DataCategory_AI.Basic, Type = typeof(float), DefaultValue = 2f });

        // ========== AI 黑板数据 ==========
        DataRegistry.Register(new DataMeta { Key = DataKey.SpawnPosition, DisplayName = "出生位置", Description = "用于巡逻计算基准点", Category = DataCategory_AI.Basic, Type = typeof(Vector2), DefaultValue = Vector2.Zero });
        DataRegistry.Register(new DataMeta { Key = DataKey.PatrolTargetPoint, DisplayName = "巡逻目标点", Description = "当前巡逻目标点", Category = DataCategory_AI.Basic, Type = typeof(Vector2), DefaultValue = Vector2.Zero });
        DataRegistry.Register(new DataMeta { Key = DataKey.PatrolWaitDone, DisplayName = "巡逻等待完成", Description = "TimerManager回调写入的完成标记", Category = DataCategory_AI.Basic, Type = typeof(bool), DefaultValue = false });
        // ========== AI 移动意图 ==========
        DataRegistry.Register(new DataMeta { Key = DataKey.AIMoveDirection, DisplayName = "AI请求移动方向", Description = "请求的移动方向（归一化），Zero表示停止", Category = DataCategory_AI.Basic, Type = typeof(Vector2), DefaultValue = Vector2.Zero });
        DataRegistry.Register(new DataMeta { Key = DataKey.AIMoveSpeedMultiplier, DisplayName = "AI移动速度倍率", Description = "请求的移动速度倍率（默认1.0）", Category = DataCategory_AI.Basic, Type = typeof(float), DefaultValue = 1.0f });
    }
}
