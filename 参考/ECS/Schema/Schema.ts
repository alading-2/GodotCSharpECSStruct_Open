/**
 * Schema 定义系统
 * 用于定义游戏对象的数据结构、验证规则和计算属性
 * 
 * 主要功能:
 * 1. 定义游戏对象的属性结构和约束
 * 2. 提供属性验证和计算功能
 * 3. 支持Schema继承和合并
 * 4. 提供事件系统用于监听属性变更
 */

/**
 * 属性约束定义
 * 用于限制属性值的范围和规则
 */
export interface PropertyConstraints {
    /** 最小值（数值类型） */
    min?: number;
    /** 最大值（数值类型） */
    max?: number;
    /** 是否必需 - 如果为true则该属性不能为空 */
    required?: boolean;
    /** 是否只读 - 如果为true则该属性不能被修改 */
    readonly?: boolean;
    /** 枚举值 - 属性值必须是数组中的一个 */
    enum?: any[];
    /** 字符串长度限制 - 字符串类型的最大长度 */
    stringLength?: number;
    /** 自定义验证函数 - 返回true表示验证通过 */
    validator?: (value: any) => boolean;
}

/**
 * 嵌套Schema属性定义
 * 用于定义复杂对象或数组中的元素结构
 */
export interface NestedSchemaDefinition {
    /** 嵌套Schema名称 - 用于引用已定义的Schema */
    schemaName?: string;
    /** 嵌套属性定义 - 用于内联定义复杂结构 */
    properties?: PropertyDefinition[];
    /** 是否数组 - 是否为数组类型 */
    isArray?: boolean;
    /** 元素类型 - 数组元素的类型 */
    elementType?: 'number' | 'string' | 'boolean' | 'object' | 'array' | 'any';
}

/**
 * 属性定义
 * 描述游戏对象的单个属性特征
 */
export interface PropertyDefinition<T = any> {
    /** 属性键名 - 必须是T类型中的一个键 */
    key: keyof T;
    /** 属性类型 - 支持基础类型和复合类型 */
    type: 'number' | 'string' | 'boolean' | 'object' | 'array' | 'any';
    /** 默认值 - 当属性未设置时使用的值 */
    defaultValue: T[keyof T];
    /** 嵌套Schema定义 - 用于复杂对象和数组 */
    nestedSchema?: NestedSchemaDefinition;
    /** 是否百分比属性，用于UI显示时多一个%符号 */
    isPercent?: boolean;
    /** 属性分类 - 用于属性分组管理 */
    category?: string;
    /** 属性描述 - 对该属性的详细说明 */
    description?: string;
    /** 属性约束 - 定义属性的验证规则 */
    constraints?: PropertyConstraints;
    /** 是否为系统属性 - 系统属性不可被用户修改 */
    system?: boolean;
}

/**
 * 计算属性定义
 * 基于其他属性计算得出的只读属性
 */
export interface ComputedPropertyDefinition<T = any> {
    /** 计算属性键名 - 必须是T类型中的一个键 */
    key: keyof T;
    /** 属性类型 - 支持基础类型和复合类型 */
    type: 'number' | 'string' | 'boolean' | 'object' | 'array' | 'any';
    /** 依赖的属性列表 - 计算属性依赖的其他属性 */
    dependencies: (keyof T)[];
    /** 计算函数 - 根据data计算属性值 */
    compute: (data: T) => T[keyof T];
    /** 是否百分比属性，用于UI显示时多一个%符号 */
    isPercent?: boolean;
    /** 是否缓存计算结果 - 启用缓存可提高性能 */
    cache?: boolean;
    /** 缓存过期时间（毫秒） - 超过此时间缓存失效 */
    cacheTimeout?: number;
    /** 计算属性描述 - 对该计算属性的详细说明 */
    description?: string;
    /** 属性约束 - 定义属性的验证规则 */
    constraints?: PropertyConstraints;
}

/**
 * 验证器定义
 * 用于验证整个数据对象是否符合规则
 */
export interface ValidatorDefinition<T = any> {
    /** 验证器名称 - 用于标识验证器 */
    name: string;
    /** 验证函数 - 返回true或错误消息 */
    validate: (data: T) => boolean | string;
    /** 验证失败时的错误消息 */
    errorMessage?: string;
    /** 验证优先级 - 数字越大优先级越高 */
    priority?: number;
}

/**
 * 游戏对象Schema定义
 * 完整描述一个游戏对象的数据结构
 */
export interface Schema<T = any> {
    /** Schema名称 - Schema的唯一标识 */
    schemaName: string;
    /** 属性定义列表 - 对象的所有属性 */
    properties: PropertyDefinition<T>[];
    /** 计算属性定义列表 - 对象的计算属性 */
    computed?: ComputedPropertyDefinition<T>[];
    /** Schema版本 - 用于版本控制 */
    version?: string;
    /** Schema描述 - 对该Schema的详细说明 */
    description?: string;
    /** 继承的Schema名称 - 支持Schema继承 */
    extends?: string;
}

/**
 * Schema验证结果
 * 包含验证的详细信息
 */
export interface SchemaValidationResult {
    /** 是否验证通过 - true表示全部验证通过 */
    valid: boolean;
    /** 错误信息列表 - 验证失败的错误信息 */
    errors: string[];
    /** 警告信息列表 - 不影响验证结果的警告 */
    warnings: string[];
}

