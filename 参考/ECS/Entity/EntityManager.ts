import { Logger } from "../../base/object/工具/logger";
import { ComponentConstructor } from "../Component/Component";
import { Entity } from "./Entity";

const logger = Logger.createLogger("EntityManager");

/**
 * EntityManager - 游戏对象管理器（静态类）
 * 
 * 核心职责:
 * 1. 对象注册与创建
 * 2. 对象查询与检索
 * 3. 对象生命周期管理
 * 4. 对象池集成
 */
export class EntityManager {
    /** 存储所有已创建的Entity实例 */
    private static instances = new Map<string, Entity>();
    /** entityType到Entity实例的映射 */
    private static entitiesByType = new Map<string, Set<Entity>>();
    /** ID计数器 */
    private static idCounters = new Map<string, number>();

    /**
     * 创建Entity实例
     * @param entityType Entity类型
     * @param id Entity实例ID
     * @return 注册的Entity类实例
     */
    public static create(entityType: string, id?: string): Entity {
        // 生成ID（如果未提供）
        const objectId = id || EntityManager.generateId(entityType);

        // 若已存在则直接返回（避免重复创建）
        if (EntityManager.instances.has(objectId)) {
            logger.warn(`Entity ID已存在: ${objectId}`);
            return EntityManager.instances.get(objectId) as Entity;
        }

        // 使用基础 Entity；行为由组件与数据决定
        const entity = new Entity(entityType, objectId);
        EntityManager.instances.set(objectId, entity);

        // 添加到类型映射（按字符串类型分组）
        let typeSet = EntityManager.entitiesByType.get(entityType);
        if (!typeSet) {
            typeSet = new Set<Entity>();
            EntityManager.entitiesByType.set(entityType, typeSet);
        }
        typeSet.add(entity);

        return entity;
    }

    /**
     * 创建带数据管理器的Entity
     * @param entityType Entity类型
     * @param dataManagers 数据管理器配置
     * @param id 可选的ID
     * @returns 创建的Entity
     */
    public static createWithData<T>(
        entityType: string,
        dataManagers: Array<{
            schemaName: string;
            initialData?: any;
            isPrimary?: boolean;
        }>,
        id?: string
    ): Entity {
        const entity = EntityManager.create(entityType, id);

        // 添加数据管理器
        for (const config of dataManagers) {
            entity.addDataManager(config.schemaName, config.initialData);
            if (config.isPrimary) {
                const dm = entity.getDataManager(config.schemaName);
                if (dm) entity.setPrimaryDataManager(dm);
            }
        }

        return entity;
    }

    /**
     * 统一配置创建（Builder 简版）
     * @param cfg 配置对象
     * @param cfg.entityType Entity类型
     * @param cfg.id 可选的ID
     * @param cfg.data 数据管理器配置
     * @param cfg.components 组件配置
     * @returns 创建的Entity
     */
    public static createWithConfig(cfg: {
        entityType: string;
        id?: string;
        data?: Array<{ schemaName: string; initialData?: any; isPrimary?: boolean }>;
        components?: Array<{ type: ComponentConstructor<any>; props?: any }>;
    }): Entity {
        const e = EntityManager.create(cfg.entityType, cfg.id);
        if (cfg.data) {
            for (const d of cfg.data) {
                e.addDataManager(d.schemaName, d.initialData);
                if (d.isPrimary) {
                    const dm = e.getDataManager(d.schemaName);
                    if (dm) e.setPrimaryDataManager(dm);
                }
            }
        }
        if (cfg.components) {
            for (const c of cfg.components) {
                e.addComponent(c.type, c.props);
            }
        }
        return e;
    }

    /**
     * 生成唯一ID
     * @param entityType Entity类型
     * @returns 唯一ID
     */
    private static generateId(entityType: string): string {
        const counter = (EntityManager.idCounters.get(entityType) || 0) + 1;
        EntityManager.idCounters.set(entityType, counter);
        return `${entityType}_${counter}`;
    }

