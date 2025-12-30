using Godot;

/// <summary>
/// 攻击判定组件 - 定义攻击范围、伤害数值及击退属性。
/// 继承自 Area2D 以利用引擎的碰撞检测。
/// </summary>
public partial class HitboxComponent : Area2D
{
    private static readonly Log Log = new("HitboxComponent");

    // ================= Export Properties =================

    // ================= Private State =================

    /// <summary>
    /// 父实体的动态数据容器。
    /// </summary>
    private Data _data = null!;

    // ================= Runtime State =================

    /// <summary>
    /// 获取伤害值。
    /// </summary>
    public float Damage => _data.Get<float>("Damage", 10f);

    /// <summary>
    /// 获取击退力。
    /// </summary>
    public float Knockback => _data.Get<float>("Knockback", 100f);

    /// <summary>
    /// 攻击来源（用于避免自伤）。
    /// </summary>
    public Node? Source { get; set; }

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent == null)
        {
            Log.Error("HitboxComponent 错误: 必须作为实体 (Node) 的子节点存在。");
            return;
        }
        _data = parent.GetData();

        Log.Debug($"攻击判定组件初始化完成: 伤害={Damage}, 击退力={Knockback}");
    }

    public override void _ExitTree()
    {
        // 清理引用，防止内存泄漏
        Source = null;
        Log.Trace("攻击判定组件退出场景树，已清理引用。");
    }
}
