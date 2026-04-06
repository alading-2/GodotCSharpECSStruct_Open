using Godot;
using System.Collections.Generic;

/// <summary>
/// 技能测试模块。
/// <para>
/// 提供一个运行时调试界面，用于查看当前实体已挂载的技能、从资源列表中添加技能、
/// 移除技能以及切换技能启用状态。
/// </para>
/// <para>
/// 该模块本身不负责技能逻辑执行，只负责把编辑器/资源层的技能配置与实体运行时能力列表连接起来。
/// </para>
/// </summary>
public partial class AbilityTestModule : TestModuleBase
{
    /// <summary>左侧“可用技能配置”列表，用于从资源库中挑选要添加的技能。</summary>
    private ItemList _availableList = null!;

    /// <summary>右侧“当前技能”列表，展示当前实体已经拥有的技能实例。</summary>
    private ItemList _currentList = null!;

    /// <summary>提示当前是否已经选中实体的说明标签。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>操作反馈区，用于显示添加 / 移除 / 切换启用状态的结果。</summary>
    private Label _statusLabel = null!;

    /// <summary>
    /// 缓存所有可用技能配置（显示名 → Resource）。
    /// <para>
    /// 这里直接缓存 Resource 实例，避免每次点击按钮都重新加载配置资源。
    /// </para>
    /// </summary>
    private readonly List<(string DisplayName, Resource Config)> _allConfigs = new();

    /// <summary>当前已经为其订阅技能事件的实体，避免重复订阅同一个目标。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>模块在 TestSystem 下拉框中的显示名称。</summary>
    internal override string DisplayName => "技能测试";

    /// <summary>
    /// 模块初始化。
    /// <para>
    /// 先加载全部技能配置，再构建 UI，确保界面列表和资源数据保持一致。
    /// </para>
    /// </summary>
    internal override void Initialize(TestSystem system)
    {
        base.Initialize(system);
        LoadAllAbilityConfigs();
        BuildUi();
    }

    /// <summary>
    /// 当测试系统切换当前选中实体时，先解除旧实体监听，再接入新实体并刷新界面。
    /// </summary>
    internal override void OnSelectedEntityChanged(IEntity? entity)
    {
        UnsubscribeEntityEvents();
        base.OnSelectedEntityChanged(entity);
        SubscribeEntityEvents();
        Refresh();
    }

    /// <summary>
    /// 当模块被切换到前台时恢复事件订阅并刷新一次，保证面板内容是最新的。
    /// </summary>
    internal override void OnActivated()
    {
        SubscribeEntityEvents();
        Refresh();
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
        RefreshCurrentAbilityList();
    }

    // ───────────────── 初始化 ─────────────────

    /// <summary>
    /// 从资源表中加载全部技能配置，并抽取可显示名称。
    /// <para>
    /// 这里使用反射读取配置里的 Name 字段，和实体侧的技能创建逻辑保持一致。
    /// </para>
    /// </summary>
    private void LoadAllAbilityConfigs()
    {
        if (!ResourcePaths.Resources.TryGetValue(ResourceCategory.DataAbility, out var entries))
        {
            return;
        }

        foreach (var (resKey, _) in entries)
        {
            var config = ResourceManagement.Load<Resource>(resKey, ResourceCategory.DataAbility);
            if (config == null)
            {
                continue;
            }

            // 通过反射取 Name 属性（与 EntityManager_Ability 内部逻辑一致）
            var nameProp = config.GetType().GetProperty(DataKey.Name);
            var abilityName = nameProp?.GetValue(config) as string;
            _allConfigs.Add((string.IsNullOrEmpty(abilityName) ? resKey : abilityName, config));
        }
    }

