using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TargetSelectorTest : Node
{
    private static readonly Log _log = new Log("TargetSelectorTest");

    /// <summary>
    /// 轻量测试实体。
    /// - 继承 Node2D，便于直接参与空间距离计算；
    /// - 实现 IEntity，便于走 EntityTargetSelector 全流程过滤。
    /// </summary>
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

        TestEntityQuery_FilterAndSort();

        TestChainQuery_NoDuplicate();
        TestChainQuery_AllowDuplicate();
        TestChainQuery_SelfBouncePrevention();

        TestPositionQuery_Ring();
        TestGeometryCalculator_Circle();
        TestGeometryCalculator_OtherShapes();

        _log.Info("TargetSelector 测试完成");
    }

    /// <summary>
    /// 验证普通几何查询的完整流程：
    /// Circle 命中 -> TeamFilter 过滤 -> Sorting.Nearest 排序 -> MaxTargets 截断。
    /// </summary>
    private void TestEntityQuery_FilterAndSort()
    {
        _log.Info("测试实体查询 (过滤 + 排序 + 截断)...");

        var center = new MockEntity("Center", new Vector2(0, 0), Team.Player, EntityType.Unit);
        var enemyNear = new MockEntity("EnemyNear", new Vector2(40, 0), Team.Enemy, EntityType.Unit);
        var enemyFar = new MockEntity("EnemyFar", new Vector2(80, 0), Team.Enemy, EntityType.Unit);
        var friendly = new MockEntity("Friendly", new Vector2(20, 0), Team.Player, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(enemyNear);
        EntityManager.Register(enemyFar);
        EntityManager.Register(friendly);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = center.GlobalPosition,
                Range = 100f,
                CenterEntity = center,
                TeamFilter = AbilityTargetTeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                Sorting = AbilityTargetSorting.Nearest,
                MaxTargets = 2
            };

            var results = EntityTargetSelector.Query(query);
            var resultNames = string.Join(", ", results.Select(r => (r as Node)?.Name ?? "null"));
            _log.Info($"查询结果: [{resultNames}]");

            AssertTrue(results.Count == 2, $"应只保留 2 个目标 (实际: {results.Count})");
            if (results.Count >= 2)
            {
                AssertTrue(results[0] == enemyNear, $"第 1 个应为 EnemyNear, 实际: {(results[0] as Node)?.Name}");
                AssertTrue(results[1] == enemyFar, $"第 2 个应为 EnemyFar, 实际: {(results[1] as Node)?.Name}");
            }

            AssertTrue(!results.Contains(friendly), "友军不应被 Enemy 过滤器选中");
            AssertTrue(!results.Contains(center), "自身不应被 Enemy 过滤器选中");
        }
        finally
        {
            CleanupEntities(center, enemyNear, enemyFar, friendly);
        }
    }

    private void TestChainQuery_NoDuplicate()
    {
        _log.Info("测试链式查询 (不允许重复)...");

        // Setup
        var entityA = new MockEntity("A", new Vector2(0, 0));
        var entityB = new MockEntity("B", new Vector2(100, 0));
        var entityC = new MockEntity("C", new Vector2(200, 0));

        // Register manually
        EntityManager.Register(entityA);
        EntityManager.Register(entityB);
        EntityManager.Register(entityC);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Chain,
                Origin = new Vector2(0, 0),
                ChainCount = 3,
                ChainRange = 150f,
                CenterEntity = null,
                TeamFilter = AbilityTargetTeamFilter.All,
                ChainAllowDuplicate = false // Default
            };

            var results = EntityTargetSelector.Query(query);

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
            CleanupEntities(entityA, entityB, entityC);
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

        EntityManager.Register(entityA);
        EntityManager.Register(entityB);
        EntityManager.Register(entityC);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Chain,
                Origin = new Vector2(0, 0), // Start near A
                ChainCount = 4,
                ChainRange = 150f,
                TeamFilter = AbilityTargetTeamFilter.All,
                ChainAllowDuplicate = true // Allow duplicate!
            };

            var results = EntityTargetSelector.Query(query);

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
            CleanupEntities(entityA, entityB, entityC);
        }
    }

    private void TestChainQuery_SelfBouncePrevention()
    {
        _log.Info("测试链式查询 (防止自我弹跳)...");

        // Setup: Only A exists.
        var entityA = new MockEntity("A", new Vector2(0, 0));

        EntityManager.Register(entityA);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Chain,
                Origin = new Vector2(0, 0),
                ChainCount = 3,
                ChainRange = 150f,
                TeamFilter = AbilityTargetTeamFilter.All,
                ChainAllowDuplicate = true
            };

            var results = EntityTargetSelector.Query(query);

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
            CleanupEntities(entityA);
        }
    }

    private void TestPositionQuery_Ring()
    {
        _log.Info("测试 PositionTargetSelector (Ring 圆环)...");

        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring,
            Origin = new Vector2(100, 100),
            InnerRange = 50f,
            Range = 150f,
            MaxTargets = 10
        };

        var results = PositionTargetSelector.Query(query);

        AssertTrue(results.Count == 10, $"应生成 10 个随机点 (实际: {results.Count})");

        int validCount = 0;
        foreach (var p in results)
        {
            float dist = p.DistanceTo(query.Origin);
            if (dist >= query.InnerRange - 0.1f && dist <= query.Range + 0.1f)
            {
                validCount++;
            }
        }
        AssertTrue(validCount == 10, $"所有点都应在 [50, 150] 距离内 (有效: {validCount}/10)");
    }

    private void TestGeometryCalculator_Circle()
    {
        _log.Info("测试 GeometryCalculator (Circle 圆形)...");

        Vector2 center = new Vector2(0, 0);
        float radius = 100f;

        Vector2 p1 = new Vector2(50, 50); // Inside, dist ~70.7
        Vector2 p2 = new Vector2(100, 0); // On edge
        Vector2 p3 = new Vector2(101, 0); // Outside

        AssertTrue(GeometryCalculator.IsPointInCircle(p1, center, radius), "p1(50,50) 应在 Circle(100) 内");
        AssertTrue(GeometryCalculator.IsPointInCircle(p2, center, radius), "p2(100,0) 应在边缘");
        AssertTrue(!GeometryCalculator.IsPointInCircle(p3, center, radius), "p3(101,0) 应在外部");

        // Test random point generator bounds
        var randPoint = GeometryCalculator.GetRandomPointInCircle(center, radius);
        AssertTrue(GeometryCalculator.IsPointInCircle(randPoint, center, radius), $"随机生成的点 {randPoint} 必须在圆内");
    }

    private void TestGeometryCalculator_OtherShapes()
    {
        _log.Info("测试 GeometryCalculator (Box, Line, Cone)...");

        Vector2 origin = new Vector2(0, 0);
        Vector2 forward = Vector2.Right; // (1, 0)

        // --- Box Test ---
        float boxWidth = 20f; // Y-range: -10 to 10
        float boxLength = 50f; // X-range: 0 to 50
        AssertTrue(GeometryCalculator.IsPointInBox(new Vector2(25, 0), origin, forward, boxWidth, boxLength), "Box内");
        AssertTrue(GeometryCalculator.IsPointInBox(new Vector2(0, 10), origin, forward, boxWidth, boxLength), "Box边缘");
        AssertTrue(!GeometryCalculator.IsPointInBox(new Vector2(-1, 0), origin, forward, boxWidth, boxLength), "Box后方");
        AssertTrue(!GeometryCalculator.IsPointInBox(new Vector2(25, 11), origin, forward, boxWidth, boxLength), "Box外部(宽)");
        AssertTrue(!GeometryCalculator.IsPointInBox(new Vector2(51, 0), origin, forward, boxWidth, boxLength), "Box外部(长)");

        // --- Line (Capsule) Test ---
        float lineWidth = 20f; // R = 10
        float lineLength = 50f; // X 0->50
        AssertTrue(GeometryCalculator.IsPointInCapsule(new Vector2(25, 0), origin, forward, lineLength, lineWidth), "Line内");
        AssertTrue(GeometryCalculator.IsPointInCapsule(new Vector2(-5, 5), origin, forward, lineLength, lineWidth), "Line起点半圆内");
        AssertTrue(GeometryCalculator.IsPointInCapsule(new Vector2(55, 5), origin, forward, lineLength, lineWidth), "Line终点半圆内");
        AssertTrue(!GeometryCalculator.IsPointInCapsule(new Vector2(25, 11), origin, forward, lineLength, lineWidth), "Line外部");

        // --- Cone Test ---
        float coneRange = 50f;
        float coneAngle = 90f; // +/- 45 度
        AssertTrue(GeometryCalculator.IsPointInCone(new Vector2(25, 0), origin, forward, coneRange, coneAngle), "Cone内");
        AssertTrue(GeometryCalculator.IsPointInCone(new Vector2(25, 25), origin, forward, coneRange, coneAngle), "Cone边缘(45度)");
        AssertTrue(!GeometryCalculator.IsPointInCone(new Vector2(25, 26), origin, forward, coneRange, coneAngle), "Cone外部(角度)");
        AssertTrue(!GeometryCalculator.IsPointInCone(new Vector2(51, 0), origin, forward, coneRange, coneAngle), "Cone外部(距离)");
        AssertTrue(!GeometryCalculator.IsPointInCone(new Vector2(-10, 0), origin, forward, coneRange, coneAngle), "Cone后方");
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

    /// <summary>
    /// 统一清理测试实体。
    /// 遵循项目实体生命周期规范：
    /// 禁止直接 QueueFree，统一通过 EntityManager.Destroy 回收。
    /// </summary>
    private void CleanupEntities(params Node[] entities)
    {
        foreach (var entity in entities)
        {
            if (GodotObject.IsInstanceValid(entity))
            {
                EntityManager.Destroy(entity);
            }
        }
    }
}
