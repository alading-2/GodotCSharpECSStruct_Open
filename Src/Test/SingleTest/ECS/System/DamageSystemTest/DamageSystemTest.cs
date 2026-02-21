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
            TestBaseDamageAndPreChecks();
            TestDodgeLogic();
            TestCritLogic();
            TestArmorReduction();
            TestDamageTakenMultiplier();
            TestLifesteal();
            TestStatistics();
            TestSimulationMode();
            _log.Success("所有测试完成！");
        }

        private void TestBaseDamageAndPreChecks()
        {
            _log.Info("Test 1: 基础检查测试");

            // 1. 测试死亡单位不受伤
            var deadUnit = CreateDummyUnit("DeadUnit");
            deadUnit.Data.Set(DataKey.IsDead, true);
            var attacker = CreateDummyUnit("Attacker");

            var infoDead = new DamageInfo
            {
                Attacker = attacker,
                Victim = deadUnit,
                Damage = 100,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoDead);

            if (infoDead.IsEnd && infoDead.FinalDamage == 0)
            {
                _log.Success("  PASS: 死亡单位不受伤");
            }
            else
            {
                _log.Error($"  FAIL: 死亡单位应该不受伤. IsEnd: {infoDead.IsEnd}, FinalDamage: {infoDead.FinalDamage}");
            }

            // 2. 测试无敌单位不受伤
            var invulnerableUnit = CreateDummyUnit("InvulnerableUnit");
            invulnerableUnit.Data.Set(DataKey.IsInvulnerable, true);

            var infoInvulnerable = new DamageInfo
            {
                Attacker = attacker,
                Victim = invulnerableUnit,
                Damage = 100,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoInvulnerable);

            if (infoInvulnerable.IsEnd && infoInvulnerable.FinalDamage == 0)
            {
                _log.Success("  PASS: 无敌单位不受伤");
            }
            else
            {
                _log.Error($"  FAIL: 无敌单位应该不受伤. IsEnd: {infoInvulnerable.IsEnd}, FinalDamage: {infoInvulnerable.FinalDamage}");
            }

            deadUnit.QueueFree();
            invulnerableUnit.QueueFree();
            attacker.QueueFree();
        }

        private void TestDodgeLogic()
        {
            _log.Info("Test 1: 闪避逻辑测试");

            // 1. 创建受害者 (高闪避)
            var victim = CreateDummyUnit("Victim_Dodger");
            victim.Data.Set(DataKey.DodgeChance, 100f); // 100% 闪避

            // 2. 创建用于触发伤害的 Mock 攻击者（必须是一个 IEntity）
            var attacker = CreateDummyUnit("Attacker");

            // 3. 测试物理伤害 (应被闪避)
            var infoPhysical = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                Damage = 10,
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
                Damage = 10,
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

        private void TestCritLogic()
        {
            _log.Info("Test 3: 暴击逻辑测试");

            var victim = CreateDummyUnit("Victim_Crit");
            var attacker = CreateDummyUnit("Attacker_Crit");

            // 设置 100% 暴击率
            attacker.Data.Set(DataKey.CritRate, 100f);
            attacker.Data.Set(DataKey.CritDamage, 200f); // 200% = 2倍伤害

            var infoCrit = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                Damage = 100,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoCrit);

            if (infoCrit.IsCritical && Godot.Mathf.IsEqualApprox(infoCrit.FinalDamage, 200f))
            {
                _log.Success("  PASS: 100%暴击率必定触发");
            }
            else
            {
                _log.Error($"  FAIL: 暴击判定错误. Expected: 200, Actual: {infoCrit.FinalDamage}, IsCritical: {infoCrit.IsCritical}");
            }

            victim.QueueFree();
            attacker.QueueFree();
        }

        private void TestArmorReduction()
        {
            _log.Info("Test 4: 护甲减伤测试");

            var victim = CreateDummyUnit("Victim_Armor");
            victim.Data.Set(DataKey.Armor, 50f); // 正护甲
            var attacker = CreateDummyUnit("Attacker_Armor");

            var infoArmor = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                Damage = 100,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoArmor);

            // 50护甲应该有减伤效果，最终伤害应小于100
            if (infoArmor.FinalDamage > 0 && infoArmor.FinalDamage < 100)
            {
                _log.Success($"  PASS: 护甲减伤计算正确 (100 -> {infoArmor.FinalDamage:F1})");
            }
            else
            {
                _log.Error($"  FAIL: 护甲减伤错误. Expected: < 100, Actual: {infoArmor.FinalDamage}");
            }

            victim.QueueFree();
            attacker.QueueFree();
        }

        private void TestDamageTakenMultiplier()
        {
            _log.Info("Test 5: 受伤倍率测试");

            var victim = CreateDummyUnit("Victim_Amplified");
            victim.Data.Set(DataKey.DamageTakenMultiplier, 1.5f); // 易伤 +50%
            var attacker = CreateDummyUnit("Attacker_Amplified");

            var infoAmplified = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                Damage = 100,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoAmplified);

            // 1.5倍器，应该是 150伤害
            if (Godot.Mathf.IsEqualApprox(infoAmplified.FinalDamage, 150f))
            {
                _log.Success("  PASS: 易伤倍率应用正确");
            }
            else
            {
                _log.Error($"  FAIL: 易伤倍率错误. Expected: 150, Actual: {infoAmplified.FinalDamage}");
            }

            victim.QueueFree();
            attacker.QueueFree();
        }

        private void TestLifesteal()
        {
            _log.Info("Test 6: 吸血机制测试");

            var victim = CreateDummyUnit("Victim_Lifesteal");
            var attacker = CreateDummyUnit("Attacker_Lifesteal");
            attacker.Data.Set(DataKey.LifeSteal, 100f); // 100% 触发率

            bool healRequestReceived = false;
            attacker.Events.On<GameEventType.Unit.HealRequestEventData>(
                GameEventType.Unit.HealRequest,
                evt =>
                {
                    healRequestReceived = true;
                    _log.Debug($"  收到治疗请求: {evt.Amount}");
                }
            );

            var infoLifesteal = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                Damage = 100,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoLifesteal);

            if (healRequestReceived)
            {
                _log.Success("  PASS: 吸血触发事件");
            }
            else
            {
                _log.Error("  FAIL: 吸血未触发事件");
            }

            victim.QueueFree();
            attacker.QueueFree();
        }

        private void TestStatistics()
        {
            _log.Info("Test 7: 伤害统计测试");

            var victim = CreateDummyUnit("Victim_Stats");
            var attacker = CreateDummyUnit("Attacker_Stats");

            // 重置统计，确保从0开始
            attacker.Data.Set(DataKey.TotalDamageDealt, 0f);
            attacker.Data.Set(DataKey.WaveDamageDealt, 0f);
            attacker.Data.Set(DataKey.TotalHits, 0);
            attacker.Data.Set(DataKey.WaveHits, 0);

            var infoStats = new DamageInfo
            {
                Attacker = attacker,
                Victim = victim,
                Damage = 50,
                Type = DamageType.Physical
            };
            DamageService.Instance.Process(infoStats);

            float totalDamage = attacker.Data.Get<float>(DataKey.TotalDamageDealt);
            float waveDamage = attacker.Data.Get<float>(DataKey.WaveDamageDealt);
            int totalHits = attacker.Data.Get<int>(DataKey.TotalHits);
            int waveHits = attacker.Data.Get<int>(DataKey.WaveHits);

            if (totalDamage == 50f && waveDamage == 50f && totalHits == 1 && waveHits == 1)
            {
                _log.Success("  PASS: 伤害统计累加正确");
            }
            else
            {
                _log.Error($"  FAIL: 伤害统计错误. Total: {totalDamage}, Wave: {waveDamage}, Hits: {totalHits}/{waveHits}");
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
                Damage = 50,
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
            // EntityId 由 IEntity 默认实现（从 DataKey.Id 读取）
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
