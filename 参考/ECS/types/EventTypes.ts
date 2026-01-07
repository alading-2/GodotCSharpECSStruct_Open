/** @noSelfInFile **/

import { Entity } from "..";
import { Position } from "../../base/math/Position";
import { ItemComponent, UnitComponent } from "../Component";

/**
 * 事件处理器类型
 */
export interface EventHandler<T = any> {
    (data: T): void;
}

/**
 * 事件优先级
 */
export enum EventPriority {
    HIGHEST = 100,
    HIGH = 75,
    NORMAL = 50,
    LOW = 25,
    LOWEST = 0
}

/**
 * 事件订阅对象
 */
export interface EventSubscription {
    eventType: string;
    handler: EventHandler;
    priority: EventPriority;
    once: boolean;
}

/**
 * 基础事件数据接口
 */
export interface BaseEventData {
    timestamp?: number;
    source?: any;
    [key: string]: any;
}

/**
 * 统一事件系统
 * 事件常量和数据接口就近定义，简洁直观
 */
export namespace EventTypes {

    // ==================== Entity 事件 ====================
    /** 实体创建事件 */
    export const ENTITY_CREATED = 'entity:created' as const;
    /** 实体初始化事件 */
    export const ENTITY_INITIALIZED = 'entity:initialized' as const;
    /** 实体销毁事件 */
    export const ENTITY_DESTROYED = 'entity:destroyed' as const;
    /** 实体状态变更事件 */
    export const ENTITY_STATE_CHANGED = 'entity:state_changed' as const;
    /** 实体错误事件 */
    export const ENTITY_ERROR = 'entity:error' as const;

    /** 实体事件数据接口 */
    export interface EntityEventData extends BaseEventData {
        /** 触发事件的实体 */
        entity: any;
        /** 实体类型 */
        entityType?: string;
        /** 相关配置数据 */
        config?: any;
    }

    // ==================== Component 事件 ====================
    /** 组件添加事件 */
    export const COMPONENT_ADDED = 'component:added' as const;
    /** 组件移除事件 */
    export const COMPONENT_REMOVED = 'component:removed' as const;
    /** 组件启用事件 */
    export const COMPONENT_ENABLED = 'component:enabled' as const;
    /** 组件禁用事件 */
    export const COMPONENT_DISABLED = 'component:disabled' as const;
    /** 组件初始化事件 */
    export const COMPONENT_INITIALIZED = 'component:initialized' as const;
    /** 组件销毁事件 */
    export const COMPONENT_DESTROYED = 'component:destroyed' as const;
    /** 组件错误事件 */
    export const COMPONENT_ERROR = 'component:error' as const;

    /** 组件事件数据接口 */
    export interface ComponentEventData extends BaseEventData {
        /** 组件所属的实体 */
        entity: any;
        /** 触发事件的组件 */
        component: any;
        /** 组件类型 */
        componentType: string;
    }

    // ==================== Data 事件 ====================
    /** 数据属性变更事件 */
    export const DATA_PROPERTY_CHANGED = 'data:property_changed' as const;
    /** 数据属性增加事件 */
    export const DATA_PROPERTY_ADDED = 'data:property_added' as const;
    /** 数据属性乘以事件 */
    export const DATA_PROPERTY_MULTIPLIED = 'data:property_multiplied' as const;
    /** 数据重置事件 */
    export const DATA_RESET = 'data:reset' as const;

    /** 属性变更事件数据接口 */
    export interface PropertyChangedEventData extends BaseEventData {
        /** 变更的属性键 */
        key: string;
        /** 旧值 */
        oldValue: any;
        /** 新值 */
        newValue: any;
        /** 目标对象 */
        target?: any;
    }

    // ==================== System 事件 ====================
    /** 系统初始化事件 */
    export const SYSTEM_INITIALIZED = 'system:initialized' as const;
    /** 系统激活事件 */
    export const SYSTEM_ACTIVATED = 'system:activated' as const;
    /** 系统停用事件 */
    export const SYSTEM_DEACTIVATED = 'system:deactivated' as const;
    /** 系统更新事件 */
    export const SYSTEM_UPDATED = 'system:updated' as const;
    /** 系统销毁事件 */
    export const SYSTEM_DESTROYED = 'system:destroyed' as const;
    /** 系统错误事件 */
    export const SYSTEM_ERROR = 'system:error' as const;

