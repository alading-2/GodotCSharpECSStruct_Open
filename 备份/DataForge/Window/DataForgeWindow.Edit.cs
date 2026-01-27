#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataForge
{
    /// <summary>
    /// DataForge 增强版主窗口 - 编辑操作
    /// 
    /// 职责：
    /// - 行操作（新增、删除、复制、插入）
    /// - 剪贴板操作（复制、粘贴、剪切）
    /// - 单元格编辑（编辑、验证、枚举选择）
    /// </summary>
    public partial class DataForgeWindowEnhanced
    {
        #region === 行操作 ===

        private void OnAddRow()
        {
            SaveSnapshot();
            
            var newRow = new Dictionary<string, string>
            {
                { "RowID", $"New_{_currentTemplate?.Name}_{_tableData.Count + 1}" }
            };

            foreach (var col in _columns)
            {
                if (!string.IsNullOrEmpty(col.DefaultValue) && !newRow.ContainsKey(col.Key))
                {
                    newRow[col.Key] = col.DefaultValue;
                }
            }

            _tableData.Add(newRow);
            RenderTable(_searchBox?.Text);
            MarkDirty();
            UpdateStatus($"✅ 已添加新行: {newRow["RowID"]}");
        }

        private void InsertRowAbove()
        {
            var selected = _tree.GetSelected();
            if (selected == null) return;

            SaveSnapshot();
            int index = GetRowIndex(selected);
            InsertRowAt(index);
        }

        private void InsertRowBelow()
        {
            var selected = _tree.GetSelected();
            if (selected == null) return;

            SaveSnapshot();
            int index = GetRowIndex(selected);
            InsertRowAt(index + 1);
        }

        private void InsertRowAt(int index)
        {
            var newRow = new Dictionary<string, string>
            {
                { "RowID", $"New_{_currentTemplate?.Name}_{_tableData.Count + 1}" }
            };

            foreach (var col in _columns)
            {
                if (!string.IsNullOrEmpty(col.DefaultValue))
                    newRow[col.Key] = col.DefaultValue;
            }

            _tableData.Insert(index, newRow);
            RenderTable(_searchBox?.Text);
            MarkDirty();
        }

        private void OnRemoveSelectedRows()
        {
            if (_selectedRows.Count == 0)
            {
                var selected = _tree.GetSelected();
                if (selected != null)
                {
                    _selectedRows.Add(GetRowIndex(selected));
                }
            }

            if (_selectedRows.Count == 0)
            {
                UpdateStatus("⚠️ 请先选择要删除的行");
                return;
            }

            SaveSnapshot();

            var sortedIndices = _selectedRows.OrderByDescending(i => i).ToList();
            foreach (var index in sortedIndices)
            {
                if (index >= 0 && index < _tableData.Count)
                {
                    _tableData.RemoveAt(index);
                }
            }

            _selectedRows.Clear();
            RenderTable(_searchBox?.Text);
            MarkDirty();
            UpdateStatus($"✅ 已删除 {sortedIndices.Count} 行");
        }

        private void OnDuplicateSelectedRows()
        {
            if (_selectedRows.Count == 0)
            {
                var selected = _tree.GetSelected();
                if (selected != null)
                {
                    _selectedRows.Add(GetRowIndex(selected));
                }
            }

            if (_selectedRows.Count == 0)
            {
                UpdateStatus("⚠️ 请先选择要复制的行");
                return;
            }

            SaveSnapshot();

            var sortedIndices = _selectedRows.OrderBy(i => i).ToList();
            int offset = 0;
            
            foreach (var index in sortedIndices)
            {
                if (index + offset >= 0 && index + offset < _tableData.Count)
                {
                    var original = _tableData[index + offset];
                    var copy = new Dictionary<string, string>(original);
                    copy["RowID"] = original.GetValueOrDefault("RowID", "Copy") + "_Copy";
                    
                    _tableData.Insert(index + offset + 1, copy);
                    offset++;
                }
            }

            _selectedRows.Clear();
            RenderTable(_searchBox?.Text);
            MarkDirty();
            UpdateStatus($"✅ 已复制 {sortedIndices.Count} 行");
        }

        #endregion

        #region === 剪贴板操作 ===

        private void CopySelectedRows()
        {
            var selected = _tree.GetSelected();
            if (selected == null) return;

            int index = GetRowIndex(selected);
            if (index >= 0 && index < _tableData.Count)
            {
                _clipboard = new Dictionary<string, string>(_tableData[index]);
                UpdateStatus("✅ 已复制到剪贴板");
            }
        }

        private void CutSelectedRows()
        {
            CopySelectedRows();
            OnRemoveSelectedRows();
        }

        private void PasteRows()
        {
            if (_clipboard == null)
            {
                UpdateStatus("⚠️ 剪贴板为空");
                return;
            }

            SaveSnapshot();
            
            var newRow = new Dictionary<string, string>(_clipboard);
            newRow["RowID"] = _clipboard.GetValueOrDefault("RowID", "Paste") + "_Paste";
            
            _tableData.Add(newRow);
            RenderTable(_searchBox?.Text);
            MarkDirty();
            UpdateStatus("✅ 已粘贴");
        }

        private void ClearCellContent()
        {
            var selected = _tree.GetSelected();
            if (selected == null) return;

            SaveSnapshot();
            
            int rowIndex = GetRowIndex(selected);
            int colIndex = _tree.GetSelectedColumn();
            
            if (rowIndex >= 0 && rowIndex < _tableData.Count && colIndex >= 0 && colIndex < _columns.Count)
            {
                var colKey = _columns[colIndex].Key;
                _tableData[rowIndex][colKey] = "";
                RenderTable(_searchBox?.Text);
                MarkDirty();
            }
        }

        private void ShowRowDetails()
        {
            var selected = _tree.GetSelected();
            if (selected == null) return;

            int index = GetRowIndex(selected);
            if (index >= 0 && index < _tableData.Count)
            {
                var row = _tableData[index];
                var details = string.Join("\n", row.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                GD.Print($"[DataForge] 行详情:\n{details}");
                UpdateStatus($"📊 查看行 {index + 1} 详情（见控制台）");
            }
        }

        #endregion

        #region === 单元格编辑 ===

        private void OnItemEdited()
        {
            var item = _tree.GetEdited();
            int col = _tree.GetEditedColumn();
            if (item == null) return;

            SaveSnapshot();

            int rowIndex = GetRowIndex(item);
            if (rowIndex < 0 || rowIndex >= _tableData.Count) return;

            string colKey = _columns[col].Key;
            string newValue = item.GetText(col);
            
            if (!ValidateInput(_columns[col], newValue))
            {
                UpdateStatus($"⚠️ 输入验证失败: {colKey}");
                return;
            }

            _tableData[rowIndex][colKey] = newValue;

            if (string.IsNullOrEmpty(newValue))
            {
                item.SetCustomColor(col, new Color(1, 1, 1, 0.3f));
            }
            else
            {
                item.ClearCustomColor(col);
            }

            MarkDirty();
        }

        private bool ValidateInput(ColumnDefinition col, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return !col.IsRequired;
            }

            switch (col.FieldType)
            {
                case FieldType.Int:
                    return int.TryParse(value, out _);
                case FieldType.Float:
                    return float.TryParse(value, out _);
                case FieldType.Bool:
                    return bool.TryParse(value, out _);
            }

            return true;
        }

        private void OnItemDoubleClicked()
        {
            var selected = _tree.GetSelected();
            var col = _tree.GetSelectedColumn();
            if (selected == null || col < 0) return;

            var colDef = _columns[col];

            if (colDef.EditorType == FieldEditorType.Enum)
            {
                ShowEnumSelector(selected, col, colDef);
            }
            else if (colDef.EditorType == FieldEditorType.SubObject)
            {
                UpdateStatus($"⚠️ 子对象编辑器 [{colDef.Key}] 尚未实现");
            }
            else if (colDef.EditorType == FieldEditorType.ResourcePath)
            {
                UpdateStatus($"⚠️ 资源浏览器 [{colDef.Key}] 尚未实现");
            }
        }

        private void ShowEnumSelector(TreeItem item, int colIndex, ColumnDefinition colDef)
        {
            var enumOptions = GetEnumOptions(colDef.EnumTypeName);
            if (enumOptions == null) return;

            var popup = new PopupMenu();
            AddChild(popup);

            foreach (var option in enumOptions)
            {
                popup.AddItem(option);
            }

            popup.IndexPressed += (index) =>
            {
                item.SetText(colIndex, enumOptions[index]);
                int rowIndex = GetRowIndex(item);
                if (rowIndex >= 0 && rowIndex < _tableData.Count)
                {
                    _tableData[rowIndex][colDef.Key] = enumOptions[index];
                    MarkDirty();
                }
                popup.QueueFree();
            };

            popup.Popup(new Rect2I((Vector2I)GetViewport().GetMousePosition(), new Vector2I(200, 1)));
        }

        #endregion
    }
}
#endif
