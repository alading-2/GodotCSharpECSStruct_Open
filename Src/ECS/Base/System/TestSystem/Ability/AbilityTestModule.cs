using Godot;
using System;

/// <summary>
/// 技能测试模块。
/// <para>
/// 提供一个运行时调试界面，用于按分组路径查看技能库，并给当前实体执行添加 / 移除 / 启停。
/// </para>
/// <para>
/// 模块本身只负责 UI 展示与输入转发，具体数据组织与操作由 <see cref="AbilityTestService"/> 负责。
/// </para>
/// </summary>
public partial class AbilityTestModule : TestModuleBase
{
    /// <summary>技能测试模块日志器，用于记录点击操作与右键菜单行为。</summary>
    private static readonly Log _log = new(nameof(AbilityTestModule));

    /// <summary>技能测试服务，负责缓存技能目录和执行业务操作。</summary>
    private readonly AbilityTestService _service = new();

    /// <summary>技能分组区块场景。</summary>
    [Export] private PackedScene? _groupSectionScene;

    /// <summary>技能库条目场景。</summary>
    [Export] private PackedScene? _catalogItemScene;

    /// <summary>已拥有技能条目场景。</summary>
    [Export] private PackedScene? _ownedItemScene;

    /// <summary>左侧技能库分组容器。</summary>
    private VBoxContainer _availableGroupContainer = null!;

    /// <summary>右侧当前技能分组容器。</summary>
    private VBoxContainer _currentGroupContainer = null!;

    /// <summary>提示当前是否已经选中实体的说明标签。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>操作反馈区，用于显示添加 / 移除 / 切换启用状态的结果。</summary>
    private Label _statusLabel = null!;

    /// <summary>当前已经为其订阅技能事件的实体，避免重复订阅同一个目标。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>是否已经排队等待一次延迟刷新，避免同一帧重复重建列表。</summary>
    private bool _refreshQueued;

    /// <summary>右侧技能项的上下文菜单。</summary>
    private PopupMenu _ownedContextMenu = null!;

    /// <summary>上下文菜单当前作用的技能实例 Id。</summary>
    private string _contextAbilityId = string.Empty;

    /// <summary>上下文菜单当前作用技能的启用状态。</summary>
    private bool _contextIsEnabled;

    private const int ContextToggleId = 1;
    private const int ContextRemoveId = 2;

    /// <summary>模块在 TestSystem 下拉框中的显示名称。</summary>
    internal override string DisplayName => "技能测试";

    /// <summary>
    /// 模块初始化。
    /// <para>
    /// 服务在字段初始化时已完成技能库缓存，这里只负责构建 UI。
    /// </para>
    /// </summary>
    internal override void Initialize(TestSystem system)
    {
        base.Initialize(system);
        CacheUiNodes();
        RequestRefresh();
    }

    /// <summary>
    /// 当测试系统切换当前选中实体时，先解除旧实体监听，再接入新实体并刷新界面。
    /// </summary>
    internal override void OnSelectedEntityChanged(IEntity? entity)
    {
        UnsubscribeEntityEvents();
        base.OnSelectedEntityChanged(entity);
        SubscribeEntityEvents();
        RequestRefresh();
    }

    /// <summary>
    /// 当模块被切换到前台时恢复事件订阅并刷新一次，保证面板内容是最新的。
    /// </summary>
    internal override void OnActivated()
    {
        SubscribeEntityEvents();
        RequestRefresh();
    }

    /// <summary>
    /// 当模块离开前台时取消事件订阅，避免后台模块继续响应实体变化。
    /// </summary>
    internal override void OnDeactivated()
    {
        UnsubscribeEntityEvents();
    }

    /// <summary>
    /// 重新渲染当前实体的技能列表。
    /// </summary>
    internal override void Refresh()
    {
        RebuildAvailableList();
        RebuildCurrentList();
    }

    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _entityHintLabel = GetNode<Label>("EntityHintLabel");
        _statusLabel = GetNode<Label>("StatusLabel");
        _availableGroupContainer = GetNode<VBoxContainer>("Split/LeftBox/AvailableScroll/AvailableGroupContainer");
        _currentGroupContainer = GetNode<VBoxContainer>("Split/RightBox/CurrentScroll/CurrentGroupContainer");

