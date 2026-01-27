#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DataForge
{
    /// <summary>
    /// DataForge 增强版主窗口 - 数据管理
    /// 
    /// 职责：
    /// - 模板加载和切换
    /// - DataKey 反射扫描
    /// - JSON 数据加载和保存
    /// - 撤销/重做系统
    /// </summary>
    public partial class DataForgeWindowEnhanced
    {
        #region === 模板加载 ===

        private void LoadTemplate(DataForgeTemplate template)
        {
            _currentTemplate = template;
            Title = $"DataForge Enhanced - {template.DisplayName}";

            ScanDataKeys();
            LoadFromJson();
            RenderTable();
            UpdateSheetTabStyles();

            UpdateStatus($"已加载 {template.DisplayName} - {_tableData.Count} 行数据");
        }

        #endregion

        #region === DataKey 反射扫描 ===

        private void ScanDataKeys()
        {
            _columns.Clear();

            _columns.Add(new ColumnDefinition
            {
                Key = "RowID",
                DisplayName = "🔑 ID",
                FieldType = FieldType.String,
                IsRequired = true,
                MinWidth = 150
            });

            try
            {
                var dataKeyType = Type.GetType("DataKey");
                if (dataKeyType == null)
                {
                    GD.PrintErr("[DataForge] 无法找到 DataKey 类型!");
                    return;
                }

                var fields = dataKeyType
                    .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                    .Select(fi => fi.GetValue(null)?.ToString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                var priorityKeys = new[] { "Name", "Team", "EntityType", "BaseHp", "BaseAttack", "MoveSpeed", "Description" };
                var sortedFields = priorityKeys
                    .Where(k => fields.Contains(k))
                    .Concat(fields.Except(priorityKeys))
                    .Distinct()
                    .ToList();

                foreach (var key in sortedFields)
                {
                    var colDef = CreateColumnDefinition(key);
                    _columns.Add(colDef);
                }

                GD.Print($"[DataForge] 扫描到 {_columns.Count} 个数据键");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DataForge] 反射 DataKey 失败: {ex.Message}");
            }
        }

        private ColumnDefinition CreateColumnDefinition(string key)
        {
            var col = new ColumnDefinition
            {
                Key = key,
                DisplayName = AddIconToFieldName(key),
                FieldType = InferFieldType(key),
                MinWidth = 120
            };

            if (_currentTemplate?.FieldOverrides != null &&
                _currentTemplate.FieldOverrides.TryGetValue(key, out var ov))
            {
                col.EditorType = ov.EditorType;
                col.EnumTypeName = ov.EnumTypeName;
                col.SubObjectTypeName = ov.SubObjectTypeName;
                col.CodeTemplate = ov.CodeTemplate;
                col.DefaultValue = ov.DefaultValue;
            }

            return col;
        }

        private string AddIconToFieldName(string key)
        {
            if (key.Contains("Hp")) return "❤️ " + key;
            if (key.Contains("Attack") || key.Contains("Damage")) return "⚔️ " + key;
            if (key.Contains("Speed")) return "🏃 " + key;
            if (key.Contains("Defense")) return "🛡️ " + key;
            if (key.Contains("Range")) return "🎯 " + key;
            if (key.Contains("Cooldown") || key.Contains("Time")) return "⏱️ " + key;
            if (key.Contains("Name")) return "📝 " + key;
            if (key.Contains("Description")) return "📄 " + key;
            if (key.Contains("Type")) return "🏷️ " + key;
            if (key.Contains("Team")) return "👥 " + key;
            return key;
        }

        private FieldType InferFieldType(string key)
        {
            if (key.StartsWith("Is") || key.StartsWith("Can") || key.Contains("Enable"))
                return FieldType.Bool;

            var floatKeywords = new[] { "Hp", "Damage", "Speed", "Range", "Time", "Cooldown", "Rate", "Interval", "Height", "Mana", "Defense", "Regen", "Reduction", "Steal" };
            if (floatKeywords.Any(k => key.Contains(k)))
                return FieldType.Float;

            var intKeywords = new[] { "Count", "Level", "Max", "Min", "Wave", "Reward", "Charges", "Index" };
            if (intKeywords.Any(k => key.Contains(k)))
                return FieldType.Int;

            return FieldType.String;
        }

        #endregion

        #region === JSON 存档 ===

        private void SaveToJson()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(_tableData, options);

                EnsureDirectoryExists(_currentTemplate.SavePath);

                using var file = FileAccess.Open(_currentTemplate.SavePath, FileAccess.ModeFlags.Write);
                if (file == null)
                {
                    UpdateStatus($"❌ 保存失败: 无法打开文件 {_currentTemplate.SavePath}");
                    return;
                }
                file.StoreString(json);

                _isDirty = false;
                UpdateSaveButtonStyle();
                UpdateStatus($"✅ 已保存至 {_currentTemplate.SavePath} ({DateTime.Now:HH:mm:ss})");
            }
            catch (Exception e)
            {
                GD.PrintErr($"[DataForge] 保存失败: {e}");
                UpdateStatus($"❌ 保存失败: {e.Message}");
            }
        }

        private void LoadFromJson()
        {
            _tableData.Clear();

            if (!FileAccess.FileExists(_currentTemplate.SavePath))
            {
                UpdateStatus($"⚠️ 文件不存在，创建新数据表: {_currentTemplate.SavePath}");
                return;
            }

            try
            {
                using var file = FileAccess.Open(_currentTemplate.SavePath, FileAccess.ModeFlags.Read);
                if (file == null) return;

                string json = file.GetAsText();
                var data = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
                if (data != null)
                {
                    _tableData = data;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"[DataForge] 加载失败: {e}");
                UpdateStatus($"❌ 加载失败: {e.Message}");
            }
        }

        #endregion

        #region === 撤销/重做系统 ===

        private void SaveSnapshot()
        {
            var snapshot = new DataSnapshot
            {
                Data = _tableData.Select(row => new Dictionary<string, string>(row)).ToList(),
                Timestamp = DateTime.Now
            };

            _undoStack.Push(snapshot);
            _redoStack.Clear();

            if (_undoStack.Count > 50)
            {
                var temp = _undoStack.ToList();
                temp.RemoveAt(0);
                _undoStack = new Stack<DataSnapshot>(temp.AsEnumerable().Reverse());
            }
        }

        private void Undo()
        {
            if (_undoStack.Count == 0)
            {
                UpdateStatus("⚠️ 没有可撤销的操作");
                return;
            }

            var currentSnapshot = new DataSnapshot
            {
                Data = _tableData.Select(row => new Dictionary<string, string>(row)).ToList(),
                Timestamp = DateTime.Now
            };
            _redoStack.Push(currentSnapshot);

            var snapshot = _undoStack.Pop();
            _tableData = snapshot.Data.Select(row => new Dictionary<string, string>(row)).ToList();

            RenderTable(_searchBox?.Text);
            MarkDirty();
            UpdateStatus("↶ 已撤销");
        }

        private void Redo()
        {
            if (_redoStack.Count == 0)
            {
                UpdateStatus("⚠️ 没有可重做的操作");
                return;
            }

            var currentSnapshot = new DataSnapshot
            {
                Data = _tableData.Select(row => new Dictionary<string, string>(row)).ToList(),
                Timestamp = DateTime.Now
            };
            _undoStack.Push(currentSnapshot);

            var snapshot = _redoStack.Pop();
            _tableData = snapshot.Data.Select(row => new Dictionary<string, string>(row)).ToList();

            RenderTable(_searchBox?.Text);
            MarkDirty();
            UpdateStatus("↷ 已重做");
        }

        #endregion
    }
}
#endif
