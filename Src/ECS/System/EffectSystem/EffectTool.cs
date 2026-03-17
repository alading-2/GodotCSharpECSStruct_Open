using Godot;
using System.Linq;

/// <summary>
/// 特效生成参数（统一独立特效和附着特效）
/// </summary>
/// <param name="VisualScene">特效视觉场景</param>
/// <param name="Host">宿主 Entity 节点（非 null 时为附着模式，跟随宿主位置）</param>
/// <param name="Name">特效名称（用于调试）</param>
/// <param name="MaxLifeTime">最大生存时间，-1 表示由动画结束控制</param>
/// <param name="PlayRate">播放倍率</param>
/// <param name="Scale">特效缩放</param>
/// <param name="Offset">生成或附着偏移</param>
/// <param name="IsLooping">是否循环播放</param>
/// <param name="Rotation">旋转角度（弧度）</param>
public readonly record struct EffectSpawnOptions(
    PackedScene VisualScene,
    Node? Host = null,
    string Name = "Effect",
    float MaxLifeTime = -1f,
    float PlayRate = 1f,
    Vector2? Scale = null,
    Vector2? Offset = null,
    bool IsLooping = false,
    float Rotation = 0f
    );

/// <summary>
/// 特效工具 - 统一的特效生成和销毁入口
///
/// 设计理念：
/// - 独立静态工具类，消费 EntityManager 的基础生命周期 API
/// - 类似 DamageService 的领域服务模式
/// - Effect 不走 EntityManager.Spawn 的通用装配流程，而是由 EffectTool 自行编排生成顺序
/// - 视觉加载由 EffectTool 完成
/// - 动画播放命令由 EffectComponent 通过事件驱动
/// - 附着跟随、生命周期、动画结束销毁由 EffectComponent 负责
///
/// 使用示例：
/// <code>
/// // 独立特效（在指定位置播放，播完自动销毁）
/// EffectTool.Spawn(position, new EffectSpawnOptions(hitEffectScene));
///
/// // 附着特效（跟随宿主，宿主销毁时自动销毁）
/// EffectTool.Spawn(Vector2.Zero, new EffectSpawnOptions(buffEffectScene, Host: hostEntity));
///
/// // 销毁宿主身上所有特效
/// EffectTool.DestroyByHost(hostEntity);
/// </code>
/// </summary>
public static partial class EffectTool
{
    private static readonly Log _log = new("EffectTool");

    // ==================== 生成 ====================

    /// <summary>
    /// 生成特效（统一入口）
    /// - Host 为 null：独立特效，在 position 播放
    /// - Host 非 null：附着特效，跟随宿主位置，自动建立关系
    /// </summary>
    /// <param name="position">生成位置（世界坐标，附着模式下使用宿主位置）</param>
    /// <param name="options">特效参数</param>
    /// <returns>生成的 EffectEntity，失败返回 null</returns>
    public static EffectEntity? Spawn(Vector2 position, EffectSpawnOptions options)
    {
        bool isAttached = options.Host != null;

        // 附着模式：使用宿主位置
        if (isAttached)
        {
            if (options.Host is not IEntity)
            {
                _log.Error("宿主不是 IEntity");
                return null;
            }
            if (options.Host is Node2D host2D)
            {
                position = host2D.GlobalPosition;
            }
        }

        var entity = AcquireEffectEntity();
        if (entity == null)
        {
            _log.Error("特效生成失败");
            return null;
        }

        string effectId = entity.GetInstanceId().ToString();
        entity.Data.Set(DataKey.Id, effectId);

        // 写入 Data
        FillEffectData(entity, options, isAttached);

        // 加载视觉场景到 VisualRoot
        InjectVisualScene(entity, options.VisualScene);

        // 应用初始变换
        ApplyInitialTransform(entity, position, options, isAttached);

        // 附着模式：必须先建立关系，再注册组件，避免 EffectComponent.OnComponentRegistered 取不到宿主
        if (isAttached)
        {
            string hostId = options.Host!.GetInstanceId().ToString();
            EntityRelationshipManager.AddRelationship(
                hostId, effectId, EntityRelationshipType.ENTITY_TO_EFFECT);
        }

        // 注册 Entity / Component（对象池复用后需要重新注册）
        if (!NodeLifecycleManager.IsRegistered(effectId))
        {
            EntityManager.Register(entity);
            EntityManager.RegisterComponents(entity);
        }

        // GlobalEventBus.Global.Emit(
        //     GameEventType.Global.EntitySpawned,
        //     new GameEventType.Global.EntitySpawnedEventData(entity));

        // 日志
        if (isAttached)
        {
            _log.Debug($"生成附着特效: {options.Name} -> 宿主: {options.Host!.Name}");
        }
        else
        {
            _log.Debug($"生成独立特效: {options.Name} 位置: {position}");
        }

        return entity;
    }

