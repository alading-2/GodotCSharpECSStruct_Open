/** @noSelfInFile **/

import { SchemaRegistry } from "./SchemaRegistry";
import { Schema, PropertyConstraints, SchemaValidationResult, SchemaUtils, ComputedPropertyDefinition, PropertyDefinition } from "./Schema";
import { Logger } from "../../base/object/工具/logger";
import { Entity } from "..";
import { EventData, EventTypes } from "../types/EventTypes";

const logger = Logger.createLogger("DataManager");

/**
 * 数据管理器 - 纯数据存储与管理
 * 
 * 现代ECS架构核心组件，专门负责数据存储和访问
 * 特点：
 * 1. 纯数据存储，无业务逻辑
 * 2. 支持Schema验证和约束
 * 3. 提供数据变更监听
 * 4. 支持计算属性缓存
 * 5. 完全独立于Entity和Component系统
 * 
 */
export class DataManager<TData = any> {
    // 数据管理器所属Entity
    private owner: Entity;

    // Schema名称
    protected schemaName: string;

    // Schema定义
    protected schema: Schema<TData>;

    // 数据存储
    protected data: TData;

    // 数据变更监听器
    protected changeListeners: Map<keyof TData, Array<(oldValue: any, newValue: any) => void>> = new Map();

    // 计算属性缓存
    protected computedCache: Map<keyof TData, any> = new Map();

    // 数据是否已初始化
    protected dataInitialized: boolean = false;

    /**
     * 构造函数
     * @param owner 数据管理器所属Entity
     * @param schemaName Schema名称
     * @param initialData 初始数据
     */
    constructor(owner: Entity, schemaName: string, initialData?: Partial<TData>) {
        this.owner = owner;
        this.schemaName = schemaName;
        this.schema = SchemaRegistry.getSchema<TData>(schemaName);
        this.data = this.initializeData(initialData);

        logger.debug(`数据管理器已创建: ${schemaName}`);
    }

    /**
     * 初始化数据
     */
    private initializeData(initialData?: Partial<TData>): TData {
        const data = {} as TData;

        // 设置默认值
        this.schema.properties.forEach(prop => {
            data[prop.key] = prop.defaultValue;
        });

        // 应用初始数据
        if (initialData) {
            Object.assign(data, initialData);
        }

        return data;
    }

    /**
     * 初始化数据管理器
     */
    public initialize(): void {
        // 验证初始数据
        const validationResult = this.validateData();
        if (!validationResult.valid) {
            logger.error(`数据管理器初始化失败 ${this.schemaName}:`, validationResult.errors);
            throw new Error(`数据验证失败: ${validationResult.errors.join(', ')}`);
        }

        this.dataInitialized = true;
        logger.debug(`数据管理器初始化完成: ${this.schemaName}`);
    }

    /**
     * 销毁数据管理器
     */
    public destroy(): void {
        this.changeListeners.clear();
        this.computedCache.clear();
        this.dataInitialized = false;
        logger.debug(`数据管理器已销毁: ${this.schemaName}`);
    }

    // ==================== 数据访问接口 ====================

    /**
     * 获取属性值
     * @param key 属性键名
     * @param defaultValue 默认值
     * @returns 属性值
     */
    public get<K extends keyof TData>(key: K, defaultValue?: TData[K]): TData[K] {
        if (!this.dataInitialized) {
            logger.warn(`尝试在未初始化的数据管理器上获取属性 '${key as string}'`);
            return defaultValue as TData[K];
        }

        // 检查是否为计算属性
        const computedProp = this.schema.computed?.find(c => c.key === key);
        if (computedProp) {
            return this.getComputedProperty(computedProp);
        }

        const value = this.data[key];
        return value !== undefined ? value : defaultValue as TData[K];
    }

    /**
     * 设置属性值
     * @param key 属性键名
     * @param value 属性值
     * @returns 是否设置成功
     */
    public set<K extends keyof TData>(key: K, value: TData[K]): boolean {
        if (!this.dataInitialized) {
            logger.warn(`尝试在未初始化的数据管理器上设置属性 '${key as string}'`);
            return false;
        }

        // 验证和钳制属性
        if (typeof value === 'number') {
            value = this.validateNumber(key, value) as any;
        } else {
            // 非数值类型只需要常规验证
            if (!this.validateProperty(key, value)) {
                return false;
            }
        }

        const oldValue = this.data[key];
        this.data[key] = value;

        // 清除相关计算属性缓存
        this.invalidateComputedCache(key);

        // 触发变更事件
        this.emitPropertyChanged(key, oldValue, value);

        return true;
    }

