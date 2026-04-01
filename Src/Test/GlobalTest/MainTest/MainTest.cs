using Godot;



public partial class MainTest : Node
{
    private static readonly Log _log = new Log("MainTest");
    private PlayerEntity? _player;
    private ActiveSkillBarUI? _skillBarUI;

    public override void _Ready()
    {
        GlobalEventBus.TriggerGameStart();
        ExecuteTestScenario();
        _log.Info("MainTest初始化完成");
    }

    private async void ExecuteTestScenario()
    {
        _log.Info("=== 开始测试: 主动技能输入系统 ===");
        _log.Info("操作说明:");
        _log.Info("  LB/RB - 切换技能");
        _log.Info("  X     - 释放技能");

        // 1. 生成玩家
        _log.Info("步骤 1: 生成玩家");
        var playerConfig = ResourceManagement.Load<Slime.Config.Units.PlayerConfig>(ResourcePaths.DataUnit_deluyi, ResourceCategory.DataUnit);
        _player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
        {
            Config = playerConfig,
            UsingObjectPool = false,
            Position = Vector2.Zero
        });

        _log.Info($"玩家生成成功: {_player.Name} at {_player.GlobalPosition}");

        // 1.5. 生成一个敌人用于测试单位目标选择
        _log.Info("步骤 1.5: 生成测试敌人");
        var enemyConfig = ResourceManagement.Load<Resource>(ResourcePaths.DataUnit_chailangren, ResourceCategory.DataUnit);
        var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
        {
            Config = enemyConfig,
            UsingObjectPool = false,
            Position = new Vector2(200, 200)
        });
        if (enemy != null)
        {
            _log.Info($"测试敌人生成成功: {enemy.Name} at {enemy.GlobalPosition}");
        }

        // 2. [已移除] 主动技能输入组件已由 PlayerEntity 自动添加，此处无需重复添加
        // var inputComponent = new ActiveSkillInputComponent();
        // EntityManager.AddComponent(_player, inputComponent);
        _log.Info("检查主动技能输入组件状态...");

        // 3. 创建技能栏UI
        CreateSkillBarUI();

        // 4. 添加主动技能
        AddManualSkills();

        _log.Info("测试场景初始化完成！");
    }

    private void CreateSkillBarUI()
    {
        if (_player == null) return;

        var uiScene = ResourceManagement.Load<PackedScene>(nameof(ActiveSkillBarUI), ResourceCategory.UI);
        if (uiScene == null)
        {
            _log.Error("无法加载 ActiveSkillBarUI.tscn");
            return;
        }

        _skillBarUI = uiScene.Instantiate<ActiveSkillBarUI>();

        // 添加到 UILayer 而不是 MainTest
        var uiLayer = GetNode<CanvasLayer>("UILayer");
        uiLayer.AddChild(_skillBarUI);

        _skillBarUI.Bind(_player);

        _log.Info("技能栏UI已创建并绑定");
    }

    private void AddManualSkills()
    {
        if (_player == null) return;

        // 加载正式技能配置，对应 Data/Data/Ability/Ability/ 中的执行器

        // 技能1: 裂地猛击 (Slam) - 范围近战AOE
        var slamConfig = ResourceManagement.Load<Slime.Config.Abilities.AbilityConfig>(
            ResourcePaths.DataAbility_SlamConfig, ResourceCategory.DataAbility);
        if (slamConfig != null) EntityManager.AddAbility(_player, slamConfig);

        // 技能2: 链式闪电 (ChainLightning) - 链式弹跳魔法
        var chainConfig = ResourceManagement.Load<Slime.Config.Abilities.AbilityConfig>(
            ResourcePaths.DataAbility_ChainLightningConfig, ResourceCategory.DataAbility);
        if (chainConfig != null) EntityManager.AddAbility(_player, chainConfig);

        // 技能3: 烈焰光环 (CircleDamage) - 周期范围伤害
        var auraConfig = ResourceManagement.Load<Slime.Config.Abilities.AbilityConfig>(
            ResourcePaths.DataAbility_CircleDamageConfig, ResourceCategory.DataAbility);
        if (auraConfig != null) EntityManager.AddAbility(_player, auraConfig);

        // 技能4: 冲刺 (Dash) - 位移技能
        var dashConfig = ResourceManagement.Load<Slime.Config.Abilities.AbilityConfig>(
            ResourcePaths.DataAbility_DashConfig, ResourceCategory.DataAbility);
        if (dashConfig != null) EntityManager.AddAbility(_player, dashConfig);

        _log.Info("已添加正式技能，等待UI自动更新");
    }

    public override void _Process(double delta)
    {
        // 按Y键显示调试信息
        if (_player != null && Input.IsActionJustPressed("BtnY"))
        {
            PrintDebugInfo();
        }
    }

    private void PrintDebugInfo()
    {
        if (_player == null) return;

        _log.Info("--- 调试信息 ---");
        _log.Info($"当前魔法: {_player.Data.Get<float>(DataKey.CurrentMana):F1}");
        _log.Info($"当前技能索引: {_player.Data.Get<int>(DataKey.CurrentActiveAbilityIndex)}");

        var abilities = EntityManager.GetAbilities(_player);
        foreach (var ability in abilities)
        {
            var name = ability.Data.Get<string>(DataKey.Name);
            var charges = ability.Data.Get<int>(DataKey.AbilityCurrentCharges);
            var maxCharges = ability.Data.Get<int>(DataKey.AbilityMaxCharges);
            var usesCharges = ability.Data.Get<bool>(DataKey.IsAbilityUsesCharges);

            if (usesCharges)
            {
                _log.Info($"  {name}: 充能 {charges}/{maxCharges}");
            }
            else
            {
                _log.Info($"  {name}: 冷却技能");
            }
        }
    }
}
