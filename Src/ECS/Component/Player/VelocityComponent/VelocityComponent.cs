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
    private static readonly Log _log = new("VelocityComponent");

    // ================= IComponent 实现 =================

    private Data _data;
    private IEntity _entity;

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

    public void OnComponentReset()
    {
        // ✅ 速度状态由 Data.Clear() 自动重置，无需手动处理
    }

    // ================= 运行时状态 =================

    /// <summary>
    /// 当前速度向量（从 Data 容器读取）
    /// </summary>
    public Vector2 Velocity => _data.Get<Vector2>(DataKey.Velocity);

    /// <summary>
    /// 获取速度
    /// </summary>
    public float Speed => _data.Get<float>(DataKey.MoveSpeed);

    /// <summary>
    /// 加速度因子
    /// <para>值越大，加速越快。</para>
    /// <para>典型值: 10.0 (正常) ~ 20.0 (快速)。</para>
    /// </summary>
    public float Acceleration => _data.Get<float>(DataKey.Acceleration);

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
    }

    public override void _ExitTree()
    {
        _data = null;
        _entity = null;
        _log.Trace("移动组件退出场景树");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_entity is not CharacterBody2D body) return;

        // 获取输入
        Vector2 inputDir = InputManager.GetMoveInput();

        // 计算期望的目标速度
        Vector2 targetVelocity = inputDir.Normalized() * Speed;

        // 平滑插值
        Vector2 newVelocity = Velocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-Acceleration * (float)delta));

        // ✅ 通过 Data 更新速度（符合纯数据驱动规范）
        _data.Set(DataKey.Velocity, newVelocity);

        // 应用位移
        body.Velocity = Velocity;
        body.MoveAndSlide();

        // 同步速度（物理引擎可能会修改）
        _data.Set(DataKey.Velocity, body.Velocity);
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 立即停止移动
    /// </summary>
    public void Stop()
    {
        // ✅ 通过 Data 重置速度
        _data.Set(DataKey.Velocity, Vector2.Zero);
        _log.Debug("移动已停止");
    }

    /// <summary>
    /// 直接设置速度（会被限制在 MaxSpeed 内）
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        // ✅ 通过 Data 设置速度
        _data.Set(DataKey.Velocity, velocity);
        _log.Trace($"设置速度: {velocity}");
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

}
