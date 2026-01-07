/** @noSelfInFile **/

import { Component } from "../../..";
import { Entity, DataManager } from "../../../..";
import { Logger } from "../../../../../base/object/工具/logger";
import { Effect } from "../../../../../base/object/特效/Effect";
import { Inte_BuffSchema } from "../../../../Schema";
import { EventTypes } from "../../../../types/EventTypes";
import { TimerComponent } from "../../TimerComponent";



const logger = Logger.createLogger("BuffComponent");

/**
 * Buff类型枚举
 */
export enum BuffType {
        BENEFICIAL = "增益",
        DETRIMENTAL = "减益",
        NEUTRAL = "中性"
}

/**
 * Buff效果接口
 */
export interface BuffEffect {
        type: string;
        value: number;
        target: string;
}

/**
 * 状态效果接口
 */
export interface StateEffect {
        type: string;
        duration: number;
        intensity: number;
}

/**
 * Buff组件属性接口
 */
export interface BuffComponentProps {
        buffName: string;
        duration: number;
        iconPath?: string;
        description?: string;
        buffType?: BuffType;
        maxStacks?: number;
        isStackable?: boolean;
        attachedUnit?: Entity;
        source?: Entity;
        level?: number;
        effects?: BuffEffect[];
        // 状态管理相关属性
        stateEffects?: StateEffect[];
        immunities?: string[];
        conflictsWith?: string[];
}

/**
 * Buff组件 - 管理War3 Buff效果和状态
 * 负责Buff和状态的统一管理
 * 使用现代ECS架构：DataManager + TimerComponent
 */
export class BuffComponent extends Component<BuffComponentProps> {
        // 组件类型名称
        protected static readonly TYPE: string = "BuffComponent";

        // 数据管理器
        private buffDataManager: DataManager<Inte_BuffSchema> | null = null;

        // 计时器组件引用
        private timerComponent: TimerComponent | null = null;

        // 计时器ID存储
        private durationTimerId: string | null = null;
        private updateTimerId: string | null = null;

        // 特效相关
        private buffEffect: Effect | null = null;
        private attachedUnit: Entity | null = null;

        // 状态标记（组件初始化与销毁状态由基类维护）
        private isPaused: boolean = false;
        private isActive: boolean = true;

        /**
         * 获取组件类型
         */
        static getType(): string {
                return BuffComponent.TYPE;
        }

        /**
         * 构造函数
         */
        constructor(owner: Entity, props: BuffComponentProps) {
                super(owner, props);

                logger.debug(`BuffComponent created: ${props.buffName}`);
        }

        /**
         * 初始化组件
         */
        initialize(): void {
                try {
                        // 获取Buff数据管理器
                        this.buffDataManager = this.owner.data.buff;
                        if (!this.buffDataManager) {
                                logger.error("BuffComponent: 未找到Buff数据管理器");
                                return;
                        }

                        // 获取计时器组件
                        this.timerComponent = this.owner.getComponent(TimerComponent);
                        if (!this.timerComponent) {
                                logger.error("BuffComponent: 未找到TimerComponent");
                                return;
                        }

                        // 初始化Buff数据
                        this.initializeBuffData();

                        // 创建计时器
                        this.createTimers();

                        // 创建特效
                        this.createEffect();

                        // 设置事件监听器
                        this.setupEventListeners();

                        // 应用Buff效果
                        this.applyBuffEffect();

                        logger.debug(`BuffComponent initialized: ${this.props.buffName}`);

                } catch (error) {
                        logger.error("BuffComponent初始化失败", error);
                }
        }

        /**
         * 更新组件
         */
        update(deltaTime: number): void {
                if (!this.buffDataManager) return;

                const isActive = this.buffDataManager.get("isActive") ?? true;
                const isPaused = this.buffDataManager.get("isPaused") ?? false;

                if (!isActive || isPaused) {
                        return;
                }

                // 更新剩余时间
                this.updateRemainingTime(deltaTime);

                // 更新特效位置
                this.updateEffect();

                // 检查Buff是否过期
                this.checkExpiration();
        }

