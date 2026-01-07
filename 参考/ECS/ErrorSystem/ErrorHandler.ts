/** @noSelfInFile **/

import { Logger } from "../../工具/logger";
import { Entity } from "../Entity";
import { GameEvents } from "../types/EventTypes";
import { EntityError, ComponentError, EventSystemError } from "./ErrorTypes";

const logger = Logger.createLogger("ErrorHandler");

/**
 * 错误处理器 - 处理游戏对象系统中的错误
 * 提供统一的错误处理、日志记录和恢复策略
 */
export class ErrorHandler {
    /**
     * 处理一般游戏对象错误
     * @param error 错误对象
     * @param context 上下文游戏对象
     */
    static handleError(error: Error, context?: Entity): void {
        // 记录错误
        this.logError(error);

        // 尝试发送错误事件
        if (context && !context.isObjectDestroyed()) {
            try {
                context.emit(GameEvents.ERROR, {
                    error,
                    source: context
                });
            } catch (e) {
                // 如果发送事件时出错，确保不会影响主要错误处理流程
                logger.error(`Failed to emit error event: ${e}`);
            }
        }

        // 基于错误类型尝试恢复
        if (error instanceof EntityError) {
            this.handleEntityError(error, context);
        } else if (error instanceof ComponentError) {
            this.handleComponentError(error, context);
        } else if (error instanceof EventSystemError) {
            this.handleEventSystemError(error, context);
        }
    }

    /**
     * 记录错误
     * @param error 错误对象
     */
    private static logError(error: Error): void {
        const errorInfo = {
            message: error.message,
            name: error.name,
            stack: error.stack
        };

        logger.error(`Error: ${JSON.stringify(errorInfo, null, 2)}`);
    }

    /**
     * 处理游戏对象错误
     * @param error 游戏对象错误
     * @param context 上下文游戏对象
     */
    private static handleEntityError(error: EntityError, context?: Entity): void {
        switch (error.type) {
            case "EntityNotFound":
                // 不需要特殊恢复
                break;

            case "EntityAlreadyDestroyed":
                // 已经销毁的对象，不需要额外处理
                break;

            case "InvalidPropertyAccess":
                // 尝试使用默认值
                if (context && error.propertyKey) {
                    logger.debug(`Attempting recovery for invalid property access: ${error.propertyKey}`);
                }
                break;

            default:
                // 默认处理
                break;
        }
    }

    /**
     * 处理组件错误
     * @param error 组件错误
     * @param context 上下文游戏对象
     */
    private static handleComponentError(error: ComponentError, context?: Entity): void {
        switch (error.type) {
            case "ComponentNotFound":
                // 缺失的组件，可能需要动态添加
                if (context && error.componentType) {
                    logger.debug(`Component not found: ${error.componentType}`);
                }
                break;

            case "ComponentAlreadyAttached":
                // 组件已附加，不需要额外处理
                break;

            case "ComponentDependencyError":
                // 组件依赖错误，可能需要添加缺失的依赖
                if (context && error.dependencyType) {
                    logger.debug(`Missing component dependency: ${error.dependencyType}`);
                }
                break;

            case "ComponentStateError":
                // 组件状态错误
                if (error.component) {
                    if (error.attemptRecovery && context) {
                        // 尝试重新初始化组件
                        try {
                            error.component.performInitialize();
                            logger.debug(`Component ${error.component.getType()} reinitialized`);
                        } catch (e) {
                            logger.error(`Failed to reinitialize component: ${e}`);
                        }
                    }
                }
                break;

            default:
                // 默认处理
                break;
        }
    }

    /**
     * 处理事件系统错误
     * @param error 事件系统错误
     * @param context 上下文游戏对象
     */
    private static handleEventSystemError(error: EventSystemError, context?: Entity): void {
        switch (error.type) {
            case "EventHandlerError":
                // 事件处理器错误，可能需要移除有问题的处理器
                if (error.subscription && context) {
                    try {
                        context.off(error.subscription);
                        logger.debug(`Removed problematic event handler for: ${error.subscription.eventType}`);
                    } catch (e) {
                        logger.error(`Failed to remove event handler: ${e}`);
                    }
                }
                break;

            case "InvalidEventType":
                // 无效的事件类型，不需要额外处理
                break;

            case "EventBusOverflow":
                // 事件队列溢出，可能需要清理队列
                logger.warn(`Event bus overflow detected${context ? ` for object ${context.getId()}` : ''}`);
                break;

            default:
                // 默认处理
                break;
        }
    }

    /**
     * 报告警告
     * @param message 警告消息
     * @param context 上下文游戏对象
     */
    static reportWarning(message: string, context?: Entity): void {
        logger.warn(message);

        // 发送警告事件
        if (context && !context.isObjectDestroyed()) {
            try {
                context.emit(GameEvents.WARNING, {
                    message,
                    source: context
                });
            } catch (e) {
                logger.error(`Failed to emit warning event: ${e}`);
            }
        }
    }

    /**
     * 断言条件，如果为false则抛出错误
     * @param condition 条件
     * @param message 错误消息
     * @param context 上下文游戏对象
     */
    static assert(condition: boolean, message: string, context?: Entity): void {
        if (!condition) {
            const error = new Error(`Assertion failed: ${message}`);
            this.handleError(error, context);
            throw error;
        }
    }

    /**
     * 带有尝试-捕获的安全执行函数
     * @param fn 要执行的函数
     * @param context 上下文游戏对象
     * @param fallback 发生错误时的回退值
     * @returns 函数执行结果或回退值
     */
    static safeExecute<T>(fn: () => T, context?: Entity, fallback?: T): T {
        try {
            return fn();
        } catch (error) {
            this.handleError(error as Error, context);
            return fallback as T;
        }
    }
}