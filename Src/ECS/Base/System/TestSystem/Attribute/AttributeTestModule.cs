using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 属性测试模块。
/// <para>
/// 提供一个运行时调试面板，用于按分类查看并直接编辑实体的 Data 属性。
/// </para>
/// <para>
/// 这个模块不关心具体属性如何计算，只负责把 DataRegistry 中的元数据转换成可交互 UI。
/// </para>
/// </summary>
public partial class AttributeTestModule : TestModuleBase
{
    private static readonly Log _log = new(nameof(AttributeTestModule));

    /// <summary>单个分类页的缓存结构，保存分类标题、显示顺序和快速查找字典。</summary>
    private sealed record CategoryEntry(
        string Title,
        List<DataMeta> Metas,
        Dictionary<string, DataMeta> MetaByKey
    );

    /// <summary>Feature 调试服务，用于临时 Modifier 的挂载、读取与清理。</summary>
    private readonly FeatureDebugService _featureDebugService = new();

    /// <summary>所有可编辑分类的缓存列表，初始化后用于驱动左侧分类面板。</summary>
    private readonly List<CategoryEntry> _categories = new();

    /// <summary>当前分类已渲染的属性行缓存，键为 DataKey。</summary>
    private readonly Dictionary<string, AttributeEditorRow> _rowsByKey = new(StringComparer.Ordinal);

    /// <summary>当前帧内累计脏掉的属性键，延迟到帧末统一 patch。</summary>
    private readonly HashSet<string> _dirtyMetaKeys = new(StringComparer.Ordinal);

    /// <summary>属性词条场景。</summary>
    [Export] private PackedScene? _editorRowScene;

    /// <summary>布尔属性编辑器场景。</summary>
    [Export] private PackedScene? _checkEditorScene;

    /// <summary>下拉属性编辑器场景。</summary>
    [Export] private PackedScene? _optionEditorScene;

    /// <summary>数值属性编辑器场景。</summary>
    [Export] private PackedScene? _numericEditorScene;

    /// <summary>文本属性编辑器场景。</summary>
    [Export] private PackedScene? _textEditorScene;

    /// <summary>临时加成编辑器场景。</summary>
    [Export] private PackedScene? _modifierEditorScene;

    /// <summary>左侧分类列表。</summary>
    private ItemList _categoryList = null!;

    /// <summary>右侧编辑器容器，当前分类下的所有属性编辑行都会动态挂在这里。</summary>
    private VBoxContainer _editorContainer = null!;

    /// <summary>顶部实体提示文字，用于显示当前编辑目标。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>当前订阅了 Data 变化事件的实体，避免重复订阅。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>当前是否需要对整个分类区域执行重建。</summary>
    private bool _rebuildRequested = true;

    /// <summary>当前已渲染的分类索引；分类变化时必须整页重建一次。</summary>
    private int _renderedCategoryIndex = -1;

    /// <summary>模块定义信息。</summary>
    internal override TestModuleDefinition Definition => new(
        "attribute", // 模块稳定 Id
        "属性测试", // 模块显示名
        100 // 模块排序
    );

    /// <summary>
    /// 模块初始化。
    /// <para>
    /// 先整理分类数据，再构建 UI；如果存在分类，默认选中第一个分类，保证界面打开后立即可用。
    /// </para>
    /// </summary>
    internal override void Initialize(ITestModuleContext context)
    {
        base.Initialize(context);
        BuildCategoryData();
        CacheUiNodes();
        PopulateCategoryList();
        if (_categories.Count > 0)
        {
            _categoryList.Select(0);
        }

        RequestFullRefresh();
    }

    /// <summary>
    /// 选中实体变化时，先解除旧订阅，再绑定新实体并标记整页重建。
    /// </summary>
    internal override void OnSelectedEntityChanged(IEntity? entity)
    {
        UnsubscribeEntityEvents();
        base.OnSelectedEntityChanged(entity);
        _dirtyMetaKeys.Clear();
        _renderedCategoryIndex = -1;
        _rebuildRequested = true;

        if (IsModuleActive)
        {
            SubscribeEntityEvents();
            QueueRefresh();
        }
    }

    /// <summary>
    /// 模块被切换到前台时，恢复订阅并在帧末刷新一次。
    /// </summary>
    internal override void OnActivated()
    {
        base.OnActivated();
        SubscribeEntityEvents();
        RequestFullRefresh();
    }