        /**
         * 销毁组件
         */
        destroy(): void {
                try {
                        // 移除Buff效果
                        this.removeBuffEffect();

                        // 清理计时器
                        if (this.timerComponent) {
                                if (this.durationTimerId) {
                                        this.timerComponent.clearTimeout(this.durationTimerId);
                                        this.durationTimerId = null;
                                }
                                if (this.updateTimerId) {
                                        this.timerComponent.clearInterval(this.updateTimerId);
                                        this.updateTimerId = null;
                                }
                        }

                        // 清理特效
                        this.destroyEffect();

                        // 更新数据状态
                        if (this.buffDataManager) {
                                this.buffDataManager.set("isActive", false);
                        }

                        // 清理引用
                        this.buffDataManager = null;
                        this.timerComponent = null;
                        this.attachedUnit = null;

                        const buffName = this.props?.buffName || "Unknown";
                        logger.debug(`BuffComponent destroyed: ${buffName}`);

                } catch (error) {
                        logger.error("BuffComponent销毁失败", error);
                } finally {
                        super.destroy();
                }
        }

        /**
         * 初始化Buff数据
         */
        private initializeBuffData(): void {
                if (!this.buffDataManager) return;

                const initialData: Partial<Inte_BuffSchema> = {
                        id: `buff_${this.owner.getId()}_${Date.now()}`,
                        name: this.props.buffName,
                        description: this.props.description || this.generateDescription(),
                        icon: this.props.iconPath || "",

                        // Buff状态
                        层数: 1,
                        计时器: null,
                        计数: 0,

                        // 时间相关
                        duration: this.props.duration,
                        remainingTime: this.props.duration,
                        tickInterval: 1.0,
                        lastTickTime: GetGameTime(),

                        // Buff类型
                        type: this.props.buffType || BuffType.NEUTRAL,
                        category: "buff",
                        priority: this.props.level || 1,

                        // 效果数据
                        effects: {},
                        modifiers: {},

                        // 来源信息
                        source: this.props.source,
                        sourceAbility: "",
                        level: this.props.level || 1,

                        // 状态标记
                        isActive: true,
                        isPermanent: this.props.duration <= 0,
                        isDispellable: true,
                        isStackable: this.props.isStackable || false,
                        maxStacks: this.props.maxStacks || 1,

                        // 触发条件
                        triggerEvents: [],
                        conditions: {},

                        // 视觉效果
                        visualEffect: "",
                        soundEffect: "",

                        // 其他
                        customData: {}
                };

                // 设置初始数据
                Object.entries(initialData).forEach(([key, value]) => {
                        this.buffDataManager!.set(key as keyof Inte_BuffSchema, value);
                });

                logger.debug("Buff数据初始化完成", initialData);
        }

        /**
         * 生成Buff描述
         */
        private generateDescription(): string {
                const name = this.props?.buffName || "未知Buff";
                const desc = this.props?.description || "无描述";
                return `${name}: ${desc}`;
        }

        // ==================== 状态管理方法 ====================

        /**
         * 添加状态效果
         */
        addState(stateType: string, duration?: number): void {
                if (!this.buffDataManager) return;

                const effects = this.buffDataManager.get("effects") || {};
                effects[stateType] = {
                        active: true,
                        duration: duration || -1,
                        startTime: GetGameTime()
                };

                this.buffDataManager.set("effects", effects);

                // 触发状态添加事件
                this.owner.emit("STATE_ADDED", {
                        stateType,
                        duration,
                        buffId: this.buffDataManager.get("id")
                });
        }

        /**
         * 移除状态效果
         */
        removeState(stateType: string): void {
                if (!this.buffDataManager) return;

                const effects = this.buffDataManager.get("effects") || {};
                if (effects[stateType]) {
                        delete effects[stateType];
                        this.buffDataManager.set("effects", effects);

                        // 触发状态移除事件
                        this.owner.emit("STATE_REMOVED", {
                                stateType,
                                buffId: this.buffDataManager.get("id")
                        });
                }
        }

        /**
         * 检查是否有指定状态
         */
        hasState(stateType: string): boolean {
                if (!this.buffDataManager) return false;

                const effects = this.buffDataManager.get("effects") || {};
                return effects[stateType]?.active === true;
        }

        /**
         * 检查状态免疫
         */
        isImmuneToState(stateType: string): boolean {
                if (!this.buffDataManager) return false;

                const immunities = this.props?.immunities || [];
                return immunities.includes(stateType);
        }

