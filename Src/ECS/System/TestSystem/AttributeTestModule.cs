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
    /// <summary>单个分类页的缓存结构，保存分类标题和该分类下的可编辑 DataMeta 列表。</summary>
    private sealed record CategoryEntry(string Title, List<DataMeta> Metas);

    private readonly FeatureDebugService _featureDebugService = new();

    /// <summary>所有可编辑分类的缓存列表，初始化后用于驱动左侧分类面板。</summary>
    private readonly List<CategoryEntry> _categories = new();

    /// <summary>左侧分类列表。</summary>
    private ItemList _categoryList = null!;

    /// <summary>右侧编辑器容器，当前分类下的所有属性编辑行都会动态挂在这里。</summary>
    private VBoxContainer _editorContainer = null!;

    /// <summary>顶部实体提示文字，用于显示当前编辑目标。</summary>
    private Label _entityHintLabel = null!;

    /// <summary>当前订阅了 Data 变化事件的实体，避免重复订阅。</summary>
    private IEntity? _subscribedEntity;

    /// <summary>模块在 TestSystem 下拉框中的显示名称。</summary>
    internal override string DisplayName => "属性测试";

    /// <summary>
    /// 模块初始化。
    /// <para>
    /// 先整理分类数据，再构建 UI；如果存在分类，默认选中第一个分类，保证界面打开后立即可用。
    /// </para>
    /// </summary>
    internal override void Initialize(TestSystem system)
    {
        base.Initialize(system);
        BuildCategoryData();
        BuildUi();
        if (_categories.Count > 0)
        {
            _categoryList.Select(0);
        }
        Refresh();
    }

    /// <summary>
    /// 选中实体变化时，先解除旧订阅，再绑定新实体并刷新界面。
    /// </summary>
    internal override void OnSelectedEntityChanged(IEntity? entity)
    {
        UnsubscribeEntityEvents();
        base.OnSelectedEntityChanged(entity);
        SubscribeEntityEvents();
        Refresh();
    }

    /// <summary>
    /// 模块被切换到前台时，恢复订阅并强制刷新。
    /// </summary>
    internal override void OnActivated()
    {
        SubscribeEntityEvents();
        Refresh();
    }

    /// <summary>
    /// 模块离开前台时，取消订阅，避免后台界面持续响应数据变化。
    /// </summary>
    internal override void OnDeactivated()
    {
        UnsubscribeEntityEvents();
    }

    /// <summary>
    /// 刷新当前显示内容。
    /// <para>
    /// 会先清空旧编辑器行，再根据当前分类重新生成对应的编辑控件。
    /// </para>
    /// </summary>
    internal override void Refresh()
    {
        ClearEditorRows();

        if (selectedEntity is not Node entityNode)
        {
            _entityHintLabel.Text = "请先选择一个实体";
            return;
        }

        _entityHintLabel.Text = $"当前实体：{selectedEntity.Data.Get<string>(DataKey.Name.Key)} ({entityNode.GetType().Name})";

        if (_categories.Count == 0)
        {
            return;
        }

        var index = Mathf.Clamp(_categoryList.GetSelectedItems().FirstOrDefault(), 0, _categories.Count - 1);
        var category = _categories[index];

        foreach (var meta in category.Metas)
        {
            _editorContainer.AddChild(CreateEditorRow(meta));
        }
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

        _categories.Add(new CategoryEntry(title, metas));
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

    /// <summary>
    /// 构建属性测试模块 UI。
    /// <para>
    /// 左侧显示分类列表，右侧显示当前分类下的所有编辑行。
    /// </para>
    /// </summary>
    private void BuildUi()
    {
        var title = new Label
        {
            Text = "直接修改运行时 Data；支持 Modifier 的数值属性可额外挂临时 Feature 加成"
        };
        AddChild(title);

        _entityHintLabel = new Label
        {
            Text = "请先选择一个实体"
        };
        AddChild(_entityHintLabel);

        var split = new HSplitContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        AddChild(split);

        _categoryList = new ItemList
        {
            CustomMinimumSize = new Vector2(160, 460),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = ItemList.SelectModeEnum.Single
        };
        _categoryList.ItemSelected += OnCategorySelected;
        split.AddChild(_categoryList);

        foreach (var category in _categories)
        {
            _categoryList.AddItem(category.Title);
        }

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        split.AddChild(scroll);

        _editorContainer = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        scroll.AddChild(_editorContainer);
    }

    private void OnCategorySelected(long index)
    {
        Refresh();
    }

    /// <summary>
    /// 创建单个分类条目对应的编辑行。
    /// <para>
    /// 上层负责标题、键名与分割线；下层由 CreateEditor 按数据类型生成真正的编辑控件。
    /// </para>
    /// </summary>
    private Control CreateEditorRow(DataMeta meta)
    {
        var wrapper = new VBoxContainer();
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var header = new HBoxContainer();
        wrapper.AddChild(header);

        var title = new Label
        {
            Text = string.IsNullOrWhiteSpace(meta.DisplayName) ? meta.Key : meta.DisplayName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        header.AddChild(title);

        var keyLabel = new Label
        {
            Text = meta.Key
        };
        header.AddChild(keyLabel);

        var editor = CreateEditor(meta);
        if (editor != null)
        {
            wrapper.AddChild(editor);
        }

        var modifierEditor = CreateTemporaryModifierEditor(meta);
        if (modifierEditor != null)
        {
            wrapper.AddChild(modifierEditor);
        }

        wrapper.AddChild(new HSeparator());
        return wrapper;
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

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var label = new Label
        {
            Text = "临时加成",
            CustomMinimumSize = new Vector2(80, 0)
        };
        row.AddChild(label);

        var modifierValue = _featureDebugService.GetTemporaryModifierValue(selectedEntity, meta.Key);
        var spin = new SpinBox
        {
            MinValue = meta.MinValue.HasValue ? -meta.MinValue.Value - 9999 : -999999,
            MaxValue = meta.MaxValue ?? 999999,
            Step = meta.IsInteger ? 1 : 0.1,
            Value = modifierValue,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddChild(spin);

        var applyButton = new Button
        {
            Text = "应用临时Feature"
        };
        applyButton.Pressed += () =>
        {
            if (selectedEntity == null)
            {
                return;
            }

            var result = _featureDebugService.ApplyTemporaryModifier(
                selectedEntity,
                meta.Key,
                GetMetaDisplayName(meta),
                meta.IsPercentage,
                (float)spin.Value);
            _entityHintLabel.Text = result.Message;
            Refresh();
        };
        row.AddChild(applyButton);

        var clearButton = new Button
        {
            Text = "清除"
        };
        clearButton.Pressed += () =>
        {
            if (selectedEntity == null)
            {
                return;
            }

            var result = _featureDebugService.ClearTemporaryModifier(
                selectedEntity,
                meta.Key,
                GetMetaDisplayName(meta));
            _entityHintLabel.Text = result.Message;
            Refresh();
        };
        row.AddChild(clearButton);

        return row;
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
            var toggle = new CheckButton
            {
                ButtonPressed = selectedEntity.Data.Get<bool>(meta.Key),
                Text = selectedEntity.Data.Get<bool>(meta.Key) ? "已开启" : "已关闭"
            };
            toggle.Toggled += pressed =>
            {
                toggle.Text = pressed ? "已开启" : "已关闭";
                ApplyMetaValue(meta, pressed);
            };
            return toggle;
        }

        if (meta.IsEnum)
        {
            var option = new OptionButton();
            var values = Enum.GetValues(meta.Type);
            var currentValue = selectedEntity.Data.Get<int>(meta.Key);
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
            var option = new OptionButton();
            for (int i = 0; i < meta.Options!.Count; i++)
            {
                option.AddItem(meta.Options[i]);
            }

            option.Selected = selectedEntity.Data.Get<int>(meta.Key);
            option.ItemSelected += idx => ApplyMetaValue(meta, (int)idx);
            return option;
        }

        if (meta.IsNumeric)
        {
            var spin = new SpinBox
            {
                MinValue = meta.MinValue ?? -999999,
                MaxValue = meta.MaxValue ?? 999999,
                Step = meta.IsInteger ? 1 : 0.1,
                Value = meta.IsInteger
                    ? selectedEntity.Data.Get<int>(meta.Key)
                    : selectedEntity.Data.Get<float>(meta.Key),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
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
            var lineEdit = new LineEdit
            {
                Text = selectedEntity.Data.Get<string>(meta.Key),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            lineEdit.TextSubmitted += text => ApplyMetaValue(meta, text);
            lineEdit.FocusExited += () => ApplyMetaValue(meta, lineEdit.Text);
            return lineEdit;
        }

        return null;
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
        return string.IsNullOrWhiteSpace(meta.DisplayName) ? meta.Key : meta.DisplayName;
    }

    /// <summary>
    /// 把 UI 中编辑后的值写回实体 Data。
    /// <para>
    /// 这里会根据元数据类型做基础转换，并在必要时对当前生命 / 魔法做上限夹紧。
    /// </para>
    /// </summary>
    private void ApplyMetaValue(DataMeta meta, object value)
    {
        if (selectedEntity == null)
        {
            return;
        }

        if (meta.Key == DataKey.CurrentHp)
        {
            var maxHp = selectedEntity.Data.Get<float>(DataKey.FinalHp);
            value = Mathf.Clamp(Convert.ToSingle(value), 0f, maxHp);
        }
        else if (meta.Key == DataKey.CurrentMana)
        {
            var maxMana = selectedEntity.Data.Get<float>(DataKey.FinalMana);
            value = Mathf.Clamp(Convert.ToSingle(value), 0f, maxMana);
        }

        if (meta.IsInteger)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToInt32(value));
        }
        else if (meta.IsFloatingPoint)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToSingle(value));
        }
        else if (meta.IsBoolean)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToBoolean(value));
        }
        else if (meta.IsString)
        {
            selectedEntity.Data.Set(meta.Key, Convert.ToString(value) ?? string.Empty);
        }
        else
        {
            selectedEntity.Data.Set(meta.Key, value);
        }

        ClampCurrentResourceIfNeeded(meta.Key);
        Refresh();
    }

    /// <summary>
    /// 当最大生命 / 最大魔法相关字段变化时，保证当前值不超过新的上限。
    /// </summary>
    private void ClampCurrentResourceIfNeeded(string key)
    {
        if (selectedEntity == null)
        {
            return;
        }

        if (key == DataKey.BaseHp || key == DataKey.HpBonus)
        {
            var currentHp = selectedEntity.Data.Get<float>(DataKey.CurrentHp);
            var maxHp = selectedEntity.Data.Get<float>(DataKey.FinalHp);
            if (currentHp > maxHp)
            {
                selectedEntity.Data.Set(DataKey.CurrentHp, maxHp);
            }
        }

        if (key == DataKey.BaseMana || key == DataKey.ManaBonus)
        {
            var currentMana = selectedEntity.Data.Get<float>(DataKey.CurrentMana);
            var maxMana = selectedEntity.Data.Get<float>(DataKey.FinalMana);
            if (currentMana > maxMana)
            {
                selectedEntity.Data.Set(DataKey.CurrentMana, maxMana);
            }
        }
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
    private void OnEntityDataChanged(GameEventType.Data.PropertyChangedEventData evt)
    {
        if (!Visible)
        {
            return;
        }

        Refresh();
    }

    /// <summary>
    /// 清理右侧编辑器中的旧行，避免重复堆叠。
    /// </summary>
    private void ClearEditorRows()
    {
        foreach (var child in _editorContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
}
