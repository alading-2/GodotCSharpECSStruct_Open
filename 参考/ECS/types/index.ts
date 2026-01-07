/** @noSelfInFile **/

/**
 * 类型定义导出
 * 统一导出所有系统类型定义
 */

// 游戏对象类型
export {
    EntityType as EntityType,
    EntityState as EntityState,
    EntityConfig as EntityConfig,
    EntityMetadata as EntityMetadata
} from "./EntityTypes";

// 组件类型
export {
    BuiltinComponentType,
    ComponentState,
    ComponentConfig,
    ComponentDependency
} from "./ComponentTypes";

// 事件类型
export {
    BuiltinEventTypes,
    EventPriority,
    EventData
} from "./EventTypes_11111";

// 错误类型
export {
    EntityError,
    EntityNotFoundError,
    EntityAlreadyDestroyedError,
    InvalidPropertyError,
    ComponentError,
    ComponentNotFoundError,
    ComponentAlreadyAttachedError,
    ComponentDependencyError,
    EventSystemError,
    EventHandlerError,
    InvalidEventTypeError,
    EventBusOverflowError
} from "./ErrorTypes";