using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TargetSelectorTest : Node
{
    private static readonly Log _log = new Log("TargetSelectorTest");

    // Mock Entity for testing
    public partial class MockEntity : Node2D, IEntity
    {
        public Data Data { get; } = new Data();
        public EventBus Events { get; } = new EventBus();

        public MockEntity(string name, Vector2 pos, Team team = Team.Enemy, EntityType type = EntityType.Unit)
        {
            Name = name;
            GlobalPosition = pos;
            Data.Set(DataKey.Team, team);
            Data.Set(DataKey.EntityType, type);
            Data.Set(DataKey.Id, GetInstanceId().ToString());
        }
    }

    public override void _Ready()
    {
        Run();
        GetTree().Quit();
    }

    public void Run()
    {
        _log.Info("开始测试 TargetSelector...");

        TestChainQuery_NoDuplicate();
        TestChainQuery_AllowDuplicate();
        TestChainQuery_SelfBouncePrevention();

        _log.Info("TargetSelector 测试完成");
    }

    private void TestChainQuery_NoDuplicate()
    {
        _log.Info("测试链式查询 (不允许重复)...");

        // Setup
        var entityA = new MockEntity("A", new Vector2(0, 0));
        var entityB = new MockEntity("B", new Vector2(100, 0));
        var entityC = new MockEntity("C", new Vector2(200, 0));

        // Register manually
        EntityManager.Register(entityA, "TestUnit");
        EntityManager.Register(entityB, "TestUnit");
        EntityManager.Register(entityC, "TestUnit");

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = AbilityTargetGeometry.Chain,
                Origin = new Vector2(0, 0),
                ChainCount = 3,
                ChainRange = 150f,
                CenterEntity = null,
                TeamFilter = AbilityTargetTeamFilter.All,
                ChainAllowDuplicate = false // Default
            };

            var results = TargetSelector.Query(query);

            var resultNames = string.Join(", ", results.Select(r => (r as Node)?.Name ?? "null"));
            _log.Info($"查询结果: [{resultNames}]");

            AssertTrue(results.Count == 3, $"应选中 3 个目标 (实际: {results.Count})");
            if (results.Count >= 3)
            {
                AssertTrue(results[0] == entityA, $"第 1 个应是 A");
                AssertTrue(results[1] == entityB, $"第 2 个应是 B");
                AssertTrue(results[2] == entityC, $"第 3 个应是 C");
            }
        }
        finally
        {
            // Cleanup
            EntityManager.UnregisterEntity(entityA);
            EntityManager.UnregisterEntity(entityB);
            EntityManager.UnregisterEntity(entityC);
            entityA.QueueFree();
            entityB.QueueFree();
            entityC.QueueFree();
        }
    }

    private void TestChainQuery_AllowDuplicate()
    {
        _log.Info("测试链式查询 (允许重复)...");

        // Setup: A <-> B bouncing
        // A at 0, B at 100. C at 1000 (too far).
        var entityA = new MockEntity("A", new Vector2(0, 0));
        var entityB = new MockEntity("B", new Vector2(100, 0));
        var entityC = new MockEntity("C", new Vector2(1000, 0));

        EntityManager.Register(entityA, "TestUnit");
        EntityManager.Register(entityB, "TestUnit");
        EntityManager.Register(entityC, "TestUnit");

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = AbilityTargetGeometry.Chain,
                Origin = new Vector2(0, 0), // Start near A
                ChainCount = 4,
                ChainRange = 150f,
                TeamFilter = AbilityTargetTeamFilter.All,
                ChainAllowDuplicate = true // Allow duplicate!
            };

            var results = TargetSelector.Query(query);

            var resultNames = string.Join(", ", results.Select(r => (r as Node)?.Name ?? "null"));
            _log.Info($"查询结果: [{resultNames}]");

            // Expected:
            // 1. Find A (dist 0).
            //    Current: A.
            // 2. From A, find nearest. B(100). A is self.
            //    Current: B.
            // 3. From B, find nearest. A(100), C(900). 
            //    Should pick A because allowDuplicate is true.
            //    Current: A.
            // 4. From A, pick B.
            //    Current: B.

            // Result: A, B, A, B
            AssertTrue(results.Count == 4, $"应选中 4 个目标 (实际: {results.Count})");
            if (results.Count >= 4)
            {
                AssertTrue(results[0] == entityA, $"1. 应为 A, 实际: {(results[0] as Node)?.Name}");
                AssertTrue(results[1] == entityB, $"2. 应为 B, 实际: {(results[1] as Node)?.Name}");
                AssertTrue(results[2] == entityA, $"3. 应为 A, 实际: {(results[2] as Node)?.Name}");
                AssertTrue(results[3] == entityB, $"4. 应为 B, 实际: {(results[3] as Node)?.Name}");
            }
        }
        finally
        {
            EntityManager.UnregisterEntity(entityA);
            EntityManager.UnregisterEntity(entityB);
            EntityManager.UnregisterEntity(entityC);
            entityA.QueueFree();
            entityB.QueueFree();
            entityC.QueueFree();
        }
    }

    private void TestChainQuery_SelfBouncePrevention()
    {
        _log.Info("测试链式查询 (防止自我弹跳)...");

        // Setup: Only A exists.
        var entityA = new MockEntity("A", new Vector2(0, 0));

        EntityManager.Register(entityA, "TestUnit");

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = AbilityTargetGeometry.Chain,
                Origin = new Vector2(0, 0),
                ChainCount = 3,
                ChainRange = 150f,
                TeamFilter = AbilityTargetTeamFilter.All,
                ChainAllowDuplicate = true
            };

            var results = TargetSelector.Query(query);

            var resultNames = string.Join(", ", results.Select(r => (r as Node)?.Name ?? "null"));
            _log.Info($"查询结果: [{resultNames}]");

            // Expected:
            // 1. Find A. Current = A.
            // 2. From A, find nearest. A is self. No other candidates.
            // Loop break.

            // Result: [A]
            AssertTrue(results.Count == 1, $"应只选中 1 个目标 (实际: {results.Count})");
            if (results.Count >= 1)
            {
                AssertTrue(results[0] == entityA, $"1. 应为 A, 实际: {(results[0] as Node)?.Name}");
            }
        }
        finally
        {
            EntityManager.UnregisterEntity(entityA);
            entityA.QueueFree();
        }
    }

    private void AssertTrue(bool condition, string message)
    {
        if (condition)
        {
            _log.Info($"[通过] {message}");
        }
        else
        {
            _log.Error($"[失败] {message}");
        }
    }
}
