/** @noSelfInFile **/

import { Entity } from "../Entity/Entity";
import { EntityManager } from "../Entity/EntityManager";
import { ComponentConstructor, Component } from "..";
import { ComponentManager } from "../Component/ComponentManager";
import { TimerManager } from "../../base/object/工具/Timer/TimerManager";

/**
 * System基类 - ECS架构中的系统基类
 * 
 * 在现代ECS架构中，System负责处理具有特定组件的Entity的业务逻辑。
 * System应该是无状态的，只包含纯粹的业务逻辑。
 * 其生命周期（初始化、更新、销毁）和状态由SystemManager统一管理。
 * 
 * 设计原则:
 * - **单一职责**: 每个System专注于一个特定的游戏机制（如移动、冷却、战斗）。
 * - **无状态**: 不在System实例中存储游戏状态数据。所有数据都应存在于Component中。
 * - **逻辑封装**: 封装对Component数据的操作和处理逻辑。
 */
export class System {
    // 计时器Id
    protected timerId: string;

    // 计时器间隔
    protected readonly timerInterval: number;

    /**
     * 获取系统类型名称 (用于标识和管理)
     * 子类必须实现此静态方法
     */
    public static readonly TYPE: string = "System";

    /**
     * 获取系统优先级 (用于决定执行顺序)
     * 子类必须实现此静态方法
     */
    public static readonly PRIORITY: number = 0;

    /**
     * 初始化系统
     * 在系统被添加到SystemManager时调用一次，用于执行初始化逻辑，如事件监听。
     */
    public initialize(): void {
        // 初始化计时器管理器
        this.timerId = TimerManager.getInstance().createTimer({
            duration: this.timerInterval,
            repeat: true,
            callback: () => {
                this.update();
            }
        });
        // 子类可重写
    }

    /**
     * 更新系统
     * 由SystemManager在每一帧调用，执行核心业务逻辑。
     * 如果一个系统不需要每帧更新（例如，它只是事件驱动的），则可以不实现此方法。
     */
    public update(): void {
        // 子类可重写
    }

    /**
     * 销毁系统
     * 在系统从SystemManager移除时调用，用于执行清理逻辑，如取消事件监听。
     */
    public destroy(): void {
        // 子类可重写
    }

    /**
     * 查询拥有指定组件的实体列表
     * 这是一个方便子类使用的工具方法。
     * @param componentClasses 组件类构造函数的数组
     * @returns 返回符合条件的实体数组
     */
    protected getEntities(componentClasses: ComponentConstructor<any>[]): Entity[] {
        return EntityManager.query({
            hasComponent: componentClasses
        });
    }

    /**
     * 获取指定组件类型的所有实例
     * 这是一个更高效的方法，直接从ComponentManager获取组件实例，无需查询Entity
     * @param componentType 组件类型
     * @returns 该类型的所有组件实例数组
     */
    protected getComponentInstances<T extends Component>(componentType: ComponentConstructor<T>): T[] {
        return ComponentManager.getAllComponentInstances(componentType);
    }

    /**
     * 获取系统名称的实例方法
     * @returns 系统名称
     */
    public getSystemName(): string {
        return (this.constructor as typeof System).TYPE;
    }

    /**
     * 获取系统优先级的实例方法
     * @returns 系统优先级
     */
    public getPriority(): number {
        return (this.constructor as typeof System).PRIORITY;
    }
}

