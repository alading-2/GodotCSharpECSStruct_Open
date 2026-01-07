/** @noSelfInFile **/

import { Component } from "../../../Component";
import { Entity } from "../../../../Entity/Entity";
import { TimerComponent } from "../../TimerComponent";
import { EventTypes } from "../../../../types/EventTypes";
import { Logger } from "../../../../../base/object/工具/logger";

const logger = Logger.createLogger("RecoveryComponent");

/**
 * 恢复组件配置属性接口
 * 
 * 定义了RecoveryComponent的所有可配置参数，支持灵活的恢复系统配置。
 * 
 * ## 配置示例
 * 
 * ### 普通单位恢复配置
 * ```typescript
 * const normalRecoveryProps: RecoveryComponentProps = {
 *     recoveryInterval: 1.0,      // 每1秒执行一次恢复
 *     autoStart: true             // 自动开始恢复
 * };
 * 
 * // 恢复速率通过属性数据设置
 * entity.data.attr.set("基础生命恢复", 2);     // 每秒恢复2点生命值
 * entity.data.attr.set("基础魔法恢复", 1);     // 每秒恢复1点魔法值
 * ```
 * 
 * ### 英雄单位恢复配置
 * ```typescript
 * const heroRecoveryProps: RecoveryComponentProps = {
 *     recoveryInterval: 0.5,      // 更频繁的恢复检查
 *     autoStart: true
 * };
 * 
 * // 英雄恢复速度更快
 * entity.data.attr.set("基础生命恢复", 10);
 * entity.data.attr.set("基础魔法恢复", 5);
 * entity.data.attr.set("百分比生命恢复", 1);   // 每秒恢复1%最大生命值
 * ```
 * 
 * ### 快速恢复配置
 * ```typescript
 * const fastRecoveryProps: RecoveryComponentProps = {
 *     recoveryInterval: 0.1,      // 极短的恢复间隔
 *     autoStart: false            // 手动控制开始时机
 * };
 * 
 * // 高速恢复
 * entity.data.attr.set("基础生命恢复", 50);
 * entity.data.attr.set("基础魔法恢复", 30);
 * entity.data.attr.set("百分比生命恢复", 5);   // 每秒恢复5%最大生命值
 * ```
 */
export interface RecoveryComponentProps {
    /** 
     * 恢复检查间隔（秒）
     * - 控制恢复计算的频率
     * - 较小的值提供更平滑的恢复，但消耗更多性能
     * - 较大的值节省性能，但恢复不够平滑
     * - 推荐范围：0.1 - 2.0秒
     * @default 1.0
     */
    recoveryInterval?: number;

    /** 
     * 是否自动开始恢复
     * - true：组件初始化后立即开始恢复过程
     * - false：需要手动调用startRecovery()开始恢复
     * @default true
     */
    autoStart?: boolean;
}

