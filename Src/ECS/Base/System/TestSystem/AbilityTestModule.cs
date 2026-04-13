using Godot;
using System;

/// <summary>
/// 技能测试模块。
/// <para>
/// 提供一个运行时调试界面，用于按分组路径查看技能库，并给当前实体执行添加 / 移除 / 启停。
/// 每个技能条目由独立场景资产承载，避免把 TreeItem 当成不可复用的程序化 UI。
/// </para>
/// </summary>
public partial class AbilityTestModule : TestModuleBase
{
    /// <summary>技能测试模块日志器。</summary>
    private static readonly Log _log = new(nameof(AbilityTestModule));

    /// <summary>技能测试服务，负责缓存技能目录和执行业务操作。</summary>
    private readonly AbilityTestService _service = new();

    [Export] private PackedScene? _groupPanelScene;
    [Export] private PackedScene? _catalogCardScene;
    [Export] private PackedScene? _ownedCardScene;

    /// <summary>左侧技能库分组宿主。</summary>
    private VBoxContainer _availableGroups = null!;

    /// <summary>右侧当前技能分组宿主。</summary>
    private VBoxContainer _currentGroups = null!;

    /// <summary>提示当前是否已经选中实体的说明标签。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>操作反馈区。</summary>
    private Label _statusLabel = null!;

    /// <summary>当前已经为其订阅技能事件的实体。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>模块在 TestSystem 下拉框中的显示名称。</summary>
    internal override string DisplayName => "技能测试";

    /// <summary>
    /// 模块初始化。
    /// </summary>
    internal override void Initialize(ITestModuleHost host)
    {
        base.Initialize(host);
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
        if (Visible)
        {
            SubscribeEntityEvents();
        }

        RequestRefresh();
    }

    /// <summary>
    /// 当模块被切换到前台时恢复事件订阅并刷新一次。
    /// </summary>
    internal override void OnActivated()
    {
        SubscribeEntityEvents();
        RequestRefresh();
    }

    /// <summary>
    /// 当模块离开前台时取消事件订阅。
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
        _availableGroups = GetNode<VBoxContainer>("Split/LeftBox/AvailableScroll/AvailableGroups");
        _currentGroups = GetNode<VBoxContainer>("Split/RightBox/CurrentScroll/CurrentGroups");
    }

    /// <summary>
    /// 重建左侧技能库列表。
    /// </summary>
    private void RebuildAvailableList()
    {
        ClearChildren(_availableGroups);

        var groups = _service.GetCatalogGroups(selectedEntity);
        var canAdd = selectedEntity != null;

        foreach (var group in groups)
        {
            var panel = InstantiateScene<AbilityGroupPanel>(_groupPanelScene, nameof(AbilityGroupPanel));
            panel.SetTitle($"{group.GroupPath} ({group.Items.Count})");

            foreach (var item in group.Items)
            {
                var card = InstantiateScene<AbilityCatalogCard>(_catalogCardScene, nameof(AbilityCatalogCard));
                card.Bind(item, canAdd, HandleAddAbility);
                panel.AddItem(card);
            }

            _availableGroups.AddChild(panel);
        }
    }

    /// <summary>
    /// 重建右侧当前技能列表，同时更新顶部实体提示。
    /// </summary>
    private void RebuildCurrentList()
    {
        ClearChildren(_currentGroups);

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

            var panel = InstantiateScene<AbilityGroupPanel>(_groupPanelScene, nameof(AbilityGroupPanel));
            panel.SetTitle($"{group.GroupPath} ({group.Items.Count})");

            foreach (var item in group.Items)
            {
                var card = InstantiateScene<AbilityOwnedCard>(_ownedCardScene, nameof(AbilityOwnedCard));
                card.Bind(
                    item, // item
                    HandleToggleAbilityEnabled, // onToggleRequested
                    HandleRemoveAbility // onRemoveRequested
                );
                panel.AddItem(card);
            }

            _currentGroups.AddChild(panel);
        }

        _entityHintLabel.Text = $"实体: {entityName} | 技能数: {totalCount}";
    }

    /// <summary>
    /// 添加技能。
    /// </summary>
    private void HandleAddAbility(string resourceKey)
    {
        if (selectedEntity == null)
        {
            ShowStatus("请先选择一个实体");
            return;
        }

        _log.Info($"[技能测试UI] 点击添加技能: resourceKey={resourceKey}");
        var result = _service.AddAbility(selectedEntity, resourceKey);
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 移除技能。
    /// </summary>
    private void HandleRemoveAbility(string abilityId)
    {
        _log.Info($"[技能测试UI] 点击移除技能: abilityId={abilityId}");
        var result = _service.RemoveAbility(selectedEntity, abilityId);
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 切换技能启停状态。
    /// </summary>
    private void HandleToggleAbilityEnabled(string abilityId, bool isEnabled)
    {
        _log.Info($"[技能测试UI] 切换技能启停: abilityId={abilityId} enabled={isEnabled}");
        var result = _service.SetAbilityEnabled(
            selectedEntity, // owner
            abilityId, // abilityId
            isEnabled // isEnabled
        );
        ShowStatus(result.Message);
        RequestRefresh();
    }

    /// <summary>
    /// 更新状态栏文本。
    /// </summary>
    private void ShowStatus(string message)
    {
        _statusLabel.Text = message;
    }

    /// <summary>
    /// 订阅当前选中实体的技能与 Feature 状态事件。
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
    /// 技能启用后的刷新回调。
    /// </summary>
    private void OnFeatureEnabled(GameEventType.Feature.EnabledEventData _)
    {
        if (Visible)
        {
            RequestRefresh();
        }
    }

    /// <summary>
    /// 技能禁用后的刷新回调。
    /// </summary>
    private void OnFeatureDisabled(GameEventType.Feature.DisabledEventData _)
    {
        if (Visible)
        {
            RequestRefresh();
        }
    }

    private static void ClearChildren(Node host)
    {
        foreach (var child in host.GetChildren())
        {
            child.QueueFree();
        }
    }

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
}
