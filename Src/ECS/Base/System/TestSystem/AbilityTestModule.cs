using Godot;

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

    /// <summary>右键菜单命令：切换启用状态。</summary>
    private const int AbilityMenuToggleEnabled = 1;

    /// <summary>右键菜单命令：移除技能。</summary>
    private const int AbilityMenuRemove = 2;

    /// <summary>技能测试服务，负责缓存技能目录和执行业务操作。</summary>
    private readonly AbilityTestService _service = new();

    /// <summary>左侧技能库树，按分组路径展示所有可添加技能。</summary>
    private Tree _availableTree = null!;

    /// <summary>右侧当前技能树，按分组路径展示当前实体已拥有技能。</summary>
    private Tree _currentTree = null!;

    /// <summary>提示当前是否已经选中实体的说明标签。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>操作反馈区，用于显示添加 / 移除 / 切换启用状态的结果。</summary>
    private Label _statusLabel = null!;

    /// <summary>右侧技能上下文菜单，用于右键启用 / 禁用 / 移除。</summary>
    private PopupMenu _abilityContextMenu = null!;

    /// <summary>当前已经为其订阅技能事件的实体，避免重复订阅同一个目标。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>当前右键菜单指向的技能实例 ID。</summary>
    private string? _contextAbilityId;

    /// <summary>是否已经排队等待一次延迟刷新，避免同一帧重复重建 Tree。</summary>
    private bool _refreshQueued;

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
        BindUiEvents();
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
        RebuildAvailableTree();
        RebuildCurrentTree();
    }

    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _entityHintLabel = GetNode<Label>("EntityHintLabel");
        _statusLabel = GetNode<Label>("StatusLabel");
        _availableTree = GetNode<Tree>("Split/LeftBox/AvailableTree");
        _currentTree = GetNode<Tree>("Split/RightBox/CurrentTree");
        _abilityContextMenu = GetNode<PopupMenu>("AbilityContextMenu");
    }

    private void BindUiEvents()
    {
        _availableTree.Connect("item_selected", Callable.From(OnAvailableTreeItemSelected));
        _currentTree.GuiInput += OnCurrentTreeGuiInput;
        _abilityContextMenu.IdPressed += OnAbilityContextMenuPressed;
    }

    // ───────────────── 刷新 ─────────────────

    /// <summary>
    /// 重建左侧技能库树。
    /// </summary>
    private void RebuildAvailableTree()
    {
        _availableTree.Clear();
        var root = _availableTree.CreateItem();

        var groups = _service.GetCatalogGroups(selectedEntity);
        foreach (var group in groups)
        {
            var categoryItem = _availableTree.CreateItem(root);
            categoryItem.SetText(0, $"{group.GroupPath} ({group.Items.Count})");
            categoryItem.SetSelectable(0, false);
            categoryItem.Collapsed = false;

            foreach (var item in group.Items)
            {
                var abilityItem = _availableTree.CreateItem(categoryItem);
                abilityItem.SetText(0, BuildCatalogItemText(item));
                abilityItem.SetTooltipText(0, BuildCatalogTooltip(item));
                abilityItem.SetSelectable(0, true);
                abilityItem.SetMetadata(0, item.ResourceKey);

                if (item.IsOwned)
                {
                    abilityItem.SetCustomColor(0, new Color(0.6f, 0.6f, 0.6f));
                }
            }
        }
    }

    /// <summary>
    /// 重建右侧当前技能树，同时更新顶部实体提示。
    /// </summary>
    private void RebuildCurrentTree()
    {
        _currentTree.Clear();
        var root = _currentTree.CreateItem();

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

            var categoryItem = _currentTree.CreateItem(root);
            categoryItem.SetText(0, $"{group.GroupPath} ({group.Items.Count})");
            categoryItem.SetSelectable(0, false);
            categoryItem.Collapsed = false;

            foreach (var item in group.Items)
            {
                var abilityItem = _currentTree.CreateItem(categoryItem);
                abilityItem.SetText(0, BuildOwnedItemText(item));
                abilityItem.SetMetadata(0, item.AbilityId);
                abilityItem.SetTooltipText(0, BuildOwnedTooltip(item));

                if (!item.IsEnabled)
                {
                    abilityItem.SetCustomColor(0, new Color(0.65f, 0.65f, 0.65f));
                }
            }
        }

        _entityHintLabel.Text = $"实体: {entityName} | 技能数: {totalCount}";
    }

    // ───────────────── 操作 ─────────────────

    /// <summary>
    /// 左侧技能树选中处理。
    /// <para>
    /// 直接订阅 Tree 的 item_selected 信号，避免依赖 GuiInput 坐标命中导致点击叶子不触发。
    /// </para>
    /// </summary>
    private void OnAvailableTreeItemSelected()
    {
        HandleAvailableTreeSelection();
    }

    /// <summary>
    /// 右侧当前技能树点击处理。
    /// </summary>
    /// <param name="inputEvent">树控件输入事件。</param>
    private void OnCurrentTreeGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseEvent)
        {
            return;
        }

        if (!mouseEvent.Pressed)
        {
            return;
        }

        var item = _currentTree.GetItemAtPosition(mouseEvent.Position);
        if (!TryGetItemMetadata(item, out var abilityId))
        {
            return;
        }

        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            _log.Info($"[技能测试UI] 点击移除技能: abilityId={abilityId}");
            var result = _service.RemoveAbility(selectedEntity, abilityId);
            ShowStatus(result.Message);
            RequestRefresh();
            return;
        }

        if (mouseEvent.ButtonIndex != MouseButton.Right)
        {
            return;
        }

        if (!_service.TryGetOwnedItem(selectedEntity, abilityId, out var itemView))
        {
            return;
        }

        _log.Info($"[技能测试UI] 打开技能右键菜单: ability={itemView.DisplayName} abilityId={itemView.AbilityId} enabled={itemView.IsEnabled}");
        OpenAbilityContextMenu(itemView, mouseEvent.Position);
    }

    /// <summary>
    /// 处理左侧技能树当前选中项。
    /// <para>
    /// 通过 deferred 等待 Tree 完成内部选中状态更新，避免点击坐标和真实选中项不一致。
    /// </para>
    /// </summary>
    private void HandleAvailableTreeSelection()
    {
        if (selectedEntity == null)
        {
            _log.Warn("[技能测试UI] 左侧点击添加技能失败：当前没有选中实体");
            ShowStatus("请先选择一个实体");
            return;
        }

        var item = _availableTree.GetSelected();
        if (!TryGetItemMetadata(item, out var resourceKey))
        {
            if (item != null)
            {
                _log.Debug($"[技能测试UI] 左侧树选中项无技能元数据: text={item.GetText(0)}");
            }
            return;
        }

        _log.Info($"[技能测试UI] 点击添加技能: resourceKey={resourceKey}");
        var result = _service.AddAbility(selectedEntity, resourceKey);
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 更新状态栏文本，用于显示当前操作结果。
    /// </summary>
    private void ShowStatus(string message)
    {
        _statusLabel.Text = message;
    }

    /// <summary>
    /// 打开技能右键菜单。
    /// </summary>
    /// <param name="itemView">当前右键命中的技能视图。</param>
    /// <param name="localPosition">鼠标在右侧技能树中的本地坐标。</param>
    private void OpenAbilityContextMenu(AbilityOwnedItemView itemView, Vector2 localPosition)
    {
        _contextAbilityId = itemView.AbilityId;

        _abilityContextMenu.Clear();
        _abilityContextMenu.AddItem(itemView.IsEnabled ? "禁用技能" : "启用技能", AbilityMenuToggleEnabled);
        _abilityContextMenu.AddSeparator();
        _abilityContextMenu.AddItem("移除技能", AbilityMenuRemove);

        _abilityContextMenu.Position = (Vector2I)(_currentTree.GetGlobalPosition() + localPosition); // 菜单弹到鼠标附近
        _abilityContextMenu.Popup();
    }

    /// <summary>
    /// 处理右键菜单动作。
    /// </summary>
    /// <param name="id">菜单命令 Id。</param>
    private void OnAbilityContextMenuPressed(long id)
    {
        if (string.IsNullOrWhiteSpace(_contextAbilityId))
        {
            return;
        }

        if (!_service.TryGetOwnedItem(selectedEntity, _contextAbilityId, out var itemView))
        {
            return;
        }

        _log.Info($"[技能测试UI] 执行右键菜单: ability={itemView.DisplayName} abilityId={itemView.AbilityId} action={id}");
        AbilityActionResult result = id switch
        {
            AbilityMenuToggleEnabled => _service.SetAbilityEnabled(
                selectedEntity,
                _contextAbilityId!,
                !itemView.IsEnabled),
            AbilityMenuRemove => _service.RemoveAbility(
                selectedEntity,
                _contextAbilityId!),
            _ => default
        };

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            ShowStatus(result.Message);
            RequestRefresh();
        }
    }

    /// <summary>
    /// 尝试从树节点读取叶子元数据。
    /// </summary>
    /// <param name="item">要读取的树节点。</param>
    /// <param name="metadata">输出的元数据字符串。</param>
    /// <returns>读取到非空元数据时返回 true。</returns>
    private static bool TryGetItemMetadata(TreeItem? item, out string metadata)
    {
        metadata = string.Empty;
        if (item == null)
        {
            return false;
        }

        var raw = item.GetMetadata(0);
        metadata = raw.AsString();
        return !string.IsNullOrWhiteSpace(metadata);
    }

    /// <summary>
    /// 构建左侧技能库条目文本。
    /// </summary>
    private static string BuildCatalogItemText(AbilityCatalogItemView item)
    {
        var ownedFlag = item.IsOwned ? " [已拥有]" : string.Empty;
        return $"{item.DisplayName}{ownedFlag}  ({item.AbilityType}/{item.TriggerMode})";
    }

    /// <summary>
    /// 构建右侧已拥有技能条目文本。
    /// </summary>
    private static string BuildOwnedItemText(AbilityOwnedItemView item)
    {
        var enabledFlag = item.IsEnabled ? "启用" : "禁用";
        return $"{item.DisplayName} [{enabledFlag}]  ({item.AbilityType}/{item.TriggerMode})";
    }

    /// <summary>
    /// 构建左侧技能条目提示文本。
    /// </summary>
    private static string BuildCatalogTooltip(AbilityCatalogItemView item)
    {
        return $"{item.DisplayName}\n分组: {item.GroupPath}\n类型: {item.AbilityType}\n触发: {item.TriggerMode}\n\n{item.Description}";
    }

    /// <summary>
    /// 构建右侧技能条目提示文本。
    /// </summary>
    private static string BuildOwnedTooltip(AbilityOwnedItemView item)
    {
        var enabledText = item.IsEnabled ? "是" : "否";
        return $"{item.DisplayName}\n分组: {item.GroupPath}\n类型: {item.AbilityType}\n触发: {item.TriggerMode}\n启用: {enabledText}\n\n{item.Description}";
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
    /// 请求在当前 UI 事件处理完成后统一刷新一次，避免 Tree 正在处理交互时立刻重建。
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