    /**
     * 数值属性加法运算
     * @param key 属性键名
     * @param delta 增量值
     * @returns 是否操作成功
     */
    public add<K extends keyof TData>(key: K, delta: number): boolean {
        const currentValue = this.get(key);

        if (typeof currentValue !== 'number') {
            logger.warn(`无法对非数值属性 '${key as string}' 进行加法运算`);
            return false;
        }

        return this.set(key, (currentValue as any) + delta);
    }

    /**
     * 检查属性是否存在
     * @param key 属性键名
     * @returns 是否存在
     */
    public has<K extends keyof TData>(key: K): boolean {
        // 检查属性是否在Schema中定义
        if (!this.hasProperty(key)) {
            return false;
        }
    }

    /**
     * 数值属性乘法运算
     * @param key 属性键名
     * @param multiplier 乘数
     * @returns 是否操作成功
     */
    public multiplyValue<K extends keyof TData>(key: K, multiplier: number): boolean {
        const currentValue = this.get(key);

        if (typeof currentValue !== 'number') {
            logger.warn(`无法对非数值属性 '${key as string}' 进行乘法运算`);
            return false;
        }

        return this.set(key, (currentValue as any) * multiplier);
    }

    // ==================== 批量处理 ====================
    /**
     * 批量设置属性
     * @param values 属性值对象
     * @returns 成功设置的属性数量
     */
    public setMultiple(values: Partial<TData>): number {
        if (!this.dataInitialized) {
            logger.warn(`尝试在未初始化的数据管理器上批量设置属性`);
            return 0;
        }

        let successCount = 0;
        for (const key in values) {
            if (this.set(key as keyof TData, values[key] as TData[keyof TData])) {
                successCount++;
            }
        }

        return successCount;
    }

    /**
     * 批量获取多个属性
     * @param keys 属性键名数组
     * @returns 属性值对象
     */
    public getMultiple(keys: (keyof TData)[]): Partial<TData> {
        const result: Partial<TData> = {};

        for (const key of keys) {
            if (this.hasProperty(key)) {
                result[key] = this.get(key);
            }
        }

        return result;
    }

    /**
     * 获取所有数据
     * @returns 完整数据对象
     */
    public getData(): TData {
        return { ...this.data };
    }

    /**
     * 设置完整数据
     * @param data 新数据对象
     */
    public setData(data: Partial<TData>): void {
        this.setMultiple(data);
    }

    /**
     * 重置为默认值
     */
    public reset(): void {
        if (!this.dataInitialized) {
            return;
        }

        const oldData = { ...this.data };
        this.data = this.initializeData();

        // 清除所有计算属性缓存
        this.computedCache.clear();

        // 触发重置事件
        this.owner.emit(EventTypes.DATA_RESET, {
            schemaName: this.schemaName,
            oldData,
            newData: this.data,
            source: this
        });
    }

    /**
     * 克隆数据
     * @returns 数据副本
     */
    public clone(): TData {
        return JSON.parse(JSON.stringify(this.data));
    }

    // ==================== 计算属性 ====================
    /**
     * 获取计算属性
     */
    private getComputedProperty<K extends keyof TData>(computedProp: ComputedPropertyDefinition<TData>): TData[K] {
        const key = computedProp.key;

        // 检查缓存
        if (computedProp.cache && this.computedCache.has(key)) {
            return this.computedCache.get(key);
        }

        // 计算值
        let value = computedProp.compute(this.data);
        // 验证属性
        if (typeof value === 'number') {
            value = this.validateNumber(key, value) as any;
        }

        // 缓存结果
        if (computedProp.cache) {
            this.computedCache.set(key, value);
        }

        return value as TData[K];
    }

    /**
     * 清除计算属性缓存
     */
    private invalidateComputedCache<K extends keyof TData>(changedKey: K): void {
        if (!this.schema.computed) return;

        // 找到依赖于此属性的计算属性
        this.schema.computed.forEach(computedProp => {
            if (computedProp.dependencies.includes(changedKey)) {
                this.computedCache.delete(computedProp.key);
            }
        });
    }

