using Godot;
using System;
using System.Collections.Generic;
using BrotatoMy.Test;
using BrotatoMy.Test.DamageSystemTest;

public partial class ECSTest : Node
{
    private static readonly Log _log = new Log("ECSTest");

    [Export] private Label? _statusLabel;
    [Export] private Button? _runButton;
    [Export] private PackedScene? _testEntityScene;

    // We will use a local pool for testing
    private ObjectPool<Node>? _testEntityPool;
    // Use MaxHp as it is a registered key that supports modifiers
    private const string PoolName = "TestEntityPool";

    private int _passCount = 0;
    private int _failCount = 0;

    public override void _Ready()
    {
        _log.Info("ECS Test Scene Ready");
        if (_statusLabel != null)
        {
            _statusLabel.Text = "Ready to run tests.";
        }

        if (_runButton != null)
        {
            _runButton.Pressed += RunAllTests;
        }
        else
        {
            // Auto run if no button (or just log warning)
            _log.Warn("No Run Button assigned. Call RunAllTests manually or assign button.");
        }
    }

    public void RunAllTests()
    {
        _passCount = 0;
        _failCount = 0;
        UpdateStatus("Running tests...");
        _log.Info("=== STARTING ECS TESTS ===");

        try
        {
            SetupPool();
            TestDataSystem();
            TestTimerSystem();
            TestObjectPoolSystem();
            TestEntitySystem();
            TestDamageSystem();
        }
        catch (Exception e)
        {
            LogFail($"Exception during tests: {e.Message}");
        }

        _log.Info("=== ECS TESTS COMPLETED ===");
        UpdateStatus($"Tests Completed. Pass: {_passCount}, Fail: {_failCount}");

        // Cleanup
        CleanupPool();
    }

    private void SetupPool()
    {
        // Setup a local object pool
        if (_testEntityScene == null)
        {
            // If not assigned in inspector, try to load it or create a placeholder
            // For this test script, we assume it's assigned or we load it.
            // Let's assume we load it from a known path if null
            _testEntityScene = GD.Load<PackedScene>("res://Src/Test/SingleTest/ECS/ECSTest/Entity/TestEntity.tscn");
        }

        if (_testEntityScene == null)
        {
            throw new Exception("TestEntityScene is null. Cannot test pool.");
        }

        // Initialize pool manually for testing if not using the global init
        _testEntityPool = new ObjectPool<Node>(
           () => _testEntityScene.Instantiate(),
           new ObjectPoolConfig
           {
               Name = PoolName,
               InitialSize = 5,
               MaxSize = 10
           }
       );

        Pass("Pool Setup");
    }

    private void CleanupPool()
    {
        _testEntityPool?.Clear();
        // Managers usually static or singleton, but ObjectPoolManager stores pools
        // We might want to remove it from ObjectPoolManager if we want to be clean, 
        // but ObjectPoolManager doesn't expose RemovePool currently? 
        // Actually ObjectPool constructor registers itself.
        // We'll leave it for now.
    }

    private void TestDataSystem()
    {
        _log.Info("--- Testing Data System ---");

        var data = new Data();
        // Use a registered key that supports modifiers
        string keyHp = DataKey.BaseHp;

        // 1. Basic Set/Get
        data.Set(keyHp, 100f);
        Assert(Mathf.IsEqualApprox(data.Get<float>(keyHp), 100f), "Data Set/Get Float");

        // 2. Add Modifier (Additive)
        string buffId = "Buff1";
        data.AddModifier(keyHp, new DataModifier(ModifierType.Additive, 50f, id: buffId));
        Assert(Mathf.IsEqualApprox(data.Get<float>(keyHp), 150f), "Data Modifier Additive");

        // 3. Add Modifier (Multiplicative)
        data.AddModifier(keyHp, new DataModifier(ModifierType.Multiplicative, 1.5f)); // (100 + 50) * 1.5 = 225
        Assert(Mathf.IsEqualApprox(data.Get<float>(keyHp), 225f), "Data Modifier Multiplicative");

        // 4. Remove Modifier
        data.RemoveModifier(keyHp, buffId); // (100) * 1.5 = 150
        Assert(Mathf.IsEqualApprox(data.Get<float>(keyHp), 150f), "Data Remove Modifier");

        // 5. Computed Data
        // We need to register a computed data to test it. 
        // DataRegistry is static, so we can register a temporary one or use an existing one if available.
        // For safety, let's just test the Data behavior with local computed logic if possible, 
        // but Data class uses DataRegistry for computed access.
        // We will skip registering global data to avoid polluting the global registry for a test run,
        // or we assume standard keys exist. Let's use standard keys.

        // Let's use a dummy key if we can, but DataRegistry needs registration.
        // We'll skip complex ComputedData test that requires global static registration to avoid side effects,
        // unless we register a test specific key.

        Pass("Data System Basic Tests");
    }

