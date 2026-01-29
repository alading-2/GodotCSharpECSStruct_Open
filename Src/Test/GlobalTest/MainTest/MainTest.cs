using Godot;
using System;
using System.Threading.Tasks;



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
        var playerConfig = ResourceManagement.Load<Brotato.Data.Config.Units.PlayerConfig>("德鲁伊", ResourceCategory.PlayerConfig);
        _player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
        {
            Config = playerConfig,
            UsingObjectPool = false,
            Position = Vector2.Zero
        });

        if (_player == null)
        {
            _log.Error("测试失败: 无法生成玩家");
            return;
        }

        _log.Info($"玩家生成成功: {_player.Name} at {_player.GlobalPosition}");

        // 2. 添加主动技能输入组件
        var inputComponent = new ActiveSkillInputComponent();
        EntityManager.AddComponent(_player, inputComponent);
        _log.Info("已添加主动技能输入组件");

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

        // 使用 EntityManager.AddAbility 从配置加载技能
        // ECS系统会自动处理组件添加、关系建立等

        // 技能1: TargetStrike (目标打击)
        var targetStrikeConfig = ResourceManagement.Load<Brotato.Data.Config.Abilities.AbilityConfig>("TargetEntitySkillConfig", ResourceCategory.AbilityConfig);
        if (targetStrikeConfig != null)
        {
            EntityManager.AddAbility(_player, targetStrikeConfig);
        }

        // 技能2: GroundSlam (地面猛击)
        var groundSlamConfig = ResourceManagement.Load<Brotato.Data.Config.Abilities.AbilityConfig>("TargetPointSkillConfig", ResourceCategory.AbilityConfig);
        if (groundSlamConfig != null)
        {
            EntityManager.AddAbility(_player, groundSlamConfig);
        }

        // 注释掉自动范围伤害技能（按需求）
        // var circleDamageConfig = ResourceManagement.Load<Brotato.Data.Config.Abilities.AbilityConfig>("CircleDamageConfig", ResourceCategory.AbilityConfig);
        // if (circleDamageConfig != null)
        // {
        //     EntityManager.AddAbility(_player, circleDamageConfig);
        // }

        _log.Info("已添加主动技能，等待UI自动更新");
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
