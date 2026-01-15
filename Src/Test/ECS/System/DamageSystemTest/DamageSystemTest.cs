using Godot;
using System;
using BrotatoMy.Test;

namespace BrotatoMy.Test.DamageSystemTest
{
    public partial class DamageSystemTest : Node
    {
        private static readonly Log _log = new Log("DamageSystemTest");

        public override void _Ready()
        {
            // 确保 DamageService 存在
            if (DamageService.Instance == null)
            {
                var ds = new DamageService();
                AddChild(ds);
                // Force _EnterTree if not automatically called (AddChild calls it)
            }

            _log.Info("开始伤害系统测试...");
            // 延迟一帧执行，确保 DamageService 初始化完成
            GetTree().CreateTimer(0.1).Timeout += RunTests;
        }

        public void RunTests()
        {
            TestDodgeLogic();
            TestSimulationMode();
            _log.Success("所有测试完成！");
        }

        private void TestDodgeLogic()
        {
            _log.Info("Test 1: 闪避逻辑测试");

            // 1. 创建受击者 (高闪避)
            var victim = CreateDummyUnit("Victim_Dodger");
            victim.Data.Set(DataKey.DodgeChance, 100f); // 100% 闪避

            // 2. 创建用于触发伤害的 Mock 攻击者（必须是一个 IEntity）
            var attacker = CreateDummyUnit("Attacker");

            // 3. 测试物理伤害 (应被闪避)
            var infoPhysical = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                BaseDamage = 10,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoPhysical);

            if (infoPhysical.IsDodged && infoPhysical.FinalDamage == 0)
            {
                _log.Success("  PASS: 物理伤害成功被闪避");
            }
            else
            {
                _log.Error($"  FAIL: 物理伤害未被闪避. FinalDamage: {infoPhysical.FinalDamage}, IsDodged: {infoPhysical.IsDodged}");
            }

            // 4. 测试真实伤害 (应无视闪避)
            var infoTrue = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                BaseDamage = 10,
                Type = DamageType.True
            };
            DamageService.Instance.Process(infoTrue);

            if (!infoTrue.IsDodged && infoTrue.FinalDamage > 0)
            {
                _log.Success("  PASS: 真实伤害未被闪避");
            }
            else
            {
                _log.Error($"  FAIL: 真实伤害被错误闪避. FinalDamage: {infoTrue.FinalDamage}, IsDodged: {infoTrue.IsDodged}");
            }

            victim.QueueFree();
            attacker.QueueFree();
        }

        private void TestSimulationMode()
        {
            _log.Info("Test 2: 模拟模式测试");

            var victim = CreateDummyUnit("Victim_Sim");
            float startHp = 100f;
            victim.Data.Set(DataKey.CurrentHp, startHp);
            victim.Data.Set(DataKey.FinalHp, startHp);

            var attacker = CreateDummyUnit("Attacker_Sim");

            var infoSim = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                BaseDamage = 50,
                Type = DamageType.Physical,
                IsSimulation = true
            };

            DamageService.Instance.Process(infoSim);

            // 检查伤害是否计算
            if (infoSim.FinalDamage == 50)
            {
                _log.Success("  PASS: 模拟伤害计算准确");
            }
            else
            {
                _log.Error($"  FAIL: 模拟伤害计算错误. Expected: 50, Actual: {infoSim.FinalDamage}");
            }

            // 检查 HP 是否未变
            float currentHp = victim.Data.Get<float>(DataKey.CurrentHp);
            if (Mathf.IsEqualApprox(currentHp, startHp))
            {
                _log.Success("  PASS: 模拟模式未实际扣血");
            }
            else
            {
                _log.Error($"  FAIL: 模拟模式导致扣血! Hp: {startHp} -> {currentHp}");
            }

            victim.QueueFree();
            attacker.QueueFree();
        }

        private TestUnit CreateDummyUnit(string name)
        {
            var unit = new TestUnit();
            unit.Name = name;
            AddChild(unit);

            // 使用 EntityManager.AddComponent 动态添加组件
            // 这会自动处理：挂载、注册到 EntityManager、建立 Entity-Component 关系、触发 OnComponentRegistered
            var healthComp = new HealthComponent();
            EntityManager.AddComponent(unit, healthComp);

            // 初始化必要数据
            unit.Data.Set(DataKey.BaseHp, 100f);
            unit.Data.Set(DataKey.CurrentHp, 100f);
            unit.Data.Set(DataKey.FinalHp, 100f);

            return unit;
        }

        // 简单的 IUnit 实现用于测试
        private partial class TestUnit : Node2D, IUnit, IEntity
        {
            public Data Data { get; } = new Data();
            public EventBus Events { get; } = new EventBus();
            // IEntity expects string EntityId
            public string EntityId { get; } = System.Guid.NewGuid().ToString();
            // IUnit expects FactionId
            public int FactionId { get; set; } = 0;

            public TestUnit()
            {
                // 初始化必要数据，使用 DataKey.BaseHp 代替不存在的 MaxHp
                // 注意：CreateDummyUnit 中已经设置了一部分，这里是类定义
            }
        }
    }
}
