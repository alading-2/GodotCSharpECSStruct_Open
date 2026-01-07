/** @noSelfInFile **/

import { ComponentManager } from "../Component/ComponentManager";
import { Component, ComponentConstructor } from "../Component/Component";
import { EventComponent } from "../Component/Components/事件/EventComponent";
import { EventHandler, EventSubscription, EventPriority, EventTypes } from "../types/EventTypes";
import { Logger } from "../../base/object/工具/logger";
import { DataManager } from "../Schema/DataManager";
import { EntityRelationshipManager } from "./EntityRelationshipManager";
import { EntityManager } from "..";
import { AttributeComponent, UnitComponent, ShieldComponent, TimerComponent, ItemComponent, AbilityComponent, PlayerComponent, BuffComponent, CooldownComponent, ExpComponent, LifecycleComponent, EffectComponent } from "../Component";
import { Inte_AttributeSchema, Inte_UnitSchema, Inte_ShieldSchema, Inte_ItemSchema, Inte_AbilitySchema, Inte_PlayerSchema, Inte_BuffSchema } from "../Schema";
import { SCHEMA_TYPES } from "../Schema/SchemaTypes";
import { EntityRelationshipType } from "./EntityRelationshipType";
import { Inte_TransformSchema } from "../Schema/Schemas/TransformSchema";

const logger = Logger.createLogger("Entity");

interface ComponentDataMap {
    attr: DataManager<Inte_AttributeSchema>;
    unit: DataManager<Inte_UnitSchema>;
    shield: DataManager<Inte_ShieldSchema>;
    item: DataManager<Inte_ItemSchema>;
    ability: DataManager<Inte_AbilitySchema>;
    player: DataManager<Inte_PlayerSchema>;
    buff: DataManager<Inte_BuffSchema>;
    // 未来可以轻松扩展更多数据类型
}

interface ComponentMap {
    attr: AttributeComponent;
    unit: UnitComponent;
    shield: ShieldComponent;
    timer: TimerComponent;
    item: ItemComponent;
    ability: AbilityComponent;
    player: PlayerComponent;
    buff: BuffComponent;
    cooldown: CooldownComponent;
    exp: ExpComponent;
    lifecycle: LifecycleComponent;
    effect: EffectComponent;
    // 未来可以轻松扩展更多组件类型
}

/**
 * Entity - 现代游戏对象基类
 * 
 * 核心职责:
 * 1. 作为组件的容器
 * 2. 管理对象生命周期
 * 3. 提供事件系统接口
 * 4. 支持数据组件架构
 */
export class Entity {
    // 唯一标识符
    protected id: string;
    // entity类型
    protected entityType: string;

    // 组件管理器
    protected componentManager: ComponentManager;

    // 事件组件引用
    protected eventComponent: EventComponent | null = null;

    // 生命周期状态
    protected isActive: boolean = true; // 是否活跃
    protected isDestroyed: boolean = false; // 是否销毁
    protected isInitialized: boolean = false; // 是否初始化
    // 创建时间
    protected createdTime: number;

    // 数据管理器支持
    protected dataManagers: Map<string, DataManager> = new Map(); // 数据管理器映射
    protected primaryDataManager: DataManager | null = null; // 主数据管理器


    /**
     * 构造函数
     * @param entityType 对象类型
     * @param id 对象唯一标识符
     */
    constructor(entityType: string, id: string) {
        this.id = id;
        this.entityType = entityType;
        this.createdTime = os.time();
        this.componentManager = new ComponentManager(this);

        logger.debug(`Entity 创建: ${entityType} (${id})`);
    }

    // ====================== 生命周期管理 ======================

    /** 
     * 初始化方法 - 子类必须实现
     */
    initialize(): void {
    }
    /**
     * 执行初始化流程
     * 会触发OBJECT_INITIALIZING和OBJECT_INITIALIZED事件
     */
    protected performInitialize(): void {
        if (this.isInitialized || this.isDestroyed) return;

        try {
            this.initialize();
            this.isInitialized = true;
            this.emit(EventTypes.ENTITY_INITIALIZED, { source: this });
        } catch (error) {
            logger.error(`Entity初始化失败 ${this.entityType} (${this.id}): ${error}`);
            throw error;
        }
    }

