using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 输入映射测试脚本
/// 用于测试所有手柄和键盘输入是否正常工作
/// 支持功能：
/// - 移动输入测试（左摇杆/方向键）
/// - 右摇杆输入测试
/// - UI导航测试（方向键）
/// - 面部按钮测试（ABXY）
/// - 肩键测试（LB/RB）
/// - 扳机键测试（LT/RT）
/// - 系统按钮测试（Start/Select）
/// - 手柄震动测试
/// - 手柄连接状态检测
/// </summary>
public partial class InputTest : Node2D
{
	// UI元素引用
	private Label _label; // 主日志显示标签
	private Label _vibrationLabel; // 震动状态显示标签

	// 日志相关变量
	private List<string> _testLog = new List<string>(); // 存储测试日志的列表
	private const int MaxLogLines = 20; // 最大显示日志行数

	// 震动相关变量
	private float _vibrationTimer = 0.0f; // 震动持续时间计时器

	/// <summary>
	/// 节点就绪时调用
	/// 初始化测试环境和显示信息
	/// </summary>
	public override void _Ready()
	{
		// 获取UI元素引用
		_label = GetNode<Label>("CanvasLayer/Label");
		_vibrationLabel = GetNode<Label>("CanvasLayer/VibrationLabel");

		// 添加测试标题和说明信息
		AddLog("=== 输入映射测试 ===");
		AddLog("开始测试所有输入...");
		AddLog("请按任意按钮测试");
		AddLog("");
		
		// 初始化震动状态显示
		UpdateVibrationStatus(0, 0, 0);
	}

	/// <summary>
	/// 每帧调用的处理函数
	/// 处理震动计时器和模拟输入测试
	/// </summary>
	/// <param name="delta">帧间隔时间（秒）</param>
	public override void _Process(double delta)
	{
		// 处理震动计时器
		if (_vibrationTimer > 0)
		{
			_vibrationTimer -= (float)delta;
			// 震动结束后重置状态
			if (_vibrationTimer <= 0)
			{
				UpdateVibrationStatus(0, 0, 0);
			}
		}

		// 测试移动输入（左摇杆/方向键）
		// 使用 GetVector 获取模拟输入，支持平滑过渡
		Vector2 moveDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (moveDir.Length() > 0.1f) // 阈值过滤，避免微小输入触发
		{
			AddLog($"移动: {moveDir.X:F2}, {moveDir.Y:F2}");
		}

		// 测试右摇杆输入
		Vector2 stickRight = Input.GetVector("stick_right_left", "stick_right_right", "stick_right_up", "stick_right_down");
		if (stickRight.Length() > 0.1f) // 阈值过滤
		{
			AddLog($"右摇杆: {stickRight.X:F2}, {stickRight.Y:F2}");
		}
	}

	/// <summary>
	/// 处理输入事件
	/// 检测并记录所有按键和按钮的按下事件
	/// </summary>
	/// <param name="event">输入事件对象</param>
	public override void _Input(InputEvent @event)
	{
		// 测试 UI 导航键
		if (@event.IsActionPressed("ui_up"))
		{
			AddLog("✓ UI 上");
		}
		if (@event.IsActionPressed("ui_down"))
		{
			AddLog("✓ UI 下");
		}
		if (@event.IsActionPressed("ui_left"))
		{
			AddLog("✓ UI 左");
		}
		if (@event.IsActionPressed("ui_right"))
		{
			AddLog("✓ UI 右");
		}

		// 测试面部按钮 (ABXY)
		if (@event.IsActionPressed("btn_a"))
		{
			AddLog("✓ A 键：确认/跳跃 (轻震动)");
			// 轻微震动：低频 0.5，持续 0.2秒
			StartVibration(0.5f, 0.0f, 0.2f, "A键: 轻震动");
		}

		if (@event.IsActionPressed("btn_b"))
		{
			AddLog("✓ B 键：取消/后退 (重震动)");
			// 强烈震动：高频 1.0，持续 0.5秒
			StartVibration(0.0f, 1.0f, 0.5f, "B键: 重震动");
		}

		if (@event.IsActionPressed("btn_x"))
		{
			AddLog("✓ X 键：攻击/菜单 (混合震动)");
			// 混合震动：低频 0.5 + 高频 0.5，持续 0.3秒
			StartVibration(0.5f, 0.5f, 0.3f, "X键: 混合震动");
		}

		if (@event.IsActionPressed("btn_y"))
		{
			AddLog("✓ Y 键：特殊动作/物品栏");
		}

		// 测试肩键 (LB/RB)
		if (@event.IsActionPressed("btn_lb"))
		{
			AddLog("✓ LB：左肩键 (心跳震动)");
			// 心跳震动模拟：低频 0.8，持续 0.1秒
			StartVibration(0.8f, 0.0f, 0.1f, "LB: 心跳(砰)");
		}

		if (@event.IsActionPressed("btn_rb"))
		{
			AddLog("✓ RB：右肩键 (高频震动)");
			// 高频点震：高频 1.0，持续 0.1秒
			StartVibration(0.0f, 1.0f, 0.1f, "RB: 高频点震");
		}

		// 测试扳机键 (LT/RT)
		if (@event.IsActionPressed("btn_lt"))
		{
			// 获取扳机键的按压强度 (0.0-1.0)
			float strength = Input.GetActionStrength("btn_lt");
			AddLog($"✓ LT：左扳机 (力度: {strength:F2})");
		}

		if (@event.IsActionPressed("btn_rt"))
		{
			// 获取扳机键的按压强度 (0.0-1.0)
			float strength = Input.GetActionStrength("btn_rt");
			AddLog($"✓ RT：右扳机 (力度: {strength:F2})");

			// 根据扳机力度产生震动反馈
			if (strength > 0.1f) // 阈值过滤
			{
				// 震动强度与扳机力度成正比
				StartVibration(strength * 0.5f, strength, 0.1f, "RT: 扳机反馈");
			}
		}

		// 测试系统按钮
		if (@event.IsActionPressed("btn_start"))
		{
			AddLog("✓ Start：暂停/菜单");
		}
		if (@event.IsActionPressed("btn_select"))
		{
			AddLog("✓ Select：地图/信息");
		}
	}

