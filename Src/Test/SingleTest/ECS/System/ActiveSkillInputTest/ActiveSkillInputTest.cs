using Godot;
using System.Collections.Generic;

namespace Slime.Test.ActiveSkillInputTest
{
    /// <summary>
    /// 主动技能输入系统测试
    /// 测试内容：
    /// 1. 技能切换 (LB/RB)
    /// 2. 技能释放 (X)
    /// 3. UI 显示更新
    /// </summary>
    public partial class ActiveSkillInputTest : Node
    {
        private static readonly Log _log = new Log("ActiveSkillInputTest");

        private TestPlayerEntity? _player;
        private ActiveSkillSlotUI? _skillUI;

        public override void _Ready()
        {
            _log.Info("========================================");
            _log.Info("主动技能输入系统测试");
            _log.Info("========================================");
            _log.Info("操作说明:");
            _log.Info("  LB/RB - 切换技能");
            _log.Info("  X     - 释放技能");
            _log.Info("========================================");

            // 创建测试玩家
            CreateTestPlayer();

            // 添加主动技能
            AddTestAbilities();

            // 创建技能UI
            CreateSkillUI();

            _log.Success("测试场景初始化完成！");
        }

        private void CreateTestPlayer()
        {
            _player = new TestPlayerEntity();
            _player.Name = "TestPlayer";
            _player.Position = new Vector2(400, 300);

            // 使用节点实例ID作为 DataKey.Id，与 NodeLifecycleManager 一致
            var instanceId = _player.GetInstanceId().ToString();
            _player.Data.Set(DataKey.Id, instanceId);

            // 注册到 EntityManager
            EntityManager.Register(_player);

            AddChild(_player);

            // 初始化玩家数据
            _player.Data.Set(DataKey.BaseHp, 100f);
            _player.Data.Set(DataKey.CurrentHp, 100f);
            _player.Data.Set(DataKey.FinalHp, 100f);
            _player.Data.Set(DataKey.CurrentMana, 100f);
            _player.Data.Set(DataKey.FinalMana, 100f);
            _player.Data.Set(DataKey.CurrentActiveAbilityIndex, 0);

            // 添加主动技能输入组件
            var inputComponent = new ActiveSkillInputComponent();
            EntityManager.AddComponent(_player, inputComponent);

            _log.Info($"创建测试玩家: {_player.Name}");
        }

        private void AddTestAbilities()
        {
            if (_player == null) return;

            // 技能1: Dash (充能技能)
            var dashAbility = CreateAbility("Dash", new Dictionary<string, object>
            {
                { DataKey.Name, "Dash" },
                { DataKey.Description, "向前冲刺" },
                { DataKey.EntityType, (int)EntityType.Ability },
                { DataKey.AbilityType, (int)AbilityType.Active },
                { DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual },
                { DataKey.IsAbilityUsesCharges, true },
                { DataKey.AbilityMaxCharges, 3 },
                { DataKey.AbilityCurrentCharges, 3 },
                { DataKey.AbilityChargeTime, 5f },
                { DataKey.AbilityCooldown, 0.5f },
                { DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.None },
                { DataKey.FeatureEnabled, true }
            });
            AddChild(dashAbility);

            // 手动注册技能关系
            EntityRelationshipManager.AddRelationship(
                _player.Data.Get<string>(DataKey.Id),
                dashAbility.Data.Get<string>(DataKey.Id),
                EntityRelationshipType.ENTITY_TO_ABILITY
            );

            // 技能2: Slam (消耗魔法)
            var slamAbility = CreateAbility("Slam", new Dictionary<string, object>
            {
                { DataKey.Name, "Slam" },
                { DataKey.Description, "猛击地面" },
                { DataKey.EntityType, (int)EntityType.Ability },
                { DataKey.AbilityType, (int)AbilityType.Active },
                { DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual },
                { DataKey.AbilityCostType, (int)AbilityCostType.Mana },
                { DataKey.AbilityCostAmount, 20f },
                { DataKey.AbilityCooldown, 3f },
                { DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.None },
                { DataKey.FeatureEnabled, true }
            });
            AddChild(slamAbility);

            // 手动注册技能关系
            EntityRelationshipManager.AddRelationship(
                _player.Data.Get<string>(DataKey.Id),
                slamAbility.Data.Get<string>(DataKey.Id),
                EntityRelationshipType.ENTITY_TO_ABILITY
            );

            // 技能3: ChainLightning (消耗魔法+冷却)
            var lightningAbility = CreateAbility("ChainLightning", new Dictionary<string, object>
            {
                { DataKey.Name, "ChainLightning" },
                { DataKey.Description, "链式闪电" },
                { DataKey.EntityType, (int)EntityType.Ability },
                { DataKey.AbilityType, (int)AbilityType.Active },
                { DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual },
                { DataKey.AbilityCostType, (int)AbilityCostType.Mana },
                { DataKey.AbilityCostAmount, 50f },
                { DataKey.AbilityCooldown, 6f },
                { DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.None },
                { DataKey.FeatureEnabled, true }
            });
            AddChild(lightningAbility);

            // 手动注册技能关系
            EntityRelationshipManager.AddRelationship(
                _player.Data.Get<string>(DataKey.Id),
                lightningAbility.Data.Get<string>(DataKey.Id),
                EntityRelationshipType.ENTITY_TO_ABILITY
            );

            _log.Info($"添加了 3 个主动技能: Dash, Slam, ChainLightning");
        }

        private AbilityEntity CreateAbility(string name, Dictionary<string, object> data)
        {
            var ability = new AbilityEntity();
            ability.Name = name;

            // 设置数据
            foreach (var kvp in data)
            {
                ability.Data.Set(kvp.Key, kvp.Value);
            }

            // 生成唯一ID - 使用节点实例ID确保与 NodeLifecycleManager 一致
            var instanceId = ability.GetInstanceId().ToString();
            ability.Data.Set(DataKey.Id, instanceId);

            // 注册到 EntityManager，使 GetEntityById 能找到
            EntityManager.Register(ability);

            // 添加必要组件
            var cooldownComponent = new CooldownComponent();
            EntityManager.AddComponent(ability, cooldownComponent);

            var chargeComponent = new ChargeComponent();
            EntityManager.AddComponent(ability, chargeComponent);

            var costComponent = new CostComponent();
            EntityManager.AddComponent(ability, costComponent);

            var triggerComponent = new TriggerComponent();
            EntityManager.AddComponent(ability, triggerComponent);

            return ability;
        }

        private void CreateSkillUI()
        {
            if (_player == null) return;

            // 加载并实例化UI场景
            var uiScene = ResourceManagement.Load<PackedScene>(nameof(ActiveSkillSlotUI), ResourceCategory.UI);
            if (uiScene == null)
            {
                _log.Error("无法加载 ActiveSkillSlotUI.tscn");
                return;
            }

            _skillUI = uiScene.Instantiate<ActiveSkillSlotUI>();
            _skillUI.Position = new Vector2(50, 50);
            AddChild(_skillUI);

            // 绑定到玩家
            _skillUI.Bind(_player);

            _log.Info("创建并绑定技能UI");
        }

        public override void _Process(double delta)
        {
            // 显示当前状态 (调试用)
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

        // 测试用玩家实体
        private partial class TestPlayerEntity : Node2D, IUnit, IEntity
        {
            public Data Data { get; } = new Data();
            public EventBus Events { get; } = new EventBus();
            public int FactionId { get; set; } = 0;

            public override void _Ready()
            {
                // ID 已在 CreateTestPlayer 中设置
            }
        }
    }
}
