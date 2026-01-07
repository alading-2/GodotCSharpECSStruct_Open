/** @noSelfInFile **/

/**
 * 游戏对象类型定义
 * 定义系统中支持的所有游戏对象类型
 */

/**
 * 游戏对象类型枚举
 */
export enum EntityType {
    /** 单位 */
    Unit = "Unit",
    /** 玩家 */
    Player = "Player",
    /** 物品 */
    Item = "Item",
    /** 技能 */
    Ability = "Ability",
    /** Buff效果 */
    Buff = "Buff"
}

/**
 * 游戏对象状态枚举
 */
export enum EntityState {
    /** 未初始化 */
    Uninitialized = "Uninitialized",
    /** 初始化中 */
    Initializing = "Initializing",
    /** 活跃状态 */
    Active = "Active",
    /** 暂停状态 */
    Paused = "Paused",
    /** 销毁中 */
    Destroying = "Destroying",
    /** 已销毁 */
    Destroyed = "Destroyed"
}

/**
 * 游戏对象配置接口
 */
export interface EntityConfig {
    /** 对象ID */
    id?: string;
    /** 对象类型 */
    type: EntityType;
    /** 初始状态 */
    initialState?: EntityState;
    /** 是否自动初始化 */
    autoInitialize?: boolean;
    /** 自定义数据 */
    customData?: Record<string, any>;
}

/**
 * 游戏对象元数据
 */
export interface EntityMetadata {
    /** 创建时间 */
    createdAt: number;
    /** 最后更新时间 */
    lastUpdatedAt: number;
    /** 更新次数 */
    updateCount: number;
    /** 对象版本 */
    version: string;
}