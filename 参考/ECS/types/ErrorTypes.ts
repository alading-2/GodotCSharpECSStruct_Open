/** @noSelfInFile **/

/**
 * 错误类型定义
 */

/**
 * 游戏对象错误基类
 */
export class EntityError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'EntityError';
    }
}

/**
 * 游戏对象未找到错误
 */
export class EntityNotFoundError extends EntityError {
    constructor(id: string) {
        super(`Entity with id "${id}" not found`);
        this.name = 'EntityNotFoundError';
    }
}

/**
 * 游戏对象已销毁错误
 */
export class EntityAlreadyDestroyedError extends EntityError {
    constructor(id: string) {
        super(`Entity with id "${id}" has already been destroyed`);
        this.name = 'EntityAlreadyDestroyedError';
    }
}

/**
 * 无效属性错误
 */
export class InvalidPropertyError extends EntityError {
    constructor(key: string, interfaceName: string) {
        super(`Invalid property "${key}" for interface "${interfaceName}"`);
        this.name = 'InvalidPropertyError';
    }
}

/**
 * 组件错误基类
 */
export class ComponentError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ComponentError';
    }
}

/**
 * 组件未找到错误
 */
export class ComponentNotFoundError extends ComponentError {
    constructor(componentType: string) {
        super(`Component "${componentType}" not found`);
        this.name = 'ComponentNotFoundError';
    }
}

/**
 * 组件已附加错误
 */
export class ComponentAlreadyAttachedError extends ComponentError {
    constructor(componentType: string) {
        super(`Component "${componentType}" is already attached`);
        this.name = 'ComponentAlreadyAttachedError';
    }
}

/**
 * 组件依赖错误
 */
export class ComponentDependencyError extends ComponentError {
    constructor(componentType: string, dependencyType: string) {
        super(`Component "${componentType}" depends on "${dependencyType}" which is not attached`);
        this.name = 'ComponentDependencyError';
    }
}

/**
 * 事件系统错误基类
 */
export class EventSystemError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'EventSystemError';
    }
}

/**
 * 事件处理器错误
 */
export class EventHandlerError extends EventSystemError {
    constructor(eventType: string, error: Error) {
        super(`Error in event handler for "${eventType}": ${error.message}`);
        this.name = 'EventHandlerError';
    }
}

/**
 * 无效事件类型错误
 */
export class InvalidEventTypeError extends EventSystemError {
    constructor(eventType: string) {
        super(`Invalid event type: "${eventType}"`);
        this.name = 'InvalidEventTypeError';
    }
}

/**
 * 事件队列溢出错误
 */
export class EventBusOverflowError extends EventSystemError {
    constructor(queueSize: number) {
        super(`Event queue overflow: ${queueSize} events in queue`);
        this.name = 'EventBusOverflowError';
    }
}