/**
 * 属性变更事件数据
 * 记录属性变更的详细信息
 */
export interface PropertyChangeEvent<T = any> {
    /** 对象ID - 发生变更的对象标识 */
    objectId: string;
    /** 属性键名 - 变更的属性名 */
    key: keyof T;
    /** 旧值 - 变更前的值 */
    oldValue: T[keyof T];
    /** 新值 - 变更后的值 */
    newValue: T[keyof T];
    /** 变更时间戳 - 变更发生的时间 */
    timestamp: number;
}

/**
 * 对象重置事件数据
 * 记录对象重置的详细信息
 */
export interface ObjectResetEvent<T = any> {
    /** 对象ID - 被重置的对象标识 */
    objectId: string;
    /** 旧数据 - 重置前的完整数据 */
    oldData: T;
    /** 新数据 - 重置后的完整数据 */
    newData: T;
    /** 重置时间戳 - 重置发生的时间 */
    timestamp: number;
}

/**
 * Schema工具类
 * 提供Schema相关的工具方法
 */
export class SchemaUtils {
    /**
     * 验证属性值类型
     * 检查值是否符合期望的类型，支持嵌套Schema验证，包括数组元素的验证
     * 
     * @param value 属性值 - 要验证的值
     * @param expectedType 期望类型 - 期望的类型字符串
     * @param nestedSchema 嵌套Schema定义 - 用于复杂对象验证
     * @returns 是否验证通过
     */
    static validatePropertyType(value: any, expectedType: string, nestedSchema?: NestedSchemaDefinition): boolean {
        switch (expectedType) {
            case 'number':
                return typeof value === 'number' && !isNaN(value);
            case 'string':
                return typeof value === 'string';
            case 'boolean':
                return typeof value === 'boolean';
            case 'object':
                return typeof value === 'object' && value !== null;
            case 'array':
                if (!Array.isArray(value)) return false;

                // 如果有嵌套Schema定义，验证数组元素
                if (nestedSchema && nestedSchema.properties) {
                    return value.every(item =>
                        nestedSchema.properties!.every(prop =>
                            this.validatePropertyType(item[prop.key], prop.type, prop.nestedSchema)
                        )
                    );
                }

                // 如果有元素类型定义，验证数组元素类型
                if (nestedSchema && nestedSchema.elementType) {
                    return value.every(item =>
                        this.validatePropertyType(item, nestedSchema.elementType!)
                    );
                }

                return true;
            case 'any':
                return true;
            default:
                return false;
        }
    }

    /**
     * 验证属性约束
     * 检查值是否满足所有约束条件
     * 
     * @param value 属性值 - 要验证的值
     * @param constraints 属性约束 - 约束条件集合
     * @returns 错误信息列表 - 空数组表示验证通过
     */
    static validatePropertyConstraints(value: any, constraints: PropertyConstraints): string[] {
        const errors: string[] = [];

        if (constraints.required && (value === undefined || value === null)) {
            errors.push('属性值不能为空');
        }

        // if (typeof value === 'number') {
        //     if (constraints.min !== undefined && value < constraints.min) {
        //         errors.push(`值 ${value} 小于最小值 ${constraints.min}`);
        //     }
        //     if (constraints.max !== undefined && value > constraints.max) {
        //         errors.push(`值 ${value} 大于最大值 ${constraints.max}`);
        //     }
        // }

        if (typeof value === 'string' && constraints.stringLength !== undefined) {
            if (value.length > constraints.stringLength) {
                errors.push(`字符串长度 ${value.length} 超过最大长度 ${constraints.stringLength}`);
            }
        }

        if (constraints.enum && !constraints.enum.includes(value)) {
            errors.push(`值 ${value} 不在允许的枚举值 [${constraints.enum.join(', ')}] 中`);
        }

        if (constraints.validator && !constraints.validator(value)) {
            errors.push('自定义验证失败');
        }

        return errors;
    }

    /**
     * 深度克隆对象
     * 创建对象的完整副本
     * 
     * @param obj 要克隆的对象
     * @returns 克隆后的新对象
     */
    static deepClone<T>(obj: T): T {
        if (obj === null || typeof obj !== 'object') {
            return obj;
        }

        if (obj instanceof Date) {
            return new Date(obj.getTime()) as any;
        }

        if (Array.isArray(obj)) {
            return obj.map(item => this.deepClone(item)) as any;
        }

        const cloned = {} as T;
        for (const key in obj) {
            if (obj.hasOwnProperty(key)) {
                cloned[key] = this.deepClone(obj[key]);
            }
        }

        return cloned;
    }

    /**
     * 合并Schema（支持继承）
     * 将两个Schema合并为一个新的Schema
     * 
     * @param baseSchema 基础Schema
     * @param extendSchema 扩展Schema
     * @returns 合并后的新Schema
     */
    static mergeSchemas<T>(baseSchema: Schema<T>, extendSchema: Schema<T>): Schema<T> {
        return {
            ...baseSchema,
            ...extendSchema,
            properties: [...baseSchema.properties, ...extendSchema.properties],
            computed: [...(baseSchema.computed || []), ...(extendSchema.computed || [])],
        };
    }
}