    /** 系统事件数据接口 */
    export interface SystemEventData extends BaseEventData {
        /** 系统名称 */
        systemName: string;
        /** 系统类型 */
        systemType?: string;
        /** 上下文数据 */
        context?: any;
    }

    // ==================== Unit 事件 ====================

    /** 单位事件基础数据接口 */
    export interface UnitEventData extends BaseEventData {
        /** 触发事件的单位 */
        unit: UnitComponent;
        /** 目标单位 */
        target?: UnitComponent;
    }

    /** 单位创建事件 */
    export const UNIT_CREATED = 'unit:created' as const;
    /** 单位销毁事件 */
    export const UNIT_DESTROYED = 'unit:destroyed' as const;
    /** 单位死亡事件 */
    export const UNIT_DEATH = 'unit:died' as const;
    /** 单位死亡事件数据接口 */
    export interface UnitDeathEventData extends UnitEventData {
        /** 死亡的单位 */
        unit: UnitComponent;
        /** 击杀者 */
        killer: UnitComponent | null;
        /** 伤害类型 */
        damageType?: string;
        /** 最终伤害值 */
        finalDamage?: number;
    }
    /** 单位复活事件 */
    export const UNIT_REVIVED = 'unit:revived' as const;
    /** 单位承受伤害事件 */
    export const UNIT_TAKE_DAMAGE = 'unit:take_damage' as const;
    /** 单位承受伤害事件数据接口 */
    export interface UnitDamageEventData extends UnitEventData {
        /** 攻击者 */
        attacker: UnitComponent | null;
        /** 伤害值 */
        damage: number;
        /** 伤害类型 */
        damageType: string;
        /** 是否被格挡 */
        isBlocked?: boolean;
        /** 是否暴击 */
        isCritical?: boolean;
    }
    /** 单位攻击事件 */
    export const UNIT_ATTACK = 'unit:attack' as const;
    /** 单位攻击事件数据接口 */
    export interface UnitAttackEventData extends UnitEventData {
        /** 攻击者 */
        attacker: UnitComponent;
        /** 目标单位 */
        target: UnitComponent;
        /** 伤害值 */
        damage: number;
        /** 攻击类型 */
        attackType?: string;
    }
    /** 单位等级变更事件 */
    export const UNIT_LEVEL_CHANGED = 'unit:level_changed' as const;
    /** 单位等级变更事件数据接口 */
    export interface UnitLevelEventData extends UnitEventData {
        /** 旧等级 */
        oldLevel: number;
        /** 新等级 */
        newLevel: number;
        /** 经验值 */
        experience?: number;
    }
    /** 单位升级事件 */
    export const UNIT_LEVEL_UP = 'unit:level_up' as const;
    /** 单位升级事件数据接口 */
    export interface UnitLevelUpEventData extends UnitEventData {
        /** 旧等级 */
        oldLevel: number;
        /** 新等级 */
        newLevel: number;
    }
    /** 单位获得经验事件 */
    export const UNIT_EXP_GAINED = 'unit:exp_gained' as const;
    /** 单位获得经验事件数据接口 */
    export interface UnitExpGainedEventData extends UnitEventData {
        /** 获得的经验值 */
        expAmount: number;
        /** 旧经验值 */
        oldExp: number;
        /** 新经验值 */
        newExp: number;
        /** 旧等级 */
        oldLevel: number;
        /** 新等级 */
        newLevel: number;
        /** 经验来源 */
        source?: string;
    }
    /** 单位生命周期结束事件 */
    export const UNIT_LIFE_TIME_EXPIRED = 'unit:life_time_expired' as const;
    /** 单位状态变更事件 */
    export const UNIT_STATE_CHANGED = 'unit:state_changed' as const;
    /** 单位生命值变更事件 */
    export const UNIT_HEALTH_CHANGED = 'unit:health_changed' as const;
    /** 单位生命值变更事件数据接口 */
    export interface UnitHealthEventData extends UnitEventData {
        /** 生命值变化量 */
        healthChange: number;
        /** 当前生命值 */
        currentHealth: number;
        /** 最大生命值 */
        maxHealth: number;
    }
    /** 单位魔法值变更事件 */
    export const UNIT_MANA_CHANGED = 'unit:mana_changed' as const;
    /** 单位魔法值变更事件数据接口 */
    export interface UnitManaEventData extends UnitEventData {
        /** 魔法值变化量 */
        manaChange: number;
        /** 当前魔法值 */
        currentMana: number;
        /** 最大魔法值 */
        maxMana: number;
    }