        // ==================== Buff叠加管理 ====================

        /**
         * 添加Buff层数
         */
        addStack(amount: number = 1): boolean {
                if (!this.buffDataManager) return false;

                const isStackable = this.buffDataManager.get("isStackable");
                if (!isStackable) return false;

                const currentStacks = this.buffDataManager.get("层数") || 1;
                const maxStacks = this.buffDataManager.get("maxStacks") || 1;
                const newStacks = Math.min(currentStacks + amount, maxStacks);

                if (newStacks > currentStacks) {
                        this.buffDataManager.set("层数", newStacks);

                        // 触发层数变化事件
                        this.owner.emit("BUFF_STACK_CHANGED", {
                                buffId: this.buffDataManager.get("id"),
                                oldStacks: currentStacks,
                                newStacks: newStacks
                        });

                        return true;
                }

                return false;
        }

        /**
         * 移除Buff层数
         */
        removeStack(amount: number = 1): boolean {
                if (!this.buffDataManager) return false;

                const currentStacks = this.buffDataManager.get("层数") || 1;
                const newStacks = Math.max(1, currentStacks - amount);

                if (newStacks < currentStacks) {
                        this.buffDataManager.set("层数", newStacks);

                        // 触发层数变化事件
                        this.owner.emit("BUFF_STACK_CHANGED", {
                                buffId: this.buffDataManager.get("id"),
                                oldStacks: currentStacks,
                                newStacks: newStacks
                        });

                        // 如果层数降到0，移除Buff
                        if (newStacks <= 0) {
                                this.expireBuff();
                        }

                        return true;
                }

                return false;
        }

        /**
         * 获取当前层数
         */
        getStackCount(): number {
                if (!this.buffDataManager) return 0;
                return this.buffDataManager.get("层数") || 1;
        }

        /**
         * 设置层数
         */
        setStackCount(stacks: number): void {
                if (!this.buffDataManager) return;

                const maxStacks = this.buffDataManager.get("maxStacks") || 1;
                const newStacks = Math.max(1, Math.min(stacks, maxStacks));

                this.buffDataManager.set("层数", newStacks);
        }

        // ==================== Buff控制方法 ====================

        /**
         * 暂停Buff
         */
        pauseBuff(): void {
                if (!this.buffDataManager || !this.timerComponent) return;

                this.buffDataManager.set("isPaused", true);

                // 暂停计时器
                if (this.durationTimerId) {
                        this.timerComponent.pauseTimer(this.durationTimerId);
                }
                if (this.updateTimerId) {
                        this.timerComponent.pauseTimer(this.updateTimerId);
                }

                // 触发暂停事件
                this.owner.emit("BUFF_PAUSED", {
                        buffId: this.buffDataManager.get("id"),
                        buffName: this.buffDataManager.get("name")
                });

                logger.debug(`Buff paused: ${this.buffDataManager.get("name")}`);
        }

        /**
         * 恢复Buff
         */
        resumeBuff(): void {
                if (!this.buffDataManager || !this.timerComponent) return;

                this.buffDataManager.set("isPaused", false);

                // 恢复计时器
                if (this.durationTimerId) {
                        this.timerComponent.resumeTimer(this.durationTimerId);
                }
                if (this.updateTimerId) {
                        this.timerComponent.resumeTimer(this.updateTimerId);
                }

                // 触发恢复事件
                this.owner.emit("BUFF_RESUMED", {
                        buffId: this.buffDataManager.get("id"),
                        buffName: this.buffDataManager.get("name")
                });

                logger.debug(`Buff resumed: ${this.buffDataManager.get("name")}`);
        }

        /**
         * 刷新Buff持续时间
         */
        refreshDuration(newDuration?: number): void {
                if (!this.buffDataManager) return;

                const duration = newDuration || this.props.duration;
                this.buffDataManager.set("duration", duration);
                this.buffDataManager.set("remainingTime", duration);

                // 重新创建持续时间计时器
                if (this.timerComponent && this.durationTimerId) {
                        this.timerComponent.clearTimeout(this.durationTimerId);
                        this.durationTimerId = this.timerComponent.setTimeout(() => {
                                this.expireBuff();
                        }, duration);
                }

                // 触发刷新事件
                this.owner.emit("BUFF_REFRESHED", {
                        buffId: this.buffDataManager.get("id"),
                        buffName: this.buffDataManager.get("name"),
                        newDuration: duration
                });

                logger.debug(`Buff refreshed: ${this.buffDataManager.get("name")}, duration: ${duration}`);
        }

