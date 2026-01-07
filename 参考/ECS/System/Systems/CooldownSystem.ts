/** @noSelfInFile **/

import { System } from "../System";
import { CooldownComponent } from "../../Component/Components/冷却/CooldownComponent";
import { Logger } from "../../../base/object/工具/logger";
import { EventTypes } from "../../types/EventTypes";
import { Entity, EventBus } from "../..";
import { TimerManager } from "../../../base/object/工具/Timer/TimerManager";
import { SystemManager } from "../SystemManager";

const logger = Logger.createLogger("CooldownSystem");

/**
 * 冷却系统 - 统一计时器和管理所有冷却逻辑
 * 
 * 重构后的架构设计：
 * - 作为ECS架构中的统一计时器，负责驱动所有CooldownComponent的更新
 * - 集成TimerManager进行高级计时器管理，支持复杂的计时需求
 * - 通过固定时间间隔（0.1秒）批量更新所有冷却组件，提高性能
 * - 与CooldownComponent协同工作：System负责计时，Component负责数据存储
 * - 直接使用ComponentManager的全局组件实例记录，避免Entity查询开销
 * - 采用单例模式，确保全局唯一的冷却管理器
 * 
 * 架构优势：
 * 1. **性能优化**: 统一计时避免了为每个冷却创建独立Timer的开销
 * 2. **统一管理**: 所有冷却逻辑集中在System中，便于维护和调试
 * 3. **符合ECS模式**: 数据与逻辑完全分离，System专注于逻辑处理
 * 4. **可扩展性**: 易于添加全局冷却控制功能（如暂停所有冷却、加速等）
 * 5. **高效访问**: 直接获取组件实例，无需Entity查询和组件获取步骤
 * 6. **计时器集成**: 通过TimerManager支持更复杂的计时需求和管理功能
 * 7. **单例模式**: 全局唯一实例，便于外部系统访问和控制
 */
export class CooldownSystem extends System {
    // 系统类型名称
    public static readonly TYPE: string = "CooldownSystem";

    // 系统优先级（较高优先级，确保冷却及时更新）
    public static readonly PRIORITY: number = 80;

    // 单例实例
    private static instance: CooldownSystem;

    // 调试模式
    private debugMode: boolean = false;

    // 计时器间隔
    protected readonly timerInterval: number = 0.1;


    /**
     * 私有构造函数，确保单例模式
     */
    private constructor() {
        super();
    }

    /**
     * 获取单例实例
     * @returns CooldownSystem实例
     */
    public static getInstance(): CooldownSystem {
        if (!CooldownSystem.instance) {
            CooldownSystem.instance = new CooldownSystem();
        }
        return CooldownSystem.instance;
    }

    /**
     * 负责创建和注册CooldownSystem实例到SystemManager
     */
    public static onInit(): void {
        // 注册+初始化系统
        SystemManager.getInstance().registerAndInitSystem(CooldownSystem.getInstance());
        logger.info("CooldownSystem已通过onInit函数自动注册");
    }

    /**
     * 初始化系统
     */
    public initialize(): void {
        // 初始化系统计时器
        super.initialize();
        this.subscribeToEvents();
        logger.info("冷却系统已初始化");
    }

    /**
     * 更新系统 - 统一管理所有冷却组件的计时
     * @param deltaTime 时间增量
     */
    public update(): void {
        const cooldownComponents = this.getComponentInstances(CooldownComponent);
        // 直接遍历所有冷却组件实例
        cooldownComponents.forEach(cooldownComponent => {
            // 调用组件的update方法，传入固定的时间间隔
            cooldownComponent.update(this.timerInterval);
        });
    }


    /**
     * 销毁系统
     */
    public destroy(): void {
        this.unsubscribeFromEvents();
        logger.info("冷却系统已销毁");
    }

    // ========================= 公共API =========================

