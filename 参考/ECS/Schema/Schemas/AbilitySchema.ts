/** @noSelfInFile **/

import { SchemaRegistry } from "..";
import { Schema } from "../Schema";
import { SCHEMA_TYPES } from "../SchemaTypes";

/**
 * 技能数据接口
 */
export interface Inte_AbilitySchema extends Inte_base {
    // 基础信息
    /** 技能名称 */
    name: string;
    /** 技能图标路径 */
    icon: string;
    /** 目标类型：被动、无目标、单位、点 */
    abilityType?: string;
    /** 技能等级 */
    level: number;
    /** 技能提示说明 */
    tips: string;
    /** 技能目标类型：敌人、友军、所有、物品 */
    targetType: string;
    /** 技能施法动作 */
    action: string;

    // 技能参数
    /** 目标类型数值：0无目标，1单位，2点，3单位/点 */
    dataB: number;
    /** 技能选项：1图标可见，2目标选取图像，4物理魔法，8通用魔法，16单独施放 */
    dataC: number;
    /** 技能快捷键数值 */
    快捷键: number;
    /** 魔法消耗值 */
    cost: number;
    /** 施法时间(秒) */
    cast_time: number;
    /** 技能影响范围 */
    area: number;
    /** 施法距离 */
    range: number;
    /** 快捷键字符串 */
    hotkey: string;

    // 坐标
    X: number;
    Y: number;

    // 冷却
    cool: number;

    // 物品
    /** 是否是物品技能 */
    isitem: boolean;
    /** 物品提示说明 */
    itemtips: string;
    /** 物品等级 */
    itemlevel: number;

}

/**
 * AbilitySchema
 */
export const ABILITY_SCHEMA: Schema<Inte_AbilitySchema> = {
    schemaName: SCHEMA_TYPES.ABILITY_DATA,
    description: "技能数据Schema",
    version: "1.0.0",

    properties: [
        // 基础信息
        { key: "name", type: "string", defaultValue: "", category: "基础信息", description: "技能名称" },
        { key: "icon", type: "string", defaultValue: "", category: "基础信息", description: "技能图标" },
        { key: "level", type: "number", defaultValue: 1, category: "基础信息", description: "技能等级", constraints: { min: 1 } },
        { key: "tips", type: "string", defaultValue: "", category: "基础信息", description: "技能描述" },
        { key: "abilityType", type: "string", defaultValue: "无目标", category: "基础信息", description: "技能类型", constraints: { enum: ["被动", "无目标", "单位", "点"] } },
        { key: "targetType", type: "string", defaultValue: "所有", category: "基础信息", description: "技能目标类型", constraints: { enum: ["敌人", "友军", "所有", "物品"] } },
        { key: "action", type: "string", defaultValue: "", category: "基础信息", description: "技能施法动作" },
        // 技能参数
        { key: "dataB", type: "number", defaultValue: 0, category: "技能参数", description: "目标类型：0无目标，1单位，2点，3单位/点" },
        { key: "dataC", type: "number", defaultValue: 0, category: "技能参数", description: "选项：1图标可见，2目标选取图像，4物理魔法，8通用魔法，16单独施放" },
        { key: "快捷键", type: "number", defaultValue: 0, category: "技能参数", description: "快捷键" },
        { key: "cost", type: "number", defaultValue: 0, category: "技能参数", description: "魔法消耗", constraints: { min: 0 } },
        { key: "cast_time", type: "number", defaultValue: 0, category: "技能参数", description: "施法时间", constraints: { min: 0 } },
        { key: "area", type: "number", defaultValue: 0, category: "技能参数", description: "影响区域", constraints: { min: 0 } },
        { key: "range", type: "number", defaultValue: 600, category: "技能参数", description: "施法距离", constraints: { min: 0 } },
        { key: "hotkey", type: "string", defaultValue: "", category: "技能参数", description: "快捷键" },

        // 坐标
        { key: "X", type: "number", defaultValue: 0, category: "坐标", description: "X坐标" },
        { key: "Y", type: "number", defaultValue: 0, category: "坐标", description: "Y坐标" },

        // 冷却
        { key: "cool", type: "number", defaultValue: 0, category: "冷却", description: "当前冷却时间", constraints: { min: 0 } },

        // 物品
        { key: "isitem", type: "boolean", defaultValue: false, category: "物品", description: "是否是物品技能" },
        { key: "itemtips", type: "string", defaultValue: "", category: "物品", description: "物品描述" },
        { key: "itemlevel", type: "number", defaultValue: 1, category: "物品", description: "物品等级", constraints: { min: 1 } },
    ],

};

/**
 * 注册Schema
 */
export class AttributeSchema {
    //游戏初始化时运行
    private static onInit() {
        SchemaRegistry.register(SCHEMA_TYPES.ABILITY_DATA, ABILITY_SCHEMA);
    }
}