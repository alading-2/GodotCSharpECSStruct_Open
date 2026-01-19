using Godot;

/// <summary>
/// 资源分类枚举 - 用于对资源进行分组管理
/// </summary>
public enum ResourceCategory
{
    /// <summary>玩家</summary>
    Player,
    /// <summary>敌人</summary>
    Enemy,
    /// <summary>技能</summary>
    Ability,
    /// <summary>Buff</summary>
    Buff,
    /// <summary>敌人生成规则</summary>
    SpawnRule,
    /// <summary>物品</summary>
    Item,
    /// <summary>武器</summary>
    Weapon,
    /// <summary>组件场景 (PackedScene)</summary>
    Component
}

/// <summary>
/// 资源条目 - 用于在 Godot 编辑器中配置单个资源
/// 在 ResourceRegistry 的 Inspector 中使用
/// </summary>
[GlobalClass]
public partial class ResourceEntry : Resource
{
    /// <summary>资源简写名称（如 "豺狼人"、"鱼人生成规则"）</summary>
    [Export] public string Name { get; set; } = "";

    /// <summary>资源分类</summary>
    [Export] public ResourceCategory Category { get; set; }

    /// <summary>
    /// 资源引用
    /// 支持所有 Resource 类型，包括：
    /// - 配置文件 (.tres) -> Resource
    /// - 预制体/场景 (.tscn) -> PackedScene
    /// </summary>
    [Export] public Resource? Data { get; set; }
}
