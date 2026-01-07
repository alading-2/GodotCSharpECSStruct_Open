/** @noSelfInFile **/

import { SchemaRegistry } from "..";
import { MyMath } from "../../../base/math/MyMath";
import { SCHEMA_TYPES } from "../SchemaTypes";
import { Schema } from "../Schema";

export enum AttributeCategory {
    /** 基础属性 */
    基础属性 = "基础属性",
    /** 最终属性 */
    最终属性 = "最终属性",
    /** 生命相关 */
    生命相关 = "生命相关",
    /** 魔法相关 */
    魔法相关 = "魔法相关",
    /** 攻击相关 */
    攻击相关 = "攻击相关",
    /** 防御相关 */
    防御相关 = "防御相关",
    /** 技能相关 */
    技能相关 = "技能相关",
    /** 暴击相关 */
    暴击相关 = "暴击相关",
    /** 闪避相关 */
    闪避相关 = "闪避相关",
    /** 其他 */
    其他 = "其他"
}

/**
 * 属性数据接口
 */
export interface Inte_AttributeSchema {
    // 基础属性
    // "基础力量"?: number;
    // "基础敏捷"?: number;
    // "基础智力"?: number;

    // // 属性加成百分比
    // "力量加成"?: number;    //按百分比增加的力量
    // "敏捷加成"?: number;    //按百分比增加的敏捷
    // "智力加成"?: number;    //按百分比增加的智力

    // // 最终属性
    // "最终力量"?: number;
    // "最终敏捷"?: number;
    // "最终智力"?: number;

    // // 属性成长
    // "力量成长"?: number;    //提升等级时增加的力量
    // "敏捷成长"?: number;    //提升等级时增加的敏捷
    // "智力成长"?: number;    //提升等级时增加的智力

    // 生命相关
    "基础生命值"?: number;
    "生命加成"?: number;
    "最终生命值"?: number;

    "基础生命恢复"?: number;
    "生命恢复加成"?: number;
    "百分比生命恢复"?: number;  // 基于最大生命值的百分比恢复
    "最终生命恢复"?: number;

    // 魔法相关
    "基础魔法值"?: number;
    "魔法加成"?: number;
    "最终魔法值"?: number;

    "基础魔法恢复"?: number;
    "魔法恢复加成"?: number;
    "百分比魔法恢复"?: number;  // 基于最大魔法值的百分比恢复
    "最终魔法恢复"?: number;

    // 攻击相关
    "基础攻击力"?: number;
    "攻击力加成"?: number;
    "最终攻击力"?: number;

    //攻速
    "基础攻速"?: number;
    "攻速加成"?: number;
    "最终攻速"?: number;
    "攻击间隔"?: number;

    // 防御相关
    "基础防御"?: number;
    "防御加成"?: number;
    "最终防御"?: number;

    // 物穿
    "物理穿透"?: number;

    //魔抗
    "基础魔抗"?: number;
    "魔抗加成"?: number;
    "最终魔抗"?: number;

    //法穿
    "魔法穿透"?: number;

    // 技能相关
    "基础技能伤害"?: number;
    "技能伤害加成"?: number;
    "最终技能伤害"?: number;

    "技能冷却缩减"?: number;

    // 移速
    "移动速度"?: number;

    // 无视闪避
    "无视物理闪避几率"?: number;
    "无视魔法闪避几率"?: number;

    // 闪避相关
    "物理闪避几率"?: number;
    "魔法闪避几率"?: number;

    // 暴击率
    "物理暴击率"?: number;
    "魔法暴击率"?: number;

    // 暴击伤害
    "物理暴击伤害"?: number;
    "魔法暴击伤害"?: number;

    //吸血
    "物理吸血百分比"?: number;
    "魔法吸血百分比"?: number;

    // 伤害加成
    "伤害增幅"?: number;
    // 伤害减免
    "伤害减免"?: number;
}

/**
 * 属性Schema定义
 * 基于接口定义管理系统的数据自动生成
 */