        /**
         * 检查Buff是否激活
         */
        isBuffActive(): boolean {
                if (!this.buffDataManager) return false;
                return this.buffDataManager.get("isActive") === true;
        }

        /**
         * 检查Buff是否暂停
         */
        isBuffPaused(): boolean {
                if (!this.buffDataManager) return false;
                return this.buffDataManager.get("isPaused") === true;
        }

        /**
         * 创建计时器
         */
        private createTimers(): void {
                if (!this.timerComponent) return;

                const duration = this.buffDataManager?.get("duration") || 0;

                if (duration > 0) {
                        // 创建Buff持续时间计时器
                        this.durationTimerId = this.timerComponent.setTimeout(() => {
                                this.expireBuff();
                        }, duration);

                        // 创建更新计时器（每0.1秒执行一次）
                        this.updateTimerId = this.timerComponent.setInterval(() => {
                                const isPaused = this.buffDataManager?.get("isPaused") ?? false;
                                const isActive = this.buffDataManager?.get("isActive") ?? true;
                                if (!isPaused && isActive) {
                                        const currentRemaining = this.buffDataManager?.get("remainingTime") || 0;
                                        const newRemaining = Math.max(0, currentRemaining - 0.1);
                                        this.buffDataManager?.set("remainingTime", newRemaining);
                                }
                        }, 0.1);

                        logger.debug(`Buff计时器创建完成，持续时间: ${duration}秒`);
                }
        }

        /**
         * 创建特效
         */
        private createEffect(): void {
                if (!this.effectPath) return;

                try {
                        // 获取附加单位的位置
                        const unitComponent = this.attachedUnit?.getComponent("UnitComponent");
                        if (unitComponent) {
                                const position = unitComponent.getPosition();
                                this.buffEffect = new Effect(this.effectPath, position.x, position.y, 0);

                                // 将特效附加到单位
                                if (this.buffEffect && unitComponent.getUnitHandle()) {
                                        this.buffEffect.attachToUnit(unitComponent.getUnitHandle(), "overhead");
                                }
                        }
                } catch (error) {
                        logger.error(`Failed to create buff effect: ${error}`);
                }
        }

        /**
         * 销毁特效
         */
        private destroyEffect(): void {
                if (this.buffEffect) {
                        this.buffEffect.destroy();
                        this.buffEffect = null;
                }
        }

        /**
         * Tick事件处理
         */
        private onTick(): void {
                if (!this.buffDataManager) return;

                const isActive = this.buffDataManager.get("isActive");
                const isPaused = this.buffDataManager.get("isPaused") || false;

                if (!isActive || isPaused) return;

                // 更新最后Tick时间
                this.buffDataManager.set("lastTickTime", GetGameTime());

                // 执行Buff效果
                this.executeBuffEffect();

                // 触发Tick事件
                this.owner.emit("BUFF_TICK", {
                        buffId: this.buffDataManager.get("id"),
                        buffName: this.buffDataManager.get("name"),
                        remainingTime: this.buffDataManager.get("remainingTime")
                });
        }

        /**
         * 更新剩余时间
         */
        private updateRemainingTime(deltaTime: number): void {
                if (!this.buffDataManager) return;

                const duration = this.buffDataManager.get("duration") || 0;
                const isPaused = this.buffDataManager.get("isPaused") || false;

                if (duration <= 0 || isPaused) return;

                const currentRemaining = this.buffDataManager.get("remainingTime") || 0;
                const newRemaining = Math.max(0, currentRemaining - deltaTime);

                this.buffDataManager.set("remainingTime", newRemaining);
        }

        /**
         * 更新特效
         */
        private updateEffect(): void {
                if (!this.buffEffect || !this.attachedUnit) return;

                // 特效已经附加到单位，会自动跟随单位移动
                // 这里可以添加其他特效更新逻辑，如颜色变化等
        }