/**
 * 恢复组件 - 单位生命值和魔法值恢复系统
 * 
 * 这个组件专门负责管理单位的生命值和魔法值恢复功能，从LifecycleComponent中分离出来，
 * 遵循现代游戏架构的单一职责原则，只关注恢复相关的逻辑。
 * 
 * ## 核心设计理念
 * - **数据驱动**：恢复速率完全由AttributeData管理，支持基础恢复和百分比恢复
 * - **组件解耦**：不再内部存储恢复速率，完全依赖属性系统
 * - **灵活配置**：支持固定值恢复和基于最大值百分比的恢复
 * 
 * ## 核心功能
 * - 自动恢复：定时从属性数据读取恢复速率并执行恢复
 * - 手动恢复：提供治疗和魔法恢复接口
 * - 恢复控制：支持暂停、恢复、停止恢复过程
 * - 事件通知：恢复时触发相应事件
 * 
 * ## 使用示例
 * 
 * ### 基础使用
 * ```typescript
 * // 创建恢复组件
 * const recovery = new RecoveryComponent(entity, {
 *     recoveryInterval: 1.0,      // 每1秒执行一次恢复
 *     autoStart: true             // 自动开始恢复
 * });
 * 
 * // 设置恢复速率（通过属性数据）
 * entity.data.attr.set("基础生命恢复", 5);      // 每秒恢复5点生命值
 * entity.data.attr.set("基础魔法恢复", 3);      // 每秒恢复3点魔法值
 * entity.data.attr.set("百分比生命恢复", 1);    // 每秒恢复1%最大生命值
 * 
 * // 手动治疗
 * recovery.heal(50);              // 立即恢复50点生命值
 * recovery.restoreMana(30);       // 立即恢复30点魔法值
 * 
 * // 控制恢复过程
 * recovery.pauseRecovery();       // 暂停恢复
 * recovery.resumeRecovery();      // 恢复恢复
 * recovery.stopRecovery();        // 停止恢复
 * ```
 * 
 * ### 动态调整恢复速率
 * ```typescript
 * // 根据单位等级调整恢复速率
 * const level = entity.data.attr?.get("等级") || 1;
 * entity.data.attr.set("基础生命恢复", level * 2);    // 每级增加2点生命恢复
 * entity.data.attr.set("基础魔法恢复", level * 1.5);  // 每级增加1.5点魔法恢复
 * 
 * // 设置百分比恢复（高级单位）
 * if (level >= 10) {
 *     entity.data.attr.set("百分比生命恢复", 2);  // 每秒恢复2%最大生命值
 *     entity.data.attr.set("百分比魔法恢复", 1);  // 每秒恢复1%最大魔法值
 * }
 * 
 * // 获取当前恢复速率
 * const healthRate = recovery.getHealthRecoveryRate();
 * const manaRate = recovery.getManaRecoveryRate();
 * ```
 * 
 * ### 事件监听
 * ```typescript
 * // 监听恢复事件
 * entity.on(EventTypes.UNIT_HEALTH_CHANGED, (data) => {
 *     if (data.healthChange > 0) {
 *         console.log(`恢复了${data.healthChange}点生命值`);
 *     }
 * });
 * 
 * entity.on(EventTypes.UNIT_MANA_CHANGED, (data) => {
 *     if (data.manaChange > 0) {
 *         console.log(`恢复了${data.manaChange}点魔法值`);
 *     }
 * });
 * ```
 * 
 * ### 高级用法
 * ```typescript
 * // 战斗状态下的恢复管理
 * entity.on(EventTypes.UNIT_ENTER_COMBAT, () => {
 *     entity.data.attr.set("基础生命恢复", 1);  // 战斗中降低恢复速率
 *     entity.data.attr.set("百分比生命恢复", 0); // 战斗中取消百分比恢复
 * });
 * 
 * entity.on(EventTypes.UNIT_LEAVE_COMBAT, () => {
 *     entity.data.attr.set("基础生命恢复", 5);  // 脱战后恢复正常速率
 *     entity.data.attr.set("百分比生命恢复", 1); // 脱战后恢复百分比恢复
 * });
 * 
 * // 根据生命值百分比调整恢复
 * const currentHp = entity.data.unit?.get("当前生命值") || 0;
 * const maxHp = entity.data.attr?.get("最终生命值") || 100;
 * const hpPercent = currentHp / maxHp;
 * 
 * if (hpPercent < 0.3) {
 *     entity.data.attr.set("基础生命恢复", 10);     // 低血量时加速恢复
 *     entity.data.attr.set("百分比生命恢复", 3);    // 低血量时额外百分比恢复
 * }
 * ```
 * 
 * ## 架构设计
 * 
 * 本组件遵循现代游戏架构原则：
 * - **单一职责**：只负责恢复功能，不涉及其他逻辑
 * - **数据驱动**：恢复速率完全由AttributeData管理，组件只负责执行
 * - **属性集成**：与属性系统深度集成，支持复杂的恢复计算公式
 * - **事件驱动**：恢复时触发事件，便于其他系统响应
 * - **可配置性**：支持基础恢复和百分比恢复的灵活组合
 * - **组件解耦**：与LifecycleComponent等其他组件独立
 * 
 * ## 性能优化
 * 
 * 1. **智能恢复**：只有在需要恢复时才执行计算
 * 2. **批量更新**：在一个周期内同时处理生命值和魔法值恢复
 * 3. **状态检查**：只有在单位存活时才进行恢复
 * 4. **计时器管理**：组件销毁时自动清理计时器
 * 
 * ## 注意事项
 * 
 * 1. **依赖关系**：需要Entity具有unit和attr数据管理器
 * 2. **生命周期**：需要配合LifecycleComponent使用，检查单位存活状态
 * 3. **数值范围**：恢复不会超过最大值，自动进行边界检查
 * 4. **计时器清理**：组件销毁时会自动停止所有恢复计时器
 * 5. **恢复速率设置**：恢复速率通过AttributeData设置，不再通过组件属性设置
 * 6. **支持属性**：支持"基础生命恢复"、"基础魔法恢复"、"百分比生命恢复"、"百分比魔法恢复"属性
 * 7. **百分比计算**：百分比恢复基于单位的最大生命值/魔法值计算
 * 8. **方法迁移**：setHealthRecoveryRate和setManaRecoveryRate方法已废弃，建议直接操作属性数据
 * 
 * @author 游戏架构师
 * @version 1.0.0
 * @since 2.0.0
 */