    /**
     * 暂停所有冷却
     */
    public pauseAllCooldowns(): void {
        const cooldownComponents = this.getComponentInstances(CooldownComponent);
        cooldownComponents.forEach(cooldownComponent => {
            cooldownComponent.getAllCooldownIds().forEach(id => {
                cooldownComponent.pauseCooldownById(id);
            });
        });
        logger.info("已暂停所有冷却组件的冷却");
    }

    /**
     * 恢复所有冷却
     */
    public resumeAllCooldowns(): void {
        const cooldownComponents = this.getComponentInstances(CooldownComponent);
        cooldownComponents.forEach(cooldownComponent => {
            cooldownComponent.getAllCooldownIds().forEach(id => {
                cooldownComponent.resumeCooldownById(id);
            });
        });
        logger.info("已恢复所有冷却组件的冷却");
    }

    /**
     * 重置所有冷却
     */
    public resetAllCooldowns(): void {
        const cooldownComponents = this.getComponentInstances(CooldownComponent);
        cooldownComponents.forEach(cooldownComponent => {
            cooldownComponent.getAllCooldownIds().forEach(id => {
                cooldownComponent.resetCooldownById(id);
            });
        });
        logger.info("所有冷却已重置");
    }

    /**
     * 获取系统统计信息
     */
    public getSystemStats(): {
        totalEntities: number;
        activeCooldowns: number;
    } {
        const cooldownComponents = this.getComponentInstances(CooldownComponent);
        let activeCooldowns = 0;

        cooldownComponents.forEach(cooldownComponent => {
            cooldownComponent.getAllCooldownIds().forEach(id => {
                if (cooldownComponent.isOnCooldownById(id)) {
                    activeCooldowns++;
                }
            });
        });

        return {
            totalEntities: cooldownComponents.length,
            activeCooldowns,
        };
    }

    /**
     * 设置调试模式
     * @param enabled 是否启用调试模式
     */
    public setDebugMode(enabled: boolean): void {
        this.debugMode = enabled;
        logger.info(`调试模式已${enabled ? '启用' : '禁用'}`);
    }

    /**
     * 获取Entity的冷却信息（调试用）
     * @param entity 目标Entity
     */
    public getEntityCooldownInfo(entity: Entity): Record<string, any> | null {
        const cooldownComponent = entity.getComponent(CooldownComponent);
        if (!cooldownComponent) {
            return null;
        }

        const info: Record<string, any> = {};
        const cooldownIds = cooldownComponent.getAllCooldownIds();

        cooldownIds.forEach(id => {
            info[id] = {
                isOnCooldown: cooldownComponent.isOnCooldownById(id),
                remainingTime: cooldownComponent.getRemainingTimeById(id),
                progress: cooldownComponent.getCooldownProgressById(id)
            };
        });

        return info;
    }

    // ========================= 私有方法 =========================



    /**
     * 订阅事件
     */
    private subscribeToEvents(): void {
        EventBus.getInstance().on(EventTypes.COOLDOWN_COMPLETED, (data) => {
            if (this.debugMode) {
                logger.debug(`冷却完成: ${data.cooldownId}`);
            }
        });

        EventBus.getInstance().on(EventTypes.COOLDOWN_STARTED, (data) => {
            if (this.debugMode) {
                logger.debug(`冷却开始: ${data.cooldownId} (${data.cooldownTime}秒)`);
            }
        });

        EventBus.getInstance().on(EventTypes.COOLDOWN_RESET, (data) => {
            if (this.debugMode) {
                logger.debug(`冷却重置: ${data.cooldownId}`);
            }
        });
    }

    /**
     * 取消事件订阅
     */
    private unsubscribeFromEvents(): void {
        EventBus.getInstance().offAll(EventTypes.COOLDOWN_COMPLETED);
        EventBus.getInstance().offAll(EventTypes.COOLDOWN_STARTED);
        EventBus.getInstance().offAll(EventTypes.COOLDOWN_RESET);
    }
}