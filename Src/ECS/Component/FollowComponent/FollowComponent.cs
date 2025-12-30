using Godot;

/// <summary>
/// 跟随组件 - 实现 AI 实体跟随目标的逻辑。
/// 提供方向计算和距离检测功能。
/// </summary>
public partial class FollowComponent : Node
{
    private static readonly Log Log = new("FollowComponent");

    // ================= Export Properties =================

    // ================= Private State =================

    /// <summary>
    /// 父实体的动态数据容器。
    /// </summary>
    private Data _data = null!;

    // ================= Runtime State =================

    /// <summary>
    /// 跟随目标。
    /// </summary>
    public Node2D? Target { get; set; }

    /// <summary>
    /// 获取跟随速度。
    /// </summary>
    public float FollowSpeed => _data.Get<float>("FollowSpeed", 100f);

    /// <summary>
    /// 获取停止距离。
    /// </summary>
    public float StopDistance => _data.Get<float>("StopDistance", 10f);

    /// <summary>
    /// 获取所属实体（父节点）。
    /// </summary>
    private Node2D? OwnerEntity => GetParent<Node2D>();

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent == null)
        {
            Log.Error("FollowComponent 错误: 必须作为实体 (Node) 的子节点存在。");
            return;
        }
        _data = parent.GetData();

        Log.Debug($"跟随组件初始化完成: 速度={FollowSpeed}, 停止距离={StopDistance}");
    }

    public override void _ExitTree()
    {
        Target = null;
        Log.Trace("跟随组件退出场景树，已清除目标引用。");
    }

    // ================= 公开方法 =================


    /// <summary>
    /// 获取指向目标的归一化方向向量。
    /// </summary>
    /// <returns>归一化方向向量，若无有效目标则返回 Vector2.Zero。</returns>
    public Vector2 GetDirectionToTarget()
    {
        if (!IsTargetValid())
        {
            return Vector2.Zero;
        }

        var owner = OwnerEntity;
        if (owner == null)
        {
            Log.Trace("获取方向失败: 未找到所属实体节点。");
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
    /// 获取到目标的距离。
    /// </summary>
    /// <returns>距离值，若无有效目标则返回 float.MaxValue。</returns>
    public float GetDistanceToTarget()
    {
        if (!IsTargetValid())
        {
            return float.MaxValue;
        }

        var owner = OwnerEntity;
        if (owner == null)
        {
            return float.MaxValue;
        }

        return owner.GlobalPosition.DistanceTo(Target!.GlobalPosition);
    }

    /// <summary>
    /// 检查是否在停止距离内。
    /// </summary>
    public bool IsInRange()
    {
        return GetDistanceToTarget() <= StopDistance;
    }

    /// <summary>
    /// 检查是否应该继续跟随（有目标且不在停止距离内）。
    /// </summary>
    public bool ShouldFollow()
    {
        return IsTargetValid() && !IsInRange();
    }

    /// <summary>
    /// 检查目标是否有效（非空且未被释放）。
    /// </summary>
    public bool IsTargetValid()
    {
        return Target != null && IsInstanceValid(Target);
    }

    /// <summary>
    /// 设置跟随目标。
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
            Log.Debug("跟随目标已清除。");
        }
    }
}