    // ==================== Player 事件 ====================
    /** 玩家事件基础数据接口 */
    export interface PlayerEventData extends BaseEventData {
        /** 触发事件的玩家 */
        player: any;
        /** 玩家ID */
        playerId?: number;
    }

    /** 玩家金币变更事件 */
    export const PLAYER_GOLD_CHANGED = 'player:gold_changed' as const;
    /** 玩家木材变更事件 */
    export const PLAYER_LUMBER_CHANGED = 'player:lumber_changed' as const;
    /** 玩家已用人口变更事件 */
    export const PLAYER_FOOD_USED_CHANGED = 'player:food_used_changed' as const;
    /** 玩家人口上限变更事件 */
    export const PLAYER_FOOD_CAP_CHANGED = 'player:food_cap_changed' as const;
    /** 玩家在线状态变更事件 */
    export const PLAYER_ONLINE_STATUS_CHANGED = 'player:online_status_changed' as const;
    /** 玩家失败事件 */
    export const PLAYER_DEFEATED = 'player:defeated' as const;
    /** 玩家胜利事件 */
    export const PLAYER_VICTORY = 'player:victory' as const;
    /** 玩家等级变更事件 */
    export const PLAYER_LEVEL_CHANGED = 'player:level_changed' as const;
    /** 玩家等级变更事件数据接口 */
    export interface PlayerLevelEventData extends PlayerEventData {
        /** 旧等级 */
        oldLevel: number;
        /** 新等级 */
        newLevel: number;
        /** 经验值 */
        experience: number;
    }
    /** 玩家经验变更事件 */
    export const PLAYER_EXPERIENCE_CHANGED = 'player:experience_changed' as const;
    /** 玩家资源变更事件 */
    export const PLAYER_RESOURCE_CHANGED = 'player:resource_changed' as const;
    /** 玩家资源变更事件数据接口 */
    export interface PlayerResourceEventData extends PlayerEventData {
        /** 资源类型 */
        resourceType: string;
        /** 旧值 */
        oldValue: number;
        /** 新值 */
        newValue: number;
        /** 变化量 */
        change: number;
    }
    /** 玩家状态变更事件 */
    export const PLAYER_STATUS_CHANGED = 'player:status_changed' as const;
    /** 玩家状态变更事件数据接口 */
    export interface PlayerStatusEventData extends PlayerEventData {
        /** 状态类型 */
        statusType: string;
        /** 旧状态 */
        oldStatus: any;
        /** 新状态 */
        newStatus: any;
    }


    // ==================== Item 事件 ====================
    /** 物品事件基础数据接口 */
    export interface ItemEventData extends BaseEventData {
        /** 触发事件的物品 */
        item: ItemComponent;
    }
    /** 物品获得事件 */
    export const ITEM_ACQUIRED = 'item:acquired' as const;
    /** 物品失去事件 */
    export const ITEM_LOST = 'item:lost' as const;
    /** 物品使用事件 */
    export const ITEM_USED = 'item:used' as const;
    /** 物品装备事件 */
    export const ITEM_EQUIPPED = 'item:equipped' as const;
    /** 物品卸下事件 */
    export const ITEM_UNEQUIPPED = 'item:unequipped' as const;
    /** 物品堆叠变更事件 */
    export const ITEM_STACK_CHANGED = 'item:stack_changed' as const;
    /** 物品堆叠变更事件数据接口 */
    export interface ItemStackEventData extends ItemEventData {
        /** 旧堆叠数 */
        oldStack: number;
        /** 新堆叠数 */
        newStack: number;
        /** 堆叠变化量 */
        stackChange: number;
    }





    // ==================== Buff 事件 ====================
    /** Buff应用事件 */
    export const BUFF_APPLIED = 'buff:applied' as const;
    /** Buff移除事件 */
    export const BUFF_REMOVED = 'buff:removed' as const;
    /** Buff过期事件 */
    export const BUFF_EXPIRED = 'buff:expired' as const;
    /** Buff刷新事件 */
    export const BUFF_REFRESHED = 'buff:refreshed' as const;
    /** Buff堆叠增加事件 */
    export const BUFF_STACK_ADDED = 'buff:stack_added' as const;
    /** Buff堆叠移除事件 */
    export const BUFF_STACK_REMOVED = 'buff:stack_removed' as const;
    /** Buff暂停事件 */
    export const BUFF_PAUSED = 'buff:paused' as const;
    /** Buff恢复事件 */
    export const BUFF_RESUMED = 'buff:resumed' as const;