        /**
         * 检查过期
         */
        private checkExpiration(): void {
                if (!this.buffDataManager) return;

                const duration = this.buffDataManager.get("duration") || 0;
                const remainingTime = this.buffDataManager.get("remainingTime") || 0;

                if (duration > 0 && remainingTime <= 0) {
                        this.expireBuff();
                }
        }

        /**
         * 设置事件监听器
         */
        private setupEventListeners(): void {
                // 监听Buff刷新事件
                this.owner.on(EventTypes.BUFF_REFRESH, (data) => {
                        this.refreshBuff(data.duration);
                });

                // 监听Buff堆叠事件
                this.owner.on(EventTypes.BUFF_STACK, (data) => {
                        this.addStack(data.stacks);
                });

                // 监听Buff移除事件
                this.owner.on(EventTypes.BUFF_REMOVE, (data) => {
                        this.removeBuff();
                });

                // 监听暂停/恢复事件
                this.owner.on(EventTypes.BUFF_PAUSE, (data) => {
                        this.pauseBuff();
                });

                this.owner.on(EventTypes.BUFF_RESUME, (data) => {
                        this.resumeBuff();
                });
        }

        /**
         * 应用Buff效果
         */
        private applyBuffEffect(): void {
                if (!this.buffDataManager || !this.attachedUnit) return;

                // 根据Buff名称应用不同的效果
                this.applySpecificBuffEffect();

                // 触发Buff应用事件
                this.owner.emit(EventTypes.BUFF_APPLIED, {
                        buffName: this.buffDataManager.get("name"),
                        target: this.attachedUnit,
                        stacks: this.buffDataManager.get("层数") || 1
                });

                logger.debug(`Buff applied: ${this.buffDataManager.get("name")} to unit`);
        }

        /**
         * 应用特定Buff效果
         */
        private applySpecificBuffEffect(): void {
                if (!this.buffDataManager) return;

                const buffName = this.buffDataManager.get("name");
                const buffType = this.buffDataManager.get("type");

                // 根据Buff类型应用效果
                switch (buffName) {
                        case "攻击力增加":
                                this.applyAttackBonus();
                                break;
                        case "移动速度减少":
                                this.applySpeedReduction();
                                break;
                        case "生命恢复":
                                this.applyHealthRegeneration();
                                break;
                        case "眩晕":
                                this.applyStunEffect();
                                break;
                        case "沉默":
                                this.applySilenceEffect();
                                break;
                        default:
                                // 通用效果处理
                                this.applyGenericEffect();
                                break;
                }
        }

        /**
         * 应用攻击力加成
         */
        private applyAttackBonus(): void {
                if (!this.buffDataManager) return;

                // 获取目标单位的数据管理器
                const targetUnitData = this.owner.data.unit;
                if (!targetUnitData) {
                        logger.warn("BuffComponent: 目标单位缺少unit数据管理器");
                        return;
                }

                // 从Buff的modifiers中获取攻击力加成值
                const modifiers = this.buffDataManager.get("modifiers") || {};
                const bonusAmount = modifiers.attackBonus || this.props.attackBonus || 10;
                const stackCount = this.buffDataManager.get("层数") || 1;

                // 计算总加成
                const totalBonus = bonusAmount * stackCount;

                // 应用到单位属性
                const currentAttack = targetUnitData.get("当前攻击力") || 0;
                targetUnitData.set("当前攻击力", currentAttack + totalBonus);

                // 记录修改器到Buff数据中
                modifiers.appliedAttackBonus = totalBonus;
                this.buffDataManager.set("modifiers", modifiers);

                logger.debug(`应用攻击力加成: +${totalBonus} (${bonusAmount} x ${stackCount})`);
        }