    /**
     * 获取指定ID的Entity
     */
    public static get(id: string): Entity | undefined {
        return EntityManager.instances.get(id);
    }

    /**
     * 获取指定类型的所有Entity
     * @param entityType Entity类型
     */
    public static getByType(entityType: string): Entity[] {
        const typeSet = EntityManager.entitiesByType.get(entityType);
        return typeSet ? Array.from(typeSet) : [];
    }

    /**
     * 获取所有Entity
     */
    public static getAll(): Entity[] {
        return Array.from(EntityManager.instances.values());
    }

    /**
     * 检查Entity是否存在
     * @param entityId Entity ID
     */
    public static has(entityId: string): boolean {
        return EntityManager.instances.has(entityId);
    }

    /**
     * 根据条件查找Entity
     * @param predicate 查找条件
     * @returns 符合条件的Entity数组
     */
    public static find(predicate: (entity: Entity) => boolean): Entity[] {
        return Array.from(EntityManager.instances.values()).filter(predicate);
    }

    /**
     * 高级查询Entity
     * @param options 查询选项
     * @param options.entityType entity类型
     * @param options.hasComponent 查询包含的组件的entity
     * @param options.filter 自定义过滤函数
     * @param options.limit 限制返回数量
     * @return 符合条件的Entity数组
     */
    public static query(options: {
        entityType?: string;
        hasComponent?: ComponentConstructor<any>[];
        filter?: (obj: Entity) => boolean;
        limit?: number;
    }): Entity[] {

        let results = Array.from(EntityManager.instances.values());

        // 按类型过滤
        if (options.entityType) {
            results = results.filter(obj => obj.getEntityType() === options.entityType);
        }

        // 按组件过滤
        if (options.hasComponent && options.hasComponent.length > 0) {
            results = results.filter(obj =>
                options.hasComponent!.every(componentClass => obj.hasComponent(componentClass))
            );
        }

        // 自定义过滤
        if (options.filter) {
            results = results.filter(options.filter);
        }

        // 限制结果数量
        if (options.limit && options.limit > 0 && results.length > options.limit) {
            results = results.slice(0, options.limit);
        }

        return results;
    }

    /**
     * 获取实例总数
     * @param entityType Entity类型
     * @returns Entity总数
     */
    public static getCount(entityType?: string): number {
        return entityType ? EntityManager.getByType(entityType).length : EntityManager.instances.size;
    }

    /** 待销毁的Entity队列 */
    private static destroyQueue = new Set<string>();

    /**
     * 销毁Entity
     * @param id EntityID
     */
    public static destroy(id: string): boolean {
        const entity = EntityManager.instances.get(id);
        if (!entity) {
            return false;
        }

        // 从类型映射中移除
        const entityType = entity.getEntityType();
        const typeSet = EntityManager.entitiesByType.get(entityType);
        if (typeSet) {
            typeSet.delete(entity);
        }

        entity.destroy();
        EntityManager.instances.delete(id);
        return true;
    }

    /**
     * 批量销毁指定类型的所有Entity
     * @param entityType Entity类型
     */
    public static destroyAllByType(entityType: string): number {
        const entitys = EntityManager.getByType(entityType);

        for (const obj of entitys) {
            EntityManager.destroy(obj.getId());
        }

        return entitys.length;
    }

    /**
     * 清理所有Entity
     */
    public static clearAll(): void {
        for (const obj of EntityManager.getAll()) {
            EntityManager.destroy(obj.getId());
        }

        EntityManager.idCounters.clear();
    }

    /**
     * 获取统计信息
     */
    public static getStats(): { totalObjects: number; byType: Record<string, number>; pendingDestroy: number } {
        const byType: Record<string, number> = {};

        for (const obj of EntityManager.instances.values()) {
            const type = obj.getEntityType();
            byType[type] = (byType[type] || 0) + 1;
        }

        return {
            totalObjects: EntityManager.instances.size,
            byType,
            pendingDestroy: EntityManager.destroyQueue.size
        };
    }
}


