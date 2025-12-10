using Godot;

public partial class Test : Node
{
	[ExportGroup("My Properties")]
	[Export]
	public int Number { get; set; } = 3;

	[ExportSubgroup("Extra Properties")]
	[Export]
	public string Text { get; set; } = "";
	[Export]
	public bool Flag { get; set; } = false;

	[ExportCategory("Main Category")]
	[Export]
	public int Number2 { get; set; } = 3;
	[Export]
	public string Text2 { get; set; } = "";

	[ExportCategory("Extra Category")]
	[Export]
	public bool Flag2 { get; set; } = false;
}
