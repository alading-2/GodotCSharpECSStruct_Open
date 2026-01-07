/** @noSelfInFile **/

import { Component } from "../../../../Component";
import { Position } from "../../../../../../base/math/Position";
import { Entity } from "../../../../../Entity/Entity";
import { TimerComponent } from "../../../TimerComponent";
import { EventTypes } from "../../../../../types/EventTypes";
import { Logger } from "../../../../../../base/object/工具/logger";
import { AttributeConfig } from "../../../../../../base/object/接口定义管理系统";
import { MyMath } from "../../../../../../base/math/MyMath";


const logger = Logger.createLogger("LifecycleComponent");

/**
 * 生命周期状态枚举
 */
export enum LifecycleState {
    SPAWNING = "spawning",       // 生成中
    ALIVE = "alive",             // 存活
    DYING = "dying",             // 死亡中
    DEAD = "dead",               // 已死亡
    REVIVING = "reviving",       // 复活中
    DESTROYED = "destroyed"      // 已销毁
}

/**
 * 死亡类型枚举
 */
export enum DeathType {
    NORMAL = "normal",           // 普通单位死亡
    HERO = "hero",               // 英雄死亡（假死）
    INSTANT = "instant",         // 瞬间死亡
    SACRIFICE = "sacrifice"      // 献祭死亡，有死亡特效
}

/**
 * 生命周期组件配置属性接口
 * 
 * 定义了LifecycleComponent的所有可配置参数，支持灵活的单位生命周期管理。
 * 
 * ## 配置示例
 * 
 * ### 普通单位配置
 * ```typescript
 * const normalUnitProps: LifecycleComponentProps = {
 *     maxLifeTime: 120,           // 2分钟后自动死亡
 *     canRevive: false,           // 普通单位不能复活
 *     reviveTime: 0,              // 不需要复活时间
 *     invulnerabilityDuration: 0  // 不需要无敌时间
 * };
 * ```
 * 
 * ### 英雄单位配置
 * ```typescript
 * const heroProps: LifecycleComponentProps = {
 *     maxLifeTime: -1,            // 永不自然死亡
 *     canRevive: true,            // 英雄可以复活
 *     reviveTime: 30,             // 复活需要30秒
 *     invulnerabilityDuration: 5  // 复活后5秒无敌
 * };
 * ```
 * 
 * ### 召唤单位配置
 * ```typescript
 * const summonProps: LifecycleComponentProps = {
 *     maxLifeTime: 60,            // 1分钟后自动消失
 *     canRevive: false,           // 召唤单位不能复活
 *     reviveTime: 0,
 *     invulnerabilityDuration: 2  // 召唤后短暂无敌
 * };
 * ```
 */
export interface LifecycleComponentProps {
    /** 
     * 最大生存时间（秒）
     * - 正数：单位在指定时间后自动死亡
     * - -1：永久存在，不会自然死亡
     * - 0：立即死亡（一般不使用）
     * @default -1
     */
    maxLifeTime?: number;

    /** 
     * 是否可以复活
     * - true：单位死亡后可以通过revive()方法复活
     * - false：单位死亡后无法复活
     * @default true
     */
    canRevive?: boolean;

    /** 
     * 最大复活次数
     * - 正数：限制复活次数
     * - -1：无限复活
     * - 0：不能复活（等同于canRevive: false）
     * @default -1
     */
    maxReviveCount?: number;

    /** 
     * 死亡类型
     * - NORMAL：普通单位死亡
     * - HERO：英雄死亡（假死）
     * - INSTANT：瞬间死亡
     * - SACRIFICE：献祭死亡，有死亡特效
     * @default DeathType.NORMAL
     */
    deathType?: DeathType;

    /** 
     * 复活所需时间（秒）
     * - 只有当canRevive为true时才有效
     * - 复活过程中单位处于reviving状态
     * - 时间到达后自动转为alive状态
     * @default AttributeConfig.英雄复活时间
     */
    reviveTime?: number;

    /** 
     * 复活后无敌时间（秒）
     * - 复活完成后的临时保护时间
     * - 在此期间单位不会受到伤害
     * - 用于防止复活后立即被杀死
     * @default 3
     */
    invulnerabilityDuration?: number;
}

