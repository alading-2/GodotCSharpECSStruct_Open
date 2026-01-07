/** @noSelfInFile **/

import { Component, ComponentConstructor } from "./Component";
import { Logger } from "../../base/object/工具/logger";
import { Entity } from "..";

const logger = Logger.createLogger("ComponentManager");

/**
 * 组件管理器
 * 负责组件的创建、获取、移除和生命周期管理
 */
export class ComponentManager {
    // 组件管理器所属的游戏对象
    readonly owner: Entity;

    // 存储组件实例
    private components: Map<string, Component> = new Map();

    // 全局组件实例记录，按组件类型分类存储所有实例
    private static globalComponentInstances: Map<string, Set<Component>> = new Map();

    /**
     * 构造函数
     * @param owner 所属游戏对象
     */
    constructor(owner: Entity) {
        this.owner = owner;
        logger.debug(`ComponentManager 已创建: ${owner.getId()}`);
    }

    /**
     * 添加组件
     * @param componentType 组件类型，组件类
     * @param props 组件属性
     * @returns 添加的组件实例
     */
    addComponent<T extends Component>(componentType: ComponentConstructor<T>, props?: any): T {
        const componentName = componentType.getType();

        // 检查组件是否已存在
        if (this.hasComponent(componentType)) {
            logger.warn(`组件已存在，无法重复添加: ${componentName}`);
            return this.getComponent(componentType)!;
        }

        // 创建新组件
        const component = new componentType(this.owner, props);

        // 存储组件
        this.components.set(componentName, component);

        // 将组件添加到全局实例记录中
        if (!ComponentManager.globalComponentInstances.has(componentName)) {
            ComponentManager.globalComponentInstances.set(componentName, new Set());
        }
        ComponentManager.globalComponentInstances.get(componentName)!.add(component);

        // 初始化组件
        component.performInitialize();

        logger.debug(`组件已添加: ${componentName} 到 ${this.owner.getId()}`);
        return component;
    }

    /**
     * 获取组件
     * @param componentType 组件类型，组件类
     * @returns 组件实例，如果不存在则返回null
     */
    getComponent<T extends Component>(componentType: ComponentConstructor<T>): T | null {
        const componentName = componentType.getType();
        return this.getComponentByName(componentName) as T | null;
    }

    /**
     * 获取组件（按类型名称）
     * @param componentTypeName 组件类型名称
     * @returns 组件实例，如果不存在则返回null
     */
    getComponentByName(componentTypeName: string): Component | null {
        const component = this.components.get(componentTypeName);
        if (!component) {
            logger.error(`${this.owner.getEntityType()}中不存在组件: ${componentTypeName}`);
        }
        return component || null;
    }
    /**
     * 获取所有组件
     * @returns 所有组件实例数组
     */
    getAllComponents(): Component[] {
        return Array.from(this.components.values());
    }

    /**
     * 检查是否有指定组件
     * @param componentType 组件类型，组件类
     * @returns 是否存在该组件
     */
    hasComponent<T extends Component>(componentType: ComponentConstructor<T>): boolean {
        const componentName = componentType.getType();
        return this.components.has(componentName);
    }

    /**
     * 检查特定组件实例是否属于当前Entity
     * @param component 组件实例
     * @returns 是否存在该组件
     */
    hasComponentInstance(component: Component): boolean {
        if (!component) return false;

        // 检查组件是否在当前组件列表中
        for (const [, existingComponent] of this.components) {
            if (existingComponent === component) {
                return true;
            }
        }

        return false;
    }

    /**
     * 移除组件
     * @param componentType 组件类型，组件类
     * @returns 是否成功移除
     */
    removeComponent<T extends Component>(componentType: ComponentConstructor<T>): boolean {
        const componentName = componentType.getType();
        const component = this.components.get(componentName);

        if (!component) {
            return false;
        }

        // 销毁组件
        (component as any).performDestroy();

        // 从存储中移除
        this.components.delete(componentName);

        // 从全局实例记录中移除
        const globalInstances = ComponentManager.globalComponentInstances.get(componentName);
        if (globalInstances) {
            globalInstances.delete(component);
            // 如果该类型的组件实例为空，则删除整个Set
            if (globalInstances.size === 0) {
                ComponentManager.globalComponentInstances.delete(componentName);
            }
        }

        logger.debug(`组件已移除: ${componentName} 从 ${this.owner.getId()}`);
        return true;
    }

    /**
     * 更新所有启用的组件
     * @param deltaTime 时间间隔
     */
    updateComponents(deltaTime: number): void {
        for (const component of this.components.values()) {
            if ((component as any).isComponentEnabled()) {
                (component as any).performUpdate(deltaTime);
            }
        }
    }

    /**
     * 销毁所有组件
     */
    destroyComponents(): void {
        // 先销毁所有组件并从全局记录中移除
        for (const [componentName, component] of this.components) {
            (component as any).performDestroy();
            
            // 从全局实例记录中移除
            const globalInstances = ComponentManager.globalComponentInstances.get(componentName);
            if (globalInstances) {
                globalInstances.delete(component);
                // 如果该类型的组件实例为空，则删除整个Set
                if (globalInstances.size === 0) {
                    ComponentManager.globalComponentInstances.delete(componentName);
                }
            }
        }

        // 清空组件存储
        this.components.clear();

        logger.debug(`所有组件已移除 从 ${this.owner.getId()}`);
    }

    /**
     * 获取组件数量
     * @returns 组件总数
     */
    getComponentCount(): number {
        return this.components.size;
    }

    /**
     * 获取指定组件类型的所有实例
     * @param componentType 组件类型
     * @returns 该类型的所有组件实例数组
     */
    static getAllComponentInstances<T extends Component>(componentType: ComponentConstructor<T>): T[] {
        const componentName = componentType.getType();
        const instances = ComponentManager.globalComponentInstances.get(componentName);
        return instances ? Array.from(instances) as T[] : [];
    }

    /**
     * 获取指定组件类型名称的所有实例
     * @param componentTypeName 组件类型名称
     * @returns 该类型的所有组件实例数组
     */
    static getAllComponentInstancesByName(componentTypeName: string): Component[] {
        const instances = ComponentManager.globalComponentInstances.get(componentTypeName);
        return instances ? Array.from(instances) : [];
    }
}