    // ==================== 数据验证 ====================
    /**
     * 验证数值属性
     */
    private validateNumber<K extends keyof TData>(key: K, value: number): number {
        if (typeof value === 'number') {
            const prop = this.getPropertyDefinition(key);
            if (prop?.constraints) {
                const min = prop.constraints?.min;
                const max = prop.constraints?.max;

                // 钳制到最小值
                if (min && value < min) {
                    return min;
                }
                // 钳制到最大值
                if (max && value > max) {
                    return max;
                }
            }
        }
        // 如果没有约束或值在范围内，返回原值
        return value;
    }

    /**
     * 验证属性值
     */
    private validateProperty<K extends keyof TData>(key: K, value: TData[K]): boolean {
        const prop = this.schema.properties.find(p => p.key === key);
        if (!prop) return true;

        const constraints = prop.constraints;
        if (!constraints) return true;

        // 类型检查
        if (!SchemaUtils.validatePropertyType(value, prop.type)) {
            logger.warn(`属性 ${key as string} 类型错误，期望 ${prop.type}，实际 ${typeof value}`);
            return false;
        }

        return true;
    }

    /**
     * 验证数据
     */
    public validateData(): SchemaValidationResult {
        const errors: string[] = [];
        const warnings: string[] = [];

        try {
            // 验证每个属性
            for (const prop of this.schema.properties) {
                const value = this.data[prop.key];

                // 检查必需属性
                if (prop.constraints?.required && (value === undefined || value === null)) {
                    errors.push(`必需属性 ${prop.key as string} 缺失`);
                    continue;
                }

                // 跳过未定义的可选属性
                if (value === undefined || value === null) {
                    continue;
                }

                // 验证属性类型
                if (!SchemaUtils.validatePropertyType(value, prop.type)) {
                    errors.push(`属性 ${prop.key as string} 类型错误，期望 ${prop.type}，实际 ${typeof value}`);
                    continue;
                }

                // 验证属性约束
                if (prop.constraints) {
                    const constraintErrors = SchemaUtils.validatePropertyConstraints(value, prop.constraints);
                    errors.push(...constraintErrors.map(err => `属性 ${prop.key as string}: ${err}`));
                }
            }

        } catch (error) {
            errors.push(`验证过程中发生错误: ${error}`);
        }

        return {
            valid: errors.length === 0,
            errors,
            warnings
        };
    }

    // ==================== 属性检查 ====================
    /**
     * 检查属性是否存在
     */
    public hasProperty<K extends keyof TData>(key: K): boolean {
        return this.schema.properties.some(p => p.key === key) ||
            this.schema?.computed.some(c => c.key === key)
    }

    /**
     * 获取Schema名称
     */
    public getSchemaName(): string {
        return this.schemaName;
    }

    /**
     * 获取Schema定义
     */
    public getSchema(): Schema<TData> {
        return this.schema;
    }

    /**
     * 通过key获取属性定义
     * @param key 属性键名
     * @returns 属性定义
     */
    getPropertyDefinition(key: keyof TData): PropertyDefinition<TData> | ComputedPropertyDefinition<TData> {
        return this.schema.properties.find(p => p.key === key) || this.schema.computed?.find(c => c.key === key);
    }

    // ==================== 事件处理 ====================
    /**
     * 触发属性变更事件
     */
    private emitPropertyChanged<K extends keyof TData>(
        key: K,
        oldValue: TData[K],
        newValue: TData[K]
    ): void {
        // 触发Entity事件
        this.owner.emit(EventTypes.DATA_PROPERTY_CHANGED, {
            source: this.owner,
            key,
            oldValue,
            newValue,
        } as EventTypes.PropertyChangedEventData);

        const listeners = this.changeListeners.get(key);
        if (listeners) {
            listeners.forEach(listener => listener(oldValue, newValue));
        }
    }

    /**
     * 添加属性变更监听器
     */
    public onPropertyChanged<K extends keyof TData>(
        key: K,
        listener: (oldValue: TData[K], newValue: TData[K]) => void
    ): void {
        if (!this.changeListeners.has(key)) {
            this.changeListeners.set(key, []);
        }
        this.changeListeners.get(key)!.push(listener);
    }

    /**
     * 移除属性变更监听器
     */
    public offPropertyChanged<K extends keyof TData>(
        key: K,
        listener: (oldValue: TData[K], newValue: TData[K]) => void
    ): void {
        const listeners = this.changeListeners.get(key);
        if (listeners) {
            const index = listeners.indexOf(listener);
            if (index > -1) {
                listeners.splice(index, 1);
            }
        }
    }
}