    /// <summary>
    /// 模块离开前台时，取消订阅，避免后台界面持续响应数据变化。
    /// </summary>
    internal override void OnDeactivated()
    {
        base.OnDeactivated();
        UnsubscribeEntityEvents();
        _dirtyMetaKeys.Clear();
    }

    /// <summary>
    /// 外部强制刷新时，统一走重建请求，避免立刻在当前回调栈内重建 UI。
    /// </summary>
    internal override void Refresh()
    {
        RequestFullRefresh();
    }

    /// <summary>
    /// 收集所有可编辑分类的数据元信息。
    /// <para>
    /// 这里按业务域分组，保证左侧列表的分类顺序稳定且符合玩家理解习惯。
    /// </para>
    /// </summary>
    private void BuildCategoryData()
    {
        AddCategory("生命", DataCategory_Attribute.Health);
        AddCategory("魔法", DataCategory_Attribute.Mana);
        AddCategory("攻击", DataCategory_Attribute.Attack);
        AddCategory("防御", DataCategory_Attribute.Defense);
        AddCategory("技能", DataCategory_Attribute.Skill);
        AddCategory("移动", DataCategory_Attribute.Movement);
        AddCategory("闪避", DataCategory_Attribute.Dodge);
        AddCategory("暴击", DataCategory_Attribute.Crit);
        AddCategory("资源", DataCategory_Attribute.Resource);
        AddCategory("状态", DataCategory_Unit.State);
        AddCategory("恢复控制", DataCategory_Unit.Recovery);
    }

    /// <summary>
    /// 将某个分类中可编辑的 DataMeta 收集进缓存。
    /// <para>
    /// 只保留真正适合在运行时调试面板直接修改的元数据，避免把计算项或不可编辑项放进界面。
    /// </para>
    /// </summary>
    private void AddCategory(string title, Enum category)
    {
        var metas = DataRegistry.GetCachedMetaByCategory(category)
            .Where(IsEditableMeta)
            .OrderBy(meta => meta.DisplayName)
            .ToList();

        if (metas.Count == 0)
        {
            return;
        }

        var metaByKey = new Dictionary<string, DataMeta>(StringComparer.Ordinal);
        foreach (var meta in metas)
        {
            metaByKey[GetMetaKey(meta)] = meta;
        }

        _categories.Add(new CategoryEntry(title, metas, metaByKey));
    }

