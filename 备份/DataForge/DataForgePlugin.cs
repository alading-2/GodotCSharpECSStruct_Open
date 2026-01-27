#if TOOLS
using Godot;

namespace DataForge
{
    /// <summary>
    /// DataForge Enhanced 插件入口
    /// 
    /// 功能说明：
    /// - 将数据编辑工具挂载到 Godot 编辑器菜单栏
    /// - 管理编辑器窗口的生命周期（创建、显示、销毁）
    /// - 提供 Excel 级别的数据编辑体验
    /// 
    /// 使用方式：
    /// 1. 在 Godot 编辑器中启用插件（Project -> Project Settings -> Plugins）
    /// 2. 点击菜单 Project -> Tools -> DataForge Enhanced
    /// 3. 在弹出的窗口中编辑游戏配置数据
    /// </summary>
    [Tool]
    public partial class DataForgePlugin : EditorPlugin
{
    // 菜单项名称
    private const string MENU_NAME = "DataForge Enhanced - 数据锻造台";
    
    // 编辑器窗口实例（单例模式，避免重复创建）
    private DataForgeWindowEnhanced _window;

    /// <summary>
    /// 插件启用时调用
    /// 
    /// 执行时机：
    /// - 首次启用插件时
    /// - 编辑器重新加载时
    /// 
    /// 功能：
    /// - 在编辑器菜单栏添加工具入口
    /// - 打印加载日志
    /// </summary>
    public override void _EnterTree()
    {
        // 添加菜单项到 Project -> Tools -> DataForge Enhanced
        AddToolMenuItem(MENU_NAME, new Callable(this, nameof(OpenDataForge)));
        GD.Print($"[DataForge] 插件已加载 v2.0 - 菜单路径: Project -> Tools -> {MENU_NAME}");
    }

    /// <summary>
    /// 插件禁用时调用
    /// 
    /// 执行时机：
    /// - 手动禁用插件时
    /// - 编辑器关闭时
    /// 
    /// 功能：
    /// - 从菜单栏移除工具入口
    /// - 清理窗口资源
    /// </summary>
    public override void _ExitTree()
    {
        RemoveToolMenuItem(MENU_NAME);
        CloseWindow();
        GD.Print("[DataForge] 插件已卸载");
    }

    /// <summary>
    /// 打开 DataForge Enhanced 编辑器窗口
    /// 
    /// 实现逻辑：
    /// - 单例模式：如果窗口已存在，直接显示并聚焦
    /// - 首次创建：实例化窗口并添加到编辑器根节点
    /// - 居中显示：自动计算屏幕中心位置
    /// 
    /// 注意事项：
    /// - 窗口可以被用户关闭，但不会自动销毁
    /// - 再次打开时会复用已有实例（保留编辑状态）
    /// </summary>
    private void OpenDataForge()
    {
        // 检查窗口是否已创建且有效
        if (_window == null || !IsInstanceValid(_window))
        {
            // 创建新的增强版窗口实例
            _window = new DataForgeWindowEnhanced();
            
            // 将窗口添加到编辑器根节点（确保在编辑器UI层级中）
            EditorInterface.Singleton.GetBaseControl().AddChild(_window);
            
            // 居中显示窗口
            _window.PopupCentered();
            
            GD.Print("[DataForge] 已创建新窗口实例");
        }
        else
        {
            // 窗口已存在，直接显示并聚焦
            _window.PopupCentered();
            _window.GrabFocus();
            
            GD.Print("[DataForge] 已显示现有窗口");
        }
    }

    /// <summary>
    /// 关闭并销毁窗口
    /// 
    /// 调用时机：
    /// - 插件被禁用时
    /// - 编辑器关闭时
    /// 
    /// 功能：
    /// - 安全地释放窗口资源
    /// - 清空窗口引用
    /// 
    /// 注意：
    /// - 使用 QueueFree() 而不是 Free()，避免在渲染过程中销毁节点
    /// - 检查 IsInstanceValid() 确保对象未被提前释放
    /// </summary>
    private void CloseWindow()
    {
        if (_window != null && IsInstanceValid(_window))
        {
            _window.QueueFree();
            _window = null;
            GD.Print("[DataForge] 窗口已销毁");
        }
    }
}
}
#endif
