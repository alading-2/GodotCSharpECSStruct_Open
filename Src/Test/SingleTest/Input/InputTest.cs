using Godot;
using System.Collections.Generic;


namespace BrotatoMy.Test;

/// <summary>
/// 输入系统测试 - 使用 InputManager 演示版
/// <para>本类展示如何使用 InputManager 简化输入处理：</para>
/// <list type="bullet">
/// <item>无需重复写 Input.IsActionPressed 判断</item>
/// <item>统一的震动反馈接口</item>
/// <item>实时显示左右摇杆的坐标值</item>
/// <item>响应手柄热插拔事件</item>
/// </list>
/// </summary>
public partial class InputTest : Node2D
{
    // --- UI 节点引用 ---
    [Export] private Label _logLabel = null!;
    [Export] private Label _vibrationLabel = null!;
    [Export] private Label _stickLabel = null!;
    [Export] private Label _systemLabel = null!; // 新增：系统状态标签

    // --- 数据存储 ---
    private readonly List<string> _logs = [];
    private const int MaxLogLines = 15; // 稍微减少行数，让界面更整洁

    public override void _Ready()
    {
        _logLabel ??= GetNode<Label>("CanvasLayer/Label");
        _vibrationLabel ??= GetNode<Label>("CanvasLayer/VibrationLabel");
        _stickLabel ??= GetNode<Label>("CanvasLayer/StickLabel");
        _systemLabel ??= GetNodeOrNull<Label>("CanvasLayer/SystemLabel"); // 尝试获取新标签

        Log("=== 动作历史 (Action History) ===");

        UpdateSystemStatus();
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnJoyConnectionChanged;
        InputManager.StopVibration();
    }

    public override void _Process(double delta)
    {
        ProcessAnalogs();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsEcho()) return;

        // --- 核心动作检测 ---
        // 使用 InputManager 简化按钮检测，只记录有意义的动作
        if (InputManager.IsConfirm())
        {
            Log("🟢 [确认] A键 / Space");
            InputManager.VibrateLight();
        }

        if (InputManager.IsCancel())
        {
            Log("🔴 [取消] B键 / ESC");
            InputManager.VibrateHeavy();
        }

        if (InputManager.IsAttack())
        {
            Log("⚔️ [攻击] X键");
            InputManager.VibrateMedium();
        }

        if (InputManager.IsSpecial())
        {
            Log("🌟 [特殊] Y键");
        }

        if (InputManager.IsPause())
        {
            Log("⏸️ [暂停] Start / ESC");
        }

        if (InputManager.IsSelect())
        {
            Log("📋 [选择] Select / Back");
        }

        if (InputManager.IsHome())
        {
            Log("🏠 [主页] Home");
        }

        if (InputManager.IsLeftBumper())
        {
            Log("🛡️ [LB] 左肩键");
            InputManager.Vibrate(0.8f, 0.0f, 0.1f);
        }

        if (InputManager.IsRightBumper())
        {
            Log("🎯 [RB] 右肩键");
            InputManager.Vibrate(0.0f, 1.0f, 0.1f);
        }

        // 扳机检测（只在刚按下时触发）
        if (InputManager.IsActionJustPressed("BtnLT"))
        {
            float strength = InputManager.GetLeftTriggerStrength();
            Log($"📥 [LT] 深度: {strength:P0}");
        }

        if (InputManager.IsActionJustPressed("BtnRT"))
        {
            float strength = InputManager.GetRightTriggerStrength();
            Log($"📤 [RT] 深度: {strength:P0}");
            InputManager.Vibrate(strength * 0.5f, strength, 0.2f);
        }
    }

    private void ProcessAnalogs()
    {
        // 使用 InputManager 获取摇杆输入
        var move = InputManager.GetMoveInput();
        var aim = InputManager.GetAimInput();

        // 实时更新 UI 显示摇杆数值
        UpdateStickUI(move, aim);
    }

    private void UpdateStickUI(Vector2 move, Vector2 aim)
    {
        _stickLabel.Text = "=== 摇杆实时数值 ===\n" +
                           $"左摇杆 (Move): ({move.X,5:F2}, {move.Y,5:F2})\n" +
                           $"右摇杆 (Aim) : ({aim.X,5:F2}, {aim.Y,5:F2})";

        // 如果有明显输入，改变颜色提示
        if (move.LengthSquared() > 0.01f || aim.LengthSquared() > 0.01f)
            _stickLabel.Modulate = Colors.Cyan;
        else
            _stickLabel.Modulate = Colors.White;
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        UpdateSystemStatus();
        string status = connected ? "已连接" : "已断开";
        Log($"💻 [系统] 手柄 {device} {status}");
    }

    private void UpdateSystemStatus()
    {
        var joys = InputManager.GetConnectedJoypads();
        string statusText = "=== 设备状态 ===\n";

        if (joys.Count == 0)
        {
            statusText += "未检测到手柄 (仅键盘可用)";
        }
        else
        {
            foreach (var id in joys)
            {
                statusText += $"[ID:{id}] {InputManager.GetJoypadName(id)}\n";
            }
        }

        if (_systemLabel != null)
        {
            _systemLabel.Text = statusText;
        }
        else
        {
            // 如果没有独立标签，则暂时打印到日志
            // Log(statusText);
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
