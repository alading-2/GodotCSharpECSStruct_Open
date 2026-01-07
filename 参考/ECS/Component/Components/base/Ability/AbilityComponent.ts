
/** @noSelfInFile **/

import { Component } from "../../../Component";

import { Logger } from "../../../../../base/object/工具/logger";
import { Position } from "../../../../../base/math/Position";
import { EventTypes } from "../../../../types/EventTypes";
import { Entity } from "../../../..";
import { CooldownComponent } from "../../冷却/CooldownComponent";
import { xlsx_data_table_ability } from "../../../../../../output/ts/xlsx_table_ability";
import { MyMath } from "../../../../../base/math/MyMath";
import { UnitComponent } from "../../..";
import { Getid } from "../../../../../base/object/工具/Getid";
import { SCHEMA_TYPES } from "../../../../Schema/SchemaTypes";
import { TextTagUI } from "../../../../../base/GUI/UI/TextTagUI";

const logger = Logger.createLogger("AbilityComponent");

// 技能快捷键,对应技能位置
export const AbilityHotkey = {
    // 0: "A",
    // 1: "Q",
    // 2: "W",
    // 3: "E",
    // 4: "R",
    5: "D",
    6: "F",
    // 7: "U",
    8: "Q",
    9: "W",
    10: "E",
    11: "R",
}

/**
 * 技能组件属性接口
 */
interface AbilityComponentProps {
    /** 技能ID */
    abilityId?: string;
    /** 技能名称 */
    abilityName?: string;
    /** 当前等级 */
    level?: number;
    /** 最大等级 */
    maxLevel?: number;
    /** 冷却时间(秒) */
    cooldown?: number;
    /** 魔法消耗 */
    manaCost?: number;
    /** 施法距离 */
    castRange?: number;
    /** 技能目标类型 */
    targetType?: "无目标" | "单位目标" | "点目标" | "区域目标";
    /** 技能图标路径 */
    iconPath?: string;
    /** 技能热键 */
    hotkey?: string;
}

/**
 * 技能组件 - 管理War3技能的核心功能
 * 提供技能的冷却、施法、升级等功能
 */
