using Godot;
using System.Linq;
using System.Text;

namespace BrotatoMy.Tools;

/// <summary>
/// 输入映射文档生成器 (Input Mapping Document Generator)
/// <para>功能：自动遍历 Godot 项目的输入映射表 (Input Map)，并将其导出为 Markdown 文档。</para>
/// <para>用法：在场景中添加此节点，在检查器 (Inspector) 中点击 "Generate" 复选框即可触发生成。</para>
/// </summary>
[Tool]
public partial class InputMap : Node
{
    private static readonly Log _log = new("InputMap");

    private bool _generate;

    /// <summary>
    /// 触发生成文档的操作开关。
    /// 在编辑器中点击此复选框会调用 GenerateDocs 方法。
    /// </summary>
    [Export]
    public bool Generate
    {
        get => _generate;
        set
        {
            _generate = value;
            if (_generate)
            {
                GenerateDocs();
                _generate = false; // 执行后自动重置，方便下次点击
            }
        }
    }

    /// <summary>
    /// Markdown 文档的输出路径 (res:// 路径)。
    /// </summary>
    [Export(PropertyHint.File, "*.md")]
    public string OutputPath { get; set; } = "res://Src/Tools/InputMap/InputMap.md";

    /// <summary>
    /// 执行文档生成逻辑。
    /// </summary>
    private void GenerateDocs()
    {
        _log.Info("开始生成输入映射文档...");

        var sb = new StringBuilder();
        sb.AppendLine("# 输入映射文档 (Input Mapping Documentation)");
        sb.AppendLine();
        sb.AppendLine("> 由 InputMap 工具自动生成，请勿手动修改此文件。");
        sb.AppendLine();
        sb.AppendLine("## 动作映射详情 (Action Mappings)");
        sb.AppendLine();
        sb.AppendLine("| 动作名称 (Action) | 输入类型 (Type) | 详细配置 (Details) |");
        sb.AppendLine("| :--- | :--- | :--- |");

        // 获取所有输入动作并按字母顺序排序
        // 注意：由于类名也叫 InputMap，这里必须显式指定 Godot.InputMap
        var actions = Godot.InputMap.GetActions().OrderBy(x => x.ToString()).ToList();

        foreach (var actionName in actions)
        {
            string actionStr = actionName.ToString();

            // 过滤掉 Godot 内置的编辑器动作，只保留项目自定义动作
            if (actionStr.StartsWith("spatial_editor") || actionStr.StartsWith("editor_")) continue;

            var events = Godot.InputMap.ActionGetEvents(actionName);
            if (events.Count == 0)
            {
                sb.AppendLine($"| `{actionStr}` | - | *(无绑定事件)* |");
                continue;
            }

            foreach (var inputEvent in events)
            {
                string type = GetSimpleTypeName(inputEvent);
                string desc = GetEventDescription(inputEvent);
                sb.AppendLine($"| `{actionStr}` | {type} | {desc} |");
            }
        }

        // 确保输出目录存在
        string dir = OutputPath.GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(dir))
        {
            DirAccess.MakeDirRecursiveAbsolute(dir);
            _log.Debug($"创建目录: {dir}");
        }

        // 写入文件
        using var file = FileAccess.Open(OutputPath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(sb.ToString());
            _log.Success($"成功生成输入文档: {OutputPath}");
        }
        else
        {
            _log.Error($"无法写入文件，路径可能无效: {OutputPath}");
        }
    }

    /// <summary>
    /// 获取输入事件的简易类型名称（中文）。
    /// </summary>
    private string GetSimpleTypeName(InputEvent e)
    {
        if (e is InputEventKey) return "键盘 (Keyboard)";
        if (e is InputEventJoypadButton) return "手柄按钮 (Joypad Button)";
        if (e is InputEventJoypadMotion) return "手柄轴 (Joypad Axis)";
        if (e is InputEventMouseButton) return "鼠标按钮 (Mouse Button)";
        return "其他 (Other)";
    }

    /// <summary>
    /// 获取输入事件的详细描述文字（中文）。
    /// </summary>
    private string GetEventDescription(InputEvent e)
    {
        if (e is InputEventKey key)
        {
            var code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            return $"{code} ({key.AsText()})";
        }
        if (e is InputEventJoypadButton joyBtn)
        {
            return $"按钮 {joyBtn.ButtonIndex} ({joyBtn.ButtonIndex})";
        }
        if (e is InputEventJoypadMotion joyAxis)
        {
            string dir = joyAxis.AxisValue > 0 ? "正向 (+)" : "负向 (-)";
            return $"轴 {joyAxis.Axis} ({dir})";
        }
        return e.AsText();
    }
}
