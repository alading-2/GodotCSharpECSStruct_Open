using Godot;

public partial class Enemy : CharacterBody2D
{
	private static readonly Log _log = new("Enemy");

	public override void _Ready()
	{
		base._Ready();
		_log.Debug($"敌人 {Name} 已初始化。");
	}
}
