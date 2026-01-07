/** @noSelfInFile **/

import { Logger } from "../../../../base/object/工具/logger";
import { Component } from "../../Component";
import { Entity } from "../../../Entity/Entity";
import { EventBus } from "../../../EventSystem/EventBus";
import { EventHandler, EventSubscription, EventPriority } from "../../../types/EventTypes";

const logger = Logger.createLogger("EventComponent");

/**
 * 事件组件属性
 */
interface EventComponentProps {
    // 是否自动清理订阅
    autoCleanup?: boolean;
    // 是否在Entity销毁时清理所有订阅
    cleanupOnDestroy?: boolean;
}

/**
 * 事件组件 - 管理游戏对象的事件系统
 * 提供事件注册、分发和管理
 *  EventComponent看似"冗余"，而是实现了现代游戏架构中 Entity级事件系统 的关键组件。它的"简单封装"实际上提供了：
- 作用域隔离 （避免全局事件污染）
- 生命周期管理 （自动内存清理）
- 架构一致性 （符合ECS组件通信标准）
 */
export class EventComponent extends Component<EventComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "EventComponent";

    // 事件总线
    private eventBus: EventBus;

    // 本地事件订阅存储
    private subscriptions: EventSubscription[] = [];

    /**
     * 构造函数
     * @param owner 所属游戏对象
     * @param props 组件属性
     */
    constructor(owner: Entity, props?: EventComponentProps) {
        super(owner, {
            autoCleanup: true,
            cleanupOnDestroy: true,
            ...props
        });

        // 创建事件总线
        this.eventBus = new EventBus();
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        // 无需特殊初始化
    }

    /**
     * 更新组件
     * @param deltaTime 时间增量
     */
    update(deltaTime: number): void {
        // 无需特殊更新
    }

    /**
     * 销毁组件
     */
    destroy(): void {
        if (this.props.cleanupOnDestroy) {
            this.clearAllSubscriptions();
        }

        this.eventBus.clear();
    }

    /**
     * 注册事件处理器
     * @param eventType 事件类型
     * @param handler 事件处理函数
     * @param priority 优先级
     * @param once 是否只触发一次
     * @returns 订阅标识
     */
    on<T>(
        eventType: string,
        handler: EventHandler<T>,
        priority: EventPriority = EventPriority.NORMAL,
        once: boolean = false
    ): EventSubscription {
        const subscription = this.eventBus.on(eventType, handler, priority, once);

        // 如果需要自动清理，则存储订阅
        if (this.props.autoCleanup) {
            this.subscriptions.push(subscription);
        }

        return subscription;
    }

    /**
     * 注册只触发一次的事件处理器
     * @param eventType 事件类型
     * @param handler 事件处理函数
     * @param priority 优先级
     * @returns 订阅标识
     */
    once<T>(
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
        const result = this.eventBus.off(subscription);

        // 如果成功取消且需要自动清理，则从存储中移除
        if (result && this.props.autoCleanup) {
            const index = this.subscriptions.indexOf(subscription);
            if (index !== -1) {
                this.subscriptions.splice(index, 1);
            }
        }

        return result;
    }

    /**
     * 取消指定事件类型的所有订阅
     * @param eventType 事件类型
     * @returns 是否成功取消
     */
    offAll(eventType: string): boolean {
        const result = this.eventBus.offAll(eventType);

        // 如果成功取消且需要自动清理，则从存储中移除相关订阅
        if (result && this.props.autoCleanup) {
            this.subscriptions = this.subscriptions.filter(sub => sub.eventType !== eventType);
        }

        return result;
    }

    /**
     * 触发事件
     * @param eventType 事件类型
     * @param data 事件数据
     * @returns 是否有处理器处理了事件
     */
    emit<T>(
        eventType: string,
        data?: T,
    ): boolean {
        return this.eventBus.emit(eventType, data);
    }

    /**
     * 清除所有事件订阅
     */
    clearAllSubscriptions(): void {
        for (const subscription of this.subscriptions) {
            this.eventBus.off(subscription);
        }

        this.subscriptions = [];
    }

    /**
     * 获取订阅数量
     */
    getSubscriptionCount(): number {
        return this.subscriptions.length;
    }

    /**
     * 获取事件总线
     */
    getEventBus(): EventBus {
        return this.eventBus;
    }
}