    /// <summary>
    /// 构建技能测试模块 UI。
    /// <para>
    /// 左侧是可添加配置，右侧是当前实体技能列表，中间通过按钮完成添加、移除和启用切换。
    /// </para>
    /// </summary>
    private void BuildUi()
    {
        _entityHintLabel = new Label { Text = "请先选择一个实体" };
        AddChild(_entityHintLabel);

        _statusLabel = new Label { Text = "" };
        AddChild(_statusLabel);

        // 操作按钮行
        var buttonRow = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(buttonRow);

        var addBtn = new Button
        {
            Text = "添加 ▶",
            TooltipText = "将左侧选中配置添加到当前实体",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        addBtn.Pressed += OnAddPressed;
        buttonRow.AddChild(addBtn);

        var removeBtn = new Button
        {
            Text = "移除",
            TooltipText = "移除右侧选中技能",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        removeBtn.Pressed += OnRemovePressed;
        buttonRow.AddChild(removeBtn);

        var toggleBtn = new Button
        {
            Text = "切换启用",
            TooltipText = "启用/禁用右侧选中技能",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        toggleBtn.Pressed += OnToggleEnabledPressed;
        buttonRow.AddChild(toggleBtn);

        // 双列表布局
        var split = new HSplitContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        AddChild(split);

        // ── 左侧：可用技能配置 ──
        var leftBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        split.AddChild(leftBox);
        leftBox.AddChild(new Label { Text = "可用技能配置" });

        _availableList = new ItemList
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = ItemList.SelectModeEnum.Single
        };
        leftBox.AddChild(_availableList);

        foreach (var (name, _) in _allConfigs)
        {
            _availableList.AddItem(name);
        }

        // ── 右侧：当前实体技能 ──
        var rightBox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        split.AddChild(rightBox);
        rightBox.AddChild(new Label { Text = "当前技能" });

        _currentList = new ItemList
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = ItemList.SelectModeEnum.Single
        };
        rightBox.AddChild(_currentList);
    }

    // ───────────────── 刷新 ─────────────────

    /// <summary>
    /// 刷新右侧当前技能列表，同时更新顶部实体提示与状态文本。
    /// </summary>
    private void RefreshCurrentAbilityList()
    {
        _currentList.Clear();

        if (selectedEntity == null)
        {
            _entityHintLabel.Text = "请先选择一个实体";
            _statusLabel.Text = "";
            return;
        }

        var entityName = selectedEntity.Data.Get<string>(DataKey.Name);
        var abilities = EntityManager.GetAbilities(selectedEntity);
        _entityHintLabel.Text = $"实体: {entityName} | 技能数: {abilities.Count}";

        foreach (var ability in abilities)
        {
            var abilityName = ability.Data.Get<string>(DataKey.Name);
            var enabled = ability.Data.Get<bool>(DataKey.FeatureEnabled);
            var abilityType = (AbilityType)ability.Data.Get<int>(DataKey.AbilityType);
            _currentList.AddItem($"[{(enabled ? "✓" : "✗")}] {abilityName}  ({abilityType})");
        }
    }

    // ───────────────── 操作 ─────────────────

    /// <summary>
    /// 将左侧选中的技能配置添加到当前实体。
    /// </summary>
    private void OnAddPressed()
    {
        if (selectedEntity == null)
        {
            ShowStatus("请先选择一个实体");
            return;
        }

        var selected = _availableList.GetSelectedItems();
        if (selected.Length == 0)
        {
            ShowStatus("请在左侧选择要添加的技能配置");
            return;
        }

        var (displayName, config) = _allConfigs[selected[0]];
        var ability = EntityManager.AddAbility(selectedEntity, config);
        ShowStatus(ability != null ? $"已添加: {displayName}" : $"添加失败 (可能已拥有): {displayName}");
        Refresh();
    }

    /// <summary>
    /// 移除右侧选中的技能实例。
    /// </summary>
    private void OnRemovePressed()
    {
        if (selectedEntity == null)
        {
            return;
        }

        var selected = _currentList.GetSelectedItems();
        if (selected.Length == 0)
        {
            ShowStatus("请在右侧选择要移除的技能");
            return;
        }

        var abilities = EntityManager.GetAbilities(selectedEntity);
        var idx = selected[0];
        if (idx >= abilities.Count)
        {
            return;
        }

        var abilityName = abilities[idx].Data.Get<string>(DataKey.Name);
        EntityManager.RemoveAbility(selectedEntity, abilityName);
        ShowStatus($"已移除: {abilityName}");
        Refresh();
    }

    /// <summary>
    /// 在启用 / 禁用之间切换当前技能。
    /// </summary>
    private void OnToggleEnabledPressed()
    {
        if (selectedEntity == null)
        {
            return;
        }

        var selected = _currentList.GetSelectedItems();
        if (selected.Length == 0)
        {
            ShowStatus("请在右侧选择要切换的技能");
            return;
        }

        var abilities = EntityManager.GetAbilities(selectedEntity);
        var idx = selected[0];
        if (idx >= abilities.Count)
        {
            return;
        }

        var ability = abilities[idx];
        var abilityName = ability.Data.Get<string>(DataKey.Name);
        var currentEnabled = ability.Data.Get<bool>(DataKey.FeatureEnabled);
        if (currentEnabled)
            FeatureSystem.DisableFeature(ability, selectedEntity);
        else
            FeatureSystem.EnableFeature(ability, selectedEntity);

        ShowStatus($"{(currentEnabled ? "已禁用" : "已启用")}: {abilityName}");
        Refresh();
    }

    /// <summary>
    /// 更新状态栏文本，用于显示当前操作结果。
    /// </summary>
    private void ShowStatus(string message)
    {
        _statusLabel.Text = message;
    }

    // ───────────────── 事件订阅 ─────────────────

    /// <summary>
    /// 订阅当前选中实体的技能增删事件。
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
        _subscribedEntity = null;
    }

    /// <summary>
    /// 技能新增后的统一刷新回调。
    /// </summary>
    private void OnAbilityChanged(GameEventType.Ability.AddedEventData _)
    {
        if (Visible)
        {
            Refresh();
        }
    }

    /// <summary>
    /// 技能移除后的统一刷新回调。
    /// </summary>
    private void OnAbilityRemovedEvt(GameEventType.Ability.RemovedEventData _)
    {
        if (Visible)
        {
            Refresh();
        }
    }
}
