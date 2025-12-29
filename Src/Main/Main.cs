using Godot;
using System;

public partial class Main : Node
{
	private static readonly Log _log = new Log("Main");
	public override void _Ready()
	{
		EventBus.TriggerGameStart();
		_log.Info("游戏主场景初始化完成");
	}
	public override void _Process(double delta)
	{

	}
}