export class RecoveryComponent extends Component<RecoveryComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "RecoveryComponent";

    // === 恢复配置 ===
    /** 恢复间隔（秒） */
    private recoveryInterval: number = 1.0;
    /** 是否自动开始恢复 */
    private autoStart: boolean = true;

    // === 计时器管理 ===
    /** 计时器组件引用 */
    private timerComponent: TimerComponent;
    /** 恢复计时器ID */
    private recoveryTimerId: string | null = null;

    // === 状态标志 ===
    /** 恢复是否已启动 */
    private isRecoveryActive: boolean = false;
    /** 恢复是否暂停 */
    private isRecoveryPaused: boolean = false;

    /**
     * 获取组件类型
     */
    static getType(): string {
        return RecoveryComponent.TYPE;
    }

    /**
     * 构造函数
     * @param owner 所属实体
     * @param props 组件属性
     */
    constructor(owner: Entity, props: RecoveryComponentProps = {}) {
        super(owner, props);

        // 获取TimerComponent
        this.timerComponent = this.owner.component.timer;

        logger.debug(`实体 ${this.owner.getId()} 的恢复组件已创建`);
    }

    /**
     * 组件初始化
     */
    initialize(): void {

        // 初始化配置
        this.recoveryInterval = this.props.recoveryInterval ?? 1.0;
        this.autoStart = this.props.autoStart ?? true;

        // 如果设置为自动开始，则启动恢复
        if (this.autoStart) {
            this.startRecovery();
        }

        logger.debug(`实体 ${this.owner.getId()} 的恢复组件已初始化`);
    }

    // ============================= 恢复系统 =============================

    /**
     * 启动恢复系统
     */
    public startRecovery(): void {
        if (this.isRecoveryActive) {
            logger.warn(`实体 ${this.owner.getId()} 的恢复系统已经启动`);
            return;
        }

        // 停止现有计时器
        this.stopRecovery();

        // 创建新的恢复计时器
        this.recoveryTimerId = this.timerComponent.CreateTimer(this.recoveryInterval, () => {
            this.performRecovery();
        });

        this.isRecoveryActive = true;
        this.isRecoveryPaused = false;

        logger.debug(`实体 ${this.owner.getId()} 的恢复系统已启动`);
    }

    /**
     * 停止恢复系统
     */
    public stopRecovery(): void {
        if (this.recoveryTimerId) {
            this.timerComponent.removeTimer(this.recoveryTimerId);
            this.recoveryTimerId = null;
        }

        this.isRecoveryActive = false;
        this.isRecoveryPaused = false;

        logger.debug(`实体 ${this.owner.getId()} 的恢复系统已停止`);
    }

    /**
     * 暂停恢复系统
     */
    public pauseRecovery(): void {
        this.isRecoveryPaused = true;
        logger.debug(`实体 ${this.owner.getId()} 的恢复系统已暂停`);
    }

    /**
     * 恢复恢复系统
     */
    public resumeRecovery(): void {
        this.isRecoveryPaused = false;
        logger.debug(`实体 ${this.owner.getId()} 的恢复系统已恢复`);
    }

    /**
     * 执行恢复逻辑
     */
    private performRecovery(): void {
        // 检查是否暂停或单位不存活
        if (this.isRecoveryPaused || !this.isUnitAlive()) {
            return;
        }

        // 从属性数据获取恢复速率
        const attributeData = this.owner.data.attr;
        if (!attributeData) {
            return;
        }

        // 获取最终恢复速率（直接从属性数据读取）
        const finalHealthRecovery = attributeData.get("最终生命恢复");
        const finalManaRecovery = attributeData.get("最终魔法恢复");

        // 执行生命恢复
        if (finalHealthRecovery > 0) {
            this.healUnit(finalHealthRecovery);
        }

        // 执行魔法恢复
        if (finalManaRecovery > 0) {
            this.restoreMana(finalManaRecovery);
        }
    }

    /**
     * 检查单位是否存活
     */
    private isUnitAlive(): boolean {
        const lifecycleComponent = this.owner.component.lifecycle;
        if (lifecycleComponent) {
            return lifecycleComponent.isAlive();
        }

        // 如果没有生命周期组件，检查单位数据
        const unitData = this.owner.data.unit;
        if (unitData) {
            return !unitData.get("死亡");
        }

        return true; // 默认认为存活
    }

    // ============================= 治疗和恢复接口 =============================

    /**
     * 治疗单位
     * @param amount 治疗量
     * @param triggerEvent 是否触发事件，默认true
     */
    public healUnit(amount: number, triggerEvent: boolean = true): void {
        if (amount <= 0) return;

        const unitData = this.owner.data.unit;
        const attributeData = this.owner.data.attr;

        if (!unitData || !attributeData) {
            logger.warn(`实体 ${this.owner.getId()} 缺少必要的数据组件进行治疗`);
            return;
        }

        const currentHp = unitData.get("当前生命值");
        const maxHp = attributeData.get("最终生命值");

        // 计算新的生命值（不超过最大值）
        const newHp = Math.min(currentHp + amount, maxHp);
        const actualHeal = newHp - currentHp;

        if (actualHeal > 0) {
            unitData.set("当前生命值", newHp);

            // 触发治疗事件
            if (triggerEvent) {
                this.owner.emit(EventTypes.UNIT_HEALTH_CHANGED, {
                    unit: this.owner,
                    healthChange: actualHeal,
                    currentHealth: newHp,
                    maxHealth: maxHp,
                    source: "recovery"
                });
            }
        }
    }

    /**
     * 恢复魔法值
     * @param amount 恢复量
     * @param triggerEvent 是否触发事件，默认true
     */
    public restoreMana(amount: number, triggerEvent: boolean = true): void {
        if (amount <= 0) return;

        const unitData = this.owner.data.unit;
        const attributeData = this.owner.data.attr;

        if (!unitData || !attributeData) {
            logger.warn(`实体 ${this.owner.getId()} 缺少必要的数据组件进行魔法恢复`);
            return;
        }

        const currentMp = unitData.get("当前魔法值") || 0;
        const maxMp = attributeData.get("最终魔法值") || 0;

        if (maxMp > 0) {
            // 计算新的魔法值（不超过最大值）
            const newMp = Math.min(currentMp + amount, maxMp);
            const actualRestore = newMp - currentMp;

            if (actualRestore > 0) {
                unitData.set("当前魔法值", newMp);

                // 触发魔法恢复事件
                if (triggerEvent) {
                    this.owner.emit(EventTypes.UNIT_MANA_CHANGED, {
                        unit: this.owner,
                        manaChange: actualRestore,
                        currentMana: newMp,
                        maxMana: maxMp,
                        source: "recovery"
                    });
                }
            }
        }
    }

    // ============================= 公共API =============================


    /**
     * 设置恢复间隔
     * @param interval 恢复间隔（秒）
     */
    public setRecoveryInterval(interval: number): void {
        this.recoveryInterval = Math.max(0.1, interval);

        // 如果恢复系统正在运行，重新启动以应用新间隔
        if (this.isRecoveryActive) {
            this.startRecovery();
        }

        logger.debug(`实体 ${this.owner.getId()} 恢复间隔设置为: ${this.recoveryInterval}秒`);
    }

    // ============================= 状态查询接口 =============================


    /**
     * 获取恢复间隔
     */
    public getRecoveryInterval(): number {
        return this.recoveryInterval;
    }

    /**
     * 检查恢复系统是否活跃
     */
    public isRecoveryActiveState(): boolean {
        return this.isRecoveryActive;
    }

    /**
     * 检查恢复系统是否暂停
     */
    public isRecoveryPausedState(): boolean {
        return this.isRecoveryPaused;
    }

    // ============================= 生命周期管理 =============================

    /**
     * 组件销毁
     */
    public destroy(): void {
        // 停止恢复系统
        this.stopRecovery();

        logger.debug(`实体 ${this.owner.getId()} 的恢复组件已销毁`);
    }
}