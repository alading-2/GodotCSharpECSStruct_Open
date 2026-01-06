using Godot;


/// <summary>
/// 移动组件 - 管理实体的物理移动
/// <para>
/// 通过组合方式附加到实体上，提供平滑的移动控制。
/// 所有数据从 Entity.Data 读取。
/// </para>
/// </summary>
public partial class VelocityComponent : Node, IComponent
{
    private static readonly Log Log = new("VelocityComponent");

    // ================= IComponent 实现 =================

    private Data? _data;
    private IEntity? _entity;

    public void OnComponentRegistered(Node entity)
    {
        // 组件注册时缓存引用
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理引用
        _data = null;
        _entity = null;
    }

    // ================= 运行时状态 =================

    /// <summary>
    /// 当前速度向量
    /// </summary>
    public Vector2 Velocity { get; private set; } = Vector2.Zero;

    /// <summary>
    /// 获取速度
    /// </summary>
    public float Speed => _data?.Get<float>(DataKey.Speed, 400f) ?? 400f;

    /// <summary>
    /// 获取最大速度
    /// </summary>
    public float MaxSpeed => _data?.Get<float>(DataKey.MaxSpeed, 1000f) ?? 1000f;

    /// <summary>
    /// 加速度因子
    /// <para>值越大，加速越快。</para>
    /// <para>典型值: 10.0 (正常) ~ 20.0 (快速)。</para>
    /// </summary>
    public float Acceleration => _data?.Get<float>(DataKey.Acceleration, 10.0f) ?? 10.0f;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        Log.Success($"就绪, Parent: {(_entity as Node)?.Name}");
    }

    public override void _ExitTree()
    {
        _data = null;
        _entity = null;
        Log.Trace("移动组件退出场景树");
    }

    public override void _Process(double delta)
    {
        if (_entity is not CharacterBody2D body) return;

        // 获取输入
        Vector2 inputDir = InputManager.GetMoveInput();

        // 计算期望的目标速度
        Vector2 targetVelocity = inputDir.Normalized() * Speed;

        // 平滑插值
        Velocity = Velocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-Acceleration * (float)delta));

        // 确保不超过最大速度
        ClampVelocity();

        // 应用位移
        body.Velocity = Velocity;
        body.MoveAndSlide();

        // 同步速度（物理引擎可能会修改）
        Velocity = body.Velocity;
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 立即停止移动
    /// </summary>
    public void Stop()
    {
        Velocity = Vector2.Zero;
        Log.Debug("移动已停止");
    }

    /// <summary>
    /// 直接设置速度（会被限制在 MaxSpeed 内）
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        Velocity = velocity;
        ClampVelocity();
        Log.Trace($"设置速度: {Velocity}");
    }

    /// <summary>
    /// 获取当前速度向量
    /// </summary>
    public Vector2 GetVelocity()
    {
        return Velocity;
    }

    /// <summary>
    /// 获取当前速度大小
    /// </summary>
    public float GetSpeed()
    {
        return Velocity.Length();
    }

    // ================= 私有方法 =================

    /// <summary>
    /// 将速度限制在 MaxSpeed 内
    /// </summary>
    private void ClampVelocity()
    {
        if (Velocity.Length() > MaxSpeed)
        {
            Velocity = Velocity.Normalized() * MaxSpeed;
        }
    }
}