        /**
         * 应用速度减少
         */
        private applySpeedReduction(): void {
                if (!this.buffDataManager) return;

                // 获取目标单位的数据管理器
                const targetUnitData = this.owner.data.unit;
                if (!targetUnitData) {
                        logger.warn("BuffComponent: 目标单位缺少unit数据管理器");
                        return;
                }

                // 从Buff的modifiers中获取速度减少量
                const modifiers = this.buffDataManager.get("modifiers") || {};
                const reductionAmount = modifiers.speedReduction || 50;
                const stackCount = this.buffDataManager.get("层数") || 1;
                const totalReduction = reductionAmount * stackCount;

                const currentSpeed = targetUnitData.get("当前移速") || 270;
                const newSpeed = Math.max(0, currentSpeed - totalReduction);

                targetUnitData.set("当前移速", newSpeed);

                // 记录修改器到Buff数据中
                modifiers.appliedSpeedReduction = totalReduction;
                this.buffDataManager.set("modifiers", modifiers);

                logger.debug(`应用速度减少: -${totalReduction} (${reductionAmount} x ${stackCount})`);
        }

        /**
         * 应用生命恢复
         */
        private applyHealthRegeneration(): void {
                if (!this.buffDataManager || !this.timerComponent) return;

                // 获取目标单位的数据管理器
                const targetUnitData = this.owner.data.unit;
                if (!targetUnitData) {
                        logger.warn("BuffComponent: 目标单位缺少unit数据管理器");
                        return;
                }

                // 从Buff的modifiers中获取恢复量
                const modifiers = this.buffDataManager.get("modifiers") || {};
                const regenAmount = modifiers.healthRegen || this.props.healthRegen || 5;
                const stackCount = this.buffDataManager.get("层数") || 1;
                const totalRegen = regenAmount * stackCount;

                // 创建恢复计时器
                const regenTimerId = this.timerComponent.setInterval(() => {
                        const isActive = this.buffDataManager?.get("isActive");
                        const isPaused = this.buffDataManager?.get("isPaused");

                        if (isActive && !isPaused) {
                                const currentHealth = targetUnitData.get("当前生命值") || 0;
                                const maxHealth = targetUnitData.get("最终生命值") || 100;
                                const newHealth = Math.min(maxHealth, currentHealth + totalRegen);

                                targetUnitData.set("当前生命值", newHealth);

                                // 触发恢复事件
                                this.owner.emit("HEALTH_REGENERATED", {
                                        amount: totalRegen,
                                        newHealth: newHealth,
                                        buffId: this.buffDataManager?.get("id")
                                });
                        }
                }, 1.0);

                // 保存计时器ID到modifiers中
                modifiers.regenTimerId = regenTimerId;
                this.buffDataManager.set("modifiers", modifiers);

                logger.debug(`应用生命恢复: +${totalRegen}/秒 (${regenAmount} x ${stackCount})`);
        }

        /**
         * 应用眩晕效果
         */
        private applyStunEffect(): void {
                if (!this.buffDataManager) return;

                // 添加眩晕状态
                this.addState("stun", this.buffDataManager.get("duration"));

                // 触发眩晕事件
                this.owner.emit("UNIT_STUNNED", {
                        buffId: this.buffDataManager.get("id"),
                        duration: this.buffDataManager.get("duration")
                });

                logger.debug("应用眩晕效果");
        }

        /**
         * 应用沉默效果
         */
        private applySilenceEffect(): void {
                if (!this.buffDataManager) return;

                // 添加沉默状态
                this.addState("silence", this.buffDataManager.get("duration"));

                // 触发沉默事件
                this.owner.emit("UNIT_SILENCED", {
                        buffId: this.buffDataManager.get("id"),
                        duration: this.buffDataManager.get("duration")
                });

                logger.debug("应用沉默效果");
        }

        /**
         * 应用通用效果
         */
        private applyGenericEffect(): void {
                if (!this.buffDataManager) return;

                const modifiers = this.buffDataManager.get("modifiers") || {};
                const targetUnitData = this.owner.data.unit;

                if (!targetUnitData) return;

                // 应用属性修改器
                Object.entries(modifiers).forEach(([key, value]) => {
                        if (typeof value === "number") {
                                const currentValue = targetUnitData.get(key) || 0;
                                targetUnitData.set(key, currentValue + value);
                        }
                });

                logger.debug("应用通用Buff效果", modifiers);
        }