export const ATTRIBUTE_SCHEMA: Schema<Inte_AttributeSchema> = {
    schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
    description: "属性数据Schema",
    version: "1.0.0",

    properties: [
        // 基础属性
        // { key: "基础力量", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "基础力量值", constraints: { min: 0 } },
        // { key: "基础敏捷", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "基础敏捷值", constraints: { min: 0 } },
        // { key: "基础智力", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "基础智力值", constraints: { min: 0 } },

        // // 属性加成百分比
        // { key: "力量加成", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "力量百分比加成", isPercent: true, constraints: { min: 0 } },
        // { key: "敏捷加成", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "敏捷百分比加成", isPercent: true, constraints: { min: 0 } },
        // { key: "智力加成", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "智力百分比加成", isPercent: true, constraints: { min: 0 } },

        // // 属性成长
        // { key: "力量成长", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "每级增加的力量值", constraints: { min: 0 } },
        // { key: "敏捷成长", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "每级增加的敏捷值", constraints: { min: 0 } },
        // { key: "智力成长", type: "number", defaultValue: 0, category: AttributeCategory.基础属性, description: "每级增加的智力值", constraints: { min: 0 } },

        // 生命相关
        { key: "基础生命值", type: "number", defaultValue: 100, category: AttributeCategory.生命相关, description: "基础生命值", constraints: { min: 0 } },
        { key: "生命加成", type: "number", defaultValue: 0, category: AttributeCategory.生命相关, description: "生命值百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "基础生命恢复", type: "number", defaultValue: 0, category: AttributeCategory.生命相关, description: "每秒恢复的基础生命值", constraints: { min: 0 } },
        { key: "生命恢复加成", type: "number", defaultValue: 0, category: AttributeCategory.生命相关, description: "生命恢复百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "百分比生命恢复", type: "number", defaultValue: 0, category: AttributeCategory.生命相关, description: "基于最大生命值的百分比恢复（存储百分比数字，如10表示10%）", constraints: { min: 0, max: 10 } },
        { key: "物理吸血百分比", type: "number", defaultValue: 0, category: AttributeCategory.生命相关, description: "物理吸血百分比", isPercent: true, constraints: { min: 0 } },
        { key: "魔法吸血百分比", type: "number", defaultValue: 0, category: AttributeCategory.生命相关, description: "魔法吸血百分比", isPercent: true, constraints: { min: 0 } },

        // 魔法相关
        { key: "基础魔法值", type: "number", defaultValue: 100, category: AttributeCategory.魔法相关, description: "基础魔法值", constraints: { min: 0 } },
        { key: "魔法加成", type: "number", defaultValue: 0, category: AttributeCategory.魔法相关, description: "魔法值百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "基础魔法恢复", type: "number", defaultValue: 0, category: AttributeCategory.魔法相关, description: "每秒恢复的基础魔法值", constraints: { min: 0 } },
        { key: "魔法恢复加成", type: "number", defaultValue: 0, category: AttributeCategory.魔法相关, description: "魔法恢复百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "百分比魔法恢复", type: "number", defaultValue: 0, category: AttributeCategory.魔法相关, description: "基于最大魔法值的百分比恢复（存储百分比数字，如10表示10%）", constraints: { min: 0, max: 10 } },

        // 攻击相关
        { key: "基础攻击力", type: "number", defaultValue: 10, category: AttributeCategory.攻击相关, description: "基础攻击力", constraints: { min: 0 } },
        { key: "攻击力加成", type: "number", defaultValue: 0, category: AttributeCategory.攻击相关, description: "攻击力百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "基础攻速", type: "number", defaultValue: 100, category: AttributeCategory.攻击相关, description: "基础攻击速度", constraints: { min: 0, max: 1000 } },
        { key: "攻速加成", type: "number", defaultValue: 0, category: AttributeCategory.攻击相关, description: "攻击速度百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "攻击间隔", type: "number", defaultValue: 1, category: AttributeCategory.攻击相关, description: "攻击间隔时间", constraints: { min: 0.1 } },

        { key: "伤害增幅", type: "number", defaultValue: 0, category: AttributeCategory.攻击相关, description: "伤害增幅百分比", isPercent: true, constraints: { min: 0 } },

        { key: "物理穿透", type: "number", defaultValue: 0, category: AttributeCategory.攻击相关, description: "穿透物理防御值", constraints: { min: 0 } },
        { key: "魔法穿透", type: "number", defaultValue: 0, category: AttributeCategory.攻击相关, description: "穿透魔法抗性值", constraints: { min: 0 } },

        // 防御相关
        { key: "基础防御", type: "number", defaultValue: 0, category: AttributeCategory.防御相关, description: "基础防御力", constraints: { min: 0 } },
        { key: "防御加成", type: "number", defaultValue: 0, category: AttributeCategory.防御相关, description: "防御力百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "基础魔抗", type: "number", defaultValue: 0, category: AttributeCategory.防御相关, description: "基础魔法抗性", constraints: { min: 0 } },
        { key: "魔抗加成", type: "number", defaultValue: 0, category: AttributeCategory.防御相关, description: "魔法抗性百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "伤害减免", type: "number", defaultValue: 0, category: AttributeCategory.防御相关, description: "伤害减免百分比", isPercent: true, constraints: { min: 0 } },

        // 技能相关
        { key: "基础技能伤害", type: "number", defaultValue: 100, category: "技能相关", description: "基础技能伤害百分比", isPercent: true, constraints: { min: 0 } },
        { key: "技能伤害加成", type: "number", defaultValue: 0, category: "技能相关", description: "技能伤害百分比加成", isPercent: true, constraints: { min: 0 } },
        { key: "技能冷却缩减", type: "number", defaultValue: 0, category: "技能相关", description: "技能冷却缩减百分比", isPercent: true, constraints: { min: 0, max: 80 } },

        // 移速
        { key: "移动速度", type: "number", defaultValue: 270, category: "其他", description: "移动速度", constraints: { min: 0, max: 1000 } },

        // 无视闪避
        { key: "无视物理闪避几率", type: "number", defaultValue: 0, category: "闪避相关", description: "无视敌方物理闪避的几率", isPercent: true, constraints: { min: 0, max: 100 } },
        { key: "无视魔法闪避几率", type: "number", defaultValue: 0, category: "闪避相关", description: "无视敌方魔法闪避的几率", isPercent: true, constraints: { min: 0, max: 100 } },

        // 闪避相关
        { key: "物理闪避几率", type: "number", defaultValue: 0, category: "闪避相关", description: "物理闪避几率", isPercent: true, constraints: { min: 0, max: 100 } },
        { key: "魔法闪避几率", type: "number", defaultValue: 0, category: "闪避相关", description: "魔法闪避几率", isPercent: true, constraints: { min: 0, max: 100 } },

        // 暴击相关
        { key: "物理暴击率", type: "number", defaultValue: 0, category: "暴击相关", description: "物理暴击率", isPercent: true, constraints: { min: 0, max: 75 } },
        { key: "魔法暴击率", type: "number", defaultValue: 0, category: "暴击相关", description: "魔法暴击率", isPercent: true, constraints: { min: 0, max: 75 } },

        { key: "物理暴击伤害", type: "number", defaultValue: 100, category: "暴击相关", description: "物理暴击伤害百分比", isPercent: true, constraints: { min: 0 } },
        { key: "魔法暴击伤害", type: "number", defaultValue: 100, category: "暴击相关", description: "魔法暴击伤害百分比", isPercent: true, constraints: { min: 0 } },

    ],

    computed: [
        // 最终基础属性
        // {
        //     key: "最终力量",
        //     dependencies: ["基础力量", "力量加成"],
        //     compute: (data) => {
        //         return MyMath.CalcAttribute(data.基础力量, 0, data.力量加成)
        //     },
        //     cache: true,
        //     description: "最终力量值"
        // },
        // {
        //     key: "最终敏捷",
        //     dependencies: ["基础敏捷", "敏捷加成"],
        //     compute: (data) => {
        //         return MyMath.CalcAttribute(data.基础敏捷, 0, data.敏捷加成)
        //     },
        //     cache: true,
        //     description: "最终敏捷值"
        // },
        // {
        //     key: "最终智力",
        //     dependencies: ["基础智力", "智力加成"],
        //     compute: (data) => {
        //         return MyMath.CalcAttribute(data.基础智力, 0, data.智力加成)
        //     },
        //     cache: true,
        //     description: "最终智力值"
        // },
        // 生命值计算
        {
            key: "最终生命值",
            type: "number",
            dependencies: ["基础生命值", "生命加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础生命值, data.生命加成)
            },
            cache: true,
            description: "最终生命值"
        },

        // 魔法值计算
        {
            key: "最终魔法值",
            type: "number",
            dependencies: ["基础魔法值", "魔法加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础魔法值, data.魔法加成)
            },
            cache: true,
            description: "最终魔法值"
        },

        // 生命恢复计算
        {
            key: "最终生命恢复",
            type: "number",
            dependencies: ["基础生命恢复", "生命恢复加成", "百分比生命恢复", "最终生命值"],
            compute: (data) => {
                // 基础恢复 + 百分比恢复
                const baseRecovery = MyMath.CalcAttribute(data.基础生命恢复, data.生命恢复加成);
                const percentRecovery = (data.最终生命值 || 0) * (data.百分比生命恢复 || 0) * 0.01;
                return baseRecovery + percentRecovery;
            },
            cache: true,
            description: "最终每秒恢复的生命值（基础恢复 + 百分比恢复）"
        },

        // 魔法恢复计算
        {
            key: "最终魔法恢复",
            type: "number",
            dependencies: ["基础魔法恢复", "魔法恢复加成", "百分比魔法恢复", "最终魔法值"],
            compute: (data) => {
                // 基础恢复 + 百分比恢复
                const baseRecovery = MyMath.CalcAttribute(data.基础魔法恢复, data.魔法恢复加成);
                const percentRecovery = (data.最终魔法值 || 0) * (data.百分比魔法恢复 || 0) * 0.01;
                return baseRecovery + percentRecovery;
            },
            cache: true,
            description: "最终每秒恢复的魔法值（基础恢复 + 百分比恢复）"
        },

        // 攻击力计算
        {
            key: "最终攻击力",
            type: "number",
            dependencies: ["基础攻击力", "攻击力加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础攻击力, data.攻击力加成)
            },
            cache: true,
            description: "最终攻击力"
        },

        // 攻速计算
        {
            key: "最终攻速",
            type: "number",
            dependencies: ["基础攻速", "攻速加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础攻速, data.攻速加成)
            },
            cache: true,
            description: "最终攻击速度"
        },

        // 攻击间隔计算
        {
            key: "攻击间隔",
            type: "number",
            dependencies: ["最终攻速"],
            compute: (data) => {
                const speed = data.最终攻速;
                return 1 / (speed / 100);
            },
            cache: true,
            description: "攻击间隔：攻击一次需要的时间（秒）"
        },

        // 防御计算
        {
            key: "最终防御",
            type: "number",
            dependencies: ["基础防御", "防御加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础防御, data.防御加成)
            },
            cache: true,
            description: "最终防御值"
        },

        // 魔法抗性计算
        {
            key: "最终魔抗",
            type: "number",
            dependencies: ["基础魔抗", "魔抗加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础魔抗, data.魔抗加成)
            },
            cache: true,
            description: "最终魔法抗性值"
        },

        // 技能伤害计算
        {
            key: "最终技能伤害",
            type: "number",
            dependencies: ["基础技能伤害", "技能伤害加成"],
            compute: (data) => {
                return MyMath.CalcAttribute(data.基础技能伤害, data.技能伤害加成)
            },
            cache: true,
            isPercent: true,
            description: "最终技能伤害百分比"
        },
    ],
};

/**
 * 注册Schema
 */
export class AttributeSchema {
    //游戏初始化时运行
    private static onInit() {
        SchemaRegistry.register(SCHEMA_TYPES.ATTRIBUTE_DATA, ATTRIBUTE_SCHEMA);
    }
}
