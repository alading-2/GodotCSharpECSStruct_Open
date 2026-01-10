using Godot;

/// <summary>
/// 跟随组件 - 实现 AI 实体跟随目标的逻辑
/// <para>
/// 提供方向计算和距离检测功能。
/// 所有数据从 Entity.Data 读取。
/// </para>
/// </summary>
public partial class FollowComponent : Node, IComponent
{
    private static readonly Log Log = new("FollowComponent");

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
        Target = null;
        _data = null;
        _entity = null;
    }

    public void OnComponentReset()
    {
        // 重置状态
        Target = null;
    }

    // ================= 运行时状态 =================

    /// <summary>
    /// 跟随目标
    /// </summary>
    public Node2D? Target { get; set; }

    /// <summary>
    /// 获取跟随速度
    /// </summary>
    public float FollowSpeed => _data?.Get<float>(DataKey.FollowSpeed, 100f) ?? 100f;

    /// <summary>
    /// 获取停止距离
    /// </summary>
    public float StopDistance => _data?.Get<float>(DataKey.StopDistance, 10f) ?? 10f;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        Log.Debug($"就绪: 速度={FollowSpeed}, 停止距离={StopDistance}");
    }

    public override void _ExitTree()
    {
        Target = null;
        _data = null;
        _entity = null;
        Log.Trace("跟随组件退出场景树");
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 获取指向目标的归一化方向向量
    /// </summary>
    public Vector2 GetDirectionToTarget()
    {
        if (!IsTargetValid() || _entity is not Node2D owner)
        {
            return Vector2.Zero;
        }

        Vector2 direction = Target!.GlobalPosition - owner.GlobalPosition;

        if (direction.LengthSquared() < 0.0001f)
        {
            return Vector2.Zero;
        }

        return direction.Normalized();
    }

    /// <summary>
    /// 获取到目标的距离
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (!IsTargetValid() || _entity is not Node2D owner)
        {
            return float.MaxValue;
        }

        return owner.GlobalPosition.DistanceTo(Target!.GlobalPosition);
    }

    /// <summary>
    /// 检查是否在停止距离内
    /// </summary>
    public bool IsInRange()
    {
        return GetDistanceToTarget() <= StopDistance;
    }

    /// <summary>
    /// 检查是否应该继续跟随
    /// </summary>
    public bool ShouldFollow()
    {
        return IsTargetValid() && !IsInRange();
    }

    /// <summary>
    /// 检查目标是否有效
    /// </summary>
    public bool IsTargetValid()
    {
        return Target != null && IsInstanceValid(Target);
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetTarget(Node2D? target)
    {
        Target = target;
        if (target != null)
        {
            Log.Debug($"已设置跟随目标: {target.Name}");
        }
        else
        {
            Log.Debug("跟随目标已清除");
        }
    }
}
