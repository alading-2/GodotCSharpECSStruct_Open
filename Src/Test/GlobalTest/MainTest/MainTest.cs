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
        RegisterExecutors();
        ExecuteTestScenario();
        _log.Info("MainTest初始化完成");
    }

    private void RegisterExecutors()
    {
        // 注册 "单位目标" / "TargetStrike"
        AbilityExecutorRegistry.Register("单位目标", new TestAbilityExecutor((context) =>
        {
            _log.Info($"执行技能 [单位目标] -> 目标数量: {context.Targets?.Count ?? 0}");
            if (context.Targets != null)
            {
                foreach (var target in context.Targets)
                {
                    _log.Info($"  命中目标: {target.Data.Get<string>(DataKey.Name)}");

                    // 简单的伤害逻辑模拟
                    if (target is IUnit victim)
                    {
                        var damageInfo = new DamageInfo
                        {
                            Attacker = context.Caster as Node,
                            // Instigator 不存在于 DamageInfo，Attacker 即为直接来源
                            // 归属权由 EntityRelationshipManager 处理
                            Victim = victim,
                            BaseDamage = 10f,
                            Type = DamageType.Physical
                        };
                        DamageService.Instance.Process(damageInfo);
                    }
                }
            }
            return new AbilityExecutedResult { TargetsHit = context.Targets?.Count ?? 0 };
        }));

        AbilityExecutorRegistry.Register("TargetStrike", new TestAbilityExecutor((context) =>
        {
            _log.Info($"执行技能 [TargetStrike] -> 目标数量: {context.Targets?.Count ?? 0}");
            return new AbilityExecutedResult { TargetsHit = context.Targets?.Count ?? 0 };
        }));

        // 注册 "位置目标" / "GroundSlam"
        AbilityExecutorRegistry.Register("位置目标", new TestAbilityExecutor((context) =>
        {
            _log.Info($"执行技能 [地面猛击] -> 位置: {context.TargetPosition}");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }));

        AbilityExecutorRegistry.Register("GroundSlam", new TestAbilityExecutor((context) =>
        {
            _log.Info($"执行技能 [GroundSlam] -> 位置: {context.TargetPosition}");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }));
    }

    // 可以在 MainTest 内部定义简单的测试用执行器
    private class TestAbilityExecutor : IAbilityExecutor
    {
        private readonly Func<CastContext, AbilityExecutedResult> _action;

        public TestAbilityExecutor(Func<CastContext, AbilityExecutedResult> action)
        {
            _action = action;
        }

        public AbilityExecutedResult Execute(CastContext context)
        {
            return _action(context);
        }
    }

    private async void ExecuteTestScenario()
    {
        _log.Info("=== 开始测试: 主动技能输入系统 ===");
        _log.Info("操作说明:");
        _log.Info("  LB/RB - 切换技能");
        _log.Info("  X     - 释放技能");

        // 1. 生成玩家
        _log.Info("步骤 1: 生成玩家");
        var playerConfig = ResourceManagement.Load<Brotato.Data.Config.Units.PlayerConfig>(ResourcePaths.PlayerConfig.德鲁伊, ResourceCategory.PlayerConfig);
        _player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
        {
            Config = playerConfig,
            UsingObjectPool = false,
            Position = Vector2.Zero
        });

        _log.Info($"玩家生成成功: {_player.Name} at {_player.GlobalPosition}");

        // 1.5. 生成一个敌人用于测试单位目标选择
        _log.Info("步骤 1.5: 生成测试敌人");
        var enemyConfig = ResourceManagement.Load<Resource>("豺狼人", ResourceCategory.EnemyConfig);
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

        // 使用 EntityManager.AddAbility 从配置加载技能
        // ECS系统会自动处理组件添加、关系建立等

        // 技能1: TargetStrike (目标打击)
        var targetStrikeConfig = ResourceManagement.Load<Brotato.Data.Config.Abilities.AbilityConfig>(ResourcePaths.AbilityConfig.TargetEntitySkillConfig, ResourceCategory.AbilityConfig);
        if (targetStrikeConfig != null)
        {
            EntityManager.AddAbility(_player, targetStrikeConfig);
        }

        // 技能2: GroundSlam (地面猛击)
        var groundSlamConfig = ResourceManagement.Load<Brotato.Data.Config.Abilities.AbilityConfig>(ResourcePaths.AbilityConfig.TargetPointSkillConfig, ResourceCategory.AbilityConfig);
        if (groundSlamConfig != null)
        {
            EntityManager.AddAbility(_player, groundSlamConfig);
        }

        // 注释掉自动范围伤害技能（按需求）
        // var circleDamageConfig = ResourceManagement.Load<Brotato.Data.Config.Abilities.AbilityConfig>(ResourcePaths.AbilityConfig.CircleDamageConfig, ResourceCategory.AbilityConfig);
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