        _ownedContextMenu = new PopupMenu();
        _ownedContextMenu.IdPressed += OnOwnedContextMenuItemPressed;
        AddChild(_ownedContextMenu);
    }

    // ───────────────── 刷新 ─────────────────

    /// <summary>
    /// 重建左侧技能库列表。
    /// </summary>
    private void RebuildAvailableList()
    {
        ClearChildren(_availableGroupContainer);

        var groups = _service.GetCatalogGroups(selectedEntity);
        foreach (var group in groups)
        {
            try
            {
                var section = InstantiateScene<AbilityGroupSection>(_groupSectionScene, nameof(AbilityGroupSection));
                section.SetTitle($"{group.GroupPath} ({group.Items.Count})"); // 绑定分组标题

                foreach (var item in group.Items)
                {
                    var itemControl = InstantiateScene<AbilityCatalogItemControl>(_catalogItemScene, nameof(AbilityCatalogItemControl));
                    itemControl.Configure(item); // 绑定技能库条目
                    itemControl.AddRequested += OnCatalogAddRequested;
                    section.AddItem(itemControl);
                }

                _availableGroupContainer.AddChild(section);
            }
            catch (Exception ex)
            {
                _log.Error($"[技能测试UI] 构建技能库分组失败: group={group.GroupPath} error={ex}");
            }
        }
    }

    /// <summary>
    /// 重建右侧当前技能列表，同时更新顶部实体提示。
    /// </summary>
    private void RebuildCurrentList()
    {
        ClearChildren(_currentGroupContainer);

        if (selectedEntity == null)
        {
            _entityHintLabel.Text = "请先选择一个实体";
            return;
        }

        var entityName = selectedEntity.Data.Get<string>(DataKey.Name);
        var groups = _service.GetOwnedGroups(selectedEntity);
        var totalCount = 0;

        foreach (var group in groups)
        {
            totalCount += group.Items.Count;

            try
            {
                var section = InstantiateScene<AbilityGroupSection>(_groupSectionScene, nameof(AbilityGroupSection));
                section.SetTitle($"{group.GroupPath} ({group.Items.Count})"); // 绑定当前技能分组标题

                foreach (var item in group.Items)
                {
                    var itemControl = InstantiateScene<AbilityOwnedItemControl>(_ownedItemScene, nameof(AbilityOwnedItemControl));
                    itemControl.Configure(item); // 绑定已拥有技能条目
                    itemControl.ToggleEnabledRequested += OnOwnedToggleRequested;
                    itemControl.RemoveRequested += OnOwnedRemoveRequested;
                    itemControl.ContextRequested += OnOwnedContextRequested;
                    section.AddItem(itemControl);
                }

                _currentGroupContainer.AddChild(section);
            }
            catch (Exception ex)
            {
                _log.Error($"[技能测试UI] 构建当前技能分组失败: group={group.GroupPath} error={ex}");
            }
        }

        _entityHintLabel.Text = $"实体: {entityName} | 技能数: {totalCount}";
    }

    // ───────────────── 操作 ─────────────────

    /// <summary>
    /// 处理左侧技能库添加请求。
    /// </summary>
    private void OnCatalogAddRequested(string resourceKey)
    {
        if (selectedEntity == null)
        {
            _log.Warn("[技能测试UI] 点击添加技能失败：当前没有选中实体");
            ShowStatus("请先选择一个实体");
            return;
        }

        _log.Info($"[技能测试UI] 点击添加技能: resourceKey={resourceKey}");
        var result = _service.AddAbility(selectedEntity, resourceKey);
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 处理技能启停请求。
    /// </summary>
    private void OnOwnedToggleRequested(string abilityId, bool targetEnabled)
    {
        if (selectedEntity == null)
        {
            ShowStatus("请先选择一个实体");
            return;
        }

        _log.Info($"[技能测试UI] 点击切换技能启用状态: abilityId={abilityId} targetEnabled={targetEnabled}");
        var result = _service.SetAbilityEnabled(selectedEntity, abilityId, targetEnabled);
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 处理技能移除请求。
    /// </summary>
    private void OnOwnedRemoveRequested(string abilityId)
    {
        if (selectedEntity == null)
        {
            ShowStatus("请先选择一个实体");
            return;
        }

        _log.Info($"[技能测试UI] 点击移除技能: abilityId={abilityId}");
        var result = _service.RemoveAbility(selectedEntity, abilityId);
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 处理右侧技能项右键请求，弹出上下文菜单。
    /// </summary>
    private void OnOwnedContextRequested(string abilityId, bool isEnabled, Vector2 globalPosition)
    {
        _contextAbilityId = abilityId;
        _contextIsEnabled = isEnabled;

        _ownedContextMenu.Clear();
        _ownedContextMenu.AddItem(isEnabled ? "禁用技能" : "启用技能", ContextToggleId);
        _ownedContextMenu.AddSeparator();
        _ownedContextMenu.AddItem("移除技能", ContextRemoveId);
        _ownedContextMenu.Position = (Vector2I)globalPosition; // 菜单弹出位置
        _ownedContextMenu.Popup();

        _log.Info($"[技能测试UI] 打开技能右键菜单: abilityId={abilityId} isEnabled={isEnabled}");
    }

    /// <summary>
    /// 处理右键菜单点击。
    /// </summary>
    private void OnOwnedContextMenuItemPressed(long id)
    {
        if (string.IsNullOrWhiteSpace(_contextAbilityId))
        {
            return;
        }

        switch (id)
        {
            case ContextToggleId:
                OnOwnedToggleRequested(_contextAbilityId, !_contextIsEnabled); // 技能实例Id/目标启用状态
                break;
            case ContextRemoveId:
                OnOwnedRemoveRequested(_contextAbilityId); // 技能实例Id
                break;
        }
    }

    /// <summary>
    /// 更新状态栏文本，用于显示当前操作结果。
    /// </summary>
    private void ShowStatus(string message)
    {
        _statusLabel.Text = message;
    }

    /// <summary>
    /// 清空列表容器中的旧子节点。
    /// </summary>
    private static void ClearChildren(VBoxContainer container)
    {
        foreach (Node child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    /// <summary>
    /// 实例化一个技能测试 UI 场景。
    /// </summary>
    private static T InstantiateScene<T>(PackedScene? scene, string sceneName) where T : Node
    {
        if (scene == null)
        {
            throw new InvalidOperationException($"技能测试场景未配置: {sceneName}");
        }

        var instance = scene.Instantiate<T>();
        if (instance == null)
        {
            throw new InvalidOperationException($"技能测试场景实例化失败: {sceneName}");
        }

        return instance;
    }

    // ───────────────── 事件订阅 ─────────────────

    /// <summary>
    /// 订阅当前选中实体的技能与 Feature 状态事件。
    /// <para>
    /// 当实体技能发生变化时，界面需要自动刷新，避免用户看到过期列表。
    /// </para>
    /// </summary>
    private void SubscribeEntityEvents()
    {
        if (selectedEntity == null || ReferenceEquals(selectedEntity, _subscribedEntity))
        {
            return;
        }

        _subscribedEntity = selectedEntity;
        _subscribedEntity.Events.On<GameEventType.Ability.AddedEventData>(
            GameEventType.Ability.Added, OnAbilityChanged);
        _subscribedEntity.Events.On<GameEventType.Ability.RemovedEventData>(
            GameEventType.Ability.Removed, OnAbilityRemovedEvt);
        _subscribedEntity.Events.On<GameEventType.Feature.EnabledEventData>(
            GameEventType.Feature.Enabled, OnFeatureEnabled);
        _subscribedEntity.Events.On<GameEventType.Feature.DisabledEventData>(
            GameEventType.Feature.Disabled, OnFeatureDisabled);
    }

    /// <summary>
    /// 取消当前实体的技能事件订阅。
    /// </summary>
    private void UnsubscribeEntityEvents()
    {
        if (_subscribedEntity == null)
        {
            return;
        }

        _subscribedEntity.Events.Off<GameEventType.Ability.AddedEventData>(
            GameEventType.Ability.Added, OnAbilityChanged);
        _subscribedEntity.Events.Off<GameEventType.Ability.RemovedEventData>(
            GameEventType.Ability.Removed, OnAbilityRemovedEvt);
        _subscribedEntity.Events.Off<GameEventType.Feature.EnabledEventData>(
            GameEventType.Feature.Enabled, OnFeatureEnabled);
        _subscribedEntity.Events.Off<GameEventType.Feature.DisabledEventData>(
            GameEventType.Feature.Disabled, OnFeatureDisabled);
        _subscribedEntity = null;
    }

    /// <summary>
    /// 技能新增后的统一刷新回调。
    /// </summary>
    private void OnAbilityChanged(GameEventType.Ability.AddedEventData _)
    {
        if (Visible)
        {
            RequestRefresh();
        }
    }

    /// <summary>
    /// 技能移除后的统一刷新回调。
    /// </summary>
    private void OnAbilityRemovedEvt(GameEventType.Ability.RemovedEventData _)
    {
        if (Visible)
        {
            RequestRefresh();
        }
    }

    /// <summary>
    /// 技能启停状态变化后的刷新回调。
    /// </summary>
    private void OnFeatureEnabled(GameEventType.Feature.EnabledEventData _)
    {
        RefreshVisibleModule();
    }

    /// <summary>
    /// 技能禁用后的刷新回调。
    /// </summary>
    private void OnFeatureDisabled(GameEventType.Feature.DisabledEventData _)
    {
        RefreshVisibleModule();
    }

    /// <summary>
    /// 仅在模块可见时刷新，避免后台模块做无意义重绘。
    /// </summary>
    private void RefreshVisibleModule()
    {
        if (Visible)
        {
            RequestRefresh();
        }
    }

    /// <summary>
    /// 请求在当前 UI 事件处理完成后统一刷新一次，避免列表正在处理交互时立刻重建。
    /// </summary>
    private void RequestRefresh()
    {
        if (_refreshQueued)
        {
            return;
        }

        _refreshQueued = true;
        CallDeferred(nameof(FlushRefresh));
    }

    /// <summary>
    /// 执行延迟刷新。
    /// </summary>
    private void FlushRefresh()
    {
        _refreshQueued = false;

        if (!IsInsideTree())
        {
            return;
        }

        Refresh();
    }
}