    /**
     * 更新方法，每帧调用
     * @param deltaTime 上一帧到当前帧的时间间隔（秒）
     */
    update(deltaTime: number): void {
        if (this.isDestroyed || !this.isActive) return;

        // 确保已初始化
        if (!this.isInitialized) {
            this.performInitialize();
        }

        // 更新所有组件
        this.componentManager.updateComponents(deltaTime);
    }

    /**
     * 销毁对象
     * 会触发OBJECT_DESTROYING和OBJECT_DESTROYED事件
     */
    destroy(): void {
        if (this.isDestroyed) return;

        // 清理数据管理器
        this.dataManagers.clear();
        this.primaryDataManager = null;
        // 
        this.isDestroyed = true;
        this.isActive = false;
        // 触发销毁事件
        this.emit(EventTypes.ENTITY_DESTROYED, { source: this });
        // 销毁所有组件
        this.componentManager.destroyComponents();
        // 清理所有关系
        EntityRelationshipManager.getInstance().removeAllRelationships(this.getId());
    }


    // ====================== 基本属性访问 ======================

    /**
     * 获取组件
     */
    get component(): ComponentMap {
        return {
            attr: this.getComponent<AttributeComponent>(AttributeComponent),
            unit: this.getComponent<UnitComponent>(UnitComponent),
            shield: this.getComponent<ShieldComponent>(ShieldComponent),
            timer: this.getComponent<TimerComponent>(TimerComponent),
            item: this.getComponent<ItemComponent>(ItemComponent),
            ability: this.getComponent<AbilityComponent>(AbilityComponent),
            player: this.getComponent<PlayerComponent>(PlayerComponent),
            buff: this.getComponent<BuffComponent>(BuffComponent),
            cooldown: this.getComponent<CooldownComponent>(CooldownComponent),
            exp: this.getComponent<ExpComponent>(ExpComponent),
            lifecycle: this.getComponent<LifecycleComponent>(LifecycleComponent),
            effect: this.getComponent<EffectComponent>(EffectComponent),
        }
    }

    /**
     * 获取数据
     */
    get data(): ComponentDataMap {
        return {
            attr: this.getDataManager(SCHEMA_TYPES.ATTRIBUTE_DATA),
            unit: this.getDataManager(SCHEMA_TYPES.UNIT_DATA),
            shield: this.getDataManager(SCHEMA_TYPES.SHIELD_DATA),
            item: this.getDataManager(SCHEMA_TYPES.ITEM_DATA),
            ability: this.getDataManager(SCHEMA_TYPES.ABILITY_DATA),
            player: this.getDataManager(SCHEMA_TYPES.PLAYER_DATA),
            buff: this.getDataManager(SCHEMA_TYPES.BUFF_DATA),
        };
    }


    /**
     * 获取对象ID
     */
    getId(): string {
        return this.id;
    }

    /**
     * 获取Entity类型
     */
    getEntityType(): string {
        return this.entityType;
    }

    /**
     * 检查对象是否活跃
     */
    isActiveObject(): boolean {
        return this.isActive && !this.isDestroyed;
    }

    /**
     * 检查对象是否初始化
     */
    isObjectInitialized(): boolean {
        return this.isInitialized;
    }

    /**
     * 检查对象是否销毁
     */
    isObjectDestroyed(): boolean {
        return this.isDestroyed;
    }

    // ========================数据管理器=================================================================
    /**
     * 添加数据管理器
     * @param schemaName Schema名称
     * @param initialData 初始数据
     */
    addDataManager(schemaName: string, initialData?: any): void {
        if (this.dataManagers.has(schemaName)) {
            logger.warn(`数据管理器已存在，跳过添加: ${schemaName} 在 Entity ${this.id}`);
            return;
        }
        const dataManager = new DataManager(this, schemaName, initialData);
        this.dataManagers.set(schemaName, dataManager);
        // 确保数据在组件初始化前已就绪
        try {
            dataManager.initialize();
        } catch (error) {
            logger.error(`数据管理器初始化失败: ${schemaName} 于 Entity ${this.id}: ${error}`);
            throw error;
        }
        if (!this.primaryDataManager) {
            this.primaryDataManager = dataManager;
        }
    }

