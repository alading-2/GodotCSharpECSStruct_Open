/** @noSelfInFile **/

import { SchemaRegistry } from "../..";
import { SCHEMA_TYPES } from "../../SchemaTypes";
import { Schema } from "../../Schema";

/**
 * 护盾类型枚举
 */
export enum ShieldType {
  /** 物理护盾 */
  PHYSICAL = "physical",
  /** 魔法护盾 */
  MAGIC = "magic",
  /** 通用护盾 */
  UNIVERSAL = "universal"
}

/**
 * 护盾实例数据
 */
export interface Inte_ShieldInstance {
  /** 护盾名 */
  name: string;
  /** 护盾值 */
  value: number;
  /** 护盾类型 */
  type: ShieldType;
  /** 优先级 */
  priority: number;
  /** 剩余时间（-1表示永久） */
  remainingTime: number;
  /** 创建时间 */
  createTime: number;
  /** 是否激活 */
  isActive: boolean;
}

/**
 * 护盾数据Schema接口
 */
export interface Inte_ShieldSchema {
  /** 护盾列表 */
  shields: Inte_ShieldInstance[];
  /** 护盾总值 */
  totalValue: number;
  /** 最大护盾容量 */
  maxCapacity: number;
  /** 最后更新时间 */
  lastUpdateTime: number;
}

/**
 * 护盾实例Schema定义
 * 为 ShieldInstance 提供类型限制、默认值和验证功能
 */
export const SHIELD_INSTANCE_SCHEMA: Schema<Inte_ShieldInstance> = {
  schemaName: "ShieldInstance",
  description: "护盾实例数据Schema - 提供完整的类型约束和验证",
  version: "1.0.0",

  properties: [
    {
      key: "name",
      type: "string",
      defaultValue: "",
      description: "护盾唯一标识符",
      constraints: {
        required: true,
        readonly: true,
      }
    },
    {
      key: "value",
      type: "number",
      defaultValue: 0,
      description: "护盾当前数值",
      constraints: {
        min: 0,
      }
    },
    {
      key: "type",
      type: "string",
      defaultValue: ShieldType.UNIVERSAL,
      description: "护盾类型",
      constraints: {
        required: true,
        enum: [ShieldType.PHYSICAL, ShieldType.MAGIC, ShieldType.UNIVERSAL]
      }
    },
    {
      key: "priority",
      type: "number",
      defaultValue: 0,
      description: "护盾优先级（数值越高优先级越高）",
      constraints: {
        min: 0,
        max: 100
      }
    },
    {
      key: "remainingTime",
      type: "number",
      defaultValue: -1,
      description: "剩余持续时间（-1表示永久）",
      constraints: {
        min: -1
      }
    },
    {
      key: "createTime",
      type: "number",
      defaultValue: 0,
      description: "护盾创建时间戳",
      constraints: {
        readonly: true,
      }
    },
    {
      key: "isActive",
      type: "boolean",
      defaultValue: true,
      description: "护盾是否处于激活状态"
    }
  ],

};

/**
 * 护盾Schema定义
 * 符合现代ECS架构的数据结构定义
 */
export const SHIELD_SCHEMA: Schema<Inte_ShieldSchema> = {
  schemaName: "Inte_Shield",
  description: "护盾系统数据Schema - 现代ECS架构",
  version: "1.0.0",

  properties: [
    {
      key: "shields",
      type: "array",
      defaultValue: [],
      description: "护盾实例列表",
      nestedSchema: {
        properties: SHIELD_INSTANCE_SCHEMA.properties,
        schemaName: "ShieldInstance",
        isArray: true,
        elementType: "object"
      },
    },
    {
      key: "totalValue",
      type: "number",
      defaultValue: 0,
      description: "护盾总值",
      constraints: {
        min: 0,
        readonly: true
      }
    },
    {
      key: "maxCapacity",
      type: "number",
      defaultValue: 10,
      description: "最大护盾容量",
      constraints: {
        min: 1,
      }
    },
    {
      key: "lastUpdateTime",
      type: "number",
      defaultValue: 0,
      description: "最后更新时间戳",
      constraints: {
        readonly: true
      }
    }
  ],

  computed: [
    {
      key: "totalValue",
      type: "number",
      dependencies: ["shields"],
      compute: (data: Inte_ShieldSchema) => {
        return data.shields
          .filter(shield => shield.isActive)
          .reduce((sum, shield) => sum + shield.value, 0);
      },
      description: "基于激活护盾计算的总值",
      cache: true,
      cacheTimeout: 100
    }
  ],

};

/**
 * 护盾事件数据接口
 */
export interface ShieldEventData {
  /** 护盾实例 */
  shield: Inte_ShieldInstance;
  /** 伤害值（可选） */
  damage?: number;
  /** 总伤害（可选） */
  totalDamage?: number;
  /** 吸收的伤害（可选） */
  absorbed?: number;
  /** 剩余伤害（可选） */
  remaining?: number;
  /** 清除的护盾列表（可选） */
  clearedShields?: Inte_ShieldInstance[];
}

/**
 * 护盾事件类型
 */
export enum ShieldEventType {
  /** 护盾添加 */
  SHIELD_ADDED = "shield:added",
  /** 护盾移除 */
  SHIELD_REMOVED = "shield:removed",
  /** 护盾破碎 */
  SHIELD_BROKEN = "shield:broken",
  /** 护盾受损 */
  SHIELD_DAMAGED = "shield:damaged",
  /** 护盾过期 */
  SHIELD_EXPIRED = "shield:expired",
  /** 护盾清空 */
  SHIELD_CLEARED = "shield:cleared"
}

/**
 * 护盾统计信息接口
 */
export interface ShieldStats {
  /** 总护盾数量 */
  total: number;
  /** 激活护盾数量 */
  active: number;
  /** 护盾总值 */
  totalValue: number;
  /** 按类型分组的护盾值 */
  byType: Record<string, number>;
}

/**
 * 注册Schema
 */
export class ShieldSchema {
  //游戏初始化时运行
  private static onInit() {
    SchemaRegistry.register(SCHEMA_TYPES.SHIELD_INSTANCE, SHIELD_INSTANCE_SCHEMA);
    SchemaRegistry.register(SCHEMA_TYPES.SHIELD_DATA, SHIELD_SCHEMA);
  }
}
