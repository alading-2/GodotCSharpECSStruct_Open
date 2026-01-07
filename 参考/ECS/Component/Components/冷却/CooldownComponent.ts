/** @noSelfInFile **/

import { Component, ComponentConstructor } from "../../Component";
import { Logger } from "../../../../base/object/工具/logger";
import { Entity } from "../../..";
import { EventTypes } from "../../../types/EventTypes";

const logger = Logger.createLogger("CooldownComponent");

/**
 * 冷却组件属性接口
 */
interface CooldownComponentProps {
    /** 默认冷却时间（秒） */
    defaultCooldown?: number;
    /** 是否自动开始冷却 */
    autoStart?: boolean;
    /** 冷却完成时的回调 */
    onCooldownComplete?: () => void;
}

/**
 * 冷却数据接口
 */
interface CooldownData {
    /** 冷却时间（秒） */
    cooldownTime: number;
    /** 剩余冷却时间（秒） */
    remainingTime: number;
    /** 是否正在冷却中，true：正在冷却中，false：冷却完成 */
    isOnCooldown: boolean;
    /** 是否暂停，true：暂停中，false：正常运行 */
    isPaused: boolean;
    /** 冷却完成回调 */
    onComplete?: () => void;
}

/**
 * 冷却组件 - 纯数据组件，存储冷却状态
 * 
 * 设计原则：
 * - 数据与逻辑分离：只负责冷却数据存储，逻辑由CooldownSystem统一处理
 * - 单一职责：专门负责冷却数据管理
 * - 可复用：ItemComponent和AbilityComponent都可以使用
 * - 数据驱动：通过配置控制冷却行为
 * - 事件驱动：通过事件通知冷却状态变化
 * - 系统化管理：由CooldownSystem统一计时，符合现代游戏引擎架构
 * 
 * 使用场景：
 * - 物品技能冷却
 * - 技能冷却
 * - 任何需要冷却机制的功能
 * 
 * 架构说明：
 * - CooldownComponent：纯数据组件，存储冷却状态和配置
 * - CooldownSystem：统一计时器，每0.1秒调用所有冷却组件的update方法
 * - 符合Unity ECS和Unreal Engine的数据与逻辑分离原则
 */
