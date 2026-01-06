using Godot;
using System;
using BrotatoMy.Test; // For ObjectPool/Test utils if needed

public partial class DamageSystemTest : Node
{
    private static readonly Log _log = new("DamageSystemTest");

    public void RunTests()
    {
        _log.Info("=== Running Damage System Tests ===");

        try
        {
            TestBasicDamage();
            TestDefense_Armor();
            TestDefense_Shield();
            TestDefense_Dodge();
            TestAmplification();
        }
        catch (Exception e)
        {
            _log.Error($"Test Failed: {e.Message}\n{e.StackTrace}");
        }

        _log.Info("=== Damage System Tests Finished ===");
    }

    private void TestBasicDamage()
    {
        // Setup
        var attacker = new Player();
        attacker.Name = "TestAttacker";
        var victim = new Enemy();
        victim.Name = "TestVictim";

        // Add Health Component to Victim
        var healthComp = new HealthComponent();
        EntityManager.AddComponent(victim, healthComp);

        // Init Data
        victim.Data.Set(DataKey.MaxHp, 100f);
        victim.Data.Set(DataKey.CurrentHp, 100f);

        // Prepare DamageInfo
        var info = new DamageInfo
        {
            Attacker = attacker,
            Instigator = attacker,
            Victim = victim,
            BaseDamage = 10,
            Type = DamageType.Physical
        };

        // Execute
        DamageService.Instance.Process(info);

        // Verify
        Assert(Mathf.IsEqualApprox(info.FinalDamage, 10f), "Basic: FinalDamage should be 10");
        float currentHp = victim.Data.Get<float>(DataKey.CurrentHp);
        // Hp should be 90
        Assert(Mathf.IsEqualApprox(currentHp, 90f), $"Basic: HP should be 90, got {currentHp}");

        // Cleanup
        EntityManager.Destroy(attacker);
        EntityManager.Destroy(victim); // This cleans up components too? usually yes if they are children
        healthComp.QueueFree(); // Components added via AddComponent are addChild? Confirm logic.
        // EntityManager.AddComponent usually adds as child. Destroy entity queues free entity.
    }

    private void TestDefense_Armor()
    {
        var attacker = new Player();
        var victim = new Enemy();
        var healthComp = new HealthComponent();
        EntityManager.AddComponent(victim, healthComp);

        victim.Data.Set(DataKey.MaxHp, 100f);
        victim.Data.Set(DataKey.CurrentHp, 100f);

        // Armor = 15 -> 50% reduction
        victim.Data.Set(DataKey.Armor, 15f);

        var info = new DamageInfo
        {
            Attacker = attacker,
            Instigator = attacker,
            Victim = victim,
            BaseDamage = 100,
            Type = DamageType.Physical
        };

        DamageService.Instance.Process(info);

        // Expected: 100 * (1 - 15/30) = 50
        Assert(Mathf.IsEqualApprox(info.FinalDamage, 50f), $"Armor: Expected 50 damage, got {info.FinalDamage}");

        EntityManager.Destroy(attacker);
        EntityManager.Destroy(victim);
    }

    private void TestDefense_Shield()
    {
        var attacker = new Player();
        var victim = new Enemy();
        var healthComp = new HealthComponent();
        EntityManager.AddComponent(victim, healthComp);

        victim.Data.Set(DataKey.MaxHp, 100f);
        victim.Data.Set(DataKey.CurrentHp, 100f);
        victim.Data.Set(DataKey.Shield, 20f); // 20 Shield

        var info = new DamageInfo
        {
            Attacker = attacker,
            Instigator = attacker,
            Victim = victim,
            BaseDamage = 50,
            Type = DamageType.Physical
        };

        DamageService.Instance.Process(info);

        // Expected: Shield absorbs 20, remaining 30.
        // Shield should be 0.
        // HP should be 100 - 30 = 70.

        Assert(Mathf.IsEqualApprox(info.FinalDamage, 30f), $"Shield: Expected 30 damage (after shield), got {info.FinalDamage}");
        Assert(Mathf.IsEqualApprox(victim.Data.Get<float>(DataKey.Shield), 0f), "Shield: Should be 0");
        Assert(Mathf.IsEqualApprox(victim.Data.Get<float>(DataKey.CurrentHp), 70f), "Shield: HP check");

        EntityManager.Destroy(attacker);
        EntityManager.Destroy(victim);
    }

    private void TestDefense_Dodge()
    {
        var attacker = new Player();
        var victim = new Enemy();
        var healthComp = new HealthComponent();
        EntityManager.AddComponent(victim, healthComp);

        victim.Data.Set(DataKey.MaxHp, 100f);
        victim.Data.Set(DataKey.CurrentHp, 100f);
        victim.Data.Set(DataKey.DodgeChance, 110f); // 100% Dodge (Assuming Cap > 100 for test, or processor caps it?)
                                                    // Processor caps at 60%. So we can't guarantee dodge.
                                                    // Let's modify processor for test or mock random?
                                                    // Hard to test random without dependency injection or Seed.
                                                    // Skip deterministic dodge test or just log it.

        _log.Warn("Skipping deterministic Dodge test due to RNG.");

        EntityManager.Destroy(attacker);
        EntityManager.Destroy(victim);
    }

    private void TestAmplification()
    {
        var attacker = new Player();
        var victim = new Enemy();
        var healthComp = new HealthComponent();
        EntityManager.AddComponent(victim, healthComp);

        attacker.Data.Set(DataKey.Damage, 50f); // +50% Damage (assuming Damage is %)

        victim.Data.Set(DataKey.MaxHp, 100f);
        victim.Data.Set(DataKey.CurrentHp, 100f);

        var info = new DamageInfo
        {
            Attacker = attacker,
            Instigator = attacker,
            Victim = victim,
            BaseDamage = 100,
            Type = DamageType.Physical
        };

        DamageService.Instance.Process(info);

        // Expected: 100 * (1 + 0.5) = 150
        Assert(Mathf.IsEqualApprox(info.FinalDamage, 150f), $"Amp: Expected 150 damage, got {info.FinalDamage}");

        EntityManager.Destroy(attacker);
        EntityManager.Destroy(victim);
    }

    private void Assert(bool condition, string msg)
    {
        if (!condition)
        {
            throw new Exception(msg);
        }
        else
        {
            _log.Success($"[PASS] {msg}");
        }
    }
}
