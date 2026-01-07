/** @noSelfInFile **/

import { SchemaRegistry } from "..";
import { Schema } from "../Schema";
import { SCHEMA_TYPES } from "../SchemaTypes";

/**
 * 玩家数据接口
 */
export interface Inte_PlayerSchema extends Inte_base {
    // 资源
    金币: number;
    木材: number;
    人口: number;
    最大人口: number;

    // 统计
    杀敌: number;
    死亡: number;
    助攻: number;
    经验: number;
    等级: number;

    // 游戏状态
    在线: boolean;
    准备: boolean;
    观察者: boolean;
    离开游戏: boolean;

    // 玩家信息
    名称: string;
    颜色: string;
    种族: string;
    队伍: number;
    联盟: number[];

    // 游戏设置
    视野: boolean;
    共享控制: boolean;
    共享视野: boolean;
    共享单位控制: boolean;

    // 游戏设置开关
    关闭特效: boolean;
    物品锁定: boolean;
    关闭飘字: boolean;
    自动挑战: boolean;

    // 作弊相关
    作弊: boolean;
    锁定存档: boolean;

    // 科技和升级
    科技等级: Record<string, number>;
    升级等级: Record<string, number>;

    // 建筑和单位
    建筑数量: Record<string, number>;
    单位数量: Record<string, number>;

    // 其他
    胜利条件: string[];
    失败条件: string[];
    特殊状态: string[];
    自定义数据: Record<string, any>;
}

/**
 * 玩家Schema定义
 * 基于接口定义管理系统的数据自动生成
 */
export const PLAYER_SCHEMA: Schema<Inte_PlayerSchema> = {
    schemaName: SCHEMA_TYPES.PLAYER_DATA,
    description: "玩家数据Schema",
    version: "1.0.0",

    properties: [
        // 资源
        { key: "金币", type: "number", defaultValue: 0, category: "资源", description: "玩家金币", constraints: { min: 0 } },
        { key: "木材", type: "number", defaultValue: 0, category: "资源", description: "玩家木材", constraints: { min: 0 } },
        { key: "人口", type: "number", defaultValue: 0, category: "资源", description: "当前人口", constraints: { min: 0 } },
        { key: "最大人口", type: "number", defaultValue: 100, category: "资源", description: "最大人口上限", constraints: { min: 0 } },

        // 统计
        { key: "杀敌", type: "number", defaultValue: 0, category: "统计", description: "击杀敌人数量", constraints: { min: 0 } },
        { key: "死亡", type: "number", defaultValue: 0, category: "统计", description: "死亡数", constraints: { min: 0 } },
        { key: "助攻", type: "number", defaultValue: 0, category: "统计", description: "助攻数", constraints: { min: 0 } },
        { key: "经验", type: "number", defaultValue: 0, category: "统计", description: "经验值", constraints: { min: 0 } },
        { key: "等级", type: "number", defaultValue: 1, category: "统计", description: "等级", constraints: { min: 1 } },

        // 游戏状态
        { key: "在线", type: "boolean", defaultValue: true, category: "游戏状态", description: "在线状态" },
        { key: "准备", type: "boolean", defaultValue: false, category: "游戏状态", description: "准备状态" },
        { key: "观察者", type: "boolean", defaultValue: false, category: "游戏状态", description: "观察者模式" },
        { key: "离开游戏", type: "boolean", defaultValue: false, category: "游戏状态", description: "离开游戏" },

        // 玩家信息
        { key: "名称", type: "string", defaultValue: "", category: "玩家信息", description: "玩家名称" },
        { key: "颜色", type: "string", defaultValue: "", category: "玩家信息", description: "玩家颜色" },
        { key: "种族", type: "string", defaultValue: "", category: "玩家信息", description: "玩家种族" },
        { key: "队伍", type: "number", defaultValue: 0, category: "玩家信息", description: "队伍编号", constraints: { min: 0 } },
        { key: "联盟", type: "array", defaultValue: [], category: "玩家信息", description: "联盟列表" },

        // 游戏设置
        { key: "视野", type: "boolean", defaultValue: true, category: "游戏设置", description: "视野设置" },
        { key: "共享控制", type: "boolean", defaultValue: false, category: "游戏设置", description: "共享控制" },
        { key: "共享视野", type: "boolean", defaultValue: false, category: "游戏设置", description: "共享视野" },
        { key: "共享单位控制", type: "boolean", defaultValue: false, category: "游戏设置", description: "共享单位控制" },

        // 游戏设置开关
        { key: "关闭特效", type: "boolean", defaultValue: false, category: "游戏设置", description: "是否关闭特效显示" },
        { key: "物品锁定", type: "boolean", defaultValue: false, category: "游戏设置", description: "是否锁定物品" },
        { key: "关闭飘字", type: "boolean", defaultValue: false, category: "游戏设置", description: "是否关闭飘字显示" },
        { key: "自动挑战", type: "boolean", defaultValue: false, category: "游戏设置", description: "是否自动挑战" },

        // 作弊相关
        { key: "作弊", type: "boolean", defaultValue: false, category: "作弊", description: "作弊标记" },
        { key: "锁定存档", type: "boolean", defaultValue: false, category: "作弊", description: "是否锁定存档" },

        // 科技和升级
        { key: "科技等级", type: "object", defaultValue: {}, category: "科技和升级", description: "科技等级" },
        { key: "升级等级", type: "object", defaultValue: {}, category: "科技和升级", description: "升级等级" },

        // 建筑和单位
        { key: "建筑数量", type: "object", defaultValue: {}, category: "建筑和单位", description: "建筑数量" },
        { key: "单位数量", type: "object", defaultValue: {}, category: "建筑和单位", description: "单位数量" },

        // 其他
        { key: "胜利条件", type: "array", defaultValue: [], category: "其他", description: "胜利条件" },
        { key: "失败条件", type: "array", defaultValue: [], category: "其他", description: "失败条件" },
        { key: "特殊状态", type: "array", defaultValue: [], category: "其他", description: "特殊状态" },
        { key: "自定义数据", type: "object", defaultValue: {}, category: "其他", description: "自定义数据" }
    ],

};


/**
 * 注册Schema
 */
export class PlayerSchema {
    //游戏初始化时运行
    private static onInit() {
        SchemaRegistry.register(SCHEMA_TYPES.PLAYER_DATA, PLAYER_SCHEMA);
    }
}
