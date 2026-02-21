using Godot;

/// <summary>
/// 攻击判定组件 - 定义攻击范围、伤害数值及击退属性。
/// 继承自 Area2D 以利用引擎的碰撞检测。
/// </summary>
public partial class HitboxComponent : Area2D, IComponent
{
    private static readonly Log Log = new(nameof(HitboxComponent));

    // ================= IComponent 实现 =================

    private Data? _data;

    public void OnComponentRegistered(Node entity)
    {
        // 组件注册时缓存 Data 引用
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理引用
        _data = null;
        Source = null;
    }



    // ================= Runtime State =================

    /// <summary>
    /// 获取伤害值。
    /// </summary>
    public float Damage => _data?.Get<float>(DataKey.BaseAttack, 10f) ?? 10f;

    /// <summary>
    /// 获取击退力。
    /// </summary>
    public float Knockback => _data?.Get<float>(DataKey.Knockback, 100f) ?? 100f;

    /// <summary>
    /// 攻击来源（用于避免自伤）。
    /// </summary>
    public Node? Source { get; set; }

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        Log.Debug($"攻击判定组件初始化完成: 伤害={Damage}, 击退力={Knockback}");
    }

    public override void _ExitTree()
    {
        // 清理引用，防止内存泄漏
        Source = null;
        Log.Trace("攻击判定组件退出场景树，已清理引用。");
    }
}