    /**
     * 获取数据管理器
     * @param schemaName Schema名称
     * @returns 数据管理器实例，不存在则返回null
     */
    getDataManager(schemaName: string): DataManager | null {
        let dataManager = this.dataManagers.get(schemaName) || null;
        if (!dataManager) {
            logger.error(`${this.getEntityType()}中不存在数据管理器: ${schemaName} `);
            return null;
        }
        return dataManager;
    }

    /**
     * 获取所有数据管理器的schema名称列表
     * @returns schema名称数组
     */
    getDataManagerSchemaNames(): string[] {
        return Array.from(this.dataManagers.keys());
    }
    /**
     * 移除数据管理器
     * @param schemaName Schema名称
     */
    removeDataManager(schemaName: string): void {
        this.dataManagers.delete(schemaName);
        if (this.primaryDataManager && this.primaryDataManager.getSchemaName() === schemaName) {
            this.primaryDataManager = null;
        }
    }

    /**
     * 检查是否已存在指定schema的数据管理器
     * @param schemaName Schema名称
     * @returns 是否存在
     */
    hasDataManager(schemaName: string): boolean {
        return this.dataManagers.has(schemaName);
    }

    /**
     * 获取主数据管理器
     * @returns 主数据管理器，不存在则返回null
     */
    getPrimaryDataManager(): DataManager | null {
        return this.primaryDataManager;
    }

    /**
     * 设置主数据管理器
     * @param dataManager 数据管理器实例
     */
    setPrimaryDataManager(dataManager: DataManager): void {
        this.primaryDataManager = dataManager;
    }

    // ====================== 组件管理 ======================

    /**
     * 添加组件
     * @param componentType 组件类型
     * @param props 组件属性配置
     * @return 添加的组件实例
     */
    addComponent<T extends Component>(
        componentType: ComponentConstructor<T>,
        props?: any
    ): T {
        if (this.isDestroyed) {
            throw new Error(`Cannot add component to destroyed object ${this.id}`);
        }
        // componentManager.addComponent已执行组件初始化
        const component = this.componentManager.addComponent(componentType, props);

        if (componentType.getType() === "EventComponent") {
            this.eventComponent = component as unknown as EventComponent;
        }

        // 触发组件添加事件
        this.emit(EventTypes.COMPONENT_ADDED, {
            componentType: componentType.getType(),
            component,
            source: this
        });
        //@ts-ignore
        return component;
    }

    /**
     * 批量添加组件
     * @param components 组件类型数组
     */
    addComponents(components: ComponentConstructor<Component>[]): void {
        for (const component of components) {
            this.addComponent(component);
        }
    }
    /**
     * 获取组件
     * @param componentType 需要获取的组件类型，组件类
     * @returns 指定类型的组件实例，不存在则返回null
     */
    getComponent<T extends Component>(componentType: ComponentConstructor<T>): T | null {
        return this.componentManager.getComponent(componentType);
    }

    /**
     * 移除组件
     * @param componentType 需要移除的组件类型
     * @returns 是否成功移除组件
     */
    removeComponent<T extends Component>(componentType: ComponentConstructor<T>): boolean {
        if (this.isDestroyed) return false;

        const component = this.getComponent(componentType);
        if (!component) return false;

        const result = this.componentManager.removeComponent(componentType);

        if (componentType.getType() === "EventComponent") {
            this.eventComponent = null;
        }

        if (result) {
            this.emit(EventTypes.COMPONENT_REMOVED, {
                componentType: componentType.getType(),
                component,
                source: this
            });
        }

        return result;
    }

    /**
     * 检查组件是否存在
     * @param componentType 需要检查的组件类型
     * @returns 组件是否存在
     */
    hasComponent<T extends Component>(componentType: ComponentConstructor<T>): boolean {
        return this.componentManager.hasComponent(componentType);
    }

    // ====================== 事件系统 ======================

