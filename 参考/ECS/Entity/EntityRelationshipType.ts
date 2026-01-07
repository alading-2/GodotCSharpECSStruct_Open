
/** 
 * Entity关系常量，定义了Entity之间的各种关系类型
 * 注意不要双向定义，通常，关系类型是单向的。例如， UNIT_TO_PLAYER 已经足够表达单位与玩家之间的关系。在 EntityRelationshipManager 中， 
 * addRelationship(parentEntityId, childEntityId, relationType) 的 parentEntityId 和 childEntityId 已经定义了方向性。如果需要反向查询，可以通过索引实现，而不是定义一个反向的关系类型。
 */
export enum EntityRelationshipType {
    // 通用关系
    PARENT = "relationship.parent", //父子关系

    // 单位相关关系
    UNIT_TO_PLAYER = "relationship.unit.player",    //单位与玩家关系
    UNIT_TO_ITEM = "relationship.unit.item",        //单位与物品关系
    UNIT_TO_ABILITY = "relationship.unit.ability",  //单位与技能关系
    UNIT_TO_BUFF = "relationship.unit.buff",        //单位与Buff关系
    UNIT_TO_EFFECT = "relationship.unit.effect",    //单位与特效关系

    // 物品相关关系
    ITEM_TO_PLAYER = "relationship.item.player",    //物品与玩家关系
    ITEM_TO_ABILITY = "relationship.item.ability",        //物品与技能关系

    // 更多关系...
}
