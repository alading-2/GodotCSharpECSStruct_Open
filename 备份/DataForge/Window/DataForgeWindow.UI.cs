#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace DataForge
{
    /// <summary>
    /// DataForge 增强版主窗口 - UI 构建
    /// 
    /// 职责：
    /// - 构建主界面布局（工具栏、表格、Sheet标签、状态栏）
    /// - Sheet标签页切换逻辑
    /// - 表格渲染和视觉增强
    /// - 搜索过滤功能
    /// </summary>
    public partial class DataForgeWindowEnhanced
    {
        #region === UI 构建 ===

        private void BuildUI()
        {
            var root = new VBoxContainer();
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            root.AddThemeConstantOverride("separation", 0);
            AddChild(root);

            BuildToolbar(root);
            BuildTable(root);
            BuildSheetTabs(root);
            BuildStatusBar(root);
        }

        private void BuildToolbar(VBoxContainer root)
        {
            var toolbar = new PanelContainer();
            var toolbarBg = new StyleBoxFlat();
            toolbarBg.BgColor = new Color(0.15f, 0.15f, 0.15f);
            toolbar.AddThemeStyleboxOverride("panel", toolbarBg);
            root.AddChild(toolbar);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 8);
            hbox.CustomMinimumSize = new Vector2(0, 48);
            toolbar.AddChild(hbox);

            var searchLabel = new Label { Text = "🔍", CustomMinimumSize = new Vector2(32, 0) };
            hbox.AddChild(searchLabel);

            _searchBox = new LineEdit
            {
                PlaceholderText = "搜索 RowID 或任意字段...",
                CustomMinimumSize = new Vector2(250, 0)
            };
            _searchBox.TextChanged += OnSearchTextChanged;
            hbox.AddChild(_searchBox);

            hbox.AddChild(new VSeparator());

            var btnAdd = CreateToolButton("➕ 新增", "添加新行 (Ctrl+N)");
            btnAdd.Pressed += OnAddRow;
            hbox.AddChild(btnAdd);

            var btnDel = CreateToolButton("➖ 删除", "删除选中行 (Delete)");
            btnDel.Pressed += OnRemoveSelectedRows;
            hbox.AddChild(btnDel);

            var btnDup = CreateToolButton("📋 复制", "复制选中行 (Ctrl+D)");
            btnDup.Pressed += OnDuplicateSelectedRows;
            hbox.AddChild(btnDup);

            hbox.AddChild(new VSeparator());

            var btnUndo = CreateToolButton("↶ 撤销", "撤销 (Ctrl+Z)");
            btnUndo.Pressed += Undo;
            hbox.AddChild(btnUndo);

            var btnRedo = CreateToolButton("↷ 重做", "重做 (Ctrl+Y)");
            btnRedo.Pressed += Redo;
            hbox.AddChild(btnRedo);

            var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            hbox.AddChild(spacer);

            _btnSave = CreateToolButton("💾 保存", "保存到JSON (Ctrl+S)");
            _btnSave.Pressed += SaveToJson;
            hbox.AddChild(_btnSave);

            _btnGenerate = CreateToolButton("🔥 生成代码", "生成C#代码 (Ctrl+G)");
            _btnGenerate.Modulate = new Color(0.6f, 1f, 0.6f);
            _btnGenerate.Pressed += GenerateCode;
            hbox.AddChild(_btnGenerate);
        }

        private Button CreateToolButton(string text, string tooltip)
        {
            var btn = new Button
            {
                Text = text,
                TooltipText = tooltip,
                CustomMinimumSize = new Vector2(100, 32)
            };
            return btn;
        }

        private void BuildTable(VBoxContainer root)
        {
            var scrollContainer = new ScrollContainer
            {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            root.AddChild(scrollContainer);

            _tree = new Tree
            {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ColumnTitlesVisible = true,
                HideRoot = true,
                AllowRmbSelect = true,
                SelectMode = Tree.SelectModeEnum.Multi,
                AllowReselect = true
            };
            _tree.ItemEdited += OnItemEdited;
            _tree.ItemActivated += OnItemDoubleClicked;
            _tree.ItemSelected += OnItemSelected;
            _tree.GuiInput += OnTreeGuiInput;
            scrollContainer.AddChild(_tree);
        }

        private void BuildSheetTabs(VBoxContainer root)
        {
            var tabPanel = new PanelContainer();
            var tabBg = new StyleBoxFlat();
            tabBg.BgColor = new Color(0.12f, 0.12f, 0.12f);
            tabPanel.AddThemeStyleboxOverride("panel", tabBg);
            root.AddChild(tabPanel);

            _sheetTabsContainer = new HBoxContainer();
            _sheetTabsContainer.AddThemeConstantOverride("separation", 2);
            _sheetTabsContainer.CustomMinimumSize = new Vector2(0, 40);
            tabPanel.AddChild(_sheetTabsContainer);

            foreach (var template in Templates)
            {
                var btn = CreateSheetTabButton(template);
                _sheetTabsContainer.AddChild(btn);
                _sheetButtons[template.Name] = btn;
            }

            var btnAddSheet = new Button
            {
                Text = "➕",
                TooltipText = "添加新数据表（未来功能）",
                CustomMinimumSize = new Vector2(40, 32),
                Disabled = true
            };
            _sheetTabsContainer.AddChild(btnAddSheet);
        }

        private Button CreateSheetTabButton(DataForgeTemplate template)
        {
            var btn = new Button
            {
                Text = $"{template.Icon} {template.DisplayName}",
                TooltipText = $"切换到 {template.DisplayName}",
                CustomMinimumSize = new Vector2(150, 32),
                ToggleMode = true
            };
            btn.Pressed += () => OnSheetTabClicked(template);
            return btn;
        }

        private void BuildStatusBar(VBoxContainer root)
        {
            var statusPanel = new PanelContainer();
            var statusBg = new StyleBoxFlat();
            statusBg.BgColor = new Color(0.1f, 0.1f, 0.1f);
            statusPanel.AddThemeStyleboxOverride("panel", statusBg);
            root.AddChild(statusPanel);

            var hbox = new HBoxContainer();
            hbox.CustomMinimumSize = new Vector2(0, 28);
            statusPanel.AddChild(hbox);

            _statusLabel = new Label
            {
                Text = "就绪",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center
            };
            hbox.AddChild(_statusLabel);

            var infoLabel = new Label
            {
                Text = "💡 提示: 右键单元格查看更多操作",
                VerticalAlignment = VerticalAlignment.Center,
                Modulate = new Color(0.7f, 0.7f, 0.7f)
            };
            hbox.AddChild(infoLabel);
        }

        #endregion

        #region === Sheet标签页切换 ===

        private void OnSheetTabClicked(DataForgeTemplate template)
        {
            if (_isDirty && _currentTemplate != template)
            {
                GD.Print("[DataForge] 警告: 切换前请先保存当前数据");
            }
            LoadTemplate(template);
            UpdateSheetTabStyles();
        }

        private void UpdateSheetTabStyles()
        {
            foreach (var kvp in _sheetButtons)
            {
                kvp.Value.ButtonPressed = (kvp.Key == _currentTemplate?.Name);
            }
        }

        #endregion

        #region === 表格渲染 ===

        private void RenderTable(string? filter = null)
        {
            _tree.Clear();
            _tree.Columns = _columns.Count;

            for (int i = 0; i < _columns.Count; i++)
            {
                _tree.SetColumnTitle(i, _columns[i].DisplayName);
                _tree.SetColumnCustomMinimumWidth(i, _columns[i].MinWidth);
                _tree.SetColumnExpand(i, true);
            }

            var root = _tree.CreateItem();

            foreach (var rowData in _tableData)
            {
                if (!string.IsNullOrEmpty(filter) && !MatchesFilter(rowData, filter))
                    continue;

                var item = _tree.CreateItem(root);
                RenderRow(item, rowData);
            }

            UpdateStatus($"显示 {_tree.GetRoot()?.GetChildCount() ?? 0} 行数据");
        }

        private bool MatchesFilter(Dictionary<string, string> rowData, string filter)
        {
            return rowData.Values.Any(v => v.Contains(filter, System.StringComparison.OrdinalIgnoreCase));
        }

        private void RenderRow(TreeItem item, Dictionary<string, string> rowData)
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                var col = _columns[i];
                string val = rowData.GetValueOrDefault(col.Key, "");

                item.SetText(i, val);
                item.SetEditable(i, true);

                if (string.IsNullOrEmpty(val))
                {
                    item.SetCustomColor(i, new Color(1, 1, 1, 0.3f));
                }
                else
                {
                    item.ClearCustomColor(i);

                    switch (col.FieldType)
                    {
                        case FieldType.Float:
                            item.SetCustomColor(i, new Color(0.6f, 0.8f, 1f));
                            break;
                        case FieldType.Int:
                            item.SetCustomColor(i, new Color(0.8f, 1f, 0.6f));
                            break;
                        case FieldType.Bool:
                            item.SetCustomColor(i, new Color(1f, 0.8f, 0.6f));
                            break;
                    }
                }

                if (col.EditorType == FieldEditorType.Enum && !string.IsNullOrEmpty(col.EnumTypeName))
                {
                    var enumOptions = GetEnumOptions(col.EnumTypeName);
                    if (enumOptions != null)
                    {
                        item.SetTooltipText(i, $"可选值: {string.Join(", ", enumOptions)}");
                    }
                }
            }
        }

        private void OnSearchTextChanged(string newText)
        {
            RenderTable(newText);
        }

        private void OnItemSelected()
        {
            var selected = _tree.GetSelected();
            if (selected != null)
            {
                int rowIndex = GetRowIndex(selected);
                _selectedRows.Add(rowIndex);
                _lastSelectedRow = rowIndex;
            }
        }

        #endregion
    }
}
#endif