export class AbilityComponent extends Component<AbilityComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "AbilityComponent";

    // 技能基础信息
    private abilityId: string;  //4字符Id
    private abilityEnabled: boolean = true; //是否启用

    // 技能参数
    private abilityType: string;//"无目标" | "单位目标" | "点目标" | "区域目标"

    // 注意：isOnCooldown 和 remainingCooldown 现在通过 CooldownComponent 管理
    // 保留这些属性用于向后兼容，但实际值从 CooldownComponent 获取

    // 拥有者单位
    private unitEntity: Entity;

    // 技能位置（在技能栏中的位置）
    private slotX: number = 0;
    private slotY: number = 0;

    /**
     * 获取组件类型
     */
    static getType(): string {
        return AbilityComponent.TYPE;
    }

    /**
     * 技能组件
     * @param entity entity
     * @param u 单位
     * @param name 技能名
     * @param id id
     * // 技能物编ID：'abcd'，4位字符：
        // 第一位a：AbilityMethodType[abilitydata[name].method]	物品技能：'A'，被动: 'X', 无目标: 'X', 点: 'Z', 单位: 'Y'
        // 第二位b：(this.player.id - 1)(0 - 11)
        // 第三位c：技能按钮坐标x(0 - 3)
        // 第四位d：技能按钮坐标y(0 - 2)
    */
    constructor(entity: Entity, props?: AbilityComponentProps) {
        super(entity, props);
    }

    /**
     * 初始化组件
     */
    initialize(): void {

        // 设置数据
        let abilData = xlsx_data_table_ability[this.props.abilityName];

        this.name = this.props.abilityName //技能名
        this.icon = abilData.icon    //技能图标
        this.abilityType = abilData.method   //技能类型
        // this.range = abilData.range  //施法距离
        // this.area = abilData.area    //技能范围
        this.cool = abilData.cool    //技能冷却时间
        this.cost = abilData.cost    //魔法消耗
        this.targetType = abilData?.targettp ?? "";
        this.action = abilData.action ?? "spell";

        this.abilityId = this.props.abilityId;

        this.slotX = MyMath.S2N(this.props.abilityId.substring(2, 3));
        this.slotY = MyMath.S2N(this.props.abilityId.substring(3, 4));
        //通魔，数据——目标类型DataB1
        this.owner.data.ability.set("dataB", MyMath.R2I(
            jassjapi.EXGetAbilityDataReal(//技能数据（实数）
                jassjapi.EXGetUnitAbility(this.getUnitHandle(), Getid.string2id(this.abilityId)),
                1,
                109
            )
        ));
        if (this.abilityType == "被动") {
            this.cost = 0;
        } else {    // 非被动
            this.setHotkey()
            if (this.abilityType == "无目标") {

            } else if (this.abilityType == "单位") {
                this.range = abilData.range  //施法距离
            } else if (this.abilityType == "点") {
                this.range = abilData.range  //施法距离
                this.area = abilData.area    //技能范围
                if (this.area == 0) {
                    this.dataC = 1; //范围
                } else {
                    //范围技能
                    this.dataC = 3;
                }
            }
        }

        // 设置事件监听器
        this.setupEventListeners();

        logger.debug(`技能组件已初始化: ${this.name} (${this.abilityId})`);
    }

    /**
     * 更新组件
     */
    update(deltaTime: number): void {

        // 检查技能可用性
        this.checkAvailability();
    }

    /**
     * 销毁组件
     */
    destroy(): void {

        // 清理引用
        this.unitEntity = null;

        logger.debug(`技能组件已销毁: ${this.name}`);
    }




    getUnitHandle(): any {
        return this.unitEntity.component.unit.getUnitHandle();
    }

    /**
     * TODO 修改
     * 设置选择单位的技能数据
     * @param unit 单位
     */
    static SetAbilityData(unit: Unit) {
        for (let i = 0; i < 12; i++) {
            let ab = unit.abilButton[i];
            if (ab && ab.SetData) {
                ab.SetData();
            }
        }
    }

    /**
     * TODO 修改
     * 移除技能
     */
    Remove() {
        this.unitEntity.RemoveAbility(this.name);
    }


    /************************************************
     *********************技能属性***********************
     *************************************************/

    //位置与序号转换
    // 0	1	  2	  3
    // 4	5	  6   7
    // 8	9	  10	11
    static Num2Loc(n: number) {
        let x1 = n % 4;
        let y1 = MyMath.R2I(n / 4);
        return $multi(x1, y1);
    }
    static Loc2Num(x: number, y: number) {
        return x + y * 4;
    }

    /**设置技能位置 */
    get location() {
        return AbilityComponent.Loc2Num(this.slotX, this.slotY);
    }
    //TODO 修改
    set location(n: number) {
        let u = this.unitEntity;
        let ab = u.abilButton[n];
        let p = this.location;
        if (!ab) {
            u.RemoveAbility(this.name);
            u.AddAbility(this.name, n);
        } else {
            u.RemoveAbility(ab.name);
            u.RemoveAbility(this.name);
            u.AddAbility(ab.name, p);
            u.AddAbility(this.name, n);
        }
    }

    //等级
    get level() {
        return this.owner.data.ability.get("level");
    }
    set level(value: number) {
        this.owner.emit(EventTypes.ABILITY_LEVELED_UP, {
            /** 旧等级 */
            oldLevel: this.level,
            /** 新等级 */
            newLevel: value,
        } as EventTypes.AbilityLevelEventData);
        this.owner.data.ability.set("level", value);
    }

    //间隔
    get cool(): number {
        return this.owner.data.ability.get("cool") ?? 0;
    }
    set cool(n: number) {
        this.owner.data.ability.set("cool", n);
    }

    //技能说明
    get tips(): string {
        return this.owner.data.ability.get("tips") ?? "";
    }
    set tips(s: string) {
        this.owner.data.ability.set("tips", s);
    }
    //图标
    get icon(): string {
        return this.owner.data.ability.get("icon");
    }
    set icon(s: string) {
        this.owner.data.ability.set("icon", s);
    }
    //技能名字
    get name(): string {
        return this.owner.data.ability.get("name");
    }
    set name(s: string) {
        this.owner.data.ability.set("name", s);
    }

    //施法时间
    get cast_time(): number {
        return this.owner.data.ability.get("cast_time");
    }
    set cast_time(n: number) {
        this.owner.data.ability.set("cast_time", n);
    }
    //影响区域
    get area(): number {
        return this.owner.data.ability.get("area");
    }
    set area(n: number) {
        this.owner.data.ability.set("area", n);
        jassjapi.EXSetAbilityDataReal(
            jassjapi.EXGetUnitAbility(this.getUnitHandle(), Getid.string2id(this.abilityId)),
            1,
            106,
            n
        );
    }
    //施法距离
    get range(): number {
        return this.owner.data.ability.get("range");
    }
    set range(n: number) {
        this.owner.data.ability.set("range", n);
        jassjapi.EXSetAbilityDataReal(
            jassjapi.EXGetUnitAbility(this.getUnitHandle(), Getid.string2id(this.abilityId)),
            1,
            107,
            n
        );
    }
    //目标类型
    get targetType(): string {
        return this.owner.data.ability.get("targetType");
    }
    set targetType(s: string) {
        this.owner.data.ability.set("targetType", s);
    }

    //施法动作
    get action(): string {
        return this.owner.data.ability.get("action");
    }
    set action(s: string) {
        this.owner.data.ability.set("action", s);
    }

    /**
     * 数据——目标类型DataB1
     * 无目标：0，单位：1，点：2，单位/点：3
     */
    get dataB(): number {
        return this.owner.data.ability.get("dataB");
    }
    set dataB(n: number) {
        this.owner.data.ability.set("dataB", n);
        jassjapi.EXSetAbilityDataReal(
            jassjapi.EXGetUnitAbility(this.getUnitHandle(), Getid.string2id(this.abilityId)),
            1,
            109,
            n
        );
    }

    /**
     * 数据——选项DataC1
     * 图标可见1，目标选取图像2，物理魔法4，通用魔法8，单独施放16
     */
    get dataC(): number {
        return this.owner.data.ability.get("dataC");
    }
    set dataC(n: number) {
        this.owner.data.ability.set("dataC", n);
        jassjapi.EXSetAbilityDataReal(
            jassjapi.EXGetUnitAbility(this.getUnitHandle(), Getid.string2id(this.abilityId)),
            1,
            110,
            n
        );
    }

    //魔法消耗
    get cost(): number {
        return this.owner.data.ability.get("cost");
    }
    set cost(n: number) {
        this.owner.data.ability.set("cost", n);
    }

    /**
     * 设置热键
     */
    setHotkey() {
        let loc = AbilityComponent.Loc2Num(this.slotX, this.slotY);
        this.hotkey = AbilityHotkey[loc];
    }
    //快捷键
    get hotkey(): string {
        return this.owner.data.ability.get("hotkey");
    }
    set hotkey(s: string) {
        this.owner.data.ability.set("hotkey", s);
        jassjapi.EXSetAbilityDataInteger(
            jassjapi.EXGetUnitAbility(this.getUnitHandle(), Getid.string2id(this.abilityId)),
            1,
            200,
            MyMath.S2N(s)
        );
    }


    // ====================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================

    /**
     * 获取冷却组件实例
     */
    private getCooldownComponent(): CooldownComponent | null {
        return this.owner.component.cooldown;
    }

    /**
     * 生成技能描述
     */
    private generateDescription(): string {
        let description = `${this.name}\n`;
        description += `等级: ${this.level}/${this.level}\n`;
        description += `冷却时间: ${this.cool}秒\n`;
        description += `魔法消耗: ${this.cost}\n`;
        description += `施法距离: ${this.range}\n`;
        return description;
    }

    /**
     * 设置事件监听器
     */
    private setupEventListeners(): void {
        // 监听技能施放事件
        this.owner.on(EventTypes.ABILITY_CAST, (data) => {
            this.onAbilityCast(data);
        });

        // 监听技能升级事件
        this.owner.on(EventTypes.ABILITY_LEVELED_UP, (data) => {
            this.onAbilityLevelUp(data);
        });

        // 监听技能启用事件
        this.owner.on(EventTypes.ABILITY_ENABLED, (data) => {
            this.enableAbility();
        });

        // 监听技能禁用事件
        this.owner.on(EventTypes.ABILITY_DISABLED, (data) => {
            this.disableAbility();
        });

    }


    /**
     * 检查技能可用性
     * @returns 
     */
    public checkAvailability(): boolean {
        if (!this.unitEntity) return;

        // 获取单位数据组件
        const lifecycleComp = this.unitEntity.component.lifecycle;
        const unitData = this.unitEntity.getDataManager(SCHEMA_TYPES.UNIT_DATA);
        if (!unitData) return;

        // 检查魔法值是否足够
        const hasEnoughMana = unitData.get("当前魔法值") >= this.cost;

        // 检查单位是否活着
        const isAlive = lifecycleComp.isAlive();

        // // 检查单位是否被沉默
        // const isSilenced = unitData.get("沉默状态") || false;

        // enable，且不在冷却，且魔法值足够，且单位存活
        const isReady = this.abilityEnabled && !this.isOnCooldown && hasEnoughMana && isAlive;
        // 判断技能就绪状态变更
        //TODO 技能Entity的UnitEntitys
        if (!isReady) {
            TextTagUI.Create(`技能释放失败：${this.getUnavailableReason()}`, unitComp.position);
        }
        return isReady;

    }

    /**
     * 获取不可用原因
     */
    public getUnavailableReason(): string {
        if (!this.abilityEnabled) return "技能被禁用";
        if (this.isOnCooldown) return "技能冷却中";
        if (!this.unitEntity) return "没有拥有者";

        // 获取单位数据组件
        const unitData = this.unitEntity.getDataManager(SCHEMA_TYPES.UNIT_DATA);
        if (!unitData) return "单位数据不存在";

        if (unitData.get("当前魔法值") < this.cost) return "魔法值不足";

        if (unitData.get("死亡")) return "单位已死亡";
        // if (unitData.get("沉默状态")) return "单位被沉默";

        return "";
    }

    /**
     * 施放技能
     * @param target 目标实体
     * @param position 目标位置
     * @returns 是否成功
     */
    castAbility(target?: Entity, position?: Position): boolean {
        if (!this.checkAvailability()) {
            return false;
        }

        this.owner.emit(EventTypes.ABILITY_CAST, {
            target,
            position,
            caster: this.unitEntity
        });

        return true;
    }

    /**
     * 技能施放完成后处理，技能施放事件执行
     */
    private onAbilityCast(data: any): void {
        if (!this.checkAvailability()) {
            logger.warn(`无法施放技能 ${this.name}: ${this.getUnavailableReason()}`);
            return;
        }

        // 消耗魔法值
        this.unitEntity.data.unit.add("当前魔法值", -this.cost);

        // 开始冷却
        this.startCooldown();

        // 获取单位数据组件用于日志
        const unitType = this.unitEntity.component.unit.getunitType();
        logger.debug(`技能已施放: ${this.name} (施法者: ${unitType})`);
    }

    /**
     * 开始冷却
     */
    private startCooldown(): void {
        // 确保有冷却组件
        let cooldownComponent = this.owner.component.cooldown;
        if (!cooldownComponent) {
            cooldownComponent = this.owner.addComponent(CooldownComponent, {
                defaultCooldown: this.cool,
                autoStart: false
            });
        }

        // 创建并开始冷却
        cooldownComponent.createCooldown("ability", this.cool, () => {
            this.finishCooldown();
        });
        cooldownComponent.startCooldownById("ability");
    }

    /**
     * 完成冷却
     */
    private finishCooldown(): void {
        // 技能冷却完成
        logger.debug(`技能冷却已完成: ${this.name}`);
    }

    /**
     * 技能升级处理
     */
    private onAbilityLevelUp(data: any): void {

        const oldLevel = this.level;
        this.level++;

        // 更新技能参数（根据等级）
        this.updateAbilityParameters();

        // 更新描述
        this.tips = this.generateDescription();

        this.owner.emit(EventTypes.ABILITY_LEVELED_UP, {
            abilityId: this.abilityId,
            oldLevel,
            newLevel: this.level
        });

        logger.debug(`技能已升级: ${this.name} 至等级 ${this.level}`);
    }

    /**
     * 更新技能参数
     */
    private updateAbilityParameters(): void {
        // 根据技能等级更新参数
        // 这里可以根据具体技能类型实现不同的升级逻辑

        // 示例：每级减少冷却时间和增加伤害

        // this.cool = Math.max(1, baseCooldown - (this.level - 1) * 1);
        // this.cost = baseManaCost + (this.level - 1) * 10;

    }

    // ==================== 公共API方法 ====================

    /**
     * 设置拥有者单位
     * @param unitEntity
     */
    setUnitEntity(unitEntity: Entity): void {
        this.unitEntity = unitEntity;
    }

    /**
     * 启用技能
     */
    enableAbility(): void {
        this.abilityEnabled = true;

        this.owner.emit(EventTypes.ABILITY_ENABLED, {
            abilityId: this.abilityId
        });
    }

    /**
     * 禁用技能
     */
    disableAbility(): void {
        this.abilityEnabled = false;

        this.owner.emit(EventTypes.ABILITY_DISABLED, {
            abilityId: this.abilityId
        });
    }

    /**
     * 检查技能是否在冷却中
     */
    get isOnCooldown(): boolean {
        return this.owner.component.cooldown.isOnCooldownById("ability");
    }

    /**
     * 获取剩余冷却时间
     */
    get remainingCooldown(): number {
        return this.owner.component.cooldown.getRemainingTimeById("ability");
    }

    /**
     * 获取冷却进度 (0-1)
     */
    getCooldownProgress(): number {
        return this.owner.component.cooldown.getCooldownProgressById("ability");
    }

    /**
     * 暂停技能冷却
     */
    pauseCooldown(): void {
        this.owner.component.cooldown.pauseCooldownById("ability");
    }

    /**
     * 恢复技能冷却
     */
    resumeCooldown(): void {
        this.owner.component.cooldown.resumeCooldownById("ability");
    }

    /**
     * 获取技能信息
     */
    getAbilityInfo(): any {
        return {
            id: this.abilityId,
            name: this.name,
            level: this.level,
            cool: this.cool,
            cost: this.cost,
            range: this.range,
            abilityType: this.abilityType,
            isEnabled: this.abilityEnabled,
            isOnCooldown: this.isOnCooldown,
            remainingCooldown: this.remainingCooldown
        };
    }
}