    /// <summary>
    /// 判断某个元数据是否适合在运行时编辑。
    /// <para>
    /// 计算项、不可枚举的复杂类型等不会出现在调试面板中。
    /// </para>
    /// </summary>
    private static bool IsEditableMeta(DataMeta meta)
    {
        if (meta.IsComputed)
        {
            return false;
        }

        return meta.IsBoolean || meta.IsNumeric || meta.IsEnum || meta.IsString || meta.HasOptions;
    }

    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _entityHintLabel = GetNode<Label>("EntityHintLabel");
        _categoryList = GetNode<ItemList>("Split/CategoryList");
        _editorContainer = GetNode<VBoxContainer>("Split/EditorScroll/EditorContainer");
        _categoryList.ItemSelected += OnCategorySelected;
    }

    private void PopulateCategoryList()
    {
        _categoryList.Clear();
        foreach (var category in _categories)
        {
            _categoryList.AddItem(category.Title);
        }
    }

    /// <summary>
    /// 左侧分类切换回调。
    /// </summary>
    /// <param name="index">当前选中的分类索引。</param>
    private void OnCategorySelected(long index)
    {
        RequestFullRefresh();
    }

    /// <summary>
    /// 创建单个分类条目对应的编辑行。
    /// <para>
    /// 上层负责标题、键名与分割线；下层由 CreateEditor 按数据类型生成真正的编辑控件。
    /// </para>
    /// </summary>
    private AttributeEditorRow CreateEditorRow(DataMeta meta)
    {
        var metaKey = GetMetaKey(meta);
        var row = InstantiateScene<AttributeEditorRow>(_editorRowScene, nameof(AttributeEditorRow));

        row.ConfigureHeader(GetMetaDisplayName(meta), metaKey); // 绑定词条标题

        try
        {
            row.SetEditor(CreateEditor(meta)); // 绑定主编辑器
        }
        catch (Exception ex)
        {
            _log.Error($"[属性测试UI] 主编辑器创建失败: key={metaKey} error={ex}");
            row.SetEditor(CreateInlineErrorLabel("主编辑器创建失败"));
        }

        try
        {
            row.SetModifierEditor(CreateTemporaryModifierEditor(meta)); // 绑定临时加成编辑器
        }
        catch (Exception ex)
        {
            _log.Error($"[属性测试UI] 临时Modifier编辑器创建失败: key={metaKey} error={ex}");
            row.SetModifierEditor(null);
        }

        return row;
    }

    /// <summary>
    /// 为支持 Modifier 的数值属性创建临时 Feature 调试控件。
    /// </summary>
    private Control? CreateTemporaryModifierEditor(DataMeta meta)
    {
        if (selectedEntity == null || !SupportsTemporaryModifier(meta))
        {
            return null;
        }

        var editor = InstantiateScene<AttributeModifierEditor>(_modifierEditorScene, nameof(AttributeModifierEditor));
        var modifierValue = _featureDebugService.GetTemporaryModifierValue(selectedEntity, GetMetaKey(meta));
        editor.SetTitle("临时加成"); // 绑定固定标题
        editor.ConfigureRange(
            meta.MinValue.HasValue ? -meta.MinValue.Value - 9999 : -999999,
            meta.MaxValue ?? 999999,
            meta.IsInteger ? 1 : 0.1);
        editor.Value = modifierValue;
        editor.BindApply(() =>
        {
            if (selectedEntity == null)
            {
                return;
            }

            var result = _featureDebugService.ApplyTemporaryModifier(
                selectedEntity,
                GetMetaKey(meta),
                GetMetaDisplayName(meta),
                meta.IsPercentage,
                (float)editor.Value);
            _entityHintLabel.Text = result.Message;
            RequestPatch(GetMetaKey(meta));
        });
        editor.BindClear(() =>
        {
            if (selectedEntity == null)
            {
                return;
            }

            var result = _featureDebugService.ClearTemporaryModifier(
                selectedEntity,
                GetMetaKey(meta),
                GetMetaDisplayName(meta));
            _entityHintLabel.Text = result.Message;
            RequestPatch(GetMetaKey(meta));
        });

        return editor;
    }

    /// <summary>
    /// 创建单个 DataMeta 的编辑控件。
    /// <para>
    /// 会根据元数据类型自动生成 CheckButton、OptionButton、SpinBox 或 LineEdit。
    /// </para>
    /// </summary>
    private Control? CreateEditor(DataMeta meta)
    {
        if (selectedEntity == null)
        {
            return null;
        }

        if (meta.IsBoolean)
        {
            var toggle = InstantiateScene<CheckButton>(_checkEditorScene, nameof(CheckButton));
            toggle.ButtonPressed = selectedEntity.Data.Get<bool>(GetMetaKey(meta));
            toggle.Text = toggle.ButtonPressed ? "已开启" : "已关闭";
            toggle.Toggled += pressed =>
            {
                toggle.Text = pressed ? "已开启" : "已关闭";
                ApplyMetaValue(meta, pressed);
            };
            return toggle;
        }

        if (meta.IsEnum)
        {
            var option = InstantiateScene<OptionButton>(_optionEditorScene, nameof(OptionButton));
            var values = Enum.GetValues(meta.Type);
            var currentValue = selectedEntity.Data.Get<int>(GetMetaKey(meta));
            int selectedIndex = 0;
            int index = 0;
            foreach (var value in values)
            {
                option.AddItem(value?.ToString() ?? string.Empty);
                if (Convert.ToInt32(value) == currentValue)
                {
                    selectedIndex = index;
                }

                index++;
            }

            option.Selected = selectedIndex;
            option.ItemSelected += idx =>
            {
                var rawValue = values.GetValue((int)idx);
                if (rawValue != null)
                {
                    ApplyMetaValue(meta, rawValue);
                }
            };
            return option;
        }

        if (meta.HasOptions)
        {
            var option = InstantiateScene<OptionButton>(_optionEditorScene, nameof(OptionButton));
            for (int i = 0; i < meta.Options!.Count; i++)
            {
                option.AddItem(meta.Options[i]);
            }

            option.Selected = selectedEntity.Data.Get<int>(GetMetaKey(meta));
            option.ItemSelected += idx => ApplyMetaValue(meta, (int)idx);
            return option;
        }

        if (meta.IsNumeric)
        {
            var spin = InstantiateScene<SpinBox>(_numericEditorScene, nameof(SpinBox));
            spin.MinValue = meta.MinValue ?? -999999;
            spin.MaxValue = meta.MaxValue ?? 999999;
            spin.Step = meta.IsInteger ? 1 : 0.1;
            spin.Value = meta.IsInteger
                ? selectedEntity.Data.Get<int>(GetMetaKey(meta))
                : selectedEntity.Data.Get<float>(GetMetaKey(meta));
            spin.ValueChanged += value =>
            {
                if (meta.IsInteger)
                {
                    ApplyMetaValue(meta, (int)Math.Round(value));
                }
                else
                {
                    ApplyMetaValue(meta, (float)value);
                }
            };
            return spin;
        }

        if (meta.IsString)
        {
            var lineEdit = InstantiateScene<LineEdit>(_textEditorScene, nameof(LineEdit));
            lineEdit.Text = selectedEntity.Data.Get<string>(GetMetaKey(meta));
            lineEdit.TextSubmitted += text => ApplyMetaValue(meta, text);
            lineEdit.FocusExited += () => ApplyMetaValue(meta, lineEdit.Text);
            return lineEdit;
        }

        return null;
    }

    /// <summary>
    /// 实例化一个 UI 场景。
    /// </summary>
    private static T InstantiateScene<T>(PackedScene? scene, string sceneName) where T : Node
    {
        if (scene == null)
        {
            throw new InvalidOperationException($"属性测试场景未配置: {sceneName}");
        }

        var instance = scene.Instantiate<T>();
        if (instance == null)
        {
            throw new InvalidOperationException($"属性测试场景实例化失败: {sceneName}");
        }

        return instance;
    }

    /// <summary>
    /// 判断某个属性是否适合使用临时 Modifier 调试。
    /// </summary>
    private static bool SupportsTemporaryModifier(DataMeta meta)
    {
        return meta.IsNumeric && meta.SupportModifiers == true && !meta.IsComputed;
    }

    /// <summary>
    /// 获取元数据的稳定展示名称。
    /// </summary>
    private static string GetMetaDisplayName(DataMeta meta)
    {
        var metaKey = GetMetaKey(meta);
        return string.IsNullOrWhiteSpace(meta.DisplayName) ? metaKey : meta.DisplayName;
    }

    /// <summary>
    /// 获取元数据的键名。
    /// </summary>
    private static string GetMetaKey(DataMeta meta) => meta.Key;

    private static Control CreateInlineErrorLabel(string message)
    {
        return new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
    }

    /// <summary>
    /// 把 UI 中编辑后的值写回实体 Data。
    /// <para>
    /// 这里会根据元数据类型做基础转换，并在必要时对当前生命 / 魔法做上限夹紧。
    /// </para>
    /// </summary>
    /// <param name="meta">属性元数据。</param>
    /// <param name="value">UI 编辑后的原始值。</param>
    private void ApplyMetaValue(DataMeta meta, object value)
    {
        if (selectedEntity == null)
        {
            return;
        }

        var metaKey = GetMetaKey(meta);
        if (metaKey == GetMetaKey(DataKey.CurrentHp))
        {
            var maxHp = selectedEntity.Data.Get<float>(DataKey.FinalHp.Key);
            value = Mathf.Clamp(Convert.ToSingle(value), 0f, maxHp);
        }
        else if (metaKey == GetMetaKey(DataKey.CurrentMana))
        {
            var maxMana = selectedEntity.Data.Get<float>(DataKey.FinalMana.Key);
            value = Mathf.Clamp(Convert.ToSingle(value), 0f, maxMana);
        }

        if (meta.IsInteger)
        {
            selectedEntity.Data.Set(metaKey, Convert.ToInt32(value));
        }
        else if (meta.IsFloatingPoint)
        {
            selectedEntity.Data.Set(metaKey, Convert.ToSingle(value));
        }
        else if (meta.IsBoolean)
        {
            selectedEntity.Data.Set(metaKey, Convert.ToBoolean(value));
        }
        else if (meta.IsString)
        {
            selectedEntity.Data.Set(metaKey, Convert.ToString(value) ?? string.Empty);
        }
        else
        {
            selectedEntity.Data.Set(metaKey, value);
        }

        var clampedResourceKey = ClampCurrentResourceIfNeeded(metaKey);
        RequestPatch(metaKey);
        if (!string.IsNullOrWhiteSpace(clampedResourceKey))
        {
            RequestPatch(clampedResourceKey);
        }
    }

    /// <summary>
    /// 当最大生命 / 最大魔法相关字段变化时，保证当前值不超过新的上限。
    /// </summary>
    /// <param name="key">本次被修改的 DataKey。</param>
    /// <returns>如果额外调整了当前资源，则返回被调整的资源键。</returns>
    private string? ClampCurrentResourceIfNeeded(string key)
    {
        if (selectedEntity == null)
        {
            return null;
        }

        if (key == GetMetaKey(DataKey.BaseHp) || key == GetMetaKey(DataKey.HpBonus))
        {
            var currentHp = selectedEntity.Data.Get<float>(DataKey.CurrentHp.Key);
            var maxHp = selectedEntity.Data.Get<float>(DataKey.FinalHp.Key);
            if (currentHp > maxHp)
            {
                selectedEntity.Data.Set(DataKey.CurrentHp, maxHp);
                return DataKey.CurrentHp.Key;
            }
        }

        if (key == GetMetaKey(DataKey.BaseMana) || key == GetMetaKey(DataKey.ManaBonus))
        {
            var currentMana = selectedEntity.Data.Get<float>(DataKey.CurrentMana.Key);
            var maxMana = selectedEntity.Data.Get<float>(DataKey.FinalMana.Key);
            if (currentMana > maxMana)
            {
                selectedEntity.Data.Set(DataKey.CurrentMana, maxMana);
                return DataKey.CurrentMana.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// 订阅当前实体的 Data 变化事件。
    /// <para>
    /// 这样当其它系统修改了属性时，调试面板也能同步刷新。
    /// </para>
    /// </summary>
    private void SubscribeEntityEvents()
    {
        if (selectedEntity == null || ReferenceEquals(selectedEntity, _subscribedEntity))
        {
            return;
        }

        _subscribedEntity = selectedEntity;
        _subscribedEntity.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged,
            OnEntityDataChanged
        );
    }

    /// <summary>
    /// 取消当前实体的 Data 变化订阅。
    /// </summary>
    private void UnsubscribeEntityEvents()
    {
        if (_subscribedEntity == null)
        {
            return;
        }

        _subscribedEntity.Events.Off<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged,
            OnEntityDataChanged
        );
        _subscribedEntity = null;
    }

    /// <summary>
    /// 数据变化后的统一刷新回调。
    /// </summary>
    /// <param name="evt">属性变更事件数据。</param>
    private void OnEntityDataChanged(GameEventType.Data.PropertyChangedEventData evt)
    {
        if (!CanRefresh)
        {
            return;
        }

        if (evt.Key == DataKey.Name.Key)
        {
            RequestFullRefresh();
            return;
        }

        if (!TryGetSelectedCategory(out var category, out _))
        {
            RequestFullRefresh();
            return;
        }

        if (!category.MetaByKey.ContainsKey(evt.Key))
        {
            return;
        }

        RequestPatch(evt.Key);
    }

    /// <summary>
    /// 请求当前分类整页重建。
    /// </summary>
    private void RequestFullRefresh()
    {
        _rebuildRequested = true;
        QueueRefresh();
    }

    /// <summary>
    /// 请求只 patch 某个属性对应的单行。
    /// </summary>
    private void RequestPatch(string metaKey)
    {
        if (string.IsNullOrWhiteSpace(metaKey))
        {
            return;
        }

        _dirtyMetaKeys.Add(metaKey);
        QueueRefresh();
    }

    /// <summary>
    /// 统一把刷新合并到当前 UI 事件处理完成后执行，避免同一帧反复重建。
    /// </summary>
    private void QueueRefresh()
    {
        RequestScheduledRefresh();
    }

    /// <summary>
    /// 在帧末统一执行属性模块刷新：优先重建，否则只 patch 脏行。
    /// </summary>
    protected override void FlushScheduledRefresh()
    {
        if (!CanRefresh)
        {
            return;
        }

        if (_rebuildRequested)
        {
            RebuildCurrentCategory();
            _rebuildRequested = false;
            _dirtyMetaKeys.Clear();
            return;
        }

        if (!TryGetSelectedCategory(out var category, out var categoryIndex) || _renderedCategoryIndex != categoryIndex)
        {
            _rebuildRequested = true;
            RebuildCurrentCategory();
            _rebuildRequested = false;
            _dirtyMetaKeys.Clear();
            return;
        }

        PatchDirtyRows(category);
        _dirtyMetaKeys.Clear();
    }

    /// <summary>
    /// 重建当前分类的全部属性行；仅在分类变化、实体变化等结构变化时调用。
    /// </summary>
    private void RebuildCurrentCategory()
    {
        ClearEditorRows();

        if (selectedEntity is not Node entityNode)
        {
            _entityHintLabel.Text = "请先选择一个实体";
            ShowPlaceholder("请先选择一个实体");
            _renderedCategoryIndex = -1;
            return;
        }

        UpdateEntityHint(entityNode);
        if (!TryGetSelectedCategory(out var category, out var categoryIndex))
        {
            ShowPlaceholder("未找到可编辑属性分类");
            _renderedCategoryIndex = -1;
            return;
        }

        var renderedRowCount = 0;
        foreach (var meta in category.Metas)
        {
            try
            {
                var row = CreateEditorRow(meta);
                _rowsByKey[GetMetaKey(meta)] = row;
                _editorContainer.AddChild(row);
                renderedRowCount++;
            }
            catch (Exception ex)
            {
                _log.Error($"[属性测试UI] 渲染属性词条失败: category={category.Title} key={GetMetaKey(meta)} error={ex}");
            }
        }

        if (renderedRowCount == 0)
        {
            ShowPlaceholder($"分类“{category.Title}”未生成任何可编辑控件，请检查日志");
        }

        _renderedCategoryIndex = categoryIndex;
    }

    /// <summary>
    /// 仅对当前分类内脏掉的属性行做局部替换，避免整页重建。
    /// </summary>
    private void PatchDirtyRows(CategoryEntry category)
    {
        if (_dirtyMetaKeys.Count == 0)
        {
            return;
        }

        if (selectedEntity is Node entityNode)
        {
            UpdateEntityHint(entityNode);
        }

        foreach (var dirtyKey in _dirtyMetaKeys)
        {
            if (!category.MetaByKey.TryGetValue(dirtyKey, out var meta))
            {
                continue;
            }

            if (!_rowsByKey.TryGetValue(dirtyKey, out var oldRow))
            {
                _rebuildRequested = true;
                continue;
            }

            try
            {
                var newRow = CreateEditorRow(meta);
                var childIndex = oldRow.GetIndex();
                _editorContainer.AddChild(newRow);
                _editorContainer.MoveChild(newRow, childIndex);
                oldRow.QueueFree();
                _rowsByKey[dirtyKey] = newRow;
            }
            catch (Exception ex)
            {
                _log.Error($"[属性测试UI] 局部刷新属性词条失败: key={dirtyKey} error={ex}");
                _rebuildRequested = true;
            }
        }
    }

    /// <summary>
    /// 获取当前选中的分类。
    /// </summary>
    private bool TryGetSelectedCategory(out CategoryEntry category, out int index)
    {
        category = null!;
        index = -1;
        if (_categories.Count == 0)
        {
            return false;
        }

        var selectedItems = _categoryList.GetSelectedItems();
        index = selectedItems.Length > 0 ? selectedItems[0] : 0;
        index = Mathf.Clamp(index, 0, _categories.Count - 1);
        category = _categories[index];
        return true;
    }

    /// <summary>
    /// 更新顶部实体提示，避免名称变化后顶部说明仍显示旧值。
    /// </summary>
    private void UpdateEntityHint(Node entityNode)
    {
        if (selectedEntity == null)
        {
            _entityHintLabel.Text = "请先选择一个实体";
            return;
        }

        var entityName = selectedEntity.Data.Get<string>(DataKey.Name.Key);
        if (string.IsNullOrWhiteSpace(entityName))
        {
            entityName = entityNode.Name.ToString();
        }

        _entityHintLabel.Text = $"当前实体：{entityName} ({entityNode.GetType().Name})";
    }

    /// <summary>
    /// 清理右侧编辑器中的旧行，避免重复堆叠。
    /// </summary>
    private void ClearEditorRows()
    {
        _rowsByKey.Clear();
        foreach (Node child in _editorContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void ShowPlaceholder(string message)
    {
        var label = new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _editorContainer.AddChild(label);
    }
}
