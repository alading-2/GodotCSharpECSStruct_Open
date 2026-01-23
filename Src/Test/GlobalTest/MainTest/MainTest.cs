using Godot;
using System;
using System.Threading.Tasks;

public partial class MainTest : Node
{
    private static readonly Log _log = new Log("MainTest");

    public override void _Ready()
    {
        GlobalEventBus.TriggerGameStart();
        ExecuteTestScenario();
        _log.Info("MainTest初始化完成");
    }

    private async void ExecuteTestScenario()
    {
        _log.Info("=== 开始测试: 烈焰光环伤害测试 ===");

        // 1. 生成玩家
        _log.Info("步骤 1: 生成玩家");
        var playerConfig = PlayerData.Configs["Player1"];
        var player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
        {
            Config = playerConfig,
            UsingObjectPool = false,
            Position = Vector2.Zero
        });

        if (player == null)
        {
            _log.Error("测试失败: 无法生成玩家");
            return;
        }

        _log.Info($"玩家生成成功: {player.Name} at {player.GlobalPosition}");

        // 添加技能
        var abilityConfig = AbilityData.Configs["CircleDamage"];
        var ability = EntityManager.AddAbility(player, abilityConfig);

        if (ability == null)
        {
            _log.Error("测试失败: 无法添加技能");
            return;
        }
        _log.Info("技能添加成功，等待触发...");
    }

    public override void _Process(double delta)
    {

    }
}
