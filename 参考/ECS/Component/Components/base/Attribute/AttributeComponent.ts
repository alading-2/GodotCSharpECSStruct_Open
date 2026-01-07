/** @noSelfInFile **/

import { Component } from "../../../Component";
import { Entity } from "../../../../Entity/Entity";
import { Logger } from "../../../../../base/object/工具/logger";
import { EventTypes } from "../../../../types/EventTypes";

const logger = Logger.createLogger("AttributeComponent");

/**
 * 属性组件
 * 
 * 【设计理念】
 * AttributeComponent负责行为控制和系统集成
 * 
 * 【核心职责】
 * 1. 属性变化的监听和响应（如装备穿戴时自动应用属性）
 * 2. 属性系统与其他系统的集成（如属性变化影响AI行为）
 * 3. 属性相关的定时任务（如生命恢复、魔法恢复）
 * 4. 属性系统的状态管理和生命周期控制
 * 5. 属性变化的副作用处理（如攻击力变化时更新武器伤害）
 */
export class AttributeComponent extends Component {

    protected static readonly TYPE: string = "AttributeComponent";
    static getType(): string {
        return this.TYPE;
    }

    constructor(owner: Entity) {
        super(owner);
        logger.debug(`属性系统控制器已创建，所属对象: ${owner.getId()}`);
    }

    initialize(): void {
        // 监听属性变化，更改基础攻速时更新攻击间隔
        this.owner.on(EventTypes.DATA_PROPERTY_CHANGED, (data: any) => {
            if (data.key === "基础攻速") {
                this.updateAttackCooldown();
            }
        });

        logger.debug(`属性系统控制器已初始化，所属对象: ${this.owner.getId()}`);
    }

    destroy(): void {
        logger.debug(`属性系统控制器已销毁，所属对象: ${this.owner.getId()}`);
    }
    // ==================== 属性系统工具方法 ====================

    // 获取unithandle
    get unitHandle(): any {
        return this.owner.component.unit.getUnitHandle();
    }

    // ==================== 属性系统行为控制 ====================

    //攻击间隔设置
    private updateAttackCooldown() {
        let value = this.owner.data.attr.get("攻击间隔");
        if (value >= 0.1) {
            jassjapi.SetUnitState(this.unitHandle, jasscj.ConvertUnitState(0x25), value);
        } else if (value >= 0.02) {
            let atkcool = 0.1;
            let atkspeed = atkcool / value;
            jassjapi.SetUnitState(this.unitHandle, jasscj.ConvertUnitState(0x25), 0.1);
            jassjapi.SetUnitState(this.unitHandle, jasscj.ConvertUnitState(0x51), atkspeed);
        } else {
            jassjapi.SetUnitState(this.unitHandle, jasscj.ConvertUnitState(0x25), 0.1);
            jassjapi.SetUnitState(this.unitHandle, jasscj.ConvertUnitState(0x51), 5);
        }
    }




}