    /**
     * 注册事件处理器
     * @param eventType 事件类型
     * @param handler 事件处理函数
     * @param priority 事件优先级
     * @param once 是否只触发一次
     * @returns 事件订阅对象，可用于取消订阅
     */
    on<T>(
        eventType: string,
        handler: EventHandler<T>,
        priority: EventPriority = EventPriority.NORMAL,
        once: boolean = false
    ): EventSubscription {
        this.ensureEventComponent();
        return this.eventComponent!.on(eventType, handler, priority, once);
    }

    /**
     * 注册一次性事件处理器
     * @param eventType 事件类型
     * @param handler 事件处理函数
     * @param priority 事件优先级
     * @returns 事件订阅对象，可用于取消订阅
     */
    once<T>(
        eventType: string,
        handler: EventHandler<T>,
        priority: EventPriority = EventPriority.NORMAL
    ): EventSubscription {
        this.ensureEventComponent();
        return this.eventComponent!.once(eventType, handler, priority);
    }

    /**
     * 取消事件订阅
     * @param subscription 事件订阅对象
     * @returns 是否成功取消订阅
     */
    off(subscription: EventSubscription): boolean {
        if (!this.eventComponent) return false;
        return this.eventComponent.off(subscription);
    }

    /**
     * 触发事件
     * @param eventType 事件类型
     * @param data 事件数据
     * @returns 事件是否被成功处理
     */
    emit<T>(
        eventType: string,
        data?: T,
    ): boolean {
        if (!this.eventComponent) return false;
        return this.eventComponent.emit(eventType, data);
    }

    /**
     * 确保事件组件存在
     * 如果事件组件不存在则创建一个
     */
    private ensureEventComponent(): void {
        if (!this.eventComponent) {
            const existingComponent = this.getComponent(EventComponent as any);
            if (existingComponent) {
                this.eventComponent = existingComponent as unknown as EventComponent;
            } else {
                this.eventComponent = this.addComponent(EventComponent as any);
            }
        }
    }

    // ====================== Entity关系管理 ======================

    // 关系类型
    get entityRelationshipType() {
        return EntityRelationshipType
    }

    /**
     * 建立关系
     * @param targetEntity 目标Entity
     * @param relationType 关系类型，可选，如果不提供则根据两个Entity的类型自动生成
     */
    addRelationship(targetEntity: Entity, relationType?: string): void {
        EntityRelationshipManager.getInstance().addRelationship(this.getId(), targetEntity.getId(), relationType);
    }

    /**
     * 获取Entity的所有子Entity（指定关系类型），常用，比如获取UnitEntity的所有ItemEntity
     * @param relationType 关系类型
     * @returns 相关Entity实例数组
     */
    getChildEntities(relationType: string): Entity[] {
        return EntityRelationshipManager.getInstance().getRelationshipsByParentAndType(this.getId(), relationType).map(entityId => EntityManager.get(entityId));
    }

    /**
     * 获取Entity的所有父Entity（指定关系类型），常用，比如获取ItemEntity的所有拥有者UnitEntity
     * @param relationType 关系类型
     * @returns 相关Entity实例数组
     */
    getParentEntities(relationType: string): Entity[] {
        return EntityRelationshipManager.getInstance().getRelationshipsByChildAndType(this.getId(), relationType).map(entityId => EntityManager.get(entityId));
    }


    // 移除关系
    /**
     * 移除关系
     * @param targetEntity 目标Entity实例
     * @param relationType 关系类型
     */
    removeRelationship(targetEntity: Entity, relationType: string): void {
        EntityRelationshipManager.getInstance().removeRelationship(this.getId(), targetEntity.getId(), relationType);
    }


    // ====================== 调试支持 ======================
    /**
     * 获取调试信息
     * @returns 包含对象详细信息的记录对象
     */
    getDebugInfo(): Record<string, any> {
        return {
            id: this.id,
            interfaceName: this.entityType,
            isActive: this.isActive,
            isInitialized: this.isInitialized,
            isDestroyed: this.isDestroyed,
            componentCount: this.componentManager.getComponentCount(),
            createdTime: this.createdTime
        };
    }
}