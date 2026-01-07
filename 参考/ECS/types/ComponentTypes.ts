/** @noSelfInFile **/

/**
 * 组件系统类型定义
 */

/**
 * 内置组件类型枚举
 */
export enum BuiltinComponentType {
    /** 属性组件 */
    Attribute = "AttributeComponent",
    /** 事件组件 */
    Event = "EventComponent",
    /** 计时器组件 */
    Timer = "TimerComponent",
    /** 特效组件 */
    Effect = "EffectComponent"
}

/**
 * 组件状态枚举
 */
export enum ComponentState {
    /** 未初始化 */
    Uninitialized = "Uninitialized",
    /** 初始化中 */
    Initializing = "Initializing",
    /** 启用状态 */
    Enabled = "Enabled",
    /** 禁用状态 */
    Disabled = "Disabled",
    /** 销毁中 */
    Destroying = "Destroying",
    /** 已销毁 */
    Destroyed = "Destroyed"
}

/**
 * 组件配置接口
 */
export interface ComponentConfig {
    /** 组件类型名称 */
    typeName: string;
    /** 是否自动启用 */
    autoEnable?: boolean;
    /** 初始化数据 */
    initData?: Record<string, any>;
    /** 组件依赖 */
    dependencies?: string[];
}

/**
 * 组件依赖信息
 */
export interface ComponentDependency {
    /** 依赖的组件类型 */
    componentType: string;
    /** 是否为必需依赖 */
    required: boolean;
    /** 依赖描述 */
    description?: string;
}