        /**
         * 移除Buff效果
         */
        private removeBuffEffect(): void {
                if (!this.buffDataManager || !this.attachedUnit) return;

                // 根据Buff名称移除对应的效果
                this.removeSpecificBuffEffect();

                // 触发Buff移除事件
                this.owner.emit(EventTypes.BUFF_REMOVED, {
                        buffName: this.buffDataManager.get("name"),
                        target: this.attachedUnit,
                        stacks: this.buffDataManager.get("层数") || 1
                });

                logger.debug(`Buff removed: ${this.buffDataManager.get("name")} from unit`);
        }

        /**
         * 移除特定Buff效果
         */
        private removeSpecificBuffEffect(): void {
                if (!this.buffDataManager || !this.attachedUnit) return;

                const buffName = this.buffDataManager.get("name");
                switch (buffName) {
                        case "攻击力增加":
                                this.removeAttackBonus();
                                break;
                        case "移动速度减少":
                                this.removeSpeedReduction();
                                break;
                        case "生命恢复":
                                this.removeHealthRegeneration();
                                break;
                }
        }

        /**
         * 移除攻击力加成
         */
        private removeAttackBonus(): void {
                if (!this.buffDataManager) return;

                // 获取目标单位的数据管理器
                const targetUnitData = this.owner.data.unit;
                if (!targetUnitData) {
                        logger.warn("BuffComponent: 目标单位缺少unit数据管理器");
                        return;
                }

                // 从Buff的modifiers中获取已应用的攻击力加成
                const modifiers = this.buffDataManager.get("modifiers") || {};
                const appliedBonus = modifiers.appliedAttackBonus || 0;

                if (appliedBonus > 0) {
                        const currentAttack = targetUnitData.get("当前攻击力") || 0;
                        targetUnitData.set("当前攻击力", currentAttack - appliedBonus);

                        // 清除修改器记录
                        delete modifiers.appliedAttackBonus;
                        this.buffDataManager.set("modifiers", modifiers);

                        logger.debug(`移除攻击力加成: -${appliedBonus}`);
                }
        }

        /**
         * 移除速度减少
         */
        private removeSpeedReduction(): void {
                if (!this.buffDataManager) return;

                // 获取目标单位的数据管理器
                const targetUnitData = this.owner.data.unit;
                if (!targetUnitData) {
                        logger.warn("BuffComponent: 目标单位缺少unit数据管理器");
                        return;
                }

                // 从Buff的modifiers中获取已应用的速度减少
                const modifiers = this.buffDataManager.get("modifiers") || {};
                const appliedReduction = modifiers.appliedSpeedReduction || 0;

                if (appliedReduction > 0) {
                        const currentSpeed = targetUnitData.get("当前移速") || 270;
                        targetUnitData.set("当前移速", currentSpeed + appliedReduction);

                        // 清除修改器记录
                        delete modifiers.appliedSpeedReduction;
                        this.buffDataManager.set("modifiers", modifiers);

                        logger.debug(`移除速度减少: +${appliedReduction}`);
                }
        }

        /**
         * 移除生命恢复
         */
        private removeHealthRegeneration(): void {
                if (!this.buffDataManager || !this.timerComponent) return;

                const modifiers = this.buffDataManager.get("modifiers") || {};
                const regenTimerId = modifiers.regenTimerId;

                if (regenTimerId) {
                        this.timerComponent.clearInterval(regenTimerId);
                        delete modifiers.regenTimerId;
                        this.buffDataManager.set("modifiers", modifiers);

                        logger.debug("移除生命恢复计时器");
                }
        }

        /**
         * Buff过期处理
         */
        private expireBuff(): void {
                if (!this.buffDataManager) return;

                // 更新Buff状态
                this.buffDataManager.set("isActive", false);
                this.buffDataManager.set("remainingTime", 0);

                // 移除Buff效果
                this.removeBuffEffect();

                // 清理计时器
                if (this.timerComponent) {
                        if (this.durationTimerId) {
                                this.timerComponent.clearTimeout(this.durationTimerId);
                                this.durationTimerId = null;
                        }
                        if (this.updateTimerId) {
                                this.timerComponent.clearInterval(this.updateTimerId);
                                this.updateTimerId = null;
                        }
                }

                // 触发过期事件
                this.owner.emit("BUFF_EXPIRED", {
                        buffId: this.buffDataManager.get("id"),
                        buffName: this.buffDataManager.get("name"),
                        buffType: this.buffDataManager.get("type")
                });

                logger.debug(`Buff expired: ${this.buffDataManager.get("name")}`);
        }

