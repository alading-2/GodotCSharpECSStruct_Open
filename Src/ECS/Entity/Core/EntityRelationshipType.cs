/// <summary>
/// Entity 关系类型常量定义
/// 定义了 Entity 之间的各种关系类型
/// 
/// 注意：关系类型是单向的，例如 UNIT_TO_PLAYER 已经足够表达单位与玩家之间的关系
/// 在 EntityRelationshipManager 中，addRelationship(parentEntityId, childEntityId, relationType) 
/// 的 parentEntityId 和 childEntityId 已经定义了方向性
/// 如果需要反向查询，可以通过索引实现，而不是定义一个反向的关系类型
/// </summary>
public static class EntityRelationshipType
{
    // ==================== 核心关系 ====================

    /// <summary>Entity 与 Component 关系（核心）</summary>
    public const string ENTITY_TO_COMPONENT = "relationship.entity.component";

    /// <summary>父子关系（通用）</summary>
    public const string PARENT = "relationship.parent";

    // ==================== 单位相关关系 ====================

    /// <summary>单位与玩家关系</summary>
    public const string UNIT_TO_PLAYER = "relationship.unit.player";

    /// <summary>单位与物品关系（如装备武器）</summary>
    public const string UNIT_TO_ITEM = "relationship.unit.item";

    /// <summary>单位与技能关系</summary>
    public const string UNIT_TO_ABILITY = "relationship.unit.ability";

    /// <summary>单位与 Buff 关系</summary>
    public const string UNIT_TO_BUFF = "relationship.unit.buff";

    /// <summary>单位与特效关系</summary>
    public const string UNIT_TO_EFFECT = "relationship.unit.effect";

    // ==================== 物品相关关系 ====================

    /// <summary>物品与玩家关系</summary>
    public const string ITEM_TO_PLAYER = "relationship.item.player";

    /// <summary>物品与技能关系</summary>
    public const string ITEM_TO_ABILITY = "relationship.item.ability";

    // ==================== 技能相关关系 ====================

    public const string ENTITY_TO_ABILITY = "relationship.entity.ability";
    /// <summary>技能与子弹关系</summary>
    public const string ABILITY_TO_BULLET = "relationship.ability.bullet";

    /// <summary>技能与特效关系</summary>
    public const string ABILITY_TO_EFFECT = "relationship.ability.effect";

    // ==================== Buff 相关关系 ====================

    /// <summary>Buff 与修改器关系</summary>
    public const string BUFF_TO_MODIFIER = "relationship.buff.modifier";

    // 未来可扩展更多关系类型...
}
