/** @noSelfInFile **/

import { Entity } from "../Entity/Entity";
import { Logger } from "../../base/object/工具/logger";
import { EventTypes } from "../types/EventTypes";

const logger = Logger.createLogger("Component");

/**
 * Component类的类型接口
 * 定义组件的构造函数类型和数据映射类型
 */
export interface ComponentConstructor<T extends Component> {
    new(owner: Entity, props?: any): T;
    getType(): string;
}

/**
 * 组件基类 - 所有组件的统一基类
 * 提供组件生命周期管理和功能模块化
 */
export class Component<TProps = any> {
    // 组件所属的游戏对象
    protected owner: Entity;

    // 组件是否启用
    protected isEnabled: boolean = true;

    // 组件是否已初始化
    protected isInitialized: boolean = false;

    // 组件是否已销毁
    protected isDestroyed: boolean = false;

    // 组件属性
    protected props: TProps;

    // 组件依赖列表 - 记录该组件依赖的其他组件类型
    protected dependencies: ComponentConstructor<any>[] = [];

    // 组件类型名称
    protected static readonly TYPE: string = "Component";

    /**
     * 获取组件类型
     * 必须由子类实现以返回唯一类型名
     */
    static getType(): string {
        return this.TYPE;
    }
    //一个静态一个实例的原因：静态方法 static getType() 可以在不创建实例的情况下使用，而实例方法 getType() 需要创建实例才能使用
    /**
     * 获取组件类型实例方法
     */
    getType(): string {
        // 实例方法使用静态方法
        // this.constructor 动态获取当前实例的类型，
        return (this.constructor as typeof Component).getType();
    }

    /**
     * 构造函数
     * @param owner 所属游戏对象
     * @param props 组件属性
     */
    constructor(owner: Entity, props?: TProps) {
        this.owner = owner;
        this.props = props || {} as TProps;

        logger.debug(`组件已创建: ${this.getType()}`);
    }



    /**
     * 声明组件依赖
     * 添加此组件需要依赖的其他组件类型
     * @param componentType 依赖的组件类型
     */
    protected requireComponent<T extends Component>(componentType: ComponentConstructor<T>): void {
        this.dependencies.push(componentType);
    }

    /**
     * 获取组件依赖列表
     * @returns 依赖的组件类型数组
     */
    getDependencies(): ComponentConstructor<any>[] {
        return this.dependencies;
    }

    /**
     * 检查并解析组件依赖
     * @returns 是否所有依赖都已满足
     */
    ensureDependencies(): boolean {
        // 检查每个依赖是否存在
        for (const dependency of this.dependencies) {
            const dependentComponent = this.owner.getComponent(dependency);

            // 如果依赖不存在，尝试添加
            if (!dependentComponent) {
                try {
                    this.owner.addComponent(dependency);
                    logger.debug(`为组件 ${this.getType()} 自动添加依赖组件: ${dependency.getType()}`);
                } catch (error) {
                    logger.error(`无法满足组件 ${this.getType()} 的依赖 ${dependency.getType()}: ${error}`);
                    return false;
                }
            }
        }

        return true;
    }

    // ======================生命周期管理====================================================

    /**
     * 初始化组件，Entity添加组件时运行performInitialize()
     * 由子类实现具体逻辑
     */
    initialize(): void {
        // 组件初始化逻辑
    }

    /**
     * 执行初始化，ComponentManager.addComponent添加组件时执行
     * 确保组件只初始化一次
     */
    performInitialize(): void {
        if (this.isInitialized || this.isDestroyed) {
            return;
        }

        try {
            // 确保所有依赖组件都已添加
            if (!this.ensureDependencies()) {
                logger.error(`组件 ${this.getType()} 初始化失败: 依赖不满足`);
                return;
            }

            this.initialize();
            this.isInitialized = true;

            // 发送初始化完成事件
            this.owner.emit(EventTypes.COMPONENT_INITIALIZED, { component: this });
            logger.debug(`组件已初始化: ${this.getType()}`);
        } catch (error) {
            logger.error(`组件初始化出错 ${this.getType()}: ${error}`);
        }
    }

    /**
     * 更新组件
     * @param deltaTime 时间增量
     */
    update(deltaTime: number): void {
        // 默认实现，子类可以重写
    }

    /**
     * 执行更新
     * 只有组件启用时才会更新
     * @param deltaTime 时间增量
     */
    performUpdate(deltaTime: number): void {
        if (!this.isInitialized || this.isDestroyed || !this.isEnabled) {
            return;
        }

        try {
            this.update(deltaTime);
        } catch (error) {
            logger.error(`组件更新出错 ${this.getType()}: ${error}`);
        }
    }

    /**
     * 销毁组件
     * 由子类实现具体逻辑
     */
    destroy(): void {
        // 组件销毁逻辑
    }

    /**
     * 执行销毁
     * 确保组件只销毁一次
     */
    performDestroy(): void {
        if (this.isDestroyed) {
            return;
        }

        try {
            this.destroy();
            this.isDestroyed = true;
            logger.debug(`组件已销毁: ${this.getType()}`);
        } catch (error) {
            logger.error(`组件销毁出错 ${this.getType()}: ${error}`);
        }
    }

    /**
     * 启用组件
     */
    enable(): void {
        if (this.isEnabled || this.isDestroyed) {
            return;
        }

        this.isEnabled = true;
        this.onEnable();
        logger.debug(`组件已启用: ${this.getType()}`);
    }

    /**
     * 禁用组件
     */
    disable(): void {
        if (!this.isEnabled || this.isDestroyed) {
            return;
        }

        this.isEnabled = false;
        this.onDisable();
        logger.debug(`组件已禁用: ${this.getType()}`);
    }

    /**
     * 启用时调用
     * 由子类实现具体逻辑
     */
    protected onEnable(): void { }

    /**
     * 禁用时调用
     * 由子类实现具体逻辑
     */
    protected onDisable(): void { }


    // ==================== 组件状态检查 ====================
    /**
     * 检查组件是否已启用
     */
    isComponentEnabled(): boolean {
        return this.isEnabled && !this.isDestroyed;
    }

    /**
     * 检查组件是否已初始化
     */
    isComponentInitialized(): boolean {
        return this.isInitialized;
    }

    /**
     * 检查组件是否已销毁
     */
    isComponentDestroyed(): boolean {
        return this.isDestroyed;
    }

    /**
     * 获取组件所有者
     */
    getOwner(): Entity {
        return this.owner;
    }

    /**
     * 设置组件属性
     * @param props 属性对象
     */
    setProps(props: Partial<TProps>): void {
        this.props = { ...this.props, ...props };
    }

    /**
     * 获取组件属性
     */
    getProps(): Readonly<TProps> {
        return this.props;
    }



}