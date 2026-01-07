/** @noSelfInFile **/

// 核心导出
export { Entity } from './Entity/Entity';
export { EntityManager } from './Entity/EntityManager';

// Schema系统导出
export { SchemaRegistry } from './Schema/SchemaRegistry';
export {
    Schema,
    PropertyDefinition,
    PropertyConstraints,
    SchemaValidationResult
} from './Schema/Schema';

// 组件系统导出
export { Component } from './Component/Component';
export { ComponentManager } from './Component/ComponentManager';
export { DataManager } from './Schema/DataManager';
// 移除无效的导出
// export { ComponentPool, ComponentPoolManager } from './Component/Components/ComponentPool';
export { ComponentConstructor } from './Component/Component';

// 事件系统导出
export { EventBus } from './EventSystem/EventBus';
export {
    EventHandler,
    EventSubscription,
    EventPriority,
    GameEvents,
} from './types/EventTypes';

// 错误系统导出
export { ErrorHandler } from './ErrorSystem/ErrorHandler';

// 构建工具导出
export { EntityBuilder } from './Entity/EntityBuilder';
export { EntityPrefab} from './Entity/EntityPrefab';