    // ==================== 销毁 ====================

    /// <summary>
    /// 销毁指定宿主身上的所有附着特效
    /// </summary>
    /// <param name="host">宿主 Entity 节点</param>
    public static void DestroyByHost(Node host)
    {
        string hostId = host.GetInstanceId().ToString();

        var effectIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
            hostId, EntityRelationshipType.ENTITY_TO_EFFECT);

        var idsCopy = effectIds.ToList();
        if (idsCopy.Count == 0) return;
        int count = 0;

        foreach (var effectId in idsCopy)
        {
            var effectNode = EntityManager.GetEntityById(effectId);
            if (effectNode != null)
            {
                EntityManager.Destroy(effectNode);
                count++;
            }
        }

        _log.Debug($"销毁宿主 {host.Name} 的 {count} 个附着特效");
    }

    /// <summary>
    /// 销毁单个特效实体
    /// </summary>
    /// <param name="effect">要销毁的特效实体</param>
    public static void Destroy(EffectEntity? effect)
    {
        if (effect == null) return;
        EntityManager.Destroy(effect);
    }

    // ==================== 内部方法 ====================

    /// <summary>
    /// 写入特效运行时数据到 Data 容器
    /// </summary>
    private static void FillEffectData(EffectEntity entity, EffectSpawnOptions options, bool isAttached)
    {
        entity.Data.Set(DataKey.Name, options.Name);
        entity.Data.Set(DataKey.EntityType, EntityType.Effect);
        entity.Data.Set(DataKey.MaxLifeTime, options.MaxLifeTime);
        entity.Data.Set(DataKey.EffectPlayRate, options.PlayRate);
        entity.Data.Set(DataKey.EffectScale, options.Scale ?? Vector2.One);
        entity.Data.Set(DataKey.EffectOffset, options.Offset ?? Vector2.Zero);
        entity.Data.Set(DataKey.EffectIsLooping, options.IsLooping);
        entity.Data.Set(DataKey.EffectIsAttached, isAttached);
    }

    /// <summary>
    /// 从对象池获取特效实体
    /// </summary>
    private static EffectEntity? AcquireEffectEntity()
    {
        var pool = ObjectPoolManager.GetPool<EffectEntity>(ObjectPoolNames.EffectPool);
        if (pool == null)
        {
            _log.Error($"对象池不存在: {ObjectPoolNames.EffectPool}");
            return null;
        }

        return pool.Get();
    }

    /// <summary>
    /// 应用特效初始变换
    /// </summary>
    private static void ApplyInitialTransform(EffectEntity entity, Vector2 position, EffectSpawnOptions options, bool isAttached)
    {
        var finalPosition = position;
        if (!isAttached)
        {
            finalPosition += options.Offset ?? Vector2.Zero;
        }

        entity.GlobalPosition = finalPosition;
        entity.GlobalRotation = options.Rotation;
        entity.Scale = options.Scale ?? Vector2.One;
        entity.ForceUpdateTransform();
    }

    /// <summary>
    /// 加载视觉场景到 VisualRoot 子节点
    /// </summary>
    private static void InjectVisualScene(EffectEntity entity, PackedScene visualScene)
    {
        // 清理旧的 VisualRoot
        var existingVisual = entity.GetNodeOrNull("VisualRoot");
        if (existingVisual != null)
        {
            entity.RemoveChild(existingVisual);
            existingVisual.QueueFree();
        }

        // 实例化新的视觉场景
        var visual = visualScene.Instantiate();
        visual.Name = "VisualRoot";
        entity.AddChild(visual);
    }
}