	/// <summary>
	/// 添加日志信息到显示
	/// </summary>
	/// <param name="message">要添加的日志消息</param>
	private void AddLog(string message)
	{
		// 添加新消息到列表开头（最新消息在顶部）
		_testLog.Insert(0, message);

		// 限制日志行数，超过最大行数则删除最旧的消息
		if (_testLog.Count > MaxLogLines)
		{
			_testLog.RemoveAt(_testLog.Count - 1);
		}

		// 更新UI显示
		UpdateLabel();
	}

	/// <summary>
	/// 更新日志标签显示
	/// 将日志列表内容转换为文本并显示在标签上
	/// </summary>
	private void UpdateLabel()
	{
		if (_label != null) // 确保标签存在
		{
			_label.Text = string.Join("\n", _testLog); // 将日志列表用换行符连接为字符串
		}
	}

	/// <summary>
	/// 处理节点通知
	/// </summary>
	/// <param name="what">通知类型常量</param>
	public override void _Notification(int what)
	{
		// 当窗口获得焦点时检测手柄连接状态
		if (what == NotificationWMWindowFocusIn)
		{
			CheckJoypadConnection();
		}
	}

	/// <summary>
	/// 检测并记录连接的手柄信息
	/// </summary>
	private void CheckJoypadConnection()
	{
		// 获取所有连接的手柄ID列表
		Godot.Collections.Array<int> joypads = Input.GetConnectedJoypads();
		if (joypads.Count > 0)
		{
			// 遍历所有连接的手柄，记录其名称和ID
			foreach (int joypadId in joypads)
			{
				string joypadName = Input.GetJoyName(joypadId);
				AddLog($"手柄已连接: {joypadName} (ID: {joypadId})");
			}
		}
		else
		{
			AddLog("未检测到手柄");
		}
	}

	/// <summary>
	/// 启动手柄震动
	/// </summary>
	/// <param name="weak">低频马达强度 (0.0-1.0)</param>
	/// <param name="strong">高频马达强度 (0.0-1.0)</param>
	/// <param name="duration">震动持续时间 (秒)</param>
	/// <param name="desc">震动描述信息</param>
	private void StartVibration(float weak, float strong, float duration, string desc = "")
	{
		// 启动手柄震动（使用第一个手柄ID 0）
		Input.StartJoyVibration(0, weak, strong, duration);
		// 设置震动计时器
		_vibrationTimer = duration;
		// 更新震动状态显示
		UpdateVibrationStatus(weak, strong, duration, desc);
	}

	/// <summary>
	/// 更新震动状态显示
	/// </summary>
	/// <param name="weak">低频马达强度 (0.0-1.0)</param>
	/// <param name="strong">高频马达强度 (0.0-1.0)</param>
	/// <param name="duration">震动剩余时间 (秒)</param>
	/// <param name="desc">震动描述信息</param>
	private void UpdateVibrationStatus(float weak, float strong, float duration, string desc = "")
	{
		if (_vibrationLabel != null) // 确保震动标签存在
		{
			if (duration > 0) // 正在震动中
			{
				// 更新震动状态文本
				_vibrationLabel.Text = $"震动状态: {desc}\n低频强度: {weak:F2}\n高频强度: {strong:F2}\n剩余时间: {duration:F2} s";
				// 设置震动中颜色为红色
				_vibrationLabel.AddThemeColorOverride("font_color", new Color(1, 0.2f, 0.2f));
			}
			else // 震动已停止
			{
				// 更新为待机状态文本
				_vibrationLabel.Text = "震动状态: 无\n低频强度: 0.00\n高频强度: 0.00\n等待触发...";
				// 设置待机颜色为橙色
				_vibrationLabel.AddThemeColorOverride("font_color", new Color(1, 0.5f, 0));
			}
		}
	}
}
