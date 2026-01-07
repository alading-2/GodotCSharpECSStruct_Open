/** @noSelfInFile **/


import { Logger } from "../../base/object/工具/logger";
import { EventHandler } from "../types/EventTypes";
import { EventSubscription, EventPriority } from "../types/EventTypes";

const logger = Logger.createLogger("EventBus");

/**
 * 事件总线 - 中央事件分发和管理系统
 * 支持事件注册、触发和管理
 */
export class EventBus {
    // 全局事件总线实例
    private static instance: EventBus;

    /**
     * 获取全局事件总线实例
     */
    public static getInstance(): EventBus {
        if (!EventBus.instance) {
            EventBus.instance = new EventBus();
            logger.debug("全局事件总线实例已创建");
        }
        return EventBus.instance;
    }
    // 事件订阅存储
    private subscriptions = new Map<string, EventSubscription[]>();

    /**
     * 构造函数
     */
    constructor() {
        logger.debug("事件总线已初始化");
    }

    /**
     * 注册事件处理器
     * @param eventType 事件类型
     * @param handler 事件处理函数
     * @param priority 优先级（默认为普通）
     * @param once 是否只触发一次
     * @returns 订阅标识，用于取消订阅
     */
    on<T = any>(
        eventType: string,
        handler: EventHandler<T>,
        priority: EventPriority = EventPriority.NORMAL,
        once: boolean = false
    ): EventSubscription {
        if (!eventType || typeof handler !== "function") {
            logger.error("无效的事件类型或处理函数");
            throw new Error("无效的事件类型或处理函数");
        }

        // 确保该事件类型的订阅列表存在
        if (!this.subscriptions.has(eventType)) {
            this.subscriptions.set(eventType, []);
        }

        // 创建订阅对象
        const subscription: EventSubscription = {
            eventType,
            handler,
            priority,
            once
        };

        // 添加到订阅列表，并按优先级排序
        const subscriptions = this.subscriptions.get(eventType)!;
        subscriptions.push(subscription);
        subscriptions.sort((a, b) => b.priority - a.priority);

        logger.debug(`已注册事件处理器，事件类型："${eventType}"，优先级：${priority}`);

        return subscription;
    }

    /**
     * 注册只触发一次的事件处理器
     * @param eventType 事件类型
     * @param handler 事件处理函数
     * @param priority 优先级
     * @returns 订阅标识
     */
    once<T = any>(
        eventType: string,
        handler: EventHandler<T>,
        priority: EventPriority = EventPriority.NORMAL
    ): EventSubscription {
        return this.on(eventType, handler, priority, true);
    }

    /**
     * 取消事件订阅
     * @param subscription 订阅标识
     * @returns 是否成功取消
     */
    off(subscription: EventSubscription): boolean {
        if (!subscription || !subscription.eventType) {
            return false;
        }

        const { eventType } = subscription;

        if (!this.subscriptions.has(eventType)) {
            return false;
        }

        const subscriptions = this.subscriptions.get(eventType)!;

        // 移除指定的订阅
        const index = subscriptions.indexOf(subscription);
        if (index !== -1) {
            subscriptions.splice(index, 1);
            logger.debug(`已移除事件处理器，事件类型："${eventType}"`);

            // 如果订阅列表为空，则移除该事件类型
            if (subscriptions.length === 0) {
                this.subscriptions.delete(eventType);
                logger.debug(`事件类型"${eventType}"已没有处理器，已移除该事件类型`);
            }

            return true;
        }

        return false;
    }

    /**
     * 取消指定事件类型的所有订阅
     * @param eventType 事件类型
     * @returns 是否成功取消
     */
    offAll(eventType: string): boolean {
        if (!eventType || !this.subscriptions.has(eventType)) {
            return false;
        }

        this.subscriptions.delete(eventType);
        logger.debug(`已移除事件类型"${eventType}"的所有处理器`);

        return true;
    }

    /**
     * 同步触发事件
     * @param eventType 事件类型
     * @param data 事件数据
     * @returns 是否有处理器处理了事件
     */
    emit<T>(
        eventType: string,
        data?: T
    ): boolean {
        if (!eventType) {
            logger.error("无效的事件类型");
            return false;
        }

        // 如果没有该事件类型的订阅，直接返回
        if (!this.subscriptions.has(eventType)) {
            return false;
        }

        const subscriptions = [...this.subscriptions.get(eventType)!];
        const onceHandlers: EventSubscription[] = [];

        // 依次调用所有订阅的处理函数
        for (const subscription of subscriptions) {
            try {
                subscription.handler(data);

                // 收集一次性处理器
                if (subscription.once) {
                    onceHandlers.push(subscription);
                }
            } catch (error) {
                logger.error(`事件"${eventType}"的处理器执行出错: ${error}`);
            }
        }

        // 移除一次性处理器
        onceHandlers.forEach(handler => this.off(handler));

        return true;
    }

    /**
     * 清理所有事件订阅
     */
    clear(): void {
        this.subscriptions.clear();
        logger.debug("事件总线已清空");
    }
}