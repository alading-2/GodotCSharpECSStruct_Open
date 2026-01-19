using Godot;
using System;

public partial class MainTest : Node
{
    private static readonly Log _log = new Log("MainTest");
    public override void _Ready()
    {
        GlobalEventBus.TriggerGameStart();
        test();
        _log.Info("MainTest初始化完成");
    }

    private void test()
    {
        var playerConfig = PlayerData.Configs["Player1"];
        var player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
        {
            Config = playerConfig,
            UsingObjectPool = false,
            Position = Vector2.Zero
        });

        if (player != null)
        {
            _log.Info($"MainTest: 成功生成玩家 {player.Name}, 位置: {player.GlobalPosition}");
        }
        else
        {
            _log.Error("MainTest: 生成玩家失败");
        }
    }
    public override void _Process(double delta)
    {

    }
}
