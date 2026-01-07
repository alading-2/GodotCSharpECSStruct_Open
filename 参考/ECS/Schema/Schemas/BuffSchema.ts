/** @noSelfInFile **/

import { SchemaRegistry } from "..";
import { Schema } from "../Schema";
import { SCHEMA_TYPES } from "../SchemaTypes";

/**
 * Buff数据接口
 */
export interface Inte_BuffSchema extends Inte_base {
    // Buff基础信息
    id: string;
    name: string;
    description: string;
    icon: string;

    // Buff状态
    层数: number;
    计时器: any;
    计数: number;

    // 时间相关
    duration: number;
    remainingTime: number;
    tickInterval: number;
    lastTickTime: number;

    // Buff类型
    type: string; // "buff" | "debuff" | "neutral"
    category: string; // "attribute" | "status" | "damage" | "heal" | "control"
    priority: number;

    // 效果数据
    effects: Record<string, any>;
    modifiers: Record<string, number>;

    // 来源信息
    source: any; // 施加者
    sourceAbility: string; // 来源技能
    level: number; // 效果等级

    // 状态标记
    isActive: boolean;
    isPermanent: boolean;
    isDispellable: boolean;
    isStackable: boolean;
    maxStacks: number;

    // 触发条件
    triggerEvents: string[];
    conditions: Record<string, any>;

    // 视觉效果
    visualEffect: string;
    soundEffect: string;

    // 其他
    customData: Record<string, any>;
}


/**
 * Buff Schema定义
 * 基于接口定义管理系统的数据自动生成
 */
export const BUFF_SCHEMA: Schema<Inte_BuffSchema> = {
    schemaName: SCHEMA_TYPES.BUFF_DATA,
    description: "Buff数据Schema",
    version: "1.0.0",

    properties: [

    ],

};

/**
 * 注册Schema
 */
export class BuffSchema {
    //游戏初始化时运行
    private static onInit() {
        SchemaRegistry.register(SCHEMA_TYPES.BUFF_DATA, BUFF_SCHEMA);
    }
}