        // ==================== 公共API方法 ====================

        /**
         * 设置附加单位
         */
        setAttachedUnit(unit: Entity): void {
                this.attachedUnit = unit;

                // 重新创建特效
                this.destroyEffect();
                this.createEffect();
        }

        /**
         * 刷新Buff
         */
        refreshBuff(newDuration?: number): void {
                if (!this.buffDataManager) return;

                if (newDuration !== undefined) {
                        this.buffDataManager.set("duration", newDuration);
                }

                const duration = this.buffDataManager.get("duration") || 0;
                this.buffDataManager.set("remainingTime", duration);

                this.owner.emit(EventTypes.BUFF_REFRESHED, {
                        buffName: this.buffDataManager.get("name"),
                        newDuration: duration
                });

                logger.debug(`Buff refreshed: ${this.buffDataManager.get("name")}`);
        }

        /**
         * 添加堆叠
         */
        addStack(stacks: number = 1): boolean {
                if (!this.buffDataManager) return false;

                const isStackable = this.buffDataManager.get("isStackable") || false;
                if (!isStackable) return false;

                const currentStacks = this.buffDataManager.get("层数") || 1;
                const maxStacks = this.buffDataManager.get("maxStacks") || 1;
                const newStacks = currentStacks + stacks;

                if (newStacks > maxStacks) return false;

                const oldStacks = currentStacks;
                this.buffDataManager.set("层数", newStacks);

                // 重新应用效果（移除旧效果，应用新效果）
                this.removeBuffEffect();
                this.applyBuffEffect();

                this.owner.emit(EventTypes.BUFF_STACK_ADDED, {
                        buffName: this.buffDataManager.get("name"),
                        oldStacks,
                        newStacks: newStacks
                });

                return true;
        }

        /**
         * 移除堆叠
         */
        removeStack(stacks: number = 1): boolean {
                if (!this.buffDataManager) return false;

                const currentStacks = this.buffDataManager.get("层数") || 1;
                const newStacks = currentStacks - stacks;

                if (newStacks <= 0) {
                        this.removeBuff();
                        return true;
                }

                const oldStacks = currentStacks;
                this.buffDataManager.set("层数", newStacks);

                // 重新应用效果
                this.removeBuffEffect();
                this.applyBuffEffect();

                this.owner.emit(EventTypes.BUFF_STACK_REMOVED, {
                        buffName: this.buffDataManager.get("name"),
                        oldStacks,
                        newStacks: newStacks
                });

                return true;
        }

        /**
         * 移除Buff
         */
        removeBuff(): void {
                if (!this.buffDataManager) return;

                this.buffDataManager.set("isActive", false);

                this.owner.emit(EventTypes.BUFF_MANUALLY_REMOVED, {
                        buffName: this.buffDataManager.get("name"),
                        target: this.attachedUnit
                });

                this.destroy();
        }

        /**
         * 暂停Buff
         */
        pauseBuff(): void {
                if (!this.buffDataManager) return;

                this.buffDataManager.set("isPaused", true);

                logger.debug(`Buff paused: ${this.buffDataManager.get("name")}`);
        }

        /**
         * 恢复Buff
         */
        resumeBuff(): void {
                if (!this.buffDataManager) return;

                this.buffDataManager.set("isPaused", false);

                logger.debug(`Buff resumed: ${this.buffDataManager.get("name")}`);
        }

        /**
         * 获取剩余时间
         */
        getRemainingTime(): number {
                if (!this.buffDataManager) return 0;
                return this.buffDataManager.get("remainingTime") || 0;
        }

        /**
         * 获取当前堆叠数
         */
        getCurrentStacks(): number {
                if (!this.buffDataManager) return 1;
                return this.buffDataManager.get("层数") || 1;
        }

        /**
         * 检查Buff是否激活
         */
        isBuffActive(): boolean {
                if (!this.buffDataManager) return false;
                return this.buffDataManager.get("isActive") === true;
        }

        /**
         * 检查Buff是否暂停
         */
        isBuffPaused(): boolean {
                if (!this.buffDataManager) return false;
                return this.buffDataManager.get("isPaused") === true;
        }
}
