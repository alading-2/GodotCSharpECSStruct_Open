/** @noSelfInFile **/

import { Component, ComponentConstructor } from "../../../Component";

import { EventTypes } from "../../../../types/EventTypes";
import { Logger } from "../../../../../base/object/工具/logger";
import { HeroHpBar } from "../../../../../base/GUI/UI/HeroHpBar";
import { UnitHpBar } from "../../../../../base/GUI/UI/UnitHpBar";
import { Position } from "../../../../../base/math/Position";
import { Vector } from "../../../../../base/math/Vector";
import { TextTagUI } from "../../../../../base/GUI/UI/TextTagUI";
import { MyMath } from "../../../../../base/math/MyMath";
import { Color } from "../../../../../base/object/工具/Color";
import { PlayerComponent } from "../../..";
import { UnitId } from "../../../../../../output/ts/unitid";
import { xlsx_data_table_unit, xlsx_inte_keys_table_unit } from "../../../../../../output/ts/xlsx_table_unit";
import { Getid } from "../../../../../base/object/工具/Getid";
import { Entity } from "../../../..";
import { UNIT_SCHEMA_KEYS } from "../../../../Schema/Schemas/UnitSchema";

const logger = Logger.createLogger("UnitComponent");

/**
 * 单位组件属性接口
 */
interface UnitComponentProps {
    /** 单位类型 */
    unitType?: string;
    /** 单位所属玩家组件 */
    playerComp?: PlayerComponent,
    /** 单位位置 */
    position?: Position,
    /** 单位朝向角度 */
    face?: number,
}

/**
 * 单位组件 - 管理War3单位的核心功能
 * 提供单位的基础属性、移动、攻击、生命周期管理等功能
 */
