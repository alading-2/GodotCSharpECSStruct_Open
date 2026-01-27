#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataForge
{
    /// <summary>
    /// DataForge 增强版主窗口 - 核心逻辑
    /// 
    /// 职责：
    /// - 字段定义（模板、UI组件、数据状态）
    /// - 生命周期管理（_Ready、_Input、OnCloseRequested）
    /// - 快捷键处理（Ctrl+S/Z/Y/C/V/X/D/N/G）
    /// - 上下文菜单（右键菜单、单元格编辑菜单）
    /// - 辅助方法（MarkDirty、UpdateStatus、GetEnumOptions等）
    /// </summary>
    [Tool]
    public partial class DataForgeWindowEnhanced : Window
    {
        #region === 模板配置 ===
        
        private static readonly DataForgeTemplate[] Templates = new[]
        {
            new DataForgeTemplate
            {
                Name = "EnemyData",
                DisplayName = "敌人配置",
                Icon = "👹",
                SavePath = "res://addons/DataForge/Data/EnemyData.json",
                OutputPath = "res://Data/Data/Unit/Enemy/EnemyData.cs",
                ClassName = "EnemyData",
                FieldOverrides = new Dictionary<string, FieldOverride>
                {
                    ["Team"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "Team", DefaultValue = "Enemy" },
                    ["EntityType"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "EntityType", DefaultValue = "Unit" },
                    ["VisualScenePath"] = new() { EditorType = FieldEditorType.ResourcePath, CodeTemplate = "ResourceManagement.GetPath(\"{0}\")" },
                    ["SpawnRule"] = new() { EditorType = FieldEditorType.SubObject, SubObjectTypeName = "SpawnRule" },
                }
            },
            new DataForgeTemplate
            {
                Name = "PlayerData",
                DisplayName = "玩家配置",
                Icon = "🎮",
                SavePath = "res://addons/DataForge/Data/PlayerData.json",
                OutputPath = "res://Data/Data/Unit/Player/PlayerData.cs",
                ClassName = "PlayerData",
                FieldOverrides = new Dictionary<string, FieldOverride>
                {
                    ["Team"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "Team", DefaultValue = "Player" },
                    ["EntityType"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "EntityType", DefaultValue = "Unit" },
                    ["VisualScenePath"] = new() { EditorType = FieldEditorType.ResourcePath, CodeTemplate = "ResourceManagement.GetPath(\"{0}\")" },
                }
            },
            new DataForgeTemplate
            {
                Name = "AbilityData",
                DisplayName = "技能配置",
                Icon = "⚡",
                SavePath = "res://addons/DataForge/Data/AbilityData.json",
                OutputPath = "res://Data/Data/Ability/AbilityData.cs",
                ClassName = "AbilityData",
                FieldOverrides = new Dictionary<string, FieldOverride>
                {
                    ["EntityType"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "EntityType", DefaultValue = "Ability" },
                    ["AbilityType"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityType" },
                    ["AbilityTriggerMode"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityTriggerMode" },
                    ["AbilityTargetGeometry"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityTargetGeometry" },
                    ["AbilityTargetTeamFilter"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityTargetTeamFilter" },
                    ["AbilityTargetSorting"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityTargetSorting" },
                    ["AbilityCostType"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityCostType" },
                    ["AbilityTargetSelection"] = new() { EditorType = FieldEditorType.Enum, EnumTypeName = "AbilityTargetSelection" },
                }
            }
        };

        #endregion

        #region === UI 组件 ===
        
        private Tree? _tree;
        private Label? _statusLabel;
        private LineEdit? _searchBox;
        private Button? _btnSave;
        private Button? _btnGenerate;
        private HBoxContainer? _sheetTabsContainer;
        private PopupMenu? _contextMenu;
        private PopupMenu? _cellEditorMenu;
        private Dictionary<string, Button> _sheetButtons = new();

        #endregion

        #region === 数据状态 ===
        
        private DataForgeTemplate? _currentTemplate;
        private List<Dictionary<string, string>> _tableData = new();
        private List<ColumnDefinition> _columns = new();
        private bool _isDirty = false;
        
        private Stack<DataSnapshot> _undoStack = new();
        private Stack<DataSnapshot> _redoStack = new();
        
        private HashSet<int> _selectedRows = new();
        private int _lastSelectedRow = -1;
        
        private Dictionary<string, string>? _clipboard;

        #endregion

        #region === 生命周期 ===

        public override void _Ready()
        {
            Title = "DataForge Enhanced - 数据锻造台";
            Size = new Vector2I(1600, 900);
            MinSize = new Vector2I(1000, 600);
            Exclusive = false;

            BuildUI();
            SetupContextMenu();
            SetupShortcuts();

            CloseRequested += OnCloseRequested;

            if (Templates.Length > 0)
            {
                LoadTemplate(Templates[0]);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                HandleKeyboardShortcut(keyEvent);
            }
        }

        private void OnCloseRequested()
        {
            if (_isDirty)
            {
                GD.Print("[DataForge] 警告: 有未保存的修改");
            }
            QueueFree();
        }

        #endregion

        #region === 快捷键系统 ===

        private void SetupShortcuts()
        {
            // 快捷键在 _Input 中处理
        }

        private void HandleKeyboardShortcut(InputEventKey keyEvent)
        {
            bool ctrl = keyEvent.CtrlPressed;
            bool shift = keyEvent.ShiftPressed;

            if (ctrl && keyEvent.Keycode == Key.S)
            {
                SaveToJson();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.Z)
            {
                Undo();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.Y)
            {
                Redo();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.C)
            {
                CopySelectedRows();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.V)
            {
                PasteRows();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.X)
            {
                CutSelectedRows();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.D)
            {
                OnDuplicateSelectedRows();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.N)
            {
                OnAddRow();
                GetViewport().SetInputAsHandled();
            }
            else if (ctrl && keyEvent.Keycode == Key.G)
            {
                GenerateCode();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Delete)
            {
                OnRemoveSelectedRows();
                GetViewport().SetInputAsHandled();
            }
        }

        #endregion

        #region === 上下文菜单 ===

        private void SetupContextMenu()
        {
            _contextMenu = new PopupMenu();
            _contextMenu.Name = "ContextMenu";
            AddChild(_contextMenu);

            _contextMenu.AddItem("➕ 在上方插入行", 0);
            _contextMenu.AddItem("➕ 在下方插入行", 1);
            _contextMenu.AddSeparator();
            _contextMenu.AddItem("➖ 删除选中行", 2);
            _contextMenu.AddItem("📋 复制行", 3);
            _contextMenu.AddItem("📄 粘贴行", 4);
            _contextMenu.AddItem("✂️ 剪切行", 5);
            _contextMenu.AddSeparator();
            _contextMenu.AddItem("🧹 清除内容", 6);
            _contextMenu.AddItem("📊 查看详情", 7);

            _contextMenu.IndexPressed += OnContextMenuItemSelected;

            _cellEditorMenu = new PopupMenu();
            _cellEditorMenu.Name = "CellEditorMenu";
            AddChild(_cellEditorMenu);
            _cellEditorMenu.IndexPressed += OnCellEditorMenuItemSelected;
        }

        private void OnTreeGuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
                {
                    _contextMenu?.Popup(new Rect2I((Vector2I)mouseEvent.GlobalPosition, new Vector2I(1, 1)));
                }
            }
        }

        private void OnContextMenuItemSelected(long index)
        {
            switch (index)
            {
                case 0: InsertRowAbove(); break;
                case 1: InsertRowBelow(); break;
                case 2: OnRemoveSelectedRows(); break;
                case 3: CopySelectedRows(); break;
                case 4: PasteRows(); break;
                case 5: CutSelectedRows(); break;
                case 6: ClearCellContent(); break;
                case 7: ShowRowDetails(); break;
            }
        }

        private void OnCellEditorMenuItemSelected(long index)
        {
            // 根据单元格类型显示不同的编辑选项
        }

        #endregion

        #region === 辅助方法 ===

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateSaveButtonStyle();
        }

        private void UpdateSaveButtonStyle()
        {
            if (_btnSave != null)
            {
                _btnSave.Modulate = _isDirty ? new Color(1f, 0.8f, 0.6f) : Colors.White;
            }
        }

        private void UpdateStatus(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = message;
            }
            GD.Print($"[DataForge] {message}");
        }

        private void EnsureDirectoryExists(string resPath)
        {
            string globalPath = ProjectSettings.GlobalizePath(resPath);
            string dir = System.IO.Path.GetDirectoryName(globalPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
        }

        private int GetRowIndex(TreeItem item)
        {
            if (item == null) return -1;
            var root = _tree?.GetRoot();
            if (root == null) return -1;

            int index = 0;
            var child = root.GetFirstChild();
            while (child != null)
            {
                if (child == item) return index;
                child = child.GetNext();
                index++;
            }
            return -1;
        }

        private string[]? GetEnumOptions(string? enumTypeName)
        {
            if (string.IsNullOrEmpty(enumTypeName)) return null;
            try
            {
                var type = Type.GetType(enumTypeName);
                if (type != null && type.IsEnum)
                {
                    return Enum.GetNames(type);
                }
            }
            catch { }
            return null;
        }

        #endregion
    }
}
#endif