    /** Buff事件基础数据接口 */
    export interface BuffEventData extends BaseEventData {
        /** 触发事件的Buff */
        buff: any;
        /** Buff ID */
        buffId: string;
        /** Buff目标 */
        target: any;
        /** Buff施加者 */
        caster?: any;
        /** 持续时间 */
        duration?: number;
        /** 等级 */
        level?: number;
    }

    /** Buff堆叠变更事件数据接口 */
    export interface BuffStackEventData extends BuffEventData {
        /** 旧堆叠数 */
        oldStacks: number;
        /** 新堆叠数 */
        newStacks: number;
        /** 堆叠变化量 */
        stackChange: number;
    }

    // ==================== Ability 事件 ====================
    /** 技能施法事件 */
    export const ABILITY_CAST = 'ability:cast' as const;
    /** 技能施法事件数据接口 */
    export interface AbilityCastEventData extends AbilityEventData {
        /** 目标 */
        target?: any;
        /** 目标位置 */
        targetPosition?: { x: number; y: number };
        /** 施法时间 */
        castTime?: number;
        /** 法力消耗 */
        manaCost?: number;
    }
    // /** 技能施法开始事件 */
    // export const ABILITY_CAST_STARTED = 'ability:cast_started' as const;
    // /** 技能施法完成事件 */
    // export const ABILITY_CAST_COMPLETED = 'ability:cast_completed' as const;
    // /** 技能施法中断事件 */
    // export const ABILITY_CAST_INTERRUPTED = 'ability:cast_interrupted' as const;
    /** 技能升级事件 */
    export const ABILITY_LEVELED_UP = 'ability:leveled_up' as const;
    /** 技能升级事件数据接口 */
    export interface AbilityLevelEventData extends AbilityEventData {
        /** 旧等级 */
        oldLevel: number;
        /** 新等级 */
        newLevel: number;
    }
    /** 技能启用事件 */
    export const ABILITY_ENABLED = 'ability:enabled' as const;
    /** 技能禁用事件 */
    export const ABILITY_DISABLED = 'ability:disabled' as const;

    /** 技能事件基础数据接口 */
    export interface AbilityEventData extends BaseEventData {
        /** 触发事件的技能 */
        ability: any;
        /** 技能ID */
        abilityId: string;
        /** 施法者 */
        caster: any;
        /** 等级 */
        level?: number;
    }



    // ==================== Cooldown 事件 ====================
    /** 冷却开始事件 */
    export const COOLDOWN_STARTED = 'cooldown:started' as const;
    /** 冷却更新事件 */
    export const COOLDOWN_UPDATED = 'cooldown:updated' as const;
    /** 冷却完成事件 */
    export const COOLDOWN_COMPLETED = 'cooldown:completed' as const;
    /** 冷却重置事件 */
    export const COOLDOWN_RESET = 'cooldown:reset' as const;
    /** 冷却暂停事件 */
    export const COOLDOWN_PAUSED = 'cooldown:paused' as const;
    /** 冷却恢复事件 */
    export const COOLDOWN_RESUMED = 'cooldown:resumed' as const;

    /** 冷却事件数据接口 */
    export interface CooldownEventData extends BaseEventData {
        /** 冷却ID */
        cooldownId: string;
        /** 总时间 */
        totalTime: number;
        /** 剩余时间 */
        remainingTime: number;
        /** 进度 */
        progress: number;
        /** Entity */
        owner?: Entity;
    }

    // ==================== Effect 事件 ====================
    /** 特效移除事件 */
    export const EFFECT_REMOVED = 'effect:removed' as const;
    /** 特效事件数据接口 */
    export interface EffectEventData {
        /** 特效ID */
        effectId: string;
        /** 特效路径 */
        effectPath: string;
        /** 附加点 */
        attachPoint?: string;
        /** 持续时间 */
        duration?: number;
    }

}

// ============================================================================
// 统一事件系统 - 所有事件常量和数据接口都在 EventTypes 命名空间中
// ============================================================================

/**
 * 简化的事件处理器类型
 */
export type SimpleEventHandler<T = any> = EventHandler<T>;

/**
 * 简化的事件订阅对象
 */
export interface SimpleEventSubscription {
    eventType: string;
    handler: EventHandler;
    priority: EventPriority;
    once: boolean;
}

// 移除了复杂的 EventTypeMap，现在事件常量和数据接口都在 EventTypes 命名空间中
// 使用方式：EventTypes.ENTITY_CREATED 和 EventTypes.EntityEventData
