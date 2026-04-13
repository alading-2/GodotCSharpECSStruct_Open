using Godot;

/// <summary>
/// 输入映射管理器 (Input Mapping Manager)
/// <para>功能：封装 Godot 输入系统，提供统一的按钮/摇杆查询接口。</para>
/// <para>用法：其他脚本直接调用静态方法，无需重复写 Input.IsActionPressed 判断。</para>
/// <para>示例：if (InputManager.IsConfirm()) { ... }</para>
/// </summary>
public static class InputManager
{
    // ==================== 按钮状态查询 ====================

    /// <summary>确认键（A键/空格）是否刚按下</summary>
    public static bool IsConfirm() => Godot.Input.IsActionJustPressed("BtnA");

    /// <summary>取消键（B键/ESC）是否刚按下</summary>
    public static bool IsCancel() => Godot.Input.IsActionJustPressed("BtnB");

    /// <summary>攻击键（X键）是否刚按下</summary>
    public static bool IsX() => Godot.Input.IsActionJustPressed("BtnX");

    /// <summary>特殊键（Y键）是否刚按下</summary>
    public static bool IsY() => Godot.Input.IsActionJustPressed("BtnY");

    /// <summary>暂停键（Start/ESC）是否刚按下</summary>
    public static bool IsPause() => Godot.Input.IsActionJustPressed("BtnStart");

    /// <summary>选择键（Select/Back）是否刚按下</summary>
    public static bool IsSelect() => Godot.Input.IsActionJustPressed("BtnSelect");

    /// <summary>Home键是否刚按下</summary>
    public static bool IsHome() => Godot.Input.IsActionJustPressed("BtnHome");

    /// <summary>左肩键（LB）是否刚按下</summary>
    public static bool IsLeftBumper() => Godot.Input.IsActionJustPressed("BtnLB");

    /// <summary>右肩键（RB）是否刚按下</summary>
    public static bool IsRightBumper() => Godot.Input.IsActionJustPressed("BtnRB");

    /// <summary>左扳机（LT）是否按下（支持模拟量）</summary>
    public static bool IsLeftTrigger() => Godot.Input.IsActionPressed("BtnLT");

    /// <summary>右扳机（RT）是否按下（支持模拟量）</summary>
    public static bool IsRightTrigger() => Godot.Input.IsActionPressed("BtnRT");

    /// <summary>获取左扳机力度（0.0 ~ 1.0）</summary>
    public static float GetLeftTriggerStrength() => Godot.Input.GetActionStrength("BtnLT");

    /// <summary>获取右扳机力度（0.0 ~ 1.0）</summary>
    public static float GetRightTriggerStrength() => Godot.Input.GetActionStrength("BtnRT");

    // ==================== 摇杆/方向输入 ====================

    /// <summary>
    /// 获取移动输入向量（左摇杆/WASD）
    /// <para>返回归一化的 Vector2，范围 [-1, 1]</para>
    /// </summary>
    public static Vector2 GetMoveInput()
    {
        return Godot.Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown");
    }

    /// <summary>
    /// 获取瞄准输入向量（右摇杆）
    /// <para>返回归一化的 Vector2，范围 [-1, 1]</para>
    /// </summary>
    public static Vector2 GetAimInput()
    {
        return Godot.Input.GetVector("StickRightLeft", "StickRightRight", "StickRightUp", "StickRightDown");
    }

    /// <summary>
    /// 获取移动输入向量（未归一化，保留原始强度）
    /// <para>用于需要精确控制速度的场景</para>
    /// </summary>
    public static Vector2 GetMoveInputRaw()
    {
        float x = Godot.Input.GetAxis("MoveLeft", "MoveRight");
        float y = Godot.Input.GetAxis("MoveUp", "MoveDown");
        return new Vector2(x, y);
    }

    // ==================== 震动反馈 ====================

    /// <summary>
    /// 触发手柄震动
    /// </summary>
    /// <param name="weakMagnitude">弱马达强度（0.0 ~ 1.0）</param>
    /// <param name="strongMagnitude">强马达强度（0.0 ~ 1.0）</param>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="deviceId">手柄 ID（默认 0）</param>
    public static void Vibrate(float weakMagnitude, float strongMagnitude, float duration, int deviceId = 0)
    {
        Godot.Input.StartJoyVibration(deviceId, weakMagnitude, strongMagnitude, duration);
    }

    /// <summary>停止手柄震动</summary>
    public static void StopVibration(int deviceId = 0)
    {
        Godot.Input.StopJoyVibration(deviceId);
    }

    /// <summary>轻触反馈（弱震动 0.15 秒）</summary>
    public static void VibrateLight() => Vibrate(0.5f, 0.0f, 0.15f);

    /// <summary>重击反馈（强震动 0.3 秒）</summary>
    public static void VibrateHeavy() => Vibrate(0.0f, 1.0f, 0.3f);

    /// <summary>混合反馈（中等震动 0.2 秒）</summary>
    public static void VibrateMedium() => Vibrate(0.5f, 0.5f, 0.2f);

    // ==================== 手柄状态查询 ====================

    /// <summary>获取已连接的手柄列表</summary>
    public static Godot.Collections.Array<int> GetConnectedJoypads()
    {
        return Godot.Input.GetConnectedJoypads();
    }

    /// <summary>检查指定手柄是否已连接</summary>
    public static bool IsJoypadConnected(int deviceId = 0)
    {
        return GetConnectedJoypads().Contains(deviceId);
    }

    /// <summary>获取手柄名称</summary>
    public static string GetJoypadName(int deviceId = 0)
    {
        return Godot.Input.GetJoyName(deviceId);
    }

    // ==================== 通用输入查询（高级用法）====================

    /// <summary>检查任意动作是否刚按下</summary>
    public static bool IsActionJustPressed(string action)
    {
        return Godot.Input.IsActionJustPressed(action);
    }

    /// <summary>检查任意动作是否按住</summary>
    public static bool IsActionPressed(string action)
    {
        return Godot.Input.IsActionPressed(action);
    }

    /// <summary>检查任意动作是否刚释放</summary>
    public static bool IsActionJustReleased(string action)
    {
        return Godot.Input.IsActionJustReleased(action);
    }

    /// <summary>获取任意动作的强度（0.0 ~ 1.0）</summary>
    public static float GetActionStrength(string action)
    {
        return Godot.Input.GetActionStrength(action);
    }
}
