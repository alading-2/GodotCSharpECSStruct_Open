#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Slime.Addons.DataConfigEditor
{
    /// <summary>
    /// DataNew 配置表格面板。
    /// 本次重构重点：
    /// 1. 打通 Property -> DataKey -> DataMeta 元数据链路
    /// 2. 支持多层表头（分类 / DataKey / 注释 / 字段）
    /// 3. 支持路径校验、Flags 枚举编辑、批量修改
    /// </summary>
    public partial class ConfigTablePanel : VBoxContainer
    {
        // === 工具栏 ===
        private HBoxContainer _toolbar = null!;
        private OptionButton _configTypeSelector = null!;
        private Button _refreshBtn = null!;
        private Button _saveBtn = null!;
        private Button _toggleLayoutBtn = null!;
        private Label _statusLabel = null!;
        private LineEdit _searchBox = null!;
        private OptionButton _bulkPropertySelector = null!;
        private LineEdit _bulkValueInput = null!;
        private Button _bulkApplyBtn = null!;

        // === 表格区域 ===
        private ScrollContainer _scrollV = null!;
        private GridContainer _grid = null!;
        private Panel _emptyPanel = null!;
        private Label _detailLabel = null!;

        // === 数据 ===
        private Type? _currentType;
        private string? _currentSourceFile;
        private List<PropertyMetadata> _allProperties = new();
        private List<ConfigReflectionCache.InstanceInfo> _instances = new();
        private Dictionary<string, PropertyCommentInfo> _comments = new();
        private bool _modified;

        // === 布局模式 ===
        private bool _layoutRowsAreInstances = true; // true=行=实例, false=行=属性

        // === 尺寸常量 ===
        private const float HEADER_HEIGHT = 28;
        private const float SUB_HEADER_HEIGHT = 34;
        private const float DESC_HEADER_HEIGHT = 44;
        private const float CELL_HEIGHT = 26;
        private const float CELL_MIN_WIDTH = 120;
        private const float NAME_COL_WIDTH = 180;
        private const float DATAKEY_COL_WIDTH = 150;
        private const float DESC_COL_WIDTH = 220;

        // === 搜索过滤 ===
        private string _searchFilter = "";

        public override void _Ready()
        {
            try
            {
                BuildUI();
                EnumCommentCache.EnsureLoaded();
                PopulateTypeSelector();
            }
            catch (Exception e)
            {
                GD.PrintErr($"[DataConfigEditor] 初始化失败: {e}");
                var errorLabel = new Label
                {
                    Text = $"DataConfigEditor 初始化失败:\n{e.Message}\n\n{e.StackTrace}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    SizeFlagsVertical = SizeFlags.ExpandFill,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                };
                AddChild(errorLabel);
            }
        }

        private void BuildUI()
        {
            _toolbar = new HBoxContainer { CustomMinimumSize = new Vector2(0, 36) };

            _toolbar.AddChild(new Label
            {
                Text = " 配置类: ",
                VerticalAlignment = VerticalAlignment.Center,
            });

            _configTypeSelector = new OptionButton { CustomMinimumSize = new Vector2(280, 30) };
            _configTypeSelector.ItemSelected += OnTypeSelected;
            _toolbar.AddChild(_configTypeSelector);

            _toolbar.AddChild(new VSeparator { CustomMinimumSize = new Vector2(8, 0) });

            _searchBox = new LineEdit
            {
                PlaceholderText = "搜索属性 / DataKey / 注释...",
                CustomMinimumSize = new Vector2(170, 28),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            };
            _searchBox.TextChanged += OnSearchChanged;
            _toolbar.AddChild(_searchBox);

            _toolbar.AddChild(new VSeparator { CustomMinimumSize = new Vector2(4, 0) });

            _toggleLayoutBtn = new Button
            {
                Text = "布局: 行=实例",
                Flat = true,
                TooltipText = "切换表格布局方向",
            };
            _toggleLayoutBtn.Pressed += ToggleLayout;
            _toolbar.AddChild(_toggleLayoutBtn);

            _toolbar.AddChild(new VSeparator { CustomMinimumSize = new Vector2(4, 0) });

            _refreshBtn = new Button { Text = "刷新", Flat = true };
            _refreshBtn.Pressed += OnRefresh;
            _toolbar.AddChild(_refreshBtn);

            _saveBtn = new Button { Text = "保存", Flat = true, Disabled = true };
            _saveBtn.Pressed += OnSave;
            _toolbar.AddChild(_saveBtn);

            _toolbar.AddChild(new VSeparator { CustomMinimumSize = new Vector2(8, 0) });

            _bulkPropertySelector = new OptionButton
            {
                CustomMinimumSize = new Vector2(170, 28),
                Disabled = true,
                TooltipText = "选择要批量修改的属性",
            };
            _bulkPropertySelector.ItemSelected += OnBulkPropertySelected;
            _toolbar.AddChild(_bulkPropertySelector);

            _bulkValueInput = new LineEdit
            {
                PlaceholderText = "批量值",
                CustomMinimumSize = new Vector2(140, 28),
                Editable = false,
            };
            _toolbar.AddChild(_bulkValueInput);

            _bulkApplyBtn = new Button
            {
                Text = "批量应用",
                Flat = true,
                Disabled = true,
                TooltipText = "把当前批量值应用到所有实例",
            };
            _bulkApplyBtn.Pressed += OnApplyBulkEdit;
            _toolbar.AddChild(_bulkApplyBtn);

            _toolbar.AddChild(new VSeparator { CustomMinimumSize = new Vector2(8, 0) });

            _statusLabel = new Label
            {
                Text = "选择配置类开始",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _toolbar.AddChild(_statusLabel);

            AddChild(_toolbar);

            _scrollV = new ScrollContainer
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            };
            AddChild(_scrollV);

            _grid = new GridContainer
            {
                Columns = 1,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _scrollV.AddChild(_grid);

            _emptyPanel = new Panel
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };

            var emptyLabel = new Label
            {
                Text = "请选择配置类\n选择后将以表格形式展示所有配置属性",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            _emptyPanel.AddChild(emptyLabel);
            AddChild(_emptyPanel);

            _scrollV.Visible = false;
            _grid.Visible = false;
            _emptyPanel.Visible = true;

            _detailLabel = new Label
            {
                Text = "",
                CustomMinimumSize = new Vector2(0, 24),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _detailLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.75f, 0.8f));
            AddChild(_detailLabel);
        }

        private void PopulateTypeSelector()
        {
            _configTypeSelector.Clear();
            var types = ConfigReflectionCache.GetAllConfigTypes();

            if (types.Count == 0)
            {
                _configTypeSelector.AddItem("(未找到配置类 - 请重启编辑器)");
                _statusLabel.Text = "未在 Data/DataNew 中找到配置类。";
                return;
            }

            foreach (var typeInfo in types)
            {
                int idx = _configTypeSelector.GetItemCount();
                _configTypeSelector.AddItem(typeInfo.Name, idx);
                _configTypeSelector.SetItemMetadata(idx, typeInfo.Type.FullName ?? typeInfo.Type.Name);
            }
        }

        private void OnTypeSelected(long index)
        {
            if (index < 0)
                return;

            string? fullName = _configTypeSelector.GetItemMetadata((int)index).AsString();
            if (string.IsNullOrWhiteSpace(fullName))
                return;

            var type = FindType(fullName);
            if (type == null)
            {
                _statusLabel.Text = $"类型未找到: {fullName}";
                return;
            }

            _currentType = type;
            _currentSourceFile = ConfigReflectionCache.FindSourceFile(type);
            LoadTypeData(type);
            RebuildGrid();
        }

        private void LoadTypeData(Type type)
        {
            _allProperties = ConfigReflectionCache.GetProperties(type);

            if (_currentSourceFile != null)
            {
                _instances = ConfigReflectionCache.GetInstances(type, _currentSourceFile);
                _comments = ConfigReflectionCache.GetComments(type, _currentSourceFile);
            }
            else
            {
                _instances = new List<ConfigReflectionCache.InstanceInfo>();
                _comments = new Dictionary<string, PropertyCommentInfo>();
            }

            RefreshBulkPropertySelector();
        }

        private void RebuildGrid()
        {
            foreach (var child in _grid.GetChildren())
                child.QueueFree();

            var filtered = GetFilteredProperties();
            if (filtered.Count == 0 || _instances.Count == 0)
            {
                SetContentVisibility(showGrid: false, reason: filtered.Count == 0 ? "NoFilteredProperties" : "NoInstances");
                _statusLabel.Text = filtered.Count == 0 ? "没有匹配的属性" : "没有配置实例";
                return;
            }

            if (_layoutRowsAreInstances)
                BuildInstanceRowsLayout(filtered);
            else
                BuildPropertyRowsLayout(filtered);

            SetContentVisibility(showGrid: true, reason: $"GridBuilt cells={_grid.GetChildCount()}");

            string srcInfo = _currentSourceFile != null ? Path.GetFileName(_currentSourceFile) : "未找到";
            _statusLabel.Text = $"{_currentType?.Name} | {filtered.Count} 属性 | {_instances.Count} 实例 | 源文件: {srcInfo}";
        }

        private void SetContentVisibility(bool showGrid, string reason)
        {
            _scrollV.Visible = showGrid;
            _grid.Visible = showGrid;
            _emptyPanel.Visible = !showGrid;

            GD.Print($"[DataConfigEditor] SetContentVisibility showGrid={showGrid} reason={reason} gridChildren={_grid.GetChildCount()}");
        }

        private void BuildInstanceRowsLayout(List<PropertyMetadata> props)
        {
            _grid.Columns = 1 + props.Count;

            AddPropertyHeaderRow("分类", props, GetCategoryText, new Color(0.70f, 0.84f, 0.98f), SUB_HEADER_HEIGHT);
            AddPropertyHeaderRow("DataKey", props, GetDataKeyText, new Color(0.98f, 0.84f, 0.56f), SUB_HEADER_HEIGHT);
            AddPropertyHeaderRow("DataKey说明", props, GetDataDescriptionText, new Color(0.56f, 0.82f, 0.60f), DESC_HEADER_HEIGHT);
            AddPropertyHeaderRow("字段", props, BuildPropertyHeaderText, new Color(0.88f, 0.92f, 1.0f), DESC_HEADER_HEIGHT);

            foreach (var instance in _instances)
            {
                _grid.AddChild(MakeRowNameCell(instance.Name, NAME_COL_WIDTH));

                for (int i = 0; i < props.Count; i++)
                    _grid.AddChild(CreateTypedCell(props[i], i, instance));
            }
        }

        private void BuildPropertyRowsLayout(List<PropertyMetadata> props)
        {
            _grid.Columns = 4 + _instances.Count;

            _grid.AddChild(MakeHeaderCell("分类", NAME_COL_WIDTH, HEADER_HEIGHT));
            _grid.AddChild(MakeHeaderCell("DataKey", DATAKEY_COL_WIDTH, HEADER_HEIGHT));
            _grid.AddChild(MakeHeaderCell("DataKey说明", DESC_COL_WIDTH, HEADER_HEIGHT));
            _grid.AddChild(MakeHeaderCell("字段", NAME_COL_WIDTH, HEADER_HEIGHT));
            foreach (var instance in _instances)
                _grid.AddChild(MakeHeaderCell(instance.Name, CELL_MIN_WIDTH, HEADER_HEIGHT));

            foreach (var prop in props)
            {
                _grid.AddChild(MakeBodyTextCell(GetCategoryText(prop), NAME_COL_WIDTH, GetPropertyTooltip(prop), new Color(0.70f, 0.84f, 0.98f)));
                _grid.AddChild(MakeBodyTextCell(GetDataKeyText(prop), DATAKEY_COL_WIDTH, GetPropertyTooltip(prop), new Color(0.98f, 0.84f, 0.56f)));
                _grid.AddChild(MakeBodyTextCell(GetDataDescriptionText(prop), DESC_COL_WIDTH, GetPropertyTooltip(prop), new Color(0.56f, 0.82f, 0.60f)));
                _grid.AddChild(MakeBodyTextCell(BuildPropertyHeaderText(prop), NAME_COL_WIDTH, GetPropertyTooltip(prop), GetCellTextColor(prop)));

                for (int i = 0; i < _instances.Count; i++)
                    _grid.AddChild(CreateTypedCell(prop, i, _instances[i]));
            }
        }

        private void AddPropertyHeaderRow(
            string rowTitle,
            List<PropertyMetadata> props,
            Func<PropertyMetadata, string> valueBuilder,
            Color fontColor,
            float height)
        {
            _grid.AddChild(MakeHeaderCell(rowTitle, NAME_COL_WIDTH, height));
            foreach (var prop in props)
            {
                _grid.AddChild(MakeMetaHeaderCell(
                    valueBuilder(prop),
                    CELL_MIN_WIDTH,
                    height,
                    fontColor,
                    GetPropertyTooltip(prop)));
            }
        }

        private Control CreateTypedCell(PropertyMetadata prop, int instanceIdx, ConfigReflectionCache.InstanceInfo instance)
        {
            object? rawValue = prop.PropertyInfo.GetValue(instance.Instance);
            string currentValue = prop.FormatValue(rawValue);

            if (prop.IsEnum && prop.EnumType != null)
            {
                var enumMembers = EnumCommentCache.GetMembers(prop.EnumType);
                if (prop.IsFlags)
                    return CreateFlagsEditor(prop, instanceIdx, instance, rawValue, enumMembers);

                return CreateEnumEditor(prop, instanceIdx, instance, rawValue, enumMembers);
            }

            if (prop.IsBool)
            {
                var checkBox = new CheckBox
                {
                    ButtonPressed = string.Equals(currentValue, "true", StringComparison.OrdinalIgnoreCase),
                    CustomMinimumSize = new Vector2(CELL_MIN_WIDTH, CELL_HEIGHT),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    TooltipText = GetPropertyTooltip(prop),
                };
                checkBox.Toggled += pressed => OnCellEdited(prop, instance, pressed ? "true" : "false");
                return checkBox;
            }

            if (prop.IsNumeric)
                return CreateNumericEditor(prop, instance, currentValue);

            if (prop.IsString)
            {
                if (prop.IsPathString)
                {
                    var pathEditor = new PathLineEdit(currentValue, newText => OnCellEdited(prop, instance, newText))
                    {
                        CustomMinimumSize = new Vector2(CELL_MIN_WIDTH, CELL_HEIGHT),
                        SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    };
                    return pathEditor;
                }

                var textCell = new LineEdit
                {
                    Text = currentValue,
                    CustomMinimumSize = new Vector2(CELL_MIN_WIDTH, CELL_HEIGHT),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    TooltipText = GetPropertyTooltip(prop),
                };
                textCell.AddThemeColorOverride("font_color", GetCellTextColor(prop));
                textCell.TextChanged += newText => OnCellEdited(prop, instance, newText);
                return textCell;
            }

            return MakeUnsupportedCell(rawValue, prop);
        }

        private Control CreateNumericEditor(PropertyMetadata prop, ConfigReflectionCache.InstanceInfo instance, string currentValue)
        {
            var spin = new SpinBox
            {
                Value = double.TryParse(currentValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double numericValue)
                    ? numericValue
                    : 0d,
                MinValue = prop.MinNumericValue,
                MaxValue = prop.MaxNumericValue,
                Step = prop.PropertyType == typeof(int) ? 1 : 0.1,
                CustomMinimumSize = new Vector2(CELL_MIN_WIDTH, CELL_HEIGHT),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Prefix = "  ",
                TooltipText = GetPropertyTooltip(prop),
            };

            spin.ValueChanged += value =>
            {
                string text = prop.PropertyType == typeof(int)
                    ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                    : value.ToString("G", CultureInfo.InvariantCulture);
                OnCellEdited(prop, instance, text);
            };

            return spin;
        }

        private Control CreateEnumEditor(
            PropertyMetadata prop,
            int instanceIdx,
            ConfigReflectionCache.InstanceInfo instance,
            object? rawValue,
            EnumCommentCache.EnumMemberInfo[] enumMembers)
        {
            if (enumMembers.Length == 0 && prop.EnumType != null)
                enumMembers = BuildEnumMembersFromReflection(prop.EnumType);

            var dropdown = new OptionButton
            {
                CustomMinimumSize = new Vector2(CELL_MIN_WIDTH, CELL_HEIGHT),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = GetPropertyTooltip(prop),
            };

            int selectedIndex = -1;
            int? currentNumeric = rawValue != null ? Convert.ToInt32(rawValue) : null;
            string currentName = rawValue?.ToString() ?? "";

            for (int i = 0; i < enumMembers.Length; i++)
            {
                var member = enumMembers[i];
                string displayText = string.IsNullOrWhiteSpace(member.Comment)
                    ? member.Name
                    : $"{member.Name} / {member.Comment}";

                dropdown.AddItem(displayText);
                dropdown.SetItemMetadata(i, member.Name);
                dropdown.SetItemTooltip(i, $"{member.Name} = {member.Value}");

                if (string.Equals(member.Name, currentName, StringComparison.Ordinal)
                    || (currentNumeric.HasValue && member.Value == currentNumeric.Value))
                {
                    selectedIndex = i;
                }
            }

            if (selectedIndex < 0 && dropdown.ItemCount > 0)
                selectedIndex = 0;
            if (selectedIndex >= 0)
                dropdown.Select(selectedIndex);

            dropdown.ItemSelected += idx =>
            {
                string memberName = dropdown.GetItemMetadata((int)idx).AsString();
                OnCellEdited(prop, instance, memberName);
            };

            return dropdown;
        }

        private Control CreateFlagsEditor(
            PropertyMetadata prop,
            int instanceIdx,
            ConfigReflectionCache.InstanceInfo instance,
            object? rawValue,
            EnumCommentCache.EnumMemberInfo[] enumMembers)
        {
            if (prop.EnumType == null)
                return MakeUnsupportedCell(rawValue, prop);

            if (enumMembers.Length == 0)
                enumMembers = BuildEnumMembersFromReflection(prop.EnumType);

            var supportedMembers = enumMembers
                .Where(m => m.Value == 0 || IsSingleFlagValue(m.Value))
                .OrderBy(m => m.Value)
                .ToArray();

            int currentBits = rawValue != null ? Convert.ToInt32(rawValue) : 0;
            string zeroName = supportedMembers.FirstOrDefault(m => m.Value == 0).Name;
            if (string.IsNullOrWhiteSpace(zeroName))
                zeroName = "None";

            var button = new MenuButton
            {
                Text = BuildFlagsButtonText(supportedMembers, currentBits, zeroName),
                CustomMinimumSize = new Vector2(CELL_MIN_WIDTH, CELL_HEIGHT),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = GetPropertyTooltip(prop),
            };
            button.AddThemeColorOverride("font_color", GetCellTextColor(prop));

            var popup = button.GetPopup();
            for (int i = 0; i < supportedMembers.Length; i++)
            {
                var member = supportedMembers[i];
                string itemText = string.IsNullOrWhiteSpace(member.Comment)
                    ? member.Name
                    : $"{member.Name} / {member.Comment}";
                popup.AddCheckItem(itemText, i);

                bool isChecked = member.Value != 0 && (currentBits & member.Value) == member.Value;
                popup.SetItemChecked(i, isChecked);
                popup.SetItemTooltip(i, $"{member.Name} = {member.Value}");
            }

            popup.IdPressed += id =>
            {
                if (id < 0 || id >= supportedMembers.Length)
                    return;

                int index = (int)id;
                var member = supportedMembers[index];

                if (member.Value == 0)
                {
                    for (int i = 0; i < supportedMembers.Length; i++)
                        popup.SetItemChecked(i, false);
                    currentBits = 0;
                }
                else
                {
                    bool newCheckedState = !popup.IsItemChecked(index);
                    popup.SetItemChecked(index, newCheckedState);

                    if (newCheckedState)
                        currentBits |= member.Value;
                    else
                        currentBits &= ~member.Value;
                }

                string enumText = BuildFlagsEnumText(supportedMembers, currentBits, zeroName);
                button.Text = BuildFlagsButtonText(supportedMembers, currentBits, zeroName);
                OnCellEdited(prop, instance, enumText);
            };

            return button;
        }

        private static EnumCommentCache.EnumMemberInfo[] BuildEnumMembersFromReflection(Type enumType)
        {
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValues(enumType);
            var members = new EnumCommentCache.EnumMemberInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                members[i] = new EnumCommentCache.EnumMemberInfo
                {
                    Name = names[i],
                    Comment = "",
                    IsFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null,
                    Value = Convert.ToInt32(values.GetValue(i)),
                };
            }
            return members;
        }

        private static bool IsSingleFlagValue(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        private static string BuildFlagsButtonText(EnumCommentCache.EnumMemberInfo[] members, int currentBits, string zeroName)
        {
            var names = members
                .Where(m => m.Value != 0 && (currentBits & m.Value) == m.Value)
                .Select(m => m.Name)
                .ToArray();

            return names.Length == 0 ? zeroName : string.Join(" | ", names);
        }

        private static string BuildFlagsEnumText(EnumCommentCache.EnumMemberInfo[] members, int currentBits, string zeroName)
        {
            var names = members
                .Where(m => m.Value != 0 && (currentBits & m.Value) == m.Value)
                .Select(m => m.Name)
                .ToArray();

            return names.Length == 0 ? zeroName : string.Join(", ", names);
        }

        private Control MakeUnsupportedCell(object? rawValue, PropertyMetadata prop)
        {
            return MakeBodyTextCell(
                rawValue?.ToString() ?? "(空)",
                CELL_MIN_WIDTH,
                $"复杂类型暂不支持直接编辑\n{GetPropertyTooltip(prop)}",
                new Color(0.72f, 0.72f, 0.72f));
        }

        private Control MakeHeaderCell(string text, float minWidth, float height)
        {
            var panel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(minWidth, height),
            };
            panel.AddThemeStyleboxOverride("panel", MakeHeaderStyle(new Color(0.18f, 0.20f, 0.24f)));

            var label = new Label
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            label.AddThemeColorOverride("font_color", new Color(0.88f, 0.92f, 1.0f));
            label.AddThemeFontSizeOverride("font_size", 12);
            panel.AddChild(label);
            return panel;
        }

        private Control MakeMetaHeaderCell(string text, float minWidth, float height, Color fontColor, string tooltip)
        {
            var panel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(minWidth, height),
                TooltipText = tooltip,
            };
            panel.AddThemeStyleboxOverride("panel", MakeHeaderStyle(new Color(0.14f, 0.15f, 0.18f)));

            var label = new Label
            {
                Text = string.IsNullOrWhiteSpace(text) ? "-" : text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TooltipText = tooltip,
            };
            label.AddThemeColorOverride("font_color", fontColor);
            label.AddThemeFontSizeOverride("font_size", 11);
            panel.AddChild(label);
            return panel;
        }

        private Control MakeRowNameCell(string text, float minWidth)
        {
            var panel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(minWidth, CELL_HEIGHT),
            };
            panel.AddThemeStyleboxOverride("panel", MakeHeaderStyle(new Color(0.16f, 0.18f, 0.22f)));

            var label = new Label
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.Off,
            };
            label.AddThemeColorOverride("font_color", new Color(0.85f, 0.90f, 0.95f));
            label.AddThemeConstantOverride("line_spacing", 0);
            panel.AddChild(label);
            return panel;
        }

        private Control MakeBodyTextCell(string text, float minWidth, string tooltip, Color fontColor)
        {
            var panel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(minWidth, CELL_HEIGHT),
                TooltipText = tooltip,
            };
            panel.AddThemeStyleboxOverride("panel", MakeBodyStyle());

            var label = new Label
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TooltipText = tooltip,
            };
            label.AddThemeColorOverride("font_color", fontColor);
            label.AddThemeFontSizeOverride("font_size", 11);
            panel.AddChild(label);
            return panel;
        }

        private static StyleBoxFlat MakeHeaderStyle(Color bgColor)
        {
            return new StyleBoxFlat
            {
                BgColor = bgColor,
                BorderColor = new Color(0.26f, 0.29f, 0.35f),
                BorderWidthBottom = 1,
                BorderWidthTop = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                ContentMarginLeft = 6,
                ContentMarginRight = 6,
                ContentMarginTop = 3,
                ContentMarginBottom = 3,
            };
        }

        private static StyleBoxFlat MakeBodyStyle()
        {
            return new StyleBoxFlat
            {
                BgColor = new Color(0.11f, 0.12f, 0.14f),
                BorderColor = new Color(0.20f, 0.22f, 0.26f),
                BorderWidthBottom = 1,
                BorderWidthTop = 1,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                ContentMarginLeft = 4,
                ContentMarginRight = 4,
                ContentMarginTop = 2,
                ContentMarginBottom = 2,
            };
        }

        private static Color GetCellTextColor(PropertyMetadata prop)
        {
            if (prop.IsNumeric) return new Color(0.60f, 0.90f, 0.70f);
            if (prop.IsBool) return new Color(0.90f, 0.75f, 0.50f);
            if (prop.IsString) return new Color(0.70f, 0.80f, 0.95f);
            if (prop.IsEnum) return new Color(0.95f, 0.80f, 0.65f);
            return new Color(0.90f, 0.90f, 0.90f);
        }

        private void RefreshBulkPropertySelector()
        {
            _bulkPropertySelector.Clear();

            if (_allProperties.Count == 0)
            {
                _bulkPropertySelector.Disabled = true;
                _bulkValueInput.Editable = false;
                _bulkApplyBtn.Disabled = true;
                _bulkValueInput.PlaceholderText = "批量值";
                return;
            }

            foreach (var prop in _allProperties)
            {
                int index = _bulkPropertySelector.ItemCount;
                _bulkPropertySelector.AddItem($"{prop.DisplayName} ({prop.Name})", index);
                _bulkPropertySelector.SetItemMetadata(index, prop.Name);
                _bulkPropertySelector.SetItemTooltip(index, GetPropertyTooltip(prop));
            }

            _bulkPropertySelector.Disabled = false;
            _bulkPropertySelector.Select(0);
            UpdateBulkEditorHint(GetSelectedBulkProperty());
        }

        private void OnBulkPropertySelected(long index)
        {
            UpdateBulkEditorHint(GetSelectedBulkProperty());
        }

        private void UpdateBulkEditorHint(PropertyMetadata? prop)
        {
            if (prop == null)
            {
                _bulkValueInput.Editable = false;
                _bulkApplyBtn.Disabled = true;
                _bulkValueInput.PlaceholderText = "批量值";
                _bulkValueInput.TooltipText = "";
                return;
            }

            _bulkValueInput.Editable = prop.IsString || prop.IsBool || prop.IsNumeric || prop.IsEnum;
            _bulkApplyBtn.Disabled = !_bulkValueInput.Editable;
            _bulkValueInput.Text = "";

            if (prop.IsFlags && prop.EnumType != null)
            {
                var flagNames = string.Join(", ", Enum.GetNames(prop.EnumType));
                _bulkValueInput.PlaceholderText = "例如: Manual, Periodic";
                _bulkValueInput.TooltipText = $"[Flags] 批量输入支持逗号分隔枚举名\n{flagNames}";
            }
            else if (prop.IsEnum && prop.EnumType != null)
            {
                var enumNames = string.Join(", ", Enum.GetNames(prop.EnumType));
                _bulkValueInput.PlaceholderText = "输入枚举名";
                _bulkValueInput.TooltipText = $"可用枚举值: {enumNames}";
            }
            else if (prop.IsBool)
            {
                _bulkValueInput.PlaceholderText = "true / false";
                _bulkValueInput.TooltipText = "批量布尔值";
            }
            else if (prop.IsPathString)
            {
                _bulkValueInput.PlaceholderText = "res://...";
                _bulkValueInput.TooltipText = "批量路径值，建议使用 res:// 路径";
            }
            else
            {
                _bulkValueInput.PlaceholderText = "输入批量值";
                _bulkValueInput.TooltipText = GetPropertyTooltip(prop);
            }
        }

        private void OnApplyBulkEdit()
        {
            var prop = GetSelectedBulkProperty();
            if (prop == null)
            {
                _detailLabel.Text = "[错误] 未选择批量属性";
                return;
            }

            if (_instances.Count == 0)
            {
                _detailLabel.Text = "[错误] 当前没有可编辑实例";
                return;
            }

            string rawText = _bulkValueInput.Text.Trim();
            try
            {
                string normalizedText = prop.IsPathString ? PathLineEdit.NormalizePath(rawText) : rawText;
                object? converted = ConvertValue(normalizedText, prop.PropertyType);

                foreach (var instance in _instances)
                    prop.PropertyInfo.SetValue(instance.Instance, converted);

                _modified = true;
                _saveBtn.Disabled = false;
                _detailLabel.Text = $"[批量] 已将 {prop.Name} 应用到 {_instances.Count} 个实例";
                RebuildGrid();
            }
            catch (Exception e)
            {
                _detailLabel.Text = $"[错误] 批量修改 {prop.Name} 失败: {e.Message}";
            }
        }

        private PropertyMetadata? GetSelectedBulkProperty()
        {
            if (_bulkPropertySelector.ItemCount == 0 || _bulkPropertySelector.Selected < 0)
                return null;

            string propName = _bulkPropertySelector.GetItemMetadata(_bulkPropertySelector.Selected).AsString();
            return _allProperties.FirstOrDefault(p => p.Name == propName);
        }

        private List<PropertyMetadata> GetFilteredProperties()
        {
            if (string.IsNullOrWhiteSpace(_searchFilter))
                return _allProperties;

            return _allProperties.Where(prop =>
            {
                if (prop.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                if (prop.DisplayName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                if (prop.DataKeyName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                if (prop.DataDescription.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                if (prop.CategoryName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;

                if (_comments.TryGetValue(prop.Name, out var comment))
                {
                    if (comment.Summary.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                    if (comment.Group.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)) return true;
                }

                return false;
            }).ToList();
        }

        private void OnCellEdited(PropertyMetadata prop, ConfigReflectionCache.InstanceInfo instance, string newValue)
        {
            try
            {
                string normalizedValue = prop.IsPathString ? PathLineEdit.NormalizePath(newValue) : newValue;
                object? converted = ConvertValue(normalizedValue, prop.PropertyType);
                prop.PropertyInfo.SetValue(instance.Instance, converted);
                _modified = true;
                _saveBtn.Disabled = false;

                _detailLabel.Text = $"[编辑] {instance.Name}.{prop.Name} = {normalizedValue}";
            }
            catch (Exception e)
            {
                _detailLabel.Text = $"[错误] {prop.Name}: {e.Message}";
            }
        }

        private static object? ConvertValue(string text, Type targetType)
        {
            Type actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (actualType == typeof(string))
                return text;

            if (actualType == typeof(int))
                return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue) ? intValue : 0;

            if (actualType == typeof(float))
                return float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue) ? floatValue : 0f;

            if (actualType == typeof(double))
                return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue) ? doubleValue : 0d;

            if (actualType == typeof(bool))
            {
                if (bool.TryParse(text, out bool boolValue))
                    return boolValue;
                return string.Equals(text, "1", StringComparison.OrdinalIgnoreCase);
            }

            if (actualType.IsEnum)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    var enumValues = Enum.GetValues(actualType);
                    return enumValues.Length > 0 ? enumValues.GetValue(0) : Activator.CreateInstance(actualType);
                }

                try
                {
                    return Enum.Parse(actualType, text, ignoreCase: true);
                }
                catch
                {
                    if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int enumNumeric))
                        return Enum.ToObject(actualType, enumNumeric);
                    throw;
                }
            }

            return text;
        }

        private void OnSearchChanged(string newText)
        {
            _searchFilter = newText.Trim();
            if (_currentType != null)
                RebuildGrid();
        }

        private void ToggleLayout()
        {
            _layoutRowsAreInstances = !_layoutRowsAreInstances;
            _toggleLayoutBtn.Text = _layoutRowsAreInstances ? "布局: 行=实例" : "布局: 行=属性";
            if (_currentType != null)
                RebuildGrid();
        }

        private void OnRefresh()
        {
            ConfigReflectionCache.ClearCache();
            EnumCommentCache.EnsureLoaded();
            if (_currentType != null)
            {
                LoadTypeData(_currentType);
                RebuildGrid();
            }
            _statusLabel.Text = "已刷新";
        }

        private void OnSave()
        {
            if (_currentSourceFile == null)
            {
                _statusLabel.Text = "未找到源文件，无法保存";
                return;
            }

            int saved = CsFileWriter.WriteAllChanges(_currentSourceFile, _instances, _allProperties, _comments);
            _modified = false;
            _saveBtn.Disabled = true;
            _statusLabel.Text = $"已保存 {saved} 个字段 → {Path.GetFileName(_currentSourceFile)}";
        }

        private string GetCategoryText(PropertyMetadata prop)
        {
            if (!string.IsNullOrWhiteSpace(prop.CategoryName))
                return prop.CategoryName;

            return GetPropertyGroup(prop);
        }

        private string GetDataKeyText(PropertyMetadata prop)
        {
            return prop.HasDataKey ? prop.DataKeyName : "-";
        }

        private string GetDataDescriptionText(PropertyMetadata prop)
        {
            if (!string.IsNullOrWhiteSpace(prop.DataDescription))
                return prop.DataDescription;

            return GetPropertySummary(prop);
        }

        private string BuildPropertyHeaderText(PropertyMetadata prop)
        {
            if (!string.Equals(prop.DisplayName, prop.Name, StringComparison.Ordinal))
                return $"{prop.DisplayName}\n{prop.Name}";

            string summary = GetPropertySummary(prop);
            if (!string.IsNullOrWhiteSpace(summary) && !string.Equals(summary, prop.Name, StringComparison.Ordinal))
                return $"{prop.Name}\n{summary}";

            return prop.Name;
        }

        private string GetPropertyTooltip(PropertyMetadata prop)
        {
            string summary = GetPropertySummary(prop);
            string group = GetPropertyGroup(prop);

            var lines = new List<string>
            {
                $"字段: {prop.Name}",
            };

            if (!string.IsNullOrWhiteSpace(prop.DisplayName) && prop.DisplayName != prop.Name)
                lines.Add($"显示名: {prop.DisplayName}");
            if (!string.IsNullOrWhiteSpace(prop.CategoryName))
                lines.Add($"分类: {prop.CategoryName}");
            if (!string.IsNullOrWhiteSpace(prop.DataKeyName))
                lines.Add($"DataKey: {prop.DataKeyName}");
            if (!string.IsNullOrWhiteSpace(prop.DataDescription))
                lines.Add($"DataKey说明: {prop.DataDescription}");
            if (!string.IsNullOrWhiteSpace(group))
                lines.Add($"源码分组: {group}");
            if (!string.IsNullOrWhiteSpace(summary))
                lines.Add($"源码注释: {summary}");

            return string.Join("\n", lines);
        }

        private string GetPropertySummary(PropertyMetadata prop)
        {
            return _comments.TryGetValue(prop.Name, out var info) ? info.Summary : "";
        }

        private string GetPropertyGroup(PropertyMetadata prop)
        {
            return _comments.TryGetValue(prop.Name, out var info) ? info.Group : "";
        }

        private Type? FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t => t.FullName == fullName);
        }
    }
}
#endif
