/** @noSelfInFile **/

import { SchemaRegistry } from "..";
import { Schema } from "../Schema";
import { SCHEMA_TYPES } from "../SchemaTypes";


export enum UnitCategory {
    /** 单位属性 */
    单位属性 = "单位属性",
    /** 状态 */
    状态 = "状态",
    /** UI */
    UI = "UI",
    /** 召唤 */
    召唤 = "召唤",
    /** 弹道 */
    弹道 = "弹道",
    /** 物品 */
    物品 = "物品",
    /** 塔防 */
    塔防 = "塔防",
    /** 事件 */
    事件 = "事件",
    /** 其他 */
    其他 = "其他",
}

/**
 * 单位数据接口（从UnitDefinition中提取，排除属性相关）
 */
export interface Inte_UnitSchema extends Inte_base {
    // 单位属性
    "当前生命值"?: number;
    "当前魔法值"?: number;
    "禁止生命恢复"?: boolean;
    "禁止魔法恢复"?: boolean;
    // 单位属性
    "类别"?: string;
    "单位类型"?: string;
    "飞行高度"?: number;
    "模型"?: string;
    "acquire"?: number;  // 主动攻击范围

    // 等级
    "等级"?: number;
    "经验值"?: number;
    "总经验值"?: number;

    // 状态
    "显示"?: boolean;
    "无敌"?: boolean;
    "不受伤害"?: boolean;
    "暂停状态"?: boolean;
    "战斗状态"?: boolean;  // 暂时没用
    "是否移除单位"?: boolean;

    // UI 相关
    "血条高度"?: number;   // 单位血条显示高度

    // 弹道系统
    "弹道模型"?: string;   // 单位使用的弹道模型路径
    "弹道速度"?: number;   // 弹道移动速度
    "弹道大小"?: number;   // 弹道模型大小

    //其他
    "xscale"?: number;
    "isRemove"?: boolean;
}

/**
 * 单位Schema定义
 * 基于接口定义管理系统的数据自动生成，排除了属性相关的定义
 */
export const UNIT_SCHEMA: Schema<Inte_UnitSchema> = {
    schemaName: SCHEMA_TYPES.UNIT_DATA,
    description: "单位数据Schema",
    version: "1.0.0",

    properties: [
        // 单位属性
        { key: "当前生命值", type: "number", defaultValue: 100, category: UnitCategory.单位属性, description: "单位当前生命值", constraints: { min: 1 } },
        { key: "当前魔法值", type: "number", defaultValue: 0, category: UnitCategory.单位属性, description: "单位当前魔法值", constraints: { min: 0 } },
        { key: "禁止生命恢复", type: "boolean", defaultValue: false, category: UnitCategory.单位属性, description: "是否禁止生命恢复" },
        { key: "禁止魔法恢复", type: "boolean", defaultValue: false, category: UnitCategory.单位属性, description: "是否禁止魔法恢复" },

        // 基础信息
        { key: "类别", type: "string", defaultValue: "", category: UnitCategory.单位属性, description: "单位类别", constraints: { enum: ["英雄", "敌人", "NPC", "单位", "召唤物", "助手", "佣兵", "塔防"] } },
        { key: "单位类型", type: "string", defaultValue: "", category: UnitCategory.单位属性, description: "单位类型" },
        { key: "飞行高度", type: "number", defaultValue: 0, category: UnitCategory.单位属性, description: "单位飞行高度", constraints: { min: 0 } },
        { key: "模型", type: "string", defaultValue: "", category: UnitCategory.单位属性, description: "单位模型路径" },
        { key: "acquire", type: "number", defaultValue: 0, category: UnitCategory.单位属性, description: "主动攻击范围", constraints: { min: 0 } },
        { key: "等级", type: "number", defaultValue: 1, category: UnitCategory.单位属性, description: "单位等级", constraints: { min: 1, max: 1000 } },
        { key: "经验值", type: "number", defaultValue: 0, category: UnitCategory.单位属性, description: "单位经验值", constraints: { min: 0 } },
        { key: "总经验值", type: "number", defaultValue: 0, category: UnitCategory.单位属性, description: "单位总经验值", constraints: { min: 0 } },

        // 状态
        { key: "显示", type: "boolean", defaultValue: true, category: UnitCategory.状态, description: "是否显示单位" },
        { key: "无敌", type: "boolean", defaultValue: false, category: UnitCategory.状态, description: "是否无敌" },
        { key: "不受伤害", type: "boolean", defaultValue: false, category: UnitCategory.状态, description: "是否不受伤害" },
        { key: "暂停状态", type: "boolean", defaultValue: false, category: UnitCategory.状态, description: "是否暂停" },
        { key: "战斗状态", type: "boolean", defaultValue: true, category: UnitCategory.状态, description: "是否处于战斗状态" },
        { key: "是否移除单位", type: "boolean", defaultValue: false, category: UnitCategory.状态, description: "是否标记为移除" },

        // UI相关
        { key: "血条高度", type: "number", defaultValue: 0, category: UnitCategory.UI, description: "单位血条显示高度", constraints: { min: 0 } },

        // 弹道系统
        { key: "弹道模型", type: "string", defaultValue: "", category: UnitCategory.弹道, description: "单位使用的弹道模型路径" },
        { key: "弹道速度", type: "number", defaultValue: 0, category: UnitCategory.弹道, description: "弹道移动速度", constraints: { min: 0 } },
        { key: "弹道大小", type: "number", defaultValue: 1, category: UnitCategory.弹道, description: "弹道模型大小", constraints: { min: 0 } },

        // 其他
        { key: "xscale", type: "number", defaultValue: 1, category: UnitCategory.其他, description: "X轴缩放", constraints: { min: 0 } },
        { key: "isRemove", type: "boolean", defaultValue: false, category: UnitCategory.其他, description: "是否移除标记" }
    ],
};

export const UNIT_SCHEMA_KEYS: Set<string> = new Set(UNIT_SCHEMA.properties.map(p => p.key));

/**
 * 注册Schema
 */
export class UnitSchema {
    //游戏初始化时运行
    private static onInit() {
        SchemaRegistry.register(SCHEMA_TYPES.UNIT_DATA, UNIT_SCHEMA);
    }
}

