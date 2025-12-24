using Godot;

public partial class Player : CharacterBody2D
{
	private static readonly Log _log = new("Player");

	public override void _Ready()
	{
		base._Ready();
		_log.Info("玩家已使用资源中的数据初始化。");
	}


}