/**
 * 生命周期组件 - 单位生命周期状态管理
 * 
 * 这个组件专门负责管理单位的生命周期状态转换和相关逻辑，遵循现代游戏架构的单一职责原则。
 * 它只关注生命周期管理，不包含其他功能（如恢复系统已移至RecoveryComponent）。
 * 
 * ## 核心功能
 * - 生命周期状态管理：spawning → alive → dying → dead → reviving → alive
 * - 死亡类型处理：普通死亡、英雄死亡、瞬间死亡、献祭死亡
 * - 复活机制：自动复活、手动复活、复活时间计算
 * - 无敌状态：复活后临时无敌保护
 * - 生命时间：单位最大存活时间管理
 * 
 * ## 状态流转图
 * ```
 * spawning → alive → dying → dead → reviving → alive
 *     ↓        ↓       ↓       ↓        ↓
 *   初始化   正常状态  死亡中   已死亡   复活中
 * ```
 * 
 * ## 使用示例
 * 
 * ### 基础使用
 * ```typescript
 * // 创建生命周期组件
 * const lifecycle = new LifecycleComponent(entity, {
 *     maxLifeTime: 60,        // 60秒后自动死亡
 *     canRevive: true,        // 允许复活
 *     reviveTime: 10,         // 复活需要10秒
 *     invulnerabilityDuration: 3  // 复活后3秒无敌
 * });
 * 
 * // 检查状态
 * if (lifecycle.isAlive()) {
 *     console.log("单位存活中");
 * }
 * 
 * // 杀死单位
 * lifecycle.kill(DeathType.NORMAL);
 * 
 * // 复活单位
 * lifecycle.revive();
 * ```
 * 
 * ### 事件监听
 * ```typescript
 * // 监听死亡事件
 * entity.on(EventTypes.UNIT_DEATH, (data) => {
 *     console.log(`单位死亡，类型：${data.deathType}`);
 * });
 * 
 * // 监听复活事件
 * entity.on(EventTypes.UNIT_REVIVE, (data) => {
 *     console.log(`单位复活完成`);
 * });
 * ```
 * 
 * ### 高级用法
 * ```typescript
 * // 英雄单位配置
 * const heroLifecycle = new LifecycleComponent(heroEntity, {
 *     canRevive: true,
 *     reviveTime: 30,         // 英雄复活时间较长
 *     invulnerabilityDuration: 5,  // 英雄复活后无敌时间较长
 *     maxLifeTime: -1         // 英雄永不自然死亡
 * });
 * 
 * // 召唤单位配置
 * const summonLifecycle = new LifecycleComponent(summonEntity, {
 *     maxLifeTime: 30,        // 30秒后自动消失
 *     canRevive: false,       // 召唤单位不能复活
 * });
 * ```
 * 
 * ## 架构设计
 * 
 * 本组件遵循现代游戏架构原则：
 * - **单一职责**：只负责生命周期状态管理
 * - **数据分离**：状态数据存储在Entity.data中
 * - **事件驱动**：通过事件系统通知状态变化
 * - **组件化**：与其他组件（如RecoveryComponent）解耦
 * 
 * ## 注意事项
 * 
 * 1. **状态一致性**：确保状态转换的原子性，避免中间状态
 * 2. **计时器管理**：组件销毁时会自动清理所有计时器
 * 3. **事件触发**：状态变化会触发相应事件，便于其他系统响应
 * 4. **性能考虑**：避免频繁的状态检查，使用事件驱动模式
 * @author 游戏架构师
 * @version 2.0.0
 * @since 1.0.0
 */
