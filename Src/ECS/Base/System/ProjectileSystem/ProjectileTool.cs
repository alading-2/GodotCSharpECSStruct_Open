using Godot;

internal readonly record struct ProjectileSpawnOptions(
    PackedScene? VisualScene,
    string Name = "Projectile"
);

internal static partial class ProjectileTool
{
    private sealed partial class RuntimeProjectileConfig : Resource
    {
        public string? Name { get; set; }
        public PackedScene? VisualScenePath { get; set; }
    }

    public static ProjectileEntity? Spawn(Vector2 position, ProjectileSpawnOptions options)
    {
        var config = new RuntimeProjectileConfig
        {
            Name = options.Name,
            VisualScenePath = options.VisualScene
        };

        return EntityManager.Spawn<ProjectileEntity>(new EntitySpawnConfig
        {
            Config = config,
            UsingObjectPool = true,
            PoolName = ObjectPoolNames.ProjectilePool,
            Position = position
        });
    }
}