export class UnitComponent extends Component<UnitComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "UnitComponent";
    // 存UnitComponent，用GetHandleId作为索引
    private static _unitComponents: Map<number, UnitComponent> = new Map<number, UnitComponent>();

    // War3单位handle,getUnitHandle()
    private unit: any;

    // 单位类型
    private unitType: string;
    // 攻击类型
    // private attackType: string = "物理";

    // 移动相关
    private moveTimerId: string;

    /**
     * 获取组件类型
     */
    static getType(): string {
        return UnitComponent.TYPE;
    }

    /**
     * 构造函数
     */
    constructor(owner: Entity, props?: UnitComponentProps) {
        super(owner, props || {});
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        let typeid = UnitId[this.props.unitType];
        if (!typeid) {
            logger.error("创建单位失败，无此单位：" + this.props.unitType);
            return
        }

        this.unit = jasscj.CreateUnit(this.props.playerComp.getPlayerHandle(), typeid, this.props.position.X, this.props.position.Y, this.props.face);
        this.unitType = this.props.unitType;
        // 单位初始化
        this.UnitInit(this.props);

        logger.debug(`UnitComponent 已为单位类型 ${this.unitType} 初始化`);
    }

    /**
     * 更新组件
     */
    update(deltaTime: number): void {

    }

    /**
     * 销毁组件
     */
    destroy(): void {
        //防止单位被多次移除
        if (this.owner.data.unit.get("isRemove")) {
            return;
        }
        this.owner.data.unit.set("isRemove", true);

        UnitComponent._unitComponents.delete(this.id);

        // 清理移动计时器
        if (this.moveTimerId) {
            // @ts-ignore
            this.moveTimerId.destroy();
            this.moveTimerId = null;
        }

        //5秒后移除单位
        this.owner.component.timer.RunLater(5, () => {
            //移除单位引用
            jassdbg.handle_unref(this.unit);
            //移除单位
            jasscj.RemoveUnit(this.unit);
            this.unit = null;
        });

        logger.debug(`UnitComponent 已为单位类型 ${this.unitType} 销毁`);
    }

    // =================CreateUnit===================================================================================================
    /**
     * 通过unitHandle获取单位对象Unit
     * @param u unitHandle
     * @returns Unit对象
     */
    static U(u: any): UnitComponent | null {
        return UnitComponent._unitComponents.get(jasscj.GetHandleId(u));
    }

    /**
     * 新建单位
     * @param props 单位属性
     */
    private UnitInit(props: UnitComponentProps) {
        let type = props.unitType;  //unitType
        let udata = xlsx_data_table_unit[type];
        if (!udata) {
            logger.error(`创建单位失败，单位类型： “${type}” 不存在`);
            return;
        }

        //将属性写入
        xlsx_inte_keys_table_unit.forEach((key1) => {
            if (UNIT_SCHEMA_KEYS.has(key1)) {
                //@ts-ignore
                this.owner.data.unit.set(key1, udata[key1]);
            } else {
                logger.info(`Schema Key '${key1}' 在 Inte_UnitSchema 中未找到，跳过。`);
            }
        });
        this.type = type; //设置单位类型
        this.owner.data.attr.setMultiple({
            "移动速度": udata.spd,
            "基础攻速": udata.cool1,
            "基础攻击力": udata.攻击力,
            "基础生命值": udata.基础生命值,
            "基础防御": udata.防御,
        });
        this.owner.data.unit.setMultiple({
            "模型": udata.file, //模型
            "当前生命值": this.owner.data.attr.get("最终生命值"),
            "当前魔法值": this.owner.data.attr.get("最终魔法值"),
        });

        if (this.acquire > 0) {//模拟攻击方式
            this.AddAttack();
        }
        //飞行高度
        jasscj.UnitAddAbility(this.unit, Getid.string2id("Arav"));  //添加移除飞行技能
        jasscj.UnitRemoveAbility(this.unit, Getid.string2id("Arav"));
        this.flyHeight = udata.moveHeight;

        let scale = udata.modelScale;
        jasscj.SetUnitScale(this.unit, scale, scale, scale);  //模型大小
        //颜色
        if (udata.RGBA) {
            this.SetColor(udata.RGBA[0] ?? 255, udata.RGBA[1] ?? 255, udata.RGBA[2] ?? 255, udata.RGBA[3] ?? 255);
        }
        //引用计数
        jassdbg.handle_ref(this.unit);
        UnitComponent._unitComponents.set(this.id, this);
    }

    //停一下，原地移动一次
    Stop() {
        this.position = this.position;
    }

    // ================= Unit ========================================================================================================

    //通过单位索引玩家对象
    get playerComponent(): PlayerComponent {
        return PlayerComponent.Id(jasscj.GetPlayerId(jasscj.GetOwningPlayer(this.unit)) + 1);
    }
    set playerComponent(pl: PlayerComponent) {
        jasscj.SetUnitOwner(this.unit, pl.getPlayerHandle());
    }

    //是否是玩家的敌人
    IsEnemy(pl: PlayerComponent) {
        return jasscj.IsUnitEnemy(this.unit, pl.getPlayerHandle());
    }

    //id
    get id(): number {
        return jasscj.GetHandleId(this.unit);
    }
    //单位类型/单位名称
    set type(t: string) {
        this.owner.data.unit.set("单位类型", t);
    }
    get type(): string {
        return this.owner.data.unit.get("单位类型");
    }
    //相等
    static "=="(this: void, u1: UnitComponent, u2: UnitComponent) {
        return u1.unit == u2.unit;
    }

    //设置模型大小
    SetScale(x: number, y: number, z: number) {
        jasscj.SetUnitScale(this.unit, x, y, z);
    }

    // x轴缩放
    set scale(n: number) {
        this.owner.data.unit.set("xscale", n);
        this.SetScale(n, n, n);
    }
    get scale() {
        return this.owner.data.unit.get("xscale");
    }
    //设置颜色
    SetColor(R: number, G: number, B: number, A: number) {
        jasscj.SetUnitVertexColor(this.unit, R, G, B, A);
    }

    // ==============状态=================================

    //移动速度相关
    get moveSpeed(): number {
        return this.owner.data.attr.get("移动速度");
    }

    // 超过522时直接移动单位
    /**
     * 设置单位移动速度
     * @param speed 目标移动速度
     * 
     * 说明:
     * - 由于魔兽引擎限制,单位最大移动速度为522
     * - 当设置速度>522时,会使用计时器模拟超过522的部分
     * - 计时器每0.02秒更新一次单位位置来模拟超速移动
     * - 移动方向根据单位当前朝向计算
     */
    set moveSpeed(speed: number) {
        // 超出522的速度值
        let exceedSpeed: number = 0;
        // 移动方向向量
        let moveVector: Vector = new Vector(1, 0, 0);
        // 更新单位属性中的移动速度
        this.owner.data.attr.set("移动速度", speed);
        // 设置实际移动速度,最大不超过522
        jasscj.SetUnitMoveSpeed(this.unit, speed > 522 ? 522 : speed);

        if (speed > 522) {
            // 计算超出522的速度值
            exceedSpeed = speed - 522;
            // 如果没有移动计时器则创建
            if (!this.moveTimerId) {
                this.moveTimerId = this.owner.component.timer.CreateTimer(0.02, () => {
                    if (exceedSpeed > 0) {
                        // 获取当前位置
                        const pos = this.position;
                        // 计算朝向角度(转换为弧度)
                        const angle = this.face * MyMath.PI / 180;
                        // 根据朝向角度计算移动方向向量
                        moveVector.X = MyMath.cos(angle);
                        moveVector.Y = MyMath.sin(angle);
                        moveVector.Z = 0;
                        moveVector = moveVector.unitVector;
                        // 更新单位位置
                        this.position = new Position(
                            pos.X + moveVector.X * exceedSpeed * 0.02,
                            pos.Y + moveVector.Y * exceedSpeed * 0.02,
                            pos.Z
                        );
                    }
                });
            }
        } else if (this.moveTimerId) {
            // 速度<=522时移除计时器
            this.owner.component.timer.removeTimer(this.moveTimerId);
            this.moveTimerId = "";
            exceedSpeed = 0;
        }
    }

    //面向角度
    get face(): number { return jasscj.GetUnitFacing(this.unit); }
    set face(n: number) { jassjapi.EXSetUnitFacing(this.unit, n); }
    //位置
    get position(): Position {
        return new Position(
            jasscj.GetUnitX(this.unit),
            jasscj.GetUnitY(this.unit),
            jasscj.GetUnitFlyHeight(this.unit) - this.flyHeight
        );
    }
    set position(pt: Position) {
        jasscj.SetUnitX(this.unit, pt.X);
        jasscj.SetUnitY(this.unit, pt.Y);
        let z = pt.Z + this.flyHeight;
        jasscj.SetUnitFlyHeight(this.unit, z, z * 35);
    }

    //飞行高度
    get flyHeight() { return this.owner.data.unit.get("飞行高度"); }
    set flyHeight(n: number) {
        jasscj.SetUnitFlyHeight(this.unit, n, n * 35);  // 最后的参数是改变飞行高度的速度
        this.owner.data.unit.set("飞行高度", n);
    }

    //主动攻击范围
    set acquire(n: number) {
        jasscj.SetUnitAcquireRange(this.unit, n);
        this.owner.data.unit.set("acquire", n);
    }
    get acquire() {
        return this.owner.data.unit.get("acquire");
    }
    //攻击范围
    get range(): number {
        return jassjapi.GetUnitState(this.unit, jasscj.ConvertUnitState(0x16));
    }
    set range(sm: number) {
        jassjapi.SetUnitState(this.unit, jasscj.ConvertUnitState(0x16), sm);
    }

    // 是否无敌.怪物会远离
    get invulnerable() { return this.owner.data.unit.get("无敌") }
    set invulnerable(b: boolean) {
        this.owner.data.unit.set("无敌", b);
        jasscj.SetUnitInvulnerable(this.unit, b);
    }

    //是否免疫伤害
    get immuneDamage() { return this.owner.data.unit.get("不受伤害") }
    set immuneDamage(b: boolean) { this.owner.data.unit.set("不受伤害", b); }

    //是否暂停
    set pause(b: boolean) {
        this.owner.data.unit.set("暂停状态", b);
        if (b) {
            jasscj.PauseUnit(this.unit, true);
        } else {
            jasscj.PauseUnit(this.unit, false);
        }
    }
    //是否英雄
    get isHero() { return this.owner.data.unit.get("类别") == "英雄"; }

    //等级
    get level(): number { return this.owner.data.unit.get("等级"); }
    set level(n: number) {
        this.owner.data.unit.set("等级", n);
        this.owner.emit(EventTypes.UNIT_LEVEL_CHANGED, n);
    }

    // 经验相关功能已迁移到ExpComponent

    //设置碰撞开关
    set collision(b: boolean) {
        jasscj.SetUnitPathing(this.unit, b);
    }
    //设置显示/隐藏
    set show(b: boolean) {
        jasscj.ShowUnit(this.unit, b);
        this.owner.data.unit.set("显示", b);
    }
    get show() {
        return this.owner.data.unit.get("显示");
    }
    //是否远程
    get isremote() {
        return this.range >= 200;
    }

    //是否战斗
    get isFight() {
        return this.owner.data.unit.get("战斗状态");
    }

    /**
    * 设置肖像
    */
    set Portrait(modelpath: string) {
        jassjapi.DzSetUnitPortrait(this.unit, modelpath);
    }
    /**
     * 设置单位是否可以选中
     */
    set selectable(enable: boolean) {
        jassjapi.DzUnitSetCanSelect(this.unit, enable);
    }
    /**
     * 禁用攻击
     */
    set attackable(enable: boolean) {
        jassjapi.DzUnitDisableAttack(this.unit, enable);
    }
    /**
     * 移动类型
     * @param type MoveType.没有, MoveType.无法移动, MoveType.步行, MoveType.飞行
     * @example unit.movetype = MoveType.飞行
     */
    set movetype(type: number) {
        jassjapi.DzUnitSetMoveType(this.unit, type);
    }

    // ==================================公共API==================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================================
    /**
     * 获取单位类型
     */
    getunitType(): string {
        return this.unit.unitType;
    }
    /**
     * 获取War3单位句柄
     */
    getUnitHandle(): any {
        return this.unit;
    }




}
