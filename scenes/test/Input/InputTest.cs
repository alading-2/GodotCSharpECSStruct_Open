using Godot;
using System.Collections.Generic;
using System.Linq;

namespace BrotatoMy.Test;

/// <summary>
/// 输入系统测试 - 深度重构版
/// <para>本类展示了在 Godot 4.x 中使用 C# 处理输入的高级模式：</para>
/// <list type="bullet">
/// <item>PascalCase 映射: 与 project.godot 中的输入动作名称一致。</item>
/// <item>记录类型 (record): 轻量级震动配置数据结构。</item>
/// <item>模拟量反馈: 实时显示左右摇杆的坐标值。</item>
/// <item>事件驱动: 响应手柄热插拔事件。</item>
/// </list>
/// </summary>
public partial class InputTest : Node2D
{
	// --- UI 节点引用 ---
	[Export] private Label _logLabel = null!;
	[Export] private Label _vibrationLabel = null!;
	[Export] private Label _stickLabel = null!;

	// --- 数据存储 ---
	private readonly List<string> _logs = [];
	private const int MaxLogLines = 20;

	// --- 震动状态变量 ---
	private float _vibrationTimer;
	private VibrationProfile? _currentVibration;

	/// <summary>
	/// 定义震动配置记录 (C# record)
	/// </summary>
	private record VibrationProfile(float Weak, float Strong, float Duration, string Description);

	/// <summary>
	/// 动作映射表：驱动化设计的核心
	/// 直接使用字符串，保持简单直观
	/// </summary>
	private static readonly Dictionary<string, (string Desc, VibrationProfile? Vib)> ActionMap = new()
	{
		["BtnA"] = ("A键 (确认/跳跃)", new(0.5f, 0.0f, 0.15f, "轻触反馈")),
		["BtnB"] = ("B键 (取消/后退)", new(0.0f, 1.0f, 0.30f, "重击反馈")),
		["BtnX"] = ("X键 (攻击)", new(0.5f, 0.5f, 0.20f, "混合震动")),
		["BtnY"] = ("Y键 (特殊)", null),

		["BtnLB"] = ("LB (左肩键)", new(0.8f, 0.0f, 0.10f, "左侧点震")),
		["BtnRB"] = ("RB (右肩键)", new(0.0f, 1.0f, 0.10f, "右侧点震")),

		["BtnStart"] = ("Start (暂停)", null),
		["BtnSelect"] = ("Select (地图)", null),

		["UiUp"] = ("UI 上", null),
		["UiDown"] = ("UI 下", null),
		["UiLeft"] = ("UI 左", null),
		["UiRight"] = ("UI 右", null),
	};

	public override void _Ready()
	{
		_logLabel ??= GetNode<Label>("CanvasLayer/Label");
		_vibrationLabel ??= GetNode<Label>("CanvasLayer/VibrationLabel");
		_stickLabel ??= GetNode<Label>("CanvasLayer/StickLabel");

		Log("=== 输入系统测试 (C# PascalCase) ===");
		Log("等待输入...");

		CheckConnectedControllers();
		Input.JoyConnectionChanged += OnJoyConnectionChanged;
	}

	public override void _ExitTree()
	{
		Input.JoyConnectionChanged -= OnJoyConnectionChanged;
		StopVibration();
	}

	public override void _Process(double delta)
	{
		ProcessVibration((float)delta);
		ProcessAnalogs();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsEcho()) return;

		foreach (var (action, (desc, vib)) in ActionMap)
		{
			if (@event.IsActionPressed(action))
			{
				Log($"[按钮] {desc}");
				if (vib != null) TriggerVibration(vib);
			}
		}

		HandleTrigger("BtnLT", "LT");
		HandleTrigger("BtnRT", "RT", useVibration: true);
	}

	private void HandleTrigger(string action, string name, bool useVibration = false)
	{
		if (Input.IsActionJustPressed(action))
		{
			float strength = Input.GetActionStrength(action);
			Log($"[扳机] {name} 按下 (力度: {strength:P0})");

			if (useVibration)
			{
				TriggerVibration(new(strength * 0.5f, strength, 0.2f, "扳机动态反馈"));
			}
		}
	}

	private void ProcessAnalogs()
	{
		// 获取左摇杆/WASD
		var move = Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown");

		// 获取右摇杆
		var aim = Input.GetVector("StickRightLeft", "StickRightRight", "StickRightUp", "StickRightDown");

		// 实时更新 UI 显示摇杆数值，解决“没反应”的问题
		UpdateStickUI(move, aim);
	}

	private void UpdateStickUI(Vector2 move, Vector2 aim)
	{
		_stickLabel.Text = "=== 摇杆数值 (实时) ===\n" +
						   $"左摇杆 (Move): ({move.X,5:F2}, {move.Y,5:F2})\n" +
						   $"右摇杆 (Aim) : ({aim.X,5:F2}, {aim.Y,5:F2})";

		// 如果有明显输入，改变颜色提示
		if (move.LengthSquared() > 0.01f || aim.LengthSquared() > 0.01f)
			_stickLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		else
			_stickLabel.RemoveThemeColorOverride("font_color");
	}

	private void ProcessVibration(float delta)
	{
		if (_vibrationTimer > 0)
		{
			_vibrationTimer -= delta;
			if (_vibrationTimer <= 0)
			{
				StopVibration();
			}
			else if (_currentVibration != null)
			{
				UpdateVibrationUI(_currentVibration, _vibrationTimer);
			}
		}
	}

	private void TriggerVibration(VibrationProfile profile)
	{
		_currentVibration = profile;
		_vibrationTimer = profile.Duration;
		Input.StartJoyVibration(0, profile.Weak, profile.Strong, profile.Duration);
		UpdateVibrationUI(profile, profile.Duration);
	}

	private void StopVibration()
	{
		Input.StopJoyVibration(0);
		_vibrationTimer = 0;
		_currentVibration = null;
		_vibrationLabel.Text = "震动状态: 空闲";
		_vibrationLabel.RemoveThemeColorOverride("font_color");
	}

	private void UpdateVibrationUI(VibrationProfile profile, float remaining)
	{
		_vibrationLabel.Text = $"震动中: {profile.Description}\n" +
							   $"强度: L:{profile.Weak:F1} / H:{profile.Strong:F1}\n" +
							   $"剩余: {remaining:F2}s";
		_vibrationLabel.AddThemeColorOverride("font_color", Colors.Salmon);
	}

	private void OnJoyConnectionChanged(long device, bool connected)
	{
		string status = connected ? "已连接" : "已断开";
		Log($"[系统] 手柄 {device} {status}");
		if (connected)
		{
			Log($"      型号: {Input.GetJoyName((int)device)}");
		}
	}

	private void CheckConnectedControllers()
	{
		var joys = Input.GetConnectedJoypads();
		if (joys.Count == 0)
		{
			Log("[系统] 未检测到手柄");
		}
		else
		{
			foreach (var id in joys)
			{
				Log($"[系统] 检测到手柄 ID: {id}, {Input.GetJoyName(id)}");
			}
		}
	}

	private void Log(string message)
	{
		_logs.Insert(0, message);
		if (_logs.Count > MaxLogLines)
		{
			_logs.RemoveAt(_logs.Count - 1);
		}
		_logLabel.Text = string.Join('\n', _logs);
	}
}
