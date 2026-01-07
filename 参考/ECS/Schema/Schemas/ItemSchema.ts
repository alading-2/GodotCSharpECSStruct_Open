/** @noSelfInFile **/

import { SchemaRegistry } from "..";
import { Schema } from "../Schema";
import { SCHEMA_TYPES } from "../SchemaTypes";

// 定义品级数据结构
interface ItemQuality {
    level: number; // 品级名称 
    quality: string; // 品级名称
    color: string; // 颜色名称
    model: string; // 模型名称
    rgb?: string;  // 可选的RGB颜色值，方便后续渲染使用
}

// 定义所有品级的数组，保持一一对应关系
export const ItemQualities: ItemQuality[] = [
    { level: 1, quality: "普通", color: "白色", model: "白色", }, //rgb(255, 255, 255)
    { level: 2, quality: "优秀", color: "绿色", model: "绿色", },//rgb(39, 209, 81)
    { level: 3, quality: "精良", color: "蓝色", model: "蓝色", },//rgb(15, 75, 240)
    { level: 4, quality: "稀有", color: "紫色", model: "紫色", }, //rgb(149, 0, 199)
    { level: 5, quality: "神器", color: "粉色", model: "粉色", }, //rgb(255, 41, 201)
    { level: 6, quality: "史诗", color: "橙色", model: "橙色", }, //rgb(255, 163, 64)
    { level: 7, quality: "神话", color: "红色", model: "红色", }, //rgb(255, 60, 60)
    { level: 8, quality: "超越", color: "黄色", model: "彩色", }, //rgb(255, 215, 0)
    { level: 9, quality: "绝世", color: "黑色", model: "彩色", }, //rgb(0, 0, 0)
];

// 如果需要通过品级名称快速查找，可以再创建一个Map
export const ItemQualityMap = new Map<number, ItemQuality>();
ItemQualities.forEach(quality => {
    ItemQualityMap.set(quality.level, quality);
});
/**
 * 物品数据接口
 */
export interface Inte_ItemSchema extends Inte_base {
    // 基础信息
    // 物品类型名称
    "itemType"?: string;
    // 物品描述提示
    "tips"?: string;
    // 物品品级等级
    "品级"?: number;
    // 物品图标路径
    "图标"?: string;
    // 物品类型分类，比如消耗品
    "类型"?: string;
    // 物品价值金额
    "价格"?: number;

    // 状态
    // 物品堆叠数量
    "数量"?: number;
    // 是否已装备
    "在装备栏"?: boolean;

    // 冷却
    // 物品使用冷却时间
    "冷却时间"?: number;
    // 当前剩余冷却时间
    "剩余冷却时间"?: number;
    // 冷却计时器唯一标识
    "冷却计时器Id"?: string;

}

/**
 * 物品Schema定义
 * 基于接口定义管理系统的数据自动生成
 */
export const ITEM_SCHEMA: Schema<Inte_ItemSchema> = {
    schemaName: SCHEMA_TYPES.ITEM_DATA,
    description: "物品数据Schema",
    version: "1.0.0",

    properties: [
        // 基础信息
        { key: "itemType", type: "string", defaultValue: "", category: "基础信息", description: "物品类型名称" },
        { key: "tips", type: "string", defaultValue: "", category: "基础信息", description: "物品描述" },
        { key: "品级", type: "number", defaultValue: 1, category: "基础信息", description: "物品品级", constraints: { min: 1 } },
        { key: "图标", type: "string", defaultValue: "ReplaceableTextures\\CommandButtons\\BTNCheese.blp", category: "基础信息", description: "物品图标路径" },
        { key: "类型", type: "string", defaultValue: "", category: "基础信息", description: "物品类型" },
        { key: "价格", type: "number", defaultValue: 0, category: "基础信息", description: "物品价格", constraints: { min: 0 } },

        // 状态
        { key: "数量", type: "number", defaultValue: 1, category: "状态", description: "物品数量", constraints: { min: 0 } },
        { key: "在装备栏", type: "boolean", defaultValue: false, category: "状态", description: "是否在装备栏中" },

        // 冷却
        { key: "冷却时间", type: "number", defaultValue: 0, category: "冷却", description: "冷却时间", constraints: { min: 0 } },
        { key: "剩余冷却时间", type: "number", defaultValue: 0, category: "冷却", description: "剩余冷却时间", constraints: { min: 0 } },
        { key: "冷却计时器Id", type: "string", defaultValue: "", category: "冷却", description: "冷却计时器Id" },
    ],

};

export const ITEM_SCHEMA_KEYS: Set<string> = new Set(ITEM_SCHEMA.properties.map(p => p.key));


/**
 * 注册Schema
 */
export class ItemSchema {
    //游戏初始化时运行
    private static onInit() {
        SchemaRegistry.register(SCHEMA_TYPES.ITEM_DATA, ITEM_SCHEMA);
    }
}
