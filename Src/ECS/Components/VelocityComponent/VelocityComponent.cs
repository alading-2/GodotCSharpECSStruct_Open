using Godot;

/// <summary>
/// 移动组件 - 管理实体的物理移动，包括速度、加速度和摩擦力。
/// 通过组合方式附加到实体上，提供平滑的移动控制。
/// </summary>
public partial class VelocityComponent : Node
{
    private static readonly Log Log = new("VelocityComponent");

    // ================= Export Properties =================

    /// <summary>
    /// 是否启用玩家输入控制。
    /// 如果启用，组件将自动在 _PhysicsProcess 中处理输入移动。
    /// </summary>
    [Export] public bool EnablePlayerInput { get; set; } = false;

    // ================= Private State =================

    /// <summary>
    /// 父实体的动态数据容器。
    /// </summary>
    private Data _data = null!;

    // ================= Runtime State =================

    /// <summary>
    /// 当前速度向量。
    /// </summary>
    public Vector2 Velocity { get; private set; } = Vector2.Zero;

    /// <summary>
    /// 获取最大速度。
    /// </summary>
    public float MaxSpeed => _data.Get<float>("MaxSpeed", 200f);

    /// <summary>
    /// 获取加速度。
    /// </summary>
    public float Acceleration => _data.Get<float>("Acceleration", 1000f);

    /// <summary>
    /// 获取摩擦力。
    /// </summary>
    public float Friction => _data.Get<float>("Friction", 800f);

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent == null)
        {
            Log.Error("VelocityComponent 错误: 必须作为实体 (Node) 的子节点存在。");
            return;
        }
        _data = parent.GetData();

        Log.Debug($"移动组件初始化完成: 最大速度={MaxSpeed}, 加速度={Acceleration}, 摩擦力={Friction}");
    }

    public override void _ExitTree()
    {
        Log.Trace("移动组件退出场景树。");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!EnablePlayerInput) return;

        Vector2 inputDir = Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown");

        if (inputDir != Vector2.Zero)
        {
            MoveToward(inputDir, (float)delta);
        }
        else
        {
            ApplyFriction((float)delta);
        }
    }

    // ================= 公开方法 =================


    /// <summary>
    /// 向指定方向加速移动。
    /// </summary>
    /// <param name="direction">移动方向（应为归一化向量）。</param>
    /// <param name="delta">帧时间间隔。</param>
    public void MoveToward(Vector2 direction, float delta)
    {
        if (direction == Vector2.Zero)
        {
            return;
        }

        // 归一化方向向量
        Vector2 normalizedDir = direction.Normalized();

        // 计算目标速度
        Vector2 targetVelocity = normalizedDir * MaxSpeed;

        // 向目标速度加速
        Velocity = Velocity.MoveToward(targetVelocity, Acceleration * delta);

        // 确保不超过最大速度
        ClampVelocity();

        Log.Trace($"加速移动: 方向={normalizedDir}, 速度={Velocity}, 速率={Velocity.Length()}");
    }

    /// <summary>
    /// 应用摩擦力减速。
    /// </summary>
    /// <param name="delta">帧时间间隔。</param>
    public void ApplyFriction(float delta)
    {
        if (Velocity == Vector2.Zero)
        {
            return;
        }

        // 向零速度减速
        Velocity = Velocity.MoveToward(Vector2.Zero, Friction * delta);

        Log.Trace($"应用摩擦力: 速度={Velocity}, 速率={Velocity.Length()}");
    }

    /// <summary>
    /// 立即停止移动。
    /// </summary>
    public void Stop()
    {
        Velocity = Vector2.Zero;
        Log.Debug("移动已停止。");
    }

    /// <summary>
    /// 直接设置速度（会被限制在 MaxSpeed 内）。
    /// </summary>
    /// <param name="velocity">目标速度向量。</param>
    public void SetVelocity(Vector2 velocity)
    {
        Velocity = velocity;
        ClampVelocity();
        Log.Trace($"设置速度: {Velocity}");
    }

    /// <summary>
    /// 获取当前速度向量。
    /// </summary>
    public Vector2 GetVelocity()
    {
        return Velocity;
    }

    /// <summary>
    /// 获取当前速度大小。
    /// </summary>
    public float GetSpeed()
    {
        return Velocity.Length();
    }

    // ================= Private Methods =================

    /// <summary>
    /// 将速度限制在 MaxSpeed 内。
    /// </summary>
    private void ClampVelocity()
    {
        if (Velocity.Length() > MaxSpeed)
        {
            Velocity = Velocity.Normalized() * MaxSpeed;
        }
    }
}
