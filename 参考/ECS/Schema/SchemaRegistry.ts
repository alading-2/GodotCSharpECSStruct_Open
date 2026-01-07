
import { Logger } from "../../base/object/工具/logger";
import { SCHEMA_TYPES } from "./SchemaTypes";
import { Schema, SchemaValidationResult, SchemaUtils } from "./Schema";

const logger = Logger.createLogger("SchemaRegistry");

/**
 * Schema注册中心 - 管理所有数据定义
 */
/** Schema存储映射 */
export class SchemaRegistry {
    private static schemas = new Map<string, Schema<any>>();

    /** 是否已初始化 */
    private static initialized = false;

    /** Schema继承关系映射 */
    private static inheritanceMap = new Map<string, string>();

    /**
     * 注册Schema
     */
    static register<T>(name: string, schema: Schema<T>): void {
        // 验证Schema
        const validationResult = this.validateSchema(schema);
        if (!validationResult.valid) {
            logger.error(`Schema注册失败 ${name}:`, validationResult.errors);
            throw new Error(`Schema验证失败: ${validationResult.errors.join(', ')}`);
        }

        // 处理继承关系
        if (schema.extends) {
            this.inheritanceMap.set(name, schema.extends);
        }

        this.schemas.set(name, schema);
        logger.debug(`Schema已注册: ${name}`);
    }

    /**
     * 获取Schema
     */
    static getSchema<T>(name: string): Schema<T> {
        this.initialize();

        const schema = this.schemas.get(name);
        if (!schema) {
            logger.error(`未找到Schema: ${name}`);
            throw new Error(`未找到Schema: ${name}`);
        }

        // 处理继承
        return this.resolveInheritance<T>(name, schema);
    }

    /**
     * 检查Schema是否存在
     */
    static hasSchema(name: string): boolean {
        this.initialize();
        return this.schemas.has(name);
    }

    /**
     * 获取所有已注册的Schema名称
     */
    static getRegisteredSchemas(): string[] {
        this.initialize();
        return Array.from(this.schemas.keys());
    }

    /**
     * 验证Schema定义
     */
    static validateSchema<T>(schema: Schema<T>): SchemaValidationResult {
        const errors: string[] = [];
        const warnings: string[] = [];

        // 基本验证
        if (!schema.schemaName) {
            errors.push('Schema必须有interfaceName');
        }

        if (!schema.properties || schema.properties.length === 0) {
            errors.push('Schema必须至少有一个属性定义');
        }

        // 验证属性定义
        if (schema.properties) {
            const propertyKeys = new Set<string>();

            for (const prop of schema.properties) {
                const keyStr = prop.key as string;

                // 检查重复属性
                if (propertyKeys.has(keyStr)) {
                    errors.push(`重复的属性定义: ${keyStr}`);
                }
                propertyKeys.add(keyStr);

                // 验证属性类型
                if (!['number', 'string', 'boolean', 'object', 'array', 'any'].includes(prop.type)) {
                    errors.push(`无效的属性类型: ${prop.type} (属性: ${keyStr})`);
                }

                // 验证默认值类型
                if (!SchemaUtils.validatePropertyType(prop.defaultValue, prop.type)) {
                    errors.push(`属性 ${keyStr} 的默认值类型与定义不匹配`);
                }
            }
        }

        // 验证计算属性
        if (schema.computed) {
            const computedKeys = new Set<string>();
            const propertyKeys = new Set(schema.properties.map(p => p.key as string));

            for (const computedProp of schema.computed) {
                const keyStr = computedProp.key as string;

                // 检查重复计算属性
                if (computedKeys.has(keyStr)) {
                    errors.push(`重复的计算属性定义: ${keyStr}`);
                }
                computedKeys.add(keyStr);

                // 检查计算属性与普通属性冲突
                if (propertyKeys.has(keyStr)) {
                    errors.push(`计算属性 ${keyStr} 与普通属性冲突`);
                }

                // 验证依赖关系
                for (const dep of computedProp.dependencies) {
                    const depStr = dep as string;
                    if (!propertyKeys.has(depStr) && !computedKeys.has(depStr)) {
                        errors.push(`计算属性 ${keyStr} 依赖的属性 ${depStr} 不存在`);
                    }
                }

                // 检查循环依赖（简单检查）
                if (computedProp.dependencies.includes(computedProp.key)) {
                    errors.push(`计算属性 ${keyStr} 存在自循环依赖`);
                }
            }
        }

        // 验证继承关系
        if (schema.extends) {
            if (!this.schemas.has(schema.extends)) {
                warnings.push(`继承的Schema ${schema.extends} 尚未注册`);
            }
        }

        return {
            valid: errors.length === 0,
            errors,
            warnings
        };
    }

    /**
     * 解析Schema继承关系
     */
    private static resolveInheritance<T>(name: string, schema: Schema<T>): Schema<T> {
        if (!schema.extends) {
            return schema;
        }

        const parentSchema = this.schemas.get(schema.extends);
        if (!parentSchema) {
            logger.warn(`找不到父Schema: ${schema.extends}`);
            return schema;
        }

        // 递归解析父Schema的继承关系
        const resolvedParent = this.resolveInheritance(schema.extends, parentSchema);

        // 合并Schema
        return SchemaUtils.mergeSchemas(resolvedParent, schema);
    }

    /**
     * 获取Schema统计信息
     * 返回所有已注册Schema的统计数据，包括:
     * - 总Schema数量
     * - 带继承关系的Schema数量 
     * - 总属性数量
     * - 总计算属性数量
     * - 总验证器数量
     * 
     * @returns Schema统计信息对象
     */
    static getStats(): {
        totalSchemas: number;        // Schema总数
        schemasWithInheritance: number;  // 带继承的Schema数量
        totalProperties: number;     // 属性总数
        totalComputedProperties: number;  // 计算属性总数
    } {
        // 确保Schema系统已初始化
        this.initialize();

        // 统计各项数据
        let totalProperties = 0;         // 属性计数器
        let totalComputedProperties = 0; // 计算属性计数器

        // 遍历所有Schema进行统计
        for (const schema of this.schemas.values()) {
            totalProperties += schema.properties.length;
            totalComputedProperties += schema.computed?.length || 0;
        }

        // 返回统计结果
        return {
            totalSchemas: this.schemas.size,
            schemasWithInheritance: this.inheritanceMap.size,
            totalProperties,
            totalComputedProperties,
        };
    }

    /**
     * 初始化所有Schema
     */
    private static initialize(): void {
        if (this.initialized) return;

        try {
            // 动态导入所有Schema定义
            // this.loadAllSchemas();
            this.initialized = true;

            const stats = this.getStats();
            logger.info(`Schema注册中心初始化完成: ${stats.totalSchemas}个Schema, ${stats.totalProperties}个属性`);
        } catch (error) {
            logger.error(`Schema初始化失败:`, error);
            throw error;
        }
    }

}
