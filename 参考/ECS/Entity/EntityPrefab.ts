/** @noSelfInFile **/
/**
 * EntityPrefab - 现代ECS实体预制体系统
 *
 * 设计理念：
 * - 基于现代游戏引擎（Unity/Unreal）的Prefab概念
 * - 简化的配置合并策略：相同schemaName智能覆盖，不同schemaName追加
 * - 统一的Entity创建入口，无需额外的Builder类
 * - 支持Prefab继承和派生，便于内容迭代
 *
 * 核心优势：
 * - 简洁设计：单一职责，只负责Entity创建和配置管理
 * - 类型安全：完整的TypeScript类型支持
 * - 性能优化：配置缓存和直接合并
 * - 易于扩展：支持插件式组件和数据扩展
 */

import { Entity } from "./Entity";
import { EntityManager } from "..";
import { ComponentConstructor } from "../Component/Component";

/** DataManager 配置 */
export interface PrefabDataConfig {
    schemaName: string;
    initialData?: any;
    isPrimary?: boolean;
}

/** Component 配置 */
export interface PrefabComponentConfig {
    type: ComponentConstructor<any>;
    props?: any;
}

export interface PrefabConfig {
    entityType: string;
    data?: PrefabDataConfig[];
    components?: PrefabComponentConfig[];
}

/** 配置覆盖选项 */
export interface PrefabOverrides {
    id?: string;
    entityType?: string;
    data?: PrefabDataConfig[];
    components?: PrefabComponentConfig[];
}

/**
 * 配置合并工具类 - 简化的数据合并逻辑
 * 现代游戏引擎的核心设计原则：简洁高效
 */
class ConfigMerger {
    /**
     * 智能合并数据配置
     * 策略：相同schemaName覆盖，不同schemaName追加
     * @param base 基础配置数组
     * @param overrides 覆盖配置数组
     * @returns 合并后的配置数组
     */
    static mergeDataConfigs(base: PrefabDataConfig[] = [], overrides: PrefabDataConfig[] = []): PrefabDataConfig[] {
        const dataMap = new Map<string, PrefabDataConfig>();

        // 先添加基础配置
        base.forEach(config => {
            dataMap.set(config.schemaName, { ...config });
        });

        // 覆盖或追加override配置
        overrides.forEach(config => {
            dataMap.set(config.schemaName, { ...config });
        });

        return Array.from(dataMap.values());
    }

    /**
     * 智能合并组件配置
     * 策略：相同类型组件智能覆盖，不同类型组件追加
     * 现代ECS设计原则：一个Entity只能拥有一个同类型组件实例
     * @param base 基础配置数组
     * @param overrides 覆盖配置数组
     * @returns 合并后的配置数组
     */
    static mergeComponentConfigs(base: PrefabComponentConfig[] = [], overrides: PrefabComponentConfig[] = []): PrefabComponentConfig[] {
        const componentMap = new Map<string, PrefabComponentConfig>();

        // 先添加基础组件配置
        base.forEach(config => {
            componentMap.set(config.type.getType(), { ...config });
        });

        // 覆盖或追加override组件配置
        overrides.forEach(config => {
            componentMap.set(config.type.getType(), { ...config });
        });

        return Array.from(componentMap.values());
    }

    /**
     * 合并完整Prefab配置
     * @param base 基础配置
     * @param overrides 覆盖配置
     * @returns 合并后的完整配置
     */
    static mergePrefabConfigs(base: PrefabConfig, overrides: PrefabOverrides): PrefabConfig {
        return {
            entityType: overrides.entityType || base.entityType,
            data: this.mergeDataConfigs(base.data, overrides.data),
            components: this.mergeComponentConfigs(base.components, overrides.components)
        };
    }
}


/**
 * EntityPrefab - 现代ECS实体预制体系统
 *
 * 架构特点：
 * 1. 统一配置管理：所有Prefab配置集中存储和管理
 * 2. 智能数据合并：基于ConfigMerger的统一合并策略
 * 3. 智能组件合并：相同类型组件智能覆盖，符合现代ECS设计原则
 * 4. 简洁API设计：单一入口创建Entity，无需额外Builder类
 * 5. 模板继承系统：支持Prefab派生和组合
 * 6. 现代游戏引擎特性：配置验证、批量操作、导入导出
 */
export class EntityPrefab {
    private static prefabs: Map<string, PrefabConfig> = new Map();

    // ====================== 基础管理方法 ======================

    /**
     * 注册Prefab配置
     * @param name Prefab名称
     * @param config Prefab配置
     */
    static register(name: string, config: PrefabConfig): void {
        this.prefabs.set(name, config);
    }

    /**
     * 注销Prefab
     * @param name Prefab名称
     */
    static unregister(name: string): void {
        this.prefabs.delete(name);
    }

    /**
     * 获取Prefab配置
     * @param name Prefab名称
     * @returns Prefab配置，不存在则返回undefined
     */
    static get(name: string): PrefabConfig | undefined {
        return this.prefabs.get(name);
    }

    /**
     * 获取所有已注册的Prefab
     * @returns 所有Prefab配置的Map
     */
    static getAll(): Map<string, PrefabConfig> {
        return new Map(this.prefabs);
    }

    /**
     * 检查Prefab是否存在
     * @param name Prefab名称
     * @returns 是否存在
     */
    static has(name: string): boolean {
        return this.prefabs.has(name);
    }