export class CooldownComponent extends Component<CooldownComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "CooldownComponent";

    // 冷却数据映射 - 支持多个冷却实例
    private cooldowns: Map<string, CooldownData> = new Map();

    // 默认冷却标识符
    private static readonly DEFAULT_COOLDOWN_ID = "default";

    // 组件依赖列表 - 无依赖，纯数据组件
    protected dependencies: ComponentConstructor<any>[] = [];
    /**
     * 获取组件类型
     */
    static getType(): string {
        return CooldownComponent.TYPE;
    }

    /**
     * 构造函数
     */
    constructor(owner: Entity, props?: CooldownComponentProps) {
        super(owner, props || {});
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        // 如果设置了默认冷却时间，创建默认冷却实例
        if (this.props.defaultCooldown && this.props.defaultCooldown > 0) {
            this.createCooldown(
                CooldownComponent.DEFAULT_COOLDOWN_ID,
                this.props.defaultCooldown,
                this.props.onCooldownComplete
            );

            // 如果设置了自动开始，立即开始冷却
            if (this.props.autoStart) {
                this.startCooldown();
            }
        }

        logger.debug(`冷却组件已为Entity ${this.owner.getId()} 初始化`);
    }

    /**
     * 更新组件 - 由CooldownSystem统一调用
     * @param deltaTime 时间增量（秒）
     */
    update(deltaTime: number): void {
        // 遍历所有冷却实例，更新剩余时间
        for (const [cooldownId, cooldownData] of this.cooldowns) {
            // 只有在冷却中且未暂停的情况下才更新
            if (cooldownData.isOnCooldown && !cooldownData.isPaused && cooldownData.remainingTime > 0) {
                cooldownData.remainingTime -= deltaTime;

                // 触发冷却更新事件
                this.owner.emit(EventTypes.COOLDOWN_UPDATED, {
                    entity: this.owner,
                    cooldownId,
                    remainingTime: cooldownData.remainingTime,
                    progress: 1 - (cooldownData.remainingTime / cooldownData.cooldownTime)
                });

                // 冷却完成
                if (cooldownData.remainingTime <= 0) {
                    this.finishCooldown(cooldownId);
                }
            }
        }
    }

    /**
     * 销毁组件
     */
    destroy(): void {
        // 清理所有冷却计时器
        this.clearAllCooldowns();
        logger.debug(`冷却组件已为Entity ${this.owner.getId()} 销毁`);
    }

    // ========================= 公共API =========================

    /**
     * 创建冷却实例
     * @param cooldownId 冷却标识符，比如“技能冷却”
     * @param cooldownTime 冷却时间（秒）
     * @param onComplete 冷却完成回调
     */
    createCooldown(cooldownId: string, cooldownTime: number, onComplete?: () => void): void {
        if (cooldownTime <= 0) {
            logger.warn(`无效的冷却时间: ${cooldownTime}`);
            return;
        }

        // 如果已存在，先清理
        if (this.cooldowns.has(cooldownId)) {
            this.clearCooldown(cooldownId);
        }

        const cooldownData: CooldownData = {
            cooldownTime,
            remainingTime: 0,
            isOnCooldown: false,
            isPaused: false,
            onComplete
        };

        this.cooldowns.set(cooldownId, cooldownData);
        logger.debug(`冷却实例已创建: ${cooldownId} (${cooldownTime}秒)`);
    }

    /**
     * 开始冷却（使用默认冷却）
     * @param cooldownTime 可选的冷却时间，如果不提供则使用创建时的时间
     */
    startCooldown(cooldownTime?: number): boolean {
        return this.startCooldownById(CooldownComponent.DEFAULT_COOLDOWN_ID, cooldownTime);
    }

    /**
     * 开始指定的冷却
     * @param cooldownId 冷却标识符
     * @param cooldownTime 可选的冷却时间，如果不提供则使用创建时的时间
     */
    startCooldownById(cooldownId: string, cooldownTime?: number): boolean {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData) {
            logger.warn(`未找到冷却实例: ${cooldownId}`);
            return false;
        }

        // 更新冷却时间（如果提供了新的时间）
        if (cooldownTime !== undefined && cooldownTime > 0) {
            cooldownData.cooldownTime = cooldownTime;
        }

        // 设置冷却状态
        cooldownData.remainingTime = cooldownData.cooldownTime;
        cooldownData.isOnCooldown = true;

        // 触发冷却开始事件
        this.owner.emit(EventTypes.COOLDOWN_STARTED, {
            entity: this.owner,
            cooldownId,
            duration: cooldownData.cooldownTime
        });

        logger.debug(`冷却已开始: ${cooldownId} (${cooldownData.cooldownTime}秒)`);
        return true;
    }

    /**
     * 重置冷却（使用默认冷却）
     */
    resetCooldown(): boolean {
        return this.resetCooldownById(CooldownComponent.DEFAULT_COOLDOWN_ID);
    }

    /**
     * 重置指定的冷却
     * @param cooldownId 冷却标识符
     */
    resetCooldownById(cooldownId: string): boolean {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData) {
            logger.warn(`未找到冷却实例: ${cooldownId}`);
            return false;
        }

        // 重置状态
        cooldownData.remainingTime = 0;
        cooldownData.isOnCooldown = false;

        // 触发冷却重置事件
        this.owner.emit(EventTypes.COOLDOWN_RESET, {
            entity: this.owner,
            cooldownId
        });

        logger.debug(`冷却已重置: ${cooldownId}`);
        return true;
    }

    /**
     * 暂停冷却（使用默认冷却）
     */
    pauseCooldown(): boolean {
        return this.pauseCooldownById(CooldownComponent.DEFAULT_COOLDOWN_ID);
    }

    /**
     * 暂停指定的冷却
     * @param cooldownId 冷却标识符
     */
    pauseCooldownById(cooldownId: string): boolean {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData || !cooldownData.isOnCooldown || cooldownData.isPaused) {
            return false;
        }

        // 标记为暂停状态
        cooldownData.isPaused = true;

        // 触发冷却暂停事件
        this.owner.emit(EventTypes.COOLDOWN_PAUSED, {
            entity: this.owner,
            cooldownId
        });

        return true;
    }

    /**
     * 恢复冷却（使用默认冷却）
     */
    resumeCooldown(): boolean {
        return this.resumeCooldownById(CooldownComponent.DEFAULT_COOLDOWN_ID);
    }

    /**
     * 恢复指定的冷却
     * @param cooldownId 冷却标识符
     */
    resumeCooldownById(cooldownId: string): boolean {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData || !cooldownData.isPaused || cooldownData.remainingTime <= 0) {
            return false;
        }

        // 取消暂停状态
        cooldownData.isPaused = false;

        // 触发冷却恢复事件
        this.owner.emit(EventTypes.COOLDOWN_RESUMED, {
            entity: this.owner,
            cooldownId
        });

        return true;
    }

    /**
     * 检查是否在冷却中（使用默认冷却）
     */
    isOnCooldown(): boolean {
        return this.isOnCooldownById(CooldownComponent.DEFAULT_COOLDOWN_ID);
    }

    /**
     * 检查指定冷却是否在冷却中
     * @param cooldownId 冷却标识符
     */
    isOnCooldownById(cooldownId: string): boolean {
        const cooldownData = this.cooldowns.get(cooldownId);
        return cooldownData ? cooldownData.isOnCooldown : false;
    }

    /**
     * 获取剩余冷却时间（使用默认冷却）
     */
    getRemainingTime(): number {
        return this.getRemainingTimeById(CooldownComponent.DEFAULT_COOLDOWN_ID);
    }

    /**
     * 获取指定冷却的剩余时间
     * @param cooldownId 冷却标识符
     */
    getRemainingTimeById(cooldownId: string): number {
        const cooldownData = this.cooldowns.get(cooldownId);
        return cooldownData ? cooldownData.remainingTime : 0;
    }

    /**
     * 获取冷却进度（0-1）（使用默认冷却）
     */
    getCooldownProgress(): number {
        return this.getCooldownProgressById(CooldownComponent.DEFAULT_COOLDOWN_ID);
    }

    /**
     * 获取指定冷却的进度（0-1）
     * @param cooldownId 冷却标识符
     */
    getCooldownProgressById(cooldownId: string): number {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData || cooldownData.cooldownTime <= 0) {
            return 0;
        }
        return 1 - (cooldownData.remainingTime / cooldownData.cooldownTime);
    }

    /**
     * 获取所有冷却ID
     */
    getAllCooldownIds(): string[] {
        return Array.from(this.cooldowns.keys());
    }

    /**
     * 清理指定冷却
     * @param cooldownId 冷却标识符
     */
    clearCooldown(cooldownId: string): boolean {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData) {
            return false;
        }

        // 移除冷却数据
        this.cooldowns.delete(cooldownId);

        logger.debug(`冷却已清理: ${cooldownId}`);
        return true;
    }

    /**
     * 清理所有冷却
     */
    clearAllCooldowns(): void {
        for (const cooldownId of this.cooldowns.keys()) {
            this.clearCooldown(cooldownId);
        }
    }

    // ========================= 私有方法 =========================

    /**
     * 完成冷却
     * @param cooldownId 冷却标识符
     */
    private finishCooldown(cooldownId: string): void {
        const cooldownData = this.cooldowns.get(cooldownId);
        if (!cooldownData) {
            return;
        }

        // 重置状态
        cooldownData.remainingTime = 0;
        cooldownData.isOnCooldown = false;

        // 触发冷却完成事件
        this.owner.emit(EventTypes.COOLDOWN_COMPLETED, {
            entity: this.owner,
            cooldownId
        });

        // 执行回调
        if (cooldownData.onComplete) {
            try {
                cooldownData.onComplete();
            } catch (error) {
                logger.error(`冷却完成回调中发生错误: ${error}`);
            }
        }

        logger.debug(`冷却已完成: ${cooldownId}`);
    }

    // ========================= 便捷方法 =========================

    /**
     * 快速创建并开始冷却
     * @param entity entity
     * @param cooldownTime 冷却时间（秒）
     * @param onComplete 冷却完成回调
     * @param cooldownId 可选的冷却标识符，默认使用"default"
     * @returns {boolean} 是否成功创建并开始冷却
     */
    static quickCooldown(
        entity: Entity,
        cooldownTime: number,
        onComplete?: () => void,
        cooldownId: string = CooldownComponent.DEFAULT_COOLDOWN_ID
    ): boolean {
        let cooldownComponent = entity.getComponent(CooldownComponent);

        // 如果没有冷却组件，创建一个
        if (!cooldownComponent) {
            cooldownComponent = entity.addComponent(CooldownComponent, {});
        }

        // 创建并开始冷却
        cooldownComponent.createCooldown(cooldownId, cooldownTime, onComplete);
        return cooldownComponent.startCooldownById(cooldownId);
    }
}