export class LifecycleComponent extends Component<LifecycleComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "LifecycleComponent";

    // === 核心状态 ===
    /** 当前生命周期状态 */
    private currentState: LifecycleState = LifecycleState.SPAWNING;
    /** 上一个生命周期状态 */
    private previousState: LifecycleState = LifecycleState.SPAWNING;

    // === 时间戳记录 ===
    /** 实体创建时的游戏时间 */
    private creationTime: number = 0;
    /** 实体死亡时的游戏时间 */
    private deathTime: number = 0;
    /** 最后一次复活的游戏时间 */
    private lastReviveTime: number = 0;
    /** 最后一次状态改变的游戏时间 */
    private lastStateChangeTime: number = 0;

    // === 生命周期配置 ===
    private maxLifeTime: number = -1;        // 最大生存时间，-1表示永久
    private canRevive: boolean = true;       // 是否可以复活
    private maxReviveCount: number = -1;     // 最大复活次数，-1表示无限
    private currentReviveCount: number = 0;  // 当前复活次数

    // === 死亡和复活配置 ===
    /** 实体的死亡类型 */
    private deathType: DeathType = DeathType.NORMAL;
    /** 复活所需时间(秒) */
    private reviveTime: number = 10;         // 复活时间（秒）
    /** 复活后的无敌持续时间(秒) */
    private invulnerabilityDuration: number = 3; // 复活后无敌时间

    // === 计时器管理 ===
    /** 计时器组件引用 */
    private timerComponent: TimerComponent;
    /** 生命周期计时器ID */
    private lifeTimerId: string | null = null;
    /** 复活计时器ID */
    private reviveTimerId: string | null = null;
    /** 无敌状态计时器ID */
    private invulnerabilityTimerId: string | null = null;

    // === 状态标志 ===
    /** 是否处于无敌状态 */
    private isInvulnerable: boolean = false;
    /** 是否处于暂停状态 */
    private isPaused: boolean = false;
    /** 生命周期组件是否已销毁 */
    private isDestroyed_lifecycle: boolean = false;


    /**
     * 获取组件类型
     */
    static getType(): string {
        return LifecycleComponent.TYPE;
    }

    /**
     * 构造函数
     * @param owner 所属实体
     * @param props 组件属性
     */
    constructor(owner: Entity, props: LifecycleComponentProps = {}) {
        super(owner, props);

        // 获取TimerComponent
        this.timerComponent = this.owner.component.timer;

        logger.debug(`实体 ${this.owner.getId()} 的生命周期组件已创建`);
    }

    /**
     * 组件初始化
     * 设置生命周期状态机和事件监听
     */
    initialize(): void {

        // 初始化配置
        this.maxLifeTime = this.props.maxLifeTime ?? -1; // 最大生存时间，-1表示永久
        this.canRevive = this.props.canRevive ?? true;   // 是否可以复活
        this.maxReviveCount = this.props.maxReviveCount ?? -1; // 最大复活次数，-1表示无限
        this.deathType = this.props.deathType ?? DeathType.NORMAL; // 死亡类型
        this.reviveTime = this.props.reviveTime ?? AttributeConfig.英雄复活时间; // 复活所需时间
        this.invulnerabilityDuration = this.props.invulnerabilityDuration ?? 3; // 复活后无敌时间


        // 记录创建时间
        this.creationTime = os.time();
        this.lastStateChangeTime = this.creationTime;
        // 设置初始状态为存活
        this.changeState(LifecycleState.ALIVE);

        // 如果设置了最大生存时间，启动生命周期计时器
        if (this.maxLifeTime > 0) {
            this.startLifeTimer();
        }

        // 设置事件监听
        this.setupEventListeners();



        logger.debug(`实体 ${this.owner.getId()} 的生命周期组件已初始化`);
    }

    /**
     * 设置事件监听器
     */
    private setupEventListeners(): void {
        // 监听War3单位死亡事件
        this.owner.on(EventTypes.UNIT_DEATH, (data: EventTypes.UnitDeathEventData) => {
            this.handleUnitDeath(data.killer.getOwner());
        });

        // 监听伤害事件（用于检测死亡）
        this.owner.on(EventTypes.UNIT_TAKE_DAMAGE, (data) => {
            this.checkDeathCondition();
        });

        // 监听属性变化（生命值变化）
        this.owner.on(EventTypes.DATA_PROPERTY_CHANGED, (data: EventTypes.PropertyChangedEventData) => {
            if (data.key === "当前生命值" && data.newValue <= 0) {
                this.checkDeathCondition();
            }
        });
    }
    /**
     * 处理War3单位死亡事件
     * @param killer 击杀者
     */
    private handleUnitDeath(killer?: Entity): void {
        if (this.currentState === LifecycleState.DYING || this.currentState === LifecycleState.DEAD) {
            return; // 已经在处理死亡过程中
        }

        this.startDeathProcess(killer);
    }

    /**
     * 检查死亡条件
     */
    private checkDeathCondition(): void {
        if (!this.isAlive()) return;

        const unitComp = this.owner.component.unit;
        const unitData = this.owner.data.unit;
        if (!unitData) return;

        const currentHp = unitData.get("当前生命值");
        if (currentHp <= 0) {
            // 确定死亡类型
            const deathType = unitComp.isHero ? DeathType.HERO : DeathType.NORMAL;
            this.kill(undefined, deathType);
        }
    }


    // ============================= 状态管理系统 =============================

    /**
     * 改变生命周期状态
     * @param newState 新状态
     */
    private changeState(newState: LifecycleState): void {
        if (this.currentState === newState) return;

        const oldState = this.currentState;
        this.previousState = oldState;
        this.currentState = newState;
        this.lastStateChangeTime = os.time();

        // 触发状态变化事件
        this.owner.emit(EventTypes.UNIT_STATE_CHANGED, {
            entity: this.owner,
            oldState,
            newState,
            timestamp: this.lastStateChangeTime
        });

        // 执行状态进入逻辑
        this.onStateEnter(newState, oldState);

        logger.debug(`实体 ${this.owner.getId()} 状态改变: ${oldState} -> ${newState}`);
    }

    /**
     * 状态进入处理
     * @param newState 新状态
     * @param oldState 旧状态
     */
    private onStateEnter(newState: LifecycleState, oldState: LifecycleState): void {
        switch (newState) {
            case LifecycleState.ALIVE:// 存活
                this.onEnterAlive();
                break;
            case LifecycleState.DYING:// 死亡中
                this.onEnterDying();
                break;
            case LifecycleState.DEAD:// 已死亡
                this.onEnterDead();
                break;
            case LifecycleState.REVIVING:// 复活中
                this.onEnterReviving();
                break;
            case LifecycleState.DESTROYED:// 已销毁
                this.onEnterDestroyed();
                break;
        }
    }

    // 存活状态处理逻辑
    private onEnterAlive(): void {
        const unitData = this.owner.data.unit;
        if (unitData) {
            unitData.set("死亡", false);
        }
    }

    // 死亡中状态处理逻辑
    private onEnterDying(): void {
        // 死亡中状态的处理
    }

    // 已死亡状态处理逻辑
    private onEnterDead(): void {
        const unitData = this.owner.data.unit;
        if (unitData) {
            unitData.set("死亡", true);
        }
    }

    // 复活中状态处理逻辑
    private onEnterReviving(): void {
        // 复活中状态的处理
    }

    // 销毁状态处理逻辑
    private onEnterDestroyed(): void {
        this.isDestroyed_lifecycle = true;
        this.removeAllTimers();
    }


    // ============================= 死亡系统 =============================

    /**
     * 杀死单位
     * @param killer 击杀者实体
     * @param deathType 死亡类型
     */
    public kill(killer?: Entity, deathType?: DeathType): void {
        if (!this.isAlive()) return;
        const unitComp = this.owner.component.unit;
        // 设置死亡类型
        if (deathType) {
            this.deathType = deathType;
        } else {
            if (unitComp.isHero) {
                this.deathType = DeathType.HERO;
            } else {
                this.deathType = DeathType.NORMAL;
            }
        }

        // 开始死亡过程
        this.startDeathProcess(killer);
    }

    /**
     * 开始死亡过程
     * @param killer 击杀者
     */
    private startDeathProcess(killer?: Entity): void {
        // 改变状态为死亡中
        this.changeState(LifecycleState.DYING);

        this.deathTime = os.time();

        // 根据死亡类型执行不同的死亡逻辑
        switch (this.deathType) {
            case DeathType.NORMAL:
                this.executeNormalDeath(killer);    // 普通死亡
                break;
            case DeathType.HERO:      // 英雄死亡
                this.executeHeroDeath(killer);
                break;
            case DeathType.INSTANT:   // 瞬时死亡
                this.executeInstantDeath(killer);
                break;
            case DeathType.SACRIFICE:   // 牺牲死亡
                this.executeSacrificeDeath(killer);
                break;
            default:
                break;
        }
    }



    /**
     * 执行普通死亡
     * @param killer 击杀者
     */
    private executeNormalDeath(killer?: Entity): void {
        const unitComponent = this.owner.component.unit;
        const unitData = this.owner.data.unit;

        if (unitData) {
            unitData.set("当前生命值", 2);
            unitData.set("死亡", true);
        }

        // 设置单位外观
        if (unitComponent) {
            unitComponent.SetColor?.(150, 150, 150, 100); // 变透明
        }

        // 杀死War3单位
        const unitHandle = unitComponent.getUnitHandle();
        if (unitHandle) {
            jasscj.KillUnit(unitHandle);
        }

        // 完成死亡
        this.completeDeath(killer);
    }

    /**
     * 完成死亡过程
     * @param killer 击杀者
     */
    private completeDeath(killer?: Entity): void {
        // 改变状态为已死亡
        this.changeState(LifecycleState.DEAD);

        // 停止所有计时器
        this.removeAllTimers();

        // 触发死亡完成事件
        this.owner.emit(EventTypes.UNIT_DEATH, {
            unit: this.owner,
            killer: killer,
            deathType: this.deathType,
            deathTime: this.deathTime
        });
        //引用计数-1，this.useCount == 0时移除单位
        this.owner.component.unit.unitDeath();

        logger.debug(`实体 ${this.owner.getId()} 死亡完成`);
    }

    /**
     * 执行英雄死亡（假死）
     * @param killer 击杀者
     */
    private executeHeroDeath(killer?: Entity): void {
        const unitComponent = this.owner.component.unit;
        const unitData = this.owner.data.unit;
        const attributeData = this.owner.data.attr;

        if (unitData) {
            unitData.set("当前生命值", 2);
            unitData.set("死亡", true);
            unitData.set("死亡位置", unitComponent?.position);
            unitData.add("死亡次数", 1);
        }

        // 设置单位状态
        if (unitComponent) {
            unitComponent.Action?.("death"); // 死亡动画
            unitComponent.pause = true;    // 暂停
            unitComponent.invulnerable = true; // 无敌
            unitComponent.SetColor?.(100, 100, 100, 100); // 颜色变透明
        }

        // 创建墓碑特效
        const position = unitComponent?.position || new Position(0, 0);
        Effect.New({
            path: "天降墓碑.mdx",
            position: position,
            life: this.reviveTime,
            size: 0.7,
        });

        // 开始复活倒计时
        this.startReviveCountdown();
    }

    /**
     * 开始复活倒计时
     */
    private startReviveCountdown(): void {
        const interval = 0.1;
        const totalTime = this.reviveTime;

        this.reviveTimerId = this.timerComponent.CountDown(
            totalTime,
            interval,
            (time, progress) => {   //callback
                progress += interval;

                // 更新生命值（复活进度）
                const unitData = this.owner.data.unit;
                const attributeData = this.owner.data.attr;

                if (unitData && attributeData) {
                    const maxHp = attributeData.get("最终生命值");
                    const currentHp = maxHp * progress;
                    unitData.set("当前生命值", currentHp);
                }
            },
            () => { // end 结束回调
                // 复活完成
                this.reviveTimerId = null;
                this.revive(this.owner.data.unit.get("死亡位置"));
            },

        );
    }

    /**
     * 执行瞬间死亡
     * @param killer 击杀者
     */
    private executeInstantDeath(killer?: Entity): void {
        this.executeNormalDeath(killer);
        // 瞬间死亡不允许复活
        this.canRevive = false;
    }

    /**
     * 执行献祭死亡
     * @param killer 击杀者
     */
    private executeSacrificeDeath(killer?: Entity): void {
        // 献祭死亡有特殊的视觉效果
        const unitComponent = this.owner.component.unit;
        const position = unitComponent?.position || new Position(0, 0);

        // 献祭特效
        Effect.New({
            path: "Abilities\\Spells\\Undead\\Sacrifice\\SacrificeTarget.mdl",
            position: position,
            life: 2,
        });

        this.executeNormalDeath(killer);
    }



    // ============================= 复活系统 =============================

    /**
     * 复活单位
     * @param position 复活位置，可选
     * @param force 是否强制复活（忽略复活限制）
     */
    public revive(position: Position, force: boolean = false): boolean {
        if (!this.canReviveNow() && !force) {
            logger.warn(`实体 ${this.owner.getId()} 当前无法复活`);
            return false;
        }

        // 开始复活过程
        this.startReviveProcess(position);
        return true;
    }

    /**
     * 检查是否可以复活
     * @returns 是否可以复活
     */
    private canReviveNow(): boolean {
        // 单位死亡，且可以复活，且复活次数无限制或未达上限
        return this.isDead() &&
            this.canRevive &&
            (this.maxReviveCount === -1 || this.currentReviveCount < this.maxReviveCount);
    }

    /**
     * 开始复活过程
     * @param position 复活位置
     */
    private startReviveProcess(position: Position): void {
        this.changeState(LifecycleState.REVIVING);

        // 执行复活逻辑
        this.executeRevive(position);
    }

    /**
     * 执行复活
     * @param position 复活位置
     */
    private executeRevive(position: Position): void {
        const unitComponent = this.owner.component.unit;
        const unitData = this.owner.data.unit;
        const attributeData = this.owner.data.attr;

        // 恢复单位状态
        unitComponent.pause = false;       // 取消暂停
        unitComponent.invulnerable = false; // 取消无敌（临时）
        unitComponent.SetColor?.(255, 255, 255, 255); // 恢复颜色
        unitComponent.Action?.("stand");     // 站立动画

        // 恢复满血满蓝
        unitData.set("当前生命值", attributeData.get("最终生命值"));
        unitData.set("当前魔法值", attributeData.get("最终魔法值"));
        unitData.set("死亡", false);


        // 复活特效
        Effect.New({
            path: "Abilities\\Spells\\Human\\ReviveHuman\\ReviveHuman.mdl",
            unit: unitComponent,
            body: Body.原点,
            life: 0.5,
        });

        Effect.New({
            path: War3EffectModel.技能.神圣护甲,
            unit: unitComponent,
            body: Body.原点,
            life: 3,
        });


        // 完成复活
        this.completeRevive(position);
    }

    /**
     * 完成复活过程
     * @param position 复活位置
     */
    private completeRevive(position: Position): void {
        this.lastReviveTime = os.time();
        this.currentReviveCount++;

        // 改变状态为存活
        this.changeState(LifecycleState.ALIVE);

        // 设置复活后无敌
        this.setInvulnerability(this.invulnerabilityDuration);

        // 重新启动生命周期计时器
        // if (this.maxLifeTime > 0) {
        //     this.startLifeTimer();
        // }

        // 触发复活事件
        this.owner.emit(EventTypes.UNIT_REVIVED, {
            unit: this.owner,
            position: position,
            reviveTime: this.lastReviveTime,
            reviveCount: this.currentReviveCount
        });

        logger.debug(`实体 ${this.owner.getId()} 在位置 (${position.X}, ${position.Y}) 复活`);
    }



    // ============================= 辅助方法 =============================

    /**
     * 设置无敌状态
     * @param duration 无敌持续时间（秒）
     */
    private setInvulnerability(duration: number): void {
        if (duration <= 0) return;

        this.isInvulnerable = true;
        const unitComponent = this.owner.component.unit;
        if (unitComponent) {
            unitComponent.invulnerable = true;
        }

        // 设置无敌计时器
        this.invulnerabilityTimerId = this.timerComponent.RunLater(duration, () => {
            this.isInvulnerable = false;
            if (unitComponent) {
                unitComponent.invulnerable = false;
            }
            this.invulnerabilityTimerId = null;

            logger.debug(`实体 ${this.owner.getId()} 无敌状态结束`);
        });

        logger.debug(`实体 ${this.owner.getId()} 将无敌 ${duration} 秒`);
    }

    /**
     * 计算默认复活时间
     * @returns 复活时间（秒）
     */
    private calculateDefaultReviveTime(): number {
        // 根据英雄等级或其他因素计算复活时间
        const unitData = this.owner.data.unit;
        if (unitData && unitData.get("类别") === "英雄") {
            const level = unitData.get("等级") || 1;
            let reviveTime = AttributeConfig.英雄复活时间 || 10;
            reviveTime = MyMath.Clamp(reviveTime + level * 0.5, 7, 20);
            return reviveTime;
        }
        return 10; // 默认复活时间
    }

    /**
     * 启动生命周期计时器
     */
    private startLifeTimer(): void {
        if (this.maxLifeTime <= 0) return;

        this.lifeTimerId = this.timerComponent.RunLater(this.maxLifeTime, () => {
            this.kill(undefined, DeathType.NORMAL);
            this.lifeTimerId = null;
        });

        logger.debug(`实体 ${this.owner.getId()} 生命周期计时器启动: ${this.maxLifeTime} 秒`);
    }

    /**
     * 停止所有计时器
     */
    private removeAllTimers(): void {
        if (this.lifeTimerId) {
            this.timerComponent.removeTimer(this.lifeTimerId);
            this.lifeTimerId = null;
        }

        if (this.reviveTimerId) {
            this.timerComponent.removeTimer(this.reviveTimerId);
            this.reviveTimerId = null;
        }

        if (this.invulnerabilityTimerId) {
            this.timerComponent.removeTimer(this.invulnerabilityTimerId);
            this.invulnerabilityTimerId = null;

        }
    }


    // ============================= 公共API =============================

    /**
     * 检查单位是否存活
     * @returns 返回单位存活状态
     */
    public isAlive(): boolean {
        return this.currentState === LifecycleState.ALIVE || this.currentState === LifecycleState.SPAWNING;
    }

    /**
     * 检查单位是否已死亡
     * @returns 返回单位死亡状态
     */
    public isDead(): boolean {
        return this.currentState === LifecycleState.DEAD;
    }

    /**
     * 检查单位是否正在复活
     * @returns 返回单位复活状态
     */
    public isReviving(): boolean {
        return this.currentState === LifecycleState.REVIVING;
    }

    /**
     * 获取当前生命周期状态
     * @returns 当前状态
     */
    public getState(): LifecycleState {
        return this.currentState;
    }

    /**
     * 获取上一个生命周期状态
     * @returns 上一个状态
     */
    public getPreviousState(): LifecycleState {
        return this.previousState;
    }

    /**
     * 检查是否处于无敌状态
     * @returns 无敌状态
     */
    public isInvulnerableState(): boolean {
        return this.isInvulnerable;
    }

    /**
     * 设置最大生存时间
     * @param seconds 生存时间（秒），-1表示永久
     */
    public setMaxLifeTime(seconds: number): void {
        this.maxLifeTime = seconds;

        // 重新启动计时器
        if (this.lifeTimerId) {
            this.timerComponent.removeTimer(this.lifeTimerId);
            this.lifeTimerId = null;
        }

        if (seconds > 0 && this.isAlive()) {
            this.startLifeTimer();
        }
    }

    /**
     * 获取剩余生命时间
     * @returns 剩余生命时间(秒)，-1表示永久单位
     */
    public getRemainingLifeTime(): number {
        if (this.maxLifeTime <= 0) return -1; // 永久单位

        const elapsed = os.time() - this.creationTime;
        return Math.max(0, this.maxLifeTime - elapsed);
    }

    /**
     * 获取单位年龄（存在时间）
     * @returns 单位存在的时间(秒)
     */
    public getAge(): number {
        return os.time() - this.creationTime;
    }

    /**
     * 获取死亡时间
     * @returns 死亡时间戳
     */
    public getDeathTime(): number {
        return this.deathTime;
    }

    /**
     * 获取最后复活时间
     * @returns 最后复活时间戳
     */
    public getLastReviveTime(): number {
        return this.lastReviveTime;
    }

    /**
     * 设置复活时间
     * @param seconds 复活时间（秒）
     */
    public setReviveTime(seconds: number): void {
        this.reviveTime = Math.max(1, seconds);
    }

    /**
     * 设置是否可以复活
     * @param canRevive 是否可以复活
     */
    public setCanRevive(canRevive: boolean): void {
        this.canRevive = canRevive;
    }

    /**
     * 检查是否可以复活
     * @returns 是否可以复活
     */
    public getCanRevive(): boolean {
        return this.canRevive;
    }

    /**
     * 设置最大复活次数
     * @param count 最大复活次数，-1表示无限
     */
    public setMaxReviveCount(count: number): void {
        this.maxReviveCount = count;
    }

    /**
     * 获取当前复活次数
     * @returns 当前复活次数
     */
    public getCurrentReviveCount(): number {
        return this.currentReviveCount;
    }

    /**
     * 设置死亡类型
     * @param deathType 死亡类型
     */
    public setDeathType(deathType: DeathType): void {
        this.deathType = deathType;
    }

    /**
     * 获取死亡类型
     * @returns 死亡类型
     */
    public getDeathType(): DeathType {
        return this.deathType;
    }

    /**
     * 销毁单位（彻底移除）
     */
    public destroy(): void {
        this.changeState(LifecycleState.DESTROYED);

        // 停止所有计时器
        this.removeAllTimers();

        // 移除War3单位
        const unitComponent = this.owner.component.unit;
        if (unitComponent) {
            const unitHandle = unitComponent.unit || unitComponent.getUnitHandle?.();
            if (unitHandle) {
                jasscj.RemoveUnit(unitHandle);
            }
        }

        // 触发销毁事件
        this.owner.emit(EventTypes.UNIT_DESTROYED, {
            unit: this.owner,
            destroyTime: os.time()
        });

        // 调用父类销毁
        super.destroy();

        logger.debug(`实体 ${this.owner.getId()} 已销毁`);
    }


}