    /**
     * 创建Entity实例（基于ConfigMerger的统一合并策略）
     * @param name Prefab名称
     * @param overrides 覆盖配置
     * @returns Entity实例
     */
    static create(name: string, overrides?: PrefabOverrides): Entity {
        const base = this.prefabs.get(name);
        if (!base) {
            throw new Error(`Prefab 未注册: ${name}`);
        }

        // 使用统一的配置合并逻辑
        const mergedConfig = ConfigMerger.mergePrefabConfigs(base, overrides || {});

        return EntityManager.createWithConfig({
            entityType: mergedConfig.entityType,
            id: overrides?.id,
            data: mergedConfig.data,
            components: mergedConfig.components,
        });
    }

    /**
     * 派生新Prefab（基于ConfigMerger的统一合并策略）
     * @param baseName 基础Prefab名称
     * @param newName 新Prefab名称
     * @param modifications 修改配置
     * @returns 是否成功创建
     */
    static derive(baseName: string, newName: string, modifications: PrefabOverrides): boolean {
        const base = this.prefabs.get(baseName);
        if (!base) {
            throw new Error(`基础Prefab未注册: ${baseName}`);
        }

        if (this.prefabs.has(newName)) {
            throw new Error(`派生Prefab名称已存在: ${newName}`);
        }

        // 使用统一的配置合并逻辑
        const derivedConfig = ConfigMerger.mergePrefabConfigs(base, modifications);

        this.register(newName, derivedConfig);
        return true;
    }

    /**
     * 批量注册Prefab（支持配置文件导入）
     * @param configs Prefab配置映射
     */
    static registerBatch(configs: Record<string, PrefabConfig>): void {
        Object.entries(configs).forEach(([name, config]) => {
            this.register(name, config);
        });
    }

    /**
     * 清空所有Prefab（主要用于测试和重载）
     */
    static clear(): void {
        this.prefabs.clear();
    }

    /**
     * 获取Prefab统计信息
     * @returns 统计信息对象
     */
    static getStats(): {
        totalPrefabs: number;
        prefabsByEntityType: Record<string, number>;
        averageComponentsPerPrefab: number;
        averageDataPerPrefab: number;
    } {
        const prefabArray = Array.from(this.prefabs.values());
        const prefabsByEntityType: Record<string, number> = {};
        let totalComponents = 0;
        let totalData = 0;

        prefabArray.forEach(config => {
            prefabsByEntityType[config.entityType] = (prefabsByEntityType[config.entityType] || 0) + 1;
            totalComponents += config.components?.length || 0;
            totalData += config.data?.length || 0;
        });

        return {
            totalPrefabs: this.prefabs.size,
            prefabsByEntityType,
            averageComponentsPerPrefab: this.prefabs.size > 0 ? totalComponents / this.prefabs.size : 0,
            averageDataPerPrefab: this.prefabs.size > 0 ? totalData / this.prefabs.size : 0
        };
    }

    /**
     * 验证Prefab配置（现代游戏引擎特性）
     * @param config Prefab配置
     * @returns 验证结果
     */
    static validateConfig(config: PrefabConfig): { valid: boolean; errors: string[] } {
        const errors: string[] = [];

        if (!config.entityType || config.entityType.trim() === '') {
            errors.push('entityType不能为空');
        }

        if (config.data) {
            const schemaNames = new Set<string>();
            config.data.forEach((dataConfig, index) => {
                if (!dataConfig.schemaName || dataConfig.schemaName.trim() === '') {
                    errors.push(`data[${index}].schemaName不能为空`);
                }
                if (schemaNames.has(dataConfig.schemaName)) {
                    errors.push(`重复的schemaName: ${dataConfig.schemaName}`);
                }
                schemaNames.add(dataConfig.schemaName);
            });
        }

        if (config.components) {
            const componentTypes = new Set<string>();
            config.components.forEach((componentConfig, index) => {
                if (!componentConfig.type) {
                    errors.push(`component[${index}].type不能为空`);
                } else {
                    const typeName = componentConfig.type.getType();
                    if (componentTypes.has(typeName)) {
                        errors.push(`重复的组件类型: ${typeName}`);
                    }
                    componentTypes.add(typeName);
                }
            });
        }

        return {
            valid: errors.length === 0,
            errors
        };
    }

    /**
     * 导出所有Prefab配置（现代游戏引擎特性）
     * 用于配置文件保存和版本控制
     * @returns Prefab配置的JSON字符串
     */
    static exportConfigs(): string {
        const configs: Record<string, PrefabConfig> = {};
        this.prefabs.forEach((config, name) => {
            configs[name] = config;
        });
        return JSON.stringify(configs, null, 2);
    }

    /**
     * 从JSON导入Prefab配置（现代游戏引擎特性）
     * @param jsonString JSON配置字符串
     * @param overwrite 是否覆盖已存在的Prefab
     * @returns 导入结果
     */
    static importConfigs(jsonString: string, overwrite: boolean = false): { success: boolean; imported: number; errors: string[] } {
        const result = { success: false, imported: 0, errors: [] as string[] };

        try {
            const configs = JSON.parse(jsonString) as Record<string, PrefabConfig>;

            Object.entries(configs).forEach(([name, config]) => {
                try {
                    const validation = this.validateConfig(config);
                    if (!validation.valid) {
                        result.errors.push(`Prefab ${name} 配置无效: ${validation.errors.join(', ')}`);
                        return;
                    }

                    if (this.has(name) && !overwrite) {
                        result.errors.push(`Prefab ${name} 已存在，跳过导入`);
                        return;
                    }

                    this.register(name, config);
                    result.imported++;
                } catch (error) {
                    result.errors.push(`导入Prefab ${name} 失败: ${error}`);
                }
            });

            result.success = result.errors.length === 0;
        } catch (error) {
            result.errors.push(`JSON解析失败: ${error}`);
        }

        return result;
    }
}


