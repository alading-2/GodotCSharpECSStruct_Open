/** @noSelfInFile **/

import { Component } from "../../../../Component";
import { Entity } from "../../../../../Entity/Entity";
import { Logger } from "../../../../../../base/object/工具/logger";
import { EventTypes } from "../../../../../types/EventTypes";
import { AttributeConfig } from "../../../../../../base/object/接口定义管理系统";
import { UnitComponent } from "../UnitComponent";

const logger = Logger.createLogger("ExpComponent");

/**
 * 经验组件属性接口
 */
interface ExpComponentProps {
    // 初始等级
    initialLevel?: number;
    // 初始经验值
    initialExp?: number;
    // 最大等级限制
    maxLevel?: number;
    // 自定义经验计算公式
    customExpFormula?: (level: number) => number;
}

/**
 * 经验组件 - 管理单位的经验和等级系统
 * 
 * 【设计理念】
 * 将经验系统从UnitComponent中分离，提供专门的经验管理功能
 * 支持灵活的经验计算公式和等级限制
 * 
 * 【核心职责】
 * 1. 经验值的获取和管理
 * 2. 等级计算和升级逻辑
 * 3. 经验相关事件的触发
 * 4. 等级上限和经验公式的管理
 */
export class ExpComponent extends Component<ExpComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "ExpComponent";

    // 依赖的组件
    private unitComponent: UnitComponent | null = null;

    /**
     * 获取组件类型
     */
    static getType(): string {
        return ExpComponent.TYPE;
    }

    /**
     * 构造函数
     */
    constructor(owner: Entity, props?: ExpComponentProps) {
        super(owner, props);
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        this.props = {
            initialLevel: 1,
            initialExp: 0,
            maxLevel: AttributeConfig.最大等级,
        };
        // 获取依赖组件
        this.unitComponent = this.owner.getComponent(UnitComponent);
        if (!this.unitComponent) {
            logger.error(`ExpComponent初始化失败: 缺少UnitComponent依赖`);
            return;
        }

        // 初始化经验数据
        this.initializeExpData();

        // 监听经验相关的数据变化
        this.setupEventListeners();

        logger.debug(`经验组件已初始化，所属对象: ${this.owner.getId()}`);
    }

    /**
     * 销毁组件
     */
    destroy(): void {
        this.unitComponent = null;
        logger.debug(`经验组件已销毁，所属对象: ${this.owner.getId()}`);
    }

    // ==================== 初始化方法 ====================

    /**
     * 初始化经验数据
     */
    private initializeExpData(): void {
        const { initialLevel, initialExp } = this.props;

        // 设置初始等级
        if (!this.owner.data.unit.has("等级")) {
            this.owner.data.unit.set("等级", initialLevel!);
        }

        // 设置初始经验值
        if (!this.owner.data.unit.has("经验值")) {
            this.owner.data.unit.set("经验值", initialExp!);
        }

        // 设置总经验值
        if (!this.owner.data.unit.has("总经验值")) {
            const currentLevel = this.level;
            const totalExp = this.getTotalExpForLevel(currentLevel) + this.currentExp;
            this.owner.data.unit.set("总经验值", totalExp);
        }
    }

    /**
     * 设置事件监听器
     */
    private setupEventListeners(): void {
        // 监听经验值变化
        this.owner.on(EventTypes.DATA_PROPERTY_CHANGED, (data: any) => {
            if (data.key === "经验值") {
                this.checkLevelUp();
            }
        });
    }

    // ==================== 经验系统核心逻辑 ====================

    /**
     * 获取当前等级
     */
    get level(): number {
        return this.owner.data.unit.get("等级") || 1;
    }

    /**
     * 设置等级
     */
    set level(newLevel: number) {
        const oldLevel = this.level;
        const maxLevel = this.props.maxLevel!;

        // 限制等级范围
        newLevel = Math.max(1, Math.min(newLevel, maxLevel));

        if (newLevel !== oldLevel) {
            this.owner.data.unit.set("等级", newLevel);

            // 触发等级变更事件
            this.owner.emit(EventTypes.UNIT_LEVEL_CHANGED, {
                unit: this.unitComponent,
                oldLevel: oldLevel,
                newLevel: newLevel,
                experience: this.currentExp
            } as EventTypes.UnitLevelEventData);

            logger.debug(`单位等级变化: ${oldLevel} -> ${newLevel}`);
        }
    }

    /**
     * 获取当前经验值
     */
    get currentExp(): number {
        return this.owner.data.unit.get("经验值") || 0;
    }

    /**
     * 获取总经验值
     */
    get totalExp(): number {
        return this.owner.data.unit.get("总经验值") || 0;
    }

    /**
     * 获取升级所需经验值
     */
    get expToNextLevel(): number {
        return this.getExpForLevel(this.level + 1);
    }

    /**
     * 获取当前等级进度百分比 (0-1)
     */
    get levelProgress(): number {
        const expForNextLevel = this.expToNextLevel;
        if (expForNextLevel <= 0) return 1; // 已达到最大等级
        return Math.min(1, this.currentExp / expForNextLevel);
    }

    /**
     * 检查是否可以升级
     */
    get canLevelUp(): boolean {
        return this.level < this.props.maxLevel! && this.currentExp >= this.expToNextLevel;
    }

    /**
     * 检查是否达到最大等级
     */
    get isMaxLevel(): boolean {
        return this.level >= this.props.maxLevel!;
    }

    // ==================== 经验计算方法 ====================

    /**
     * 获取指定等级升级所需的经验值
     * @param level 目标等级
     * @returns 升级所需经验值
     */
    getExpForLevel(level: number): number {
        if (this.props.customExpFormula) {
            return this.props.customExpFormula(level);
        }

        // 默认经验公式
        return (level - 1) * (AttributeConfig.经验等级系数 || 100) + (AttributeConfig.经验固定系数 || 50);
    }

    /**
     * 获取达到指定等级需要的总经验值
     * @param level 目标等级
     * @returns 总经验值
     */
    getTotalExpForLevel(level: number): number {
        let totalExp = 0;
        for (let i = 1; i < level; i++) {
            totalExp += this.getExpForLevel(i + 1);
        }
        return totalExp;
    }

    // ==================== 经验操作方法 ====================

    /**
     * 添加经验值
     * @param expAmount 经验值数量
     * @param source 经验来源（可选）
     */
    addExp(expAmount: number, source?: string): void {
        if (expAmount <= 0 || this.isMaxLevel) {
            return;
        }

        // 记录旧的经验值
        const oldExp = this.currentExp;
        // 记录旧的总经验值
        const oldTotalExp = this.totalExp;
        // 记录旧的等级
        const oldLevel = this.level;

        // 更新总经验值
        this.owner.data.unit.set("总经验值", oldTotalExp + expAmount);

        // 计算新获得的经验值后的当前经验值总和
        let newCurrentExp = oldExp + expAmount;
        // 记录新的等级,初始为当前等级
        let newLevel = oldLevel;

        // 检查是否需要升级
        while (newLevel < this.props.maxLevel! && newCurrentExp >= this.getExpForLevel(newLevel + 1)) {
            newCurrentExp -= this.getExpForLevel(newLevel + 1);
            newLevel++;
        }

        // 更新数据
        this.owner.data.unit.set("经验值", newCurrentExp);
        if (newLevel !== oldLevel) {
            this.level = newLevel;
        }

        // 触发经验获得事件
        this.owner.emit(EventTypes.UNIT_EXP_GAINED, {
            unit: this.unitComponent,
            expAmount,
            oldExp,
            newExp: newCurrentExp,
            oldLevel,
            newLevel,
            source
        } as EventTypes.UnitExpGainedEventData);

        logger.debug(`单位获得经验: +${expAmount} (来源: ${source || '未知'})`);
    }

    /**
     * 设置经验值
     * @param expAmount 经验值数量
     */
    setExp(expAmount: number): void {
        if (expAmount < 0) {
            expAmount = 0;
        }

        const oldExp = this.currentExp;
        const difference = expAmount - oldExp;

        if (difference !== 0) {
            this.addExp(difference, "直接设置");
        }
    }

    /**
     * 直接设置等级（会自动调整经验值）
     * @param targetLevel 目标等级
     */
    setLevel(targetLevel: number): void {
        const maxLevel = this.props.maxLevel!;
        targetLevel = Math.max(1, Math.min(targetLevel, maxLevel));

        if (targetLevel === this.level) {
            return;
        }

        const oldLevel = this.level;
        const totalExpForLevel = this.getTotalExpForLevel(targetLevel);

        // 设置等级和经验
        this.owner.data.unit.set("等级", targetLevel);
        this.owner.data.unit.set("经验值", 0);
        this.owner.data.unit.set("总经验值", totalExpForLevel);

        // 触发等级变更事件
        this.owner.emit(EventTypes.UNIT_LEVEL_CHANGED, {
            unit: this.unitComponent,
            oldLevel: oldLevel,
            newLevel: targetLevel,
            experience: this.currentExp
        } as EventTypes.UnitLevelEventData);

        logger.debug(`直接设置单位等级: ${oldLevel} -> ${targetLevel}`);
    }

    /**
     * 检查并处理升级
     */
    private checkLevelUp(): void {
        while (this.canLevelUp) {
            // 记录当前等级
            const oldLevel = this.level;
            // 获取升级所需经验值
            const expForNextLevel = this.expToNextLevel;

            // 扣除升级所需经验
            const newCurrentExp = this.currentExp - expForNextLevel;
            this.owner.data.unit.set("经验值", newCurrentExp);

            // 提升等级
            this.level = oldLevel + 1;

            // 触发升级事件
            this.owner.emit(EventTypes.UNIT_LEVEL_UP, {
                unit: this.unitComponent,
                oldLevel: oldLevel,
                newLevel: this.level
            } as EventTypes.UnitLevelUpEventData);

            logger.info(`单位升级: ${oldLevel} -> ${this.level}`);
        }
    }

    // ==================== 工具方法 ====================

    /**
     * 获取经验系统的调试信息
     */
    getDebugInfo(): Record<string, any> {
        return {
            level: this.level,
            currentExp: this.currentExp,
            totalExp: this.totalExp,
            expToNextLevel: this.expToNextLevel,
            levelProgress: this.levelProgress,
            canLevelUp: this.canLevelUp,
            isMaxLevel: this.isMaxLevel,
            maxLevel: this.props.maxLevel
        };
    }

    /**
     * 重置经验系统
     */
    reset(): void {
        this.owner.data.unit.set("等级", this.props.initialLevel!);
        this.owner.data.unit.set("经验值", this.props.initialExp!);
        this.owner.data.unit.set("总经验值", this.getTotalExpForLevel(this.props.initialLevel!) + this.props.initialExp!);

        logger.debug(`经验系统已重置`);
    }
}