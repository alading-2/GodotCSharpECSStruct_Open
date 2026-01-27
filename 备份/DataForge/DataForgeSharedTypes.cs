#if TOOLS
using System;
using System.Collections.Generic;

namespace DataForge
{
    /// <summary>
    /// 数据模板定义
    /// 
    /// 功能：
    /// - 定义一种游戏配置数据类型（如敌人、玩家、技能）
    /// - 指定数据的存储路径和代码生成路径
    /// - 配置字段的特殊处理规则（枚举、资源路径等）
    /// </summary>
    public class DataForgeTemplate
    {
        /// <summary>内部名称（英文标识符）</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>显示名称（中文，显示在UI上）</summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>图标（Emoji，显示在Sheet标签上）</summary>
        public string Icon { get; set; } = "📊";
        
        /// <summary>JSON存档路径（res://开头的Godot资源路径）</summary>
        public string SavePath { get; set; } = string.Empty;
        
        /// <summary>C#代码输出路径（res://开头的Godot资源路径）</summary>
        public string OutputPath { get; set; } = string.Empty;
        
        /// <summary>生成的C#类名</summary>
        public string ClassName { get; set; } = string.Empty;
        
        /// <summary>字段覆盖配置（键为字段名，值为覆盖规则）</summary>
        public Dictionary<string, FieldOverride> FieldOverrides { get; set; } = new();
    }

    /// <summary>
    /// 字段覆盖配置
    /// 
    /// 功能：
    /// - 为特定字段指定编辑器类型（枚举下拉、资源浏览器等）
    /// - 设置默认值
    /// - 配置代码生成模板
    /// </summary>
    public class FieldOverride
    {
        /// <summary>编辑器类型（文本、枚举、子对象、资源路径）</summary>
        public FieldEditorType EditorType { get; set; }
        
        /// <summary>枚举类型名称（当 EditorType 为 Enum 时使用）</summary>
        public string? EnumTypeName { get; set; }
        
        /// <summary>子对象类型名称（当 EditorType 为 SubObject 时使用）</summary>
        public string? SubObjectTypeName { get; set; }
        
        /// <summary>代码生成模板（{0}会被替换为实际值）</summary>
        public string? CodeTemplate { get; set; }
        
        /// <summary>默认值（新增行时自动填充）</summary>
        public string? DefaultValue { get; set; }
    }

    /// <summary>
    /// 字段编辑器类型
    /// 
    /// 说明：
    /// - Text: 普通文本输入框
    /// - Enum: 枚举下拉选择器（需配合 EnumTypeName）
    /// - SubObject: 子对象编辑器（如 SpawnRule）
    /// - ResourcePath: 资源路径浏览器（选择场景、纹理等）
    /// </summary>
    public enum FieldEditorType
    {
        Text,           // 普通文本输入框
        Enum,           // 枚举下拉选择器
        SubObject,      // 子对象编辑器
        ResourcePath    // 资源路径浏览器
    }

    /// <summary>
    /// 字段值类型
    /// 
    /// 说明：
    /// - 用于类型推断和代码生成
    /// - 影响单元格的视觉样式（颜色）
    /// - 决定代码生成时的格式化方式
    /// </summary>
    public enum FieldType
    {
        String,  // 字符串类型（生成为 "value"）
        Float,   // 浮点数类型（生成为 value.0f）
        Int,     // 整数类型（生成为 value）
        Bool     // 布尔类型（生成为 true/false）
    }

    /// <summary>
    /// 列定义（字段元数据）
    /// 
    /// 功能：
    /// - 描述表格中一列的所有属性
    /// - 包含字段名、显示名、类型、编辑器等信息
    /// - 用于渲染表格和生成代码
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>字段键名（对应 DataKey 中的常量）</summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>显示名称（表格列标题，可包含图标）</summary>
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>字段值类型（String/Float/Int/Bool）</summary>
        public FieldType FieldType { get; set; }
        
        /// <summary>编辑器类型（Text/Enum/SubObject/ResourcePath）</summary>
        public FieldEditorType EditorType { get; set; }
        
        /// <summary>枚举类型名称（EditorType 为 Enum 时使用）</summary>
        public string? EnumTypeName { get; set; }
        
        /// <summary>子对象类型名称（EditorType 为 SubObject 时使用）</summary>
        public string? SubObjectTypeName { get; set; }
        
        /// <summary>代码生成模板（{0}会被替换为实际值）</summary>
        public string? CodeTemplate { get; set; }
        
        /// <summary>默认值（新增行时自动填充）</summary>
        public string? DefaultValue { get; set; }
        
        /// <summary>是否必填（必填字段不能为空）</summary>
        public bool IsRequired { get; set; }
        
        /// <summary>最小列宽（像素）</summary>
        public int MinWidth { get; set; } = 100;
    }

    /// <summary>
    /// 数据快照（用于撤销/重做系统）
    /// 
    /// 功能：
    /// - 保存某个时间点的完整表格数据
    /// - 包含时间戳用于调试
    /// 
    /// 注意：
    /// - 快照是深拷贝，不会被后续修改影响
    /// - 栈最多保存50个快照，超出后删除最旧的
    /// </summary>
    public class DataSnapshot
    {
        /// <summary>表格数据（深拷贝）</summary>
        public List<Dictionary<string, string>> Data { get; set; } = new();
        
        /// <summary>快照创建时间（用于调试）</summary>
        public DateTime Timestamp { get; set; }
    }
}
#endif
