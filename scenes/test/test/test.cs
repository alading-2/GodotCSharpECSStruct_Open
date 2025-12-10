using Godot;

// 测试节点类，用于演示Godot属性导出功能
public partial class Test : Node2D
{
	// 导出属性分组：将相关属性组织在一起，便于在编辑器中管理
	[ExportGroup("My Properties")]
	[Export] // 导出到Godot编辑器
	public int Number { get; set; } = 3; // 默认值为3的整数属性

	// 导出属性子分组：在分组内创建更细致的分类
	[ExportSubgroup("Extra Properties")]
	[Export]
	public string Text { get; set; } = ""; // 空字符串属性，用于存储文本数据
	[Export]
	public bool Flag { get; set; } = false; // 布尔标志，默认关闭

	// 导出属性分类：创建新的属性分类区域
	[ExportCategory("Main Category")]
	[Export]
	public int Number2 { get; set; } = 3; // 第二个整数属性，与Number类似但属于不同分类
	[Export]
	public string Text2 { get; set; } = ""; // 第二个字符串属性

	// 额外的属性分类
	[ExportCategory("Extra Category")]
	[Export]
	public bool Flag2 { get; set; } = false; // 第二个布尔标志属性
}