    private void TestTimerSystem()
    {
        _log.Info("--- Testing Timer System ---");

        bool timerCalled = false;

        // Create a short timer
        // Note: This is an async test in a sync method, so we can't easily wait for it without coroutine.
        // For this simple test, we will just verify creation and cancellation API.

        var timer = TimerManager.Instance.Delay(0.1f).OnComplete(() => { timerCalled = true; });
        Assert(timer != null, "Timer Creation");
        Assert(!timer.IsDone, "Timer Initial State");

        timer.Cancel();
        Assert(timer.IsDone, "Timer Cancellation");
        Assert(!timerCalled, "Timer Cancelled Callback Check (Immediate)");

        Pass("Timer System API");
    }

    private void TestObjectPoolSystem()
    {
        _log.Info("--- Testing Object Pool System ---");

        if (_testEntityPool == null)
        {
            throw new Exception("Pool is null");
        }

        // Get from pool
        var entityNode = _testEntityPool.Get();
        Assert(entityNode != null, "Pool Get");
        Assert(entityNode is TestEntity, "Pool Object Type");

        var testEntity = entityNode as TestEntity;
        testEntity.Data.Set("TestVal", 123);

        // Return to pool (Check static return vs manual release)
        // Since we registered it with name, ObjectPoolManager.ReturnToPool should work
        ObjectPoolManager.ReturnToPool(testEntity);

        // Verify reset behavior (mock) - Actual reset depends on IPoolable implementation checked next time we get it
        var entityNode2 = _testEntityPool.Get();
        var testEntity2 = entityNode2 as TestEntity;

        Assert(testEntity2.Data.GetAll().Count == 0, "Pool Reset (Data cleared)");

        ObjectPoolManager.ReturnToPool(testEntity2);

        Pass("Object Pool System");
    }

    private void TestEntitySystem()
    {
        _log.Info("--- Testing Entity System ---");

        // 1. Spawn via EntityManager
        // We need a dummy Resource for Spawn, usually.
        // But EntityManager.Spawn signatures usually require a Resource.
        // If we want to test EntityManager specific logic, we need to respect that.
        // However, for just creating an entity, we can manually create and register.

        if (_testEntityPool == null) throw new Exception("Pool is null");

        var entity = _testEntityPool.Get() as TestEntity;

        if (entity == null) throw new Exception("Entity from pool is null or not TestEntity");

        EntityManager.Register(entity);

        Assert(!string.IsNullOrEmpty(entity.Data.Get<string>(DataKey.Id)), "Entity ID assigned");

        // 2. Add Component
        var comp = new TestComponent();
        EntityManager.AddComponent(entity, comp);

        Assert(comp.IsRegistered, "Component Registered");
        Assert(comp.GetData() == entity.Data, "Component Data Injection");

        // 3. Get Component
        var fetchedComp = EntityManager.GetComponent<TestComponent>(entity);
        Assert(fetchedComp == comp, "EntityManager GetComponent");

        // 4. Cleanup
        EntityManager.Destroy(entity);

        Pass("Entity System");
    }

    private void TestDamageSystem()
    {
        _log.Info("--- Testing Damage System ---");
        var test = new DamageSystemTest();
        AddChild(test);
        test.RunTests();
        test.QueueFree();
        Pass("Damage System");
    }

    private void Assert(bool condition, string message)
    {
        if (condition)
        {
            _log.Debug($"[PASS] {message}");
        }
        else
        {
            _log.Error($"[FAIL] {message}");
            _failCount++;
            throw new Exception($"Assertion failed: {message}");
        }
    }

    private void Pass(string section)
    {
        _log.Success($"Section Passed: {section}");
        _passCount++;
    }

    private void LogFail(string message)
    {
        _log.Error(message);
        _failCount++;
    }

    private void UpdateStatus(string msg)
    {
        if (_statusLabel != null) _statusLabel.Text = msg;
        GD.Print(msg);
    }
}
