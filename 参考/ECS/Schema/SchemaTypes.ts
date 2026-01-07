/**
 * ECS Schema类型名称统一管理
 * 
 * 集中存放所有Schema注册使用的类型名称，避免硬编码字符串
 * 便于维护和管理，支持IDE自动补全和重构
 */

export const SCHEMA_TYPES = {
    // 护盾系统相关
    SHIELD_DATA: "ShieldData",
    SHIELD_INSTANCE: "ShieldInstance",
    // 单位系统相关
    UNIT_DATA: "UnitData",
    // 物品系统相关
    ITEM_DATA: "ItemData",
    // 状态系统相关
    BUFF_DATA: "BuffData",
    // 技能系统相关
    ABILITY_DATA: "AbilityData",
    // 属性系统相关
    ATTRIBUTE_DATA: "AttributeData",
    // 技能系统相关
    PLAYER_DATA: "PlayerData",
    // 特效系统相关
    EFFECT_DATA: "EffectData",
    // 变换系统相关
    TRANSFORM_DATA: "TransformData",

} as const;

// // 类型定义，支持类型检查
// export type SchemaType = typeof SCHEMA_TYPES[keyof typeof SCHEMA_TYPES];

// /**
//  * 获取Schema类型名称
//  * @param type Schema类型枚举
//  * @returns 对应的字符串名称
//  */
// export function getSchemaType(type: keyof typeof SCHEMA_TYPES): string {
//     return SCHEMA_TYPES[type];
// }

// /**
//  * 验证是否为有效的Schema类型
//  * @param type 要验证的类型字符串
//  * @returns 是否为有效类型
//  */
// export function isValidSchemaType(type: string): type is SchemaType {
//     return Object.values(SCHEMA_TYPES).includes(type as SchemaType);
// }

// /**
//  * 获取所有Schema类型
//  * @returns 所有可用的Schema类型数组
//  */
// export function getAllSchemaTypes(): readonly string[] {
//     return Object.values(SCHEMA_TYPES);
// }