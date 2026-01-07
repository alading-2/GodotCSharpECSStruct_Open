/** @noSelfInFile **/

import { Component } from "../Component/Component";
import { ComponentConstructor } from "../Component/ComponentManager";
import { EventSubscription } from "../types/EventTypes";

/**
 * 游戏对象错误基类
 */
export class EntityError extends Error {
    // 错误类型
    type: string;

    // 错误上下文
    context?: any;

    // 属性键
    propertyKey?: string;

    /**
     * 构造函数
     * @param message 错误消息
     * @param type 错误类型
     * @param context 错误上下文
     */
    constructor(message: string, type: string, context?: any) {
        super(message);
        this.name = "EntityError";
        this.type = type;
        this.context = context;
    }
}

/**
 * 找不到游戏对象错误
 */
export class EntityNotFoundError extends EntityError {
    // 对象ID
    objectId: string;

    /**
     * 构造函数
     * @param objectId 对象ID
     * @param message 错误消息
     */
    constructor(objectId: string, message?: string) {
        super(
            message || `Entity not found: ${objectId}`,
            "EntityNotFound",
            { objectId }
        );
        this.name = "EntityNotFoundError";
        this.objectId = objectId;
    }
}

/**
 * 游戏对象已销毁错误
 */
export class EntityAlreadyDestroyedError extends EntityError {
    // 对象ID
    objectId: string;

    /**
     * 构造函数
     * @param objectId 对象ID
     * @param message 错误消息
     */
    constructor(objectId: string, message?: string) {
        super(
            message || `Entity already destroyed: ${objectId}`,
            "EntityAlreadyDestroyed",
            { objectId }
        );
        this.name = "EntityAlreadyDestroyedError";
        this.objectId = objectId;
    }
}

/**
 * 无效属性访问错误
 */
export class InvalidPropertyError extends EntityError {
    /**
     * 构造函数
     * @param propertyKey 属性键
     * @param value 尝试设置的值
     * @param message 错误消息
     */
    constructor(propertyKey: string, value: any, message?: string) {
        super(
            message || `Invalid property access: ${propertyKey} = ${value}`,
            "InvalidPropertyAccess",
            { propertyKey, value }
        );
        this.name = "InvalidPropertyError";
        this.propertyKey = propertyKey;
    }
}

/**
 * 组件错误基类
 */
export class ComponentError extends Error {
    // 错误类型
    type: string;

    // 组件实例
    component?: Component;

    // 组件类型
    componentType?: string;

    // 依赖组件类型
    dependencyType?: string;

    // 是否尝试恢复
    attemptRecovery: boolean = false;

    /**
     * 构造函数
     * @param message 错误消息
     * @param type 错误类型
     * @param component 组件实例
     */
    constructor(message: string, type: string, component?: Component) {
        super(message);
        this.name = "ComponentError";
        this.type = type;
        this.component = component;
        if (component) {
            this.componentType = component.getType();
        }
    }
}

/**
 * 找不到组件错误
 */
export class ComponentNotFoundError extends ComponentError {
    /**
     * 构造函数
     * @param componentType 组件类型
     * @param objectId 对象ID
     */
    constructor(componentType: ComponentConstructor | string, objectId?: string) {
        const typeStr = typeof componentType === 'string'
            ? componentType
            : componentType.getType();

        super(
            `Component not found: ${typeStr}${objectId ? ` in object ${objectId}` : ''}`,
            "ComponentNotFound"
        );
        this.name = "ComponentNotFoundError";
        this.componentType = typeStr;
    }
}

/**
 * 组件已附加错误
 */
export class ComponentAlreadyAttachedError extends ComponentError {
    /**
     * 构造函数
     * @param componentType 组件类型
     * @param objectId 对象ID
     */
    constructor(componentType: ComponentConstructor | string, objectId?: string) {
        const typeStr = typeof componentType === 'string'
            ? componentType
            : componentType.getType();

        super(
            `Component already attached: ${typeStr}${objectId ? ` to object ${objectId}` : ''}`,
            "ComponentAlreadyAttached"
        );
        this.name = "ComponentAlreadyAttachedError";
        this.componentType = typeStr;
    }
}

/**
 * 组件依赖错误
 */
export class ComponentDependencyError extends ComponentError {
    /**
     * 构造函数
     * @param componentType 组件类型
     * @param dependencyType 依赖组件类型
     * @param objectId 对象ID
     */
    constructor(
        componentType: ComponentConstructor | string,
        dependencyType: ComponentConstructor | string,
        objectId?: string
    ) {
        const compTypeStr = typeof componentType === 'string'
            ? componentType
            : componentType.getType();

        const depTypeStr = typeof dependencyType === 'string'
            ? dependencyType
            : dependencyType.getType();

        super(
            `Missing component dependency: ${depTypeStr} for ${compTypeStr}${objectId ? ` in object ${objectId}` : ''}`,
            "ComponentDependencyError"
        );
        this.name = "ComponentDependencyError";
        this.componentType = compTypeStr;
        this.dependencyType = depTypeStr;
    }
}

/**
 * 组件状态错误
 */
export class ComponentStateError extends ComponentError {
    /**
     * 构造函数
     * @param component 组件实例
     * @param state 预期状态
     * @param message 错误消息
     * @param attemptRecovery 是否尝试恢复
     */
    constructor(component: Component, state: string, message?: string, attemptRecovery: boolean = false) {
        super(
            message || `Invalid component state: expected ${state} for ${component.getType()}`,
            "ComponentStateError",
            component
        );
        this.name = "ComponentStateError";
        this.attemptRecovery = attemptRecovery;
    }
}

/**
 * 事件系统错误基类
 */
export class EventSystemError extends Error {
    // 错误类型
    type: string;

    // 事件类型
    eventType?: string;

    // 订阅标识
    subscription?: EventSubscription;

    /**
     * 构造函数
     * @param message 错误消息
     * @param type 错误类型
     */
    constructor(message: string, type: string) {
        super(message);
        this.name = "EventSystemError";
        this.type = type;
    }
}

/**
 * 事件处理器错误
 */
export class EventHandlerError extends EventSystemError {
    // 原始错误
    originalError?: Error;

    /**
     * 构造函数
     * @param subscription 订阅标识
     * @param error 原始错误
     */
    constructor(subscription: EventSubscription, error?: Error) {
        super(
            `Error in event handler for "${subscription.eventType}": ${error ? error.message : 'unknown error'}`,
            "EventHandlerError"
        );
        this.name = "EventHandlerError";
        this.eventType = subscription.eventType;
        this.subscription = subscription;
        this.originalError = error;
    }
}

/**
 * 无效事件类型错误
 */
export class InvalidEventTypeError extends EventSystemError {
    /**
     * 构造函数
     * @param eventType 事件类型
     */
    constructor(eventType: string) {
        super(
            `Invalid event type: ${eventType}`,
            "InvalidEventType"
        );
        this.name = "InvalidEventTypeError";
        this.eventType = eventType;
    }
}

/**
 * 事件总线溢出错误
 */
export class EventBusOverflowError extends EventSystemError {
    // 队列大小
    queueSize: number;

    // 最大队列大小
    maxQueueSize: number;

    /**
     * 构造函数
     * @param queueSize 当前队列大小
     * @param maxQueueSize 最大队列大小
     */
    constructor(queueSize: number, maxQueueSize: number) {
        super(
            `Event queue overflow: ${queueSize}/${maxQueueSize}`,
            "EventBusOverflow"
        );
        this.name = "EventBusOverflowError";
        this.queueSize = queueSize;
        this.maxQueueSize = maxQueueSize;
    }
}