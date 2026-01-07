/**
 * Schema系统入口文件
 * 导出所有Schema相关的类和接口
 */

// 核心Schema定义
export { Schema, PropertyDefinition, PropertyConstraints, SchemaValidationResult } from './Schema';

// Schema注册中心
export { SchemaRegistry } from './SchemaRegistry';

// 数据管理器系统
export { DataManager } from './DataManager';

//Inte
export { Inte_UnitSchema } from './Schemas/UnitSchema';
export { Inte_PlayerSchema } from './Schemas/PlayerSchema';
export { Inte_AttributeSchema } from './Schemas/AttributeSchema';
export { Inte_ItemSchema } from './Schemas/ItemSchema';
export { Inte_AbilitySchema } from './Schemas/AbilitySchema';
export { Inte_BuffSchema } from './Schemas/BuffSchema';
export { Inte_ShieldSchema, Inte_ShieldInstance } from './Schemas/护盾/ShieldSchema';


// Schema定义
export { UnitSchema } from './Schemas/UnitSchema';
export { PlayerSchema } from './Schemas/PlayerSchema';
export { AttributeSchema } from './Schemas/AttributeSchema';
export { ItemSchema } from './Schemas/ItemSchema';
export { ABILITY_SCHEMA } from './Schemas/AbilitySchema';
export { BuffSchema } from './Schemas/BuffSchema';
export { ShieldSchema } from './Schemas/护盾/ShieldSchema';


