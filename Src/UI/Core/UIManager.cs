using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;



/// <summary>
/// UI 管理器 (AutoLoad)
/// 
/// 职责：
/// 1. 管理 UIBase 的生命周期（注册、注销）
/// 2. 管理 Entity-UI 绑定关系
/// 3. 提供 Entity-UI 查询接口
/// 
/// 设计理念：
/// - 底层使用 NodeLifecycleManager 进行注册/查询
/// - 使用 EntityRelationshipManager 管理 Entity-UI 关系
/// - 具体 UI 类型的监听逻辑（如血条）移到独立的 System
/// </summary>
public partial class UIManager : Node
{
    private static readonly Log _log = new Log("UIManager");

    /// <summary>
    /// 模块初始化：向AutoLoad注册
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(UIManager),
            Scene = ResourceManagement.Load<PackedScene>(nameof(UIManager), ResourceCategory.UI),
            Priority = AutoLoad.Priority.System
        });
    }

    // ============================================================
    // Godot 生命周期
    // ============================================================

    public override void _EnterTree()
    {
        _log.Success("UIManager (AutoLoad) 初始化完成");
    }

    public override void _ExitTree()
    {
        // 清理所有 UI
        ClearAllUI();
    }

    // ============================================================
    // UI 注册
    // ============================================================

    /// <summary>
    /// 注册 UI 到管理器（不绑定 Entity）
    /// </summary>
    public static void Register(UIBase ui)
    {
        NodeLifecycleManager.Register(ui);
    }

    /// <summary>
    /// 注销 UI
    /// </summary>
    public static void Unregister(UIBase ui)
    {
        NodeLifecycleManager.Unregister(ui);
    }

    // ============================================================
    // Entity-UI 绑定
    // ============================================================

    /// <summary>
    /// 为 Entity 绑定 UI（核心方法）
    /// 
    /// 自动处理：从对象池获取/实例化 → 注册 → 绑定 → 建立关系
    /// </summary>
    /// <typeparam name="T">UI 类型（必须继承 UIBase）</typeparam>
    /// <param name="entity">要绑定的 Entity</param>
    /// <param name="poolName">对象池名称（可选，不提供则实例化新 UI）</param>
    /// <returns>绑定成功的 UI 实例</returns>
    public static T? BindUI<T>(IEntity entity, string? poolName = null) where T : UIBase
    {
        if (entity == null)
        {
            _log.Error("无法绑定 UI：Entity 为空");
            return null;
        }

        T? ui;

        // 1. 获取或创建 UI
        if (!string.IsNullOrEmpty(poolName))
        {
            // 从对象池获取
            var pool = ObjectPoolManager.GetPool<T>(poolName);
            if (pool == null)
            {
                _log.Error($"对象池不存在: {poolName}");
                return null;
            }
            ui = pool.Get();
        }
        else
        {
            // 从 ResourceManagement 实例化
            // 使用 ResourceManagement.Load 直接加载 PackedScene，避免手动 GetPath + Load
            // 使用 typeof(T).Name 作为资源名称，确保类型和资源名一致
            var scene = ResourceManagement.Load<PackedScene>(typeof(T).Name, ResourceCategory.UI);
            if (scene == null)
            {
                _log.Error($"UI场景加载失败: {typeof(T).Name} (请检查 ResourcePaths)");
                return null;
            }
            ui = scene.Instantiate<T>();
        }

        if (ui == null)
        {
            _log.Error($"UI 创建失败: {typeof(T).Name}");
            return null;
        }

        // 2. 注册到 NodeLifecycleManager
        NodeLifecycleManager.Register(ui);

        // 3. 调用绑定
        ui.Bind(entity);

        // 4. 建立关系
        var entityId = entity.Data.Get<string>(DataKey.Id);
        if (!string.IsNullOrEmpty(entityId))
        {
            EntityRelationshipManager.AddRelationship(
                entityId,
                ui.GetInstanceId().ToString(),
                EntityRelationshipType.ENTITY_TO_UI
            );
        }

        _log.Debug($"已绑定 UI: {typeof(T).Name} -> Entity {entityId}");
        return ui;
    }

    /// <summary>
    /// 解绑单个 UI
    /// </summary>
    public static void UnbindUI(UIBase ui)
    {
        if (ui == null) return;

        // 移除关系
        var entity = ui.GetBoundEntity();
        if (entity != null)
        {
            var entityId = entity.Data.Get<string>(DataKey.Id);
            if (!string.IsNullOrEmpty(entityId))
            {
                EntityRelationshipManager.RemoveRelationship(
                    entityId,
                    ui.GetInstanceId().ToString(),
                    EntityRelationshipType.ENTITY_TO_UI
                );
            }
        }

        // 解绑
        ui.Unbind();

        // 注销
        NodeLifecycleManager.Unregister(ui);

        // 归还对象池或销毁
        if (ui is IPoolable)
        {
            ObjectPoolManager.ReturnToPool(ui);
        }
        else
        {
            ui.QueueFree();
        }
    }

    /// <summary>
    /// 解绑并销毁 Entity 的所有 UI
    /// </summary>
    public static void UnbindAllUI(IEntity entity)
    {
        if (entity == null) return;

        var uis = GetUIForEntity(entity).ToList();

        foreach (var ui in uis)
        {
            UnbindUI(ui);
        }

        if (uis.Count > 0)
        {
            var entityId = entity.Data.Get<string>(DataKey.Id);
            _log.Debug($"已解绑 Entity {entityId} 的所有 UI，共 {uis.Count} 个");
        }
    }

    // ============================================================
    // 查询接口
    // ============================================================

    /// <summary>
    /// 获取 Entity 绑定的所有 UI
    /// </summary>
    public static IEnumerable<UIBase> GetUIForEntity(IEntity entity)
    {
        if (entity == null) yield break;

        var entityId = entity.Data.Get<string>(DataKey.Id);
        if (string.IsNullOrEmpty(entityId)) yield break;

        var uiIds = EntityRelationshipManager
            .GetChildEntitiesByParentAndType(entityId, EntityRelationshipType.ENTITY_TO_UI);

        foreach (var uiId in uiIds)
        {
            var uiNode = NodeLifecycleManager.GetNodeById(uiId);
            if (uiNode is UIBase ui)
            {
                yield return ui;
            }
        }
    }

    /// <summary>
    /// 获取 Entity 绑定的指定类型 UI
    /// </summary>
    public static T? GetUI<T>(IEntity entity) where T : UIBase
    {
        return GetUIForEntity(entity).OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// 获取所有已注册的指定类型 UI
    /// </summary>
    public static IEnumerable<T> GetAllUI<T>() where T : UIBase
    {
        return NodeLifecycleManager.GetNodesByType<T>();
    }

    // ============================================================
    // 清理
    // ============================================================

    /// <summary>
    /// 清理所有 UI（场景切换时调用）
    /// </summary>
    public static void ClearAllUI()
    {
        var allUI = NodeLifecycleManager.GetNodesByInterface<UIBase>().ToList();

        foreach (var ui in allUI)
        {
            ui.Unbind();
            NodeLifecycleManager.Unregister(ui);

            if (ui is IPoolable)
            {
                ObjectPoolManager.ReturnToPool(ui);
            }
            else
            {
                ui.QueueFree();
            }
        }

        _log.Info($"UIManager 已清空，共清理 {allUI.Count} 个 UI");
    }
}
