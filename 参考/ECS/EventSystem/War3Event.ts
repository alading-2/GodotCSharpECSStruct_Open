/** @noSelfInFile **/
/**
 * 模拟事件
 * 框架部分模拟可以通过此事件追踪
 * 不会被其它文件索引的，单独运行
 */

import { MyMath } from "../../base/math/MyMath";
import { Position } from "../../base/math/Position";
import { Getid } from "../../base/object/工具/Getid";
import { Timer } from "../../base/object/工具/Timer/Timer";
import { AbilityComponent } from "../Component";
import { ItemComponent } from "../Component/Components/base/Item/ItemComponent";
import { PlayerComponent } from "../Component/Components/PlayerComponent";
import { UnitComponent } from "../Component/Components/base/Unit/UnitComponent";



export namespace War3Event {
    export class WEtrigger {
        /**触发单位 */
        static get unit(): UnitComponent {
            return UnitComponent.U(jasscj.GetTriggerUnit());
        }
        /**攻击单位 */
        static get attacker(): UnitComponent {
            return UnitComponent.U(jasscj.GetAttacker());
        }
        /**目标单位 */
        static get spelltarget(): UnitComponent {
            return UnitComponent.U(jasscj.GetSpellTargetUnit());
        }
        /**释放技能Id */
        static get spellid(): string {
            return Getid.id2string(jasscj.GetSpellAbilityId());
        }
        /**命令 */
        static get order(): string {
            return jasscj.OrderId2String(jasscj.GetIssuedOrderId());
        }
        /**触发物品 */
        static get item(): ItemComponent {
            return jasscj.GetHandleId(jasscj.GetManipulatedItem()) != 0
                ? ItemComponent.I(jasscj.GetManipulatedItem())
                : ItemComponent.I(jasscj.GetSoldItem());
        }
        /**目标物品 */
        static get targetitem(): ItemComponent {
            return ItemComponent.I(jasscj.GetSpellTargetItem());
        }
        /**触发玩家 */
        static get player(): PlayerComponent {
            return PlayerComponent.Id(jasscj.GetPlayerId(jasscj.GetTriggerPlayer()) + 1);
        }
        /**硬件玩家 */
        static get keyplayer(): PlayerComponent {
            return PlayerComponent.Id(
                jasscj.GetPlayerId(jassjapi.DzGetTriggerKeyPlayer()) + 1
            );
        }
        /**同步玩家 */
        static get syncplayer(): PlayerComponent {
            return PlayerComponent.Id(
                jasscj.GetPlayerId(jassjapi.DzGetTriggerSyncPlayer()) + 1
            );
        }
        /**释放点 */
        static get spellposition() {
            let pt = jasscj.GetSpellTargetLoc();
            let p = new Position(jasscj.GetLocationX(pt), jasscj.GetLocationY(pt), 0);
            jasscj.RemoveLocation(pt);
            pt = null;
            return p;
        }
        /**命令点 */
        static get orderposition() {
            let pt = jasscj.GetOrderPointLoc();
            let p = new Position(jasscj.GetLocationX(pt), jasscj.GetLocationY(pt), 0);
            jasscj.RemoveLocation(pt);
            pt = null;
            return p;
        }
        /**同步数据 */
        static get syncdata(): string {
            return jassjapi.DzGetTriggerSyncData();
        }
        /**消息 */
        static get chatstring(): string {
            return jasscj.GetEventPlayerChatString();
        }
        /**鼠标滚轮值 */
        static get wheeldelta(): number {
            return jassjapi.DzGetWheelDelta();
        }
    }

    //单位进入区域事件
    export class UnitEnterRectEvent {
        action: (this: void, unit: UnitComponent) => void;
        /**
         * 单位进入区域事件，返回动作，通过Null清楚动作
         * @param func 参数：进入单位
         */
        constructor(rect: Rect, func: (this: void, unit: UnitComponent) => void) {
            this.action = func;
            let trigger = jasscj.CreateTrigger();
            let rg = jasscj.CreateRegion();
            jasscj.RegionAddRect(rg, rect.rect);

            jassdbg.handle_ref(trigger);
            jassdbg.handle_ref(rg);

            let ev = jasscj.TriggerRegisterEnterRegion(trigger, rg, null);

            jassdbg.handle_ref(ev);
            let ac = jasscj.TriggerAddAction(trigger, () => {
                let u = WEtrigger.unit;
                if (u) {
                    this.action(u);
                }
            });
            jassdbg.handle_ref(ac);
        }

        //
    }

    //单位离开区域事件
    export class UnitLeaveRectEvent {
        action: (this: void, unit: UnitComponent) => void;
        rect: Rect;
        /**
         * 单位离开区域事件，返回动作，通过Null清楚动作
         * @param func 参数：离开单位
         */
        constructor(rect: Rect, func: (this: void, unit: UnitComponent) => void) {
            this.action = func;
            let trigger = jasscj.CreateTrigger();
            let rg = jasscj.CreateRegion();

            jassdbg.handle_ref(trigger);
            jassdbg.handle_ref(rg);

            jasscj.RegionAddRect(rg, rect.rect);
            let ev = jasscj.TriggerRegisterLeaveRegion(trigger, rg, null);
            jassdbg.handle_ref(ev);
            let ac = jasscj.TriggerAddAction(trigger, () => {
                let u = WEtrigger.unit;
                if (u) {
                    this.action(u);
                }
            });
            jassdbg.handle_ref(ac);
        }

        //
    }

    //单位施放技能事件
    export class UnitSpellEvent {
        //定义
        private static action_table: ((this: void, unit: UnitComponent) => void)[] = [];
        static Action(this: void, unit: UnitComponent): void {
            let actions = UnitSpellEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit);
            }
        }

        private static ison = false;//是否打开

        action: (this: void, unit: UnitComponent) => void;
        /**
         * 单位施放技能事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位
         */
        constructor(func: (this: void, unit: UnitComponent) => void) {
            this.action = func;
            UnitSpellEvent.action_table.push(func);
            if (UnitSpellEvent.ison == false) {
                UnitSpellEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    //let ev= jasscj.TriggerRegisterPlayerUnitEvent(trigger,jasscj.PlayerComponent(i-1),jasscj.ConvertPlayerUnitEvent(272),null)
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(272),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    UnitSpellEvent.Action(u);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitSpellEvent.action_table;
            t.splice(t.indexOf(this.action));
            //ArrayRemove(UnitSpellEvent.action_table,UnitSpellEvent.action_table.indexOf(this.action) + 1);
        }
    }

    //玩家窗口大小变化
    export class WindowResizeEvent {
        //定义
        private static action_table: ((
            this: void,
            pl: PlayerComponent,
            w: number,
            h: number
        ) => void)[] = [];
        static Action(this: void, pl: PlayerComponent, w: number, h: number): void {
            let actions = WindowResizeEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](pl, w, h);
            }
        }

        private static ison = false;

        action: (this: void, pl: PlayerComponent, w: number, h: number) => void;
        /**
         * 玩家窗口大小变化，返回动作，通过Null清楚动作
         * @param func 参数：触发玩家
         */
        constructor(func: (this: void, pl: PlayerComponent, w: number, h: number) => void) {
            this.action = func;
            WindowResizeEvent.action_table.push(func);
            //注册war3窗口大小变化事件
            if (WindowResizeEvent.ison == false) {
                WindowResizeEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                let ev = jassjapi.DzTriggerRegisterWindowResizeEvent(
                    trigger,
                    true,
                    null
                );
                jassdbg.handle_ref(ev);

                //限制每0.5秒才能触发一次
                let t = 0;
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    if (t == 0) {
                        let pl = WEtrigger.keyplayer;
                        let w = jassjapi.DzGetWindowWidth();
                        let h = jassjapi.DzGetWindowHeight();
                        if (w > 100 && h > 100) {
                            WindowResizeEvent.Action(pl, w, h);
                        }

                        t = 1;
                        Timer.RunLater(0.5, () => {
                            t = 0;
                        });
                    }
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = WindowResizeEvent.action_table;
            t.splice(t.indexOf(this.action));
            //ArrayRemove(WindowResizeEvent.action_table,WindowResizeEvent.action_table.indexOf(this.action) + 1);
        }
    }

    //单位使用物品事件
    export class UnitUseItemEvent {
        //定义
        private static action_table: ((this: void, unit: UnitComponent, i: ItemComponent) => void)[] =
            [];
        static Action(this: void, unit: UnitComponent, item: ItemComponent): void {
            let actions = UnitUseItemEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, item);
            }
        }

        private static ison = false;
        action: (this: void, unit: UnitComponent, item: ItemComponent) => void;
        /**
         * 单位使用物品事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(func: (this: void, unit: UnitComponent, i: ItemComponent) => void) {
            this.action = func;
            UnitUseItemEvent.action_table.push(func);
            if (UnitUseItemEvent.ison == false) {
                UnitUseItemEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                //EVENT_PLAYER_UNIT_USE_ITEM= ConvertPlayerUnitEvent(50)
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(50),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let it = WEtrigger.item;
                    UnitUseItemEvent.Action(u, it);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitUseItemEvent.action_table;
            t.splice(t.indexOf(this.action));
            //ArrayRemove(UnitUseItemEvent.action_table,UnitUseItemEvent.action_table.indexOf(this.action) + 1);
        }
    }

    //单位获得物品事件
    export class UnitGetItemEvent {
        //定义
        private static action_table: ((this: void, unit: UnitComponent, i: ItemComponent) => void)[] =
            [];
        static Action(this: void, unit: UnitComponent, item: ItemComponent): void {
            let actions = UnitGetItemEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, item);
            }
        }
        private static ison = false;
        action: (this: void, unit: UnitComponent, item: ItemComponent) => void;
        /**
         * 单位获得物品事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(func: (this: void, unit: UnitComponent, i: ItemComponent) => void) {
            this.action = func;
            UnitGetItemEvent.action_table.push(func);
            if (UnitGetItemEvent.ison == false) {
                UnitGetItemEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(49),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let it = WEtrigger.item;
                    UnitGetItemEvent.Action(u, it);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitGetItemEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }
    //单位丢弃物品事件
    export class UnitDropItemEvent {
        //定义
        private static action_table: ((this: void, unit: UnitComponent, i: ItemComponent) => void)[] =
            [];
        static Action(this: void, unit: UnitComponent, item: ItemComponent): void {
            let actions = UnitDropItemEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, item);
            }
        }

        private static ison = false;
        action: (this: void, unit: UnitComponent, item: ItemComponent) => void;
        /**
         * 单位丢弃物品事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(func: (this: void, unit: UnitComponent, i: ItemComponent) => void) {
            this.action = func;
            UnitDropItemEvent.action_table.push(func);
            if (UnitDropItemEvent.ison == false) {
                UnitDropItemEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(48),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let it = WEtrigger.item;
                    UnitDropItemEvent.Action(u, it);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitDropItemEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }
    //单位出售物品事件
    export class UnitSellItemEvent {
        //定义
        private static action_table: ((this: void, unit: UnitComponent, i: ItemComponent) => void)[] =
            [];
        static Action(this: void, unit: UnitComponent, item: ItemComponent): void {
            let actions = UnitSellItemEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, item);
            }
        }
        private static ison = false;
        action: (this: void, unit: UnitComponent, item: ItemComponent) => void;
        /**
         * 单位出售物品事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(func: (this: void, unit: UnitComponent, i: ItemComponent) => void) {
            this.action = func;
            UnitSellItemEvent.action_table.push(func);
            if (UnitSellItemEvent.ison == false) {
                UnitSellItemEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(271),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let it = WEtrigger.item;
                    UnitSellItemEvent.Action(u, it);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitSellItemEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //单位抵押物品事件
    export class UnitPawnItemEvent {
        //定义
        private static action_table: ((this: void, unit: UnitComponent, i: ItemComponent) => void)[] =
            [];
        static Action(this: void, unit: UnitComponent, item: ItemComponent): void {
            let actions = UnitPawnItemEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, item);
            }
        }
        private static ison = false;
        action: (this: void, unit: UnitComponent, item: ItemComponent) => void;
        /**
         * 单位抵押物品事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(func: (this: void, unit: UnitComponent, i: ItemComponent) => void) {
            this.action = func;
            UnitPawnItemEvent.action_table.push(func);
            if (UnitPawnItemEvent.ison == false) {
                UnitPawnItemEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(277),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let it = WEtrigger.item;
                    UnitSellItemEvent.Action(u, it);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitPawnItemEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //发布目标事件
    export class OrderTargetEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            i: ItemComponent | UnitComponent,
            order: string
        ) => void)[] = [];
        static Action(
            this: void,
            order: string,
            unit: UnitComponent,
            item: ItemComponent | UnitComponent
        ): void {
            let actions = OrderTargetEvent.action_table;
            if (actions) {
                for (let i = 0; i < actions.length; i++) {
                    actions[i](unit, item, order);
                }
            }
        }

        private static ison = false;
        action: (this: void, unit: UnitComponent, item: ItemComponent | UnitComponent, order: string) => void;
        order: string;
        /**
         * 发布物品目标事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(
            func: (this: void, unit: UnitComponent, i: ItemComponent | UnitComponent, order: string) => void
        ) {
            this.action = func;
            let t = OrderTargetEvent.action_table;
            t.push(func);
            if (OrderTargetEvent.ison == false) {
                OrderTargetEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                //EVENT_PLAYER_UNIT_ISSUED_TARGET_ORDER = ConvertPlayerUnitEvent(40)
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(40),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let s = WEtrigger.order;
                    let i = ItemComponent.I(jasscj.GetOrderTargetItem());
                    if (i) {
                        OrderTargetEvent.Action(s, u, i);
                    } else {
                        OrderTargetEvent.Action(s, u, UnitComponent.U(jasscj.GetOrderTargetUnit()));
                    }
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = OrderTargetEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //发布点目标事件
    export class OrderLocEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            pt: Position,
            order: string
        ) => void)[] = [];
        static Action(this: void, order: string, unit: UnitComponent, pt: Position): void {
            let actions = OrderLocEvent.action_table;
            if (actions) {
                for (let i = 0; i < actions.length; i++) {
                    actions[i](unit, pt, order);
                }
            }
        }

        private static ison = false;
        action: (this: void, unit: UnitComponent, pt: Position, order: string) => void;
        /**
         * 发布物品目标事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发点
         */
        constructor(
            func: (this: void, unit: UnitComponent, pt: Position, order: string) => void
        ) {
            this.action = func;
            let t = OrderLocEvent.action_table;
            t.push(func);

            //ArrayInsert(OrderLocEvent.action_table[order],func)
            if (OrderLocEvent.ison == false) {
                OrderLocEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(39),//EVENT_PLAYER_UNIT_ISSUED_POINT_ORDER
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let s = WEtrigger.order;
                    let pt = WEtrigger.orderposition;

                    OrderLocEvent.Action(s, u, pt);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = OrderLocEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //单位攻击事件
    export class UnitAttackEvent {
        //定义
        private static action_table: ((
            this: void,
            atc: UnitComponent,
            atced: UnitComponent
        ) => void)[] = [];
        static Action(this: void, atc: UnitComponent, atced: UnitComponent): void {
            let actions = UnitAttackEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](atc, atced);
            }
        }

        private static ison = false;
        action: (this: void, atc: UnitComponent, atced: UnitComponent) => void;
        /**
         * 单位攻击事件，返回动作，通过Null清楚动作
         * @param func 参数：攻击单位，被攻击单位
         */
        constructor(func: (this: void, atc: UnitComponent, atced: UnitComponent) => void) {
            this.action = func;
            UnitAttackEvent.action_table.push(func);
            if (UnitAttackEvent.ison == false) {
                UnitAttackEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(18),//EVENT_PLAYER_UNIT_ATTACKED
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let u2 = WEtrigger.attacker;
                    UnitAttackEvent.Action(u2, u);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = UnitAttackEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }
    //单位被选择事件
    export class SeletUnitEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            player: PlayerComponent
        ) => void)[] = [];
        static Action(this: void, unit: UnitComponent, player: PlayerComponent): void {
            let actions = SeletUnitEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, player);
            }
        }

        private static ison = false;
        action: (this: void, unit: UnitComponent, player: PlayerComponent) => void;
        /**
         * 单位被选择事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发玩家
         */
        constructor(func: (this: void, unit: UnitComponent, player: PlayerComponent) => void) {
            this.action = func;
            SeletUnitEvent.action_table.push(func);
            if (SeletUnitEvent.ison == false) {
                SeletUnitEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                //EVENT_PLAYER_UNIT_SELECTED = ConvertPlayerUnitEvent(24)
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(24),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let pl = WEtrigger.player;
                    SeletUnitEvent.Action(u, pl);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = SeletUnitEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }
    //单位被取消选择事件
    export class DeSeletUnitEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            player: PlayerComponent
        ) => void)[] = [];
        static Action(this: void, unit: UnitComponent, player: PlayerComponent): void {
            let actions = DeSeletUnitEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, player);
            }
        }
        private static ison = false;
        action: (this: void, unit: UnitComponent, player: PlayerComponent) => void;
        /**
         * 单位被取消选择事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发玩家
         */
        constructor(func: (this: void, unit: UnitComponent, player: PlayerComponent) => void) {
            this.action = func;
            DeSeletUnitEvent.action_table.push(func);
            if (DeSeletUnitEvent.ison == false) {
                DeSeletUnitEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                //EVENT_PLAYER_UNIT_DESELECTED = ConvertPlayerUnitEvent(25)
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerUnitEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerUnitEvent(25),
                        null
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let u = WEtrigger.unit;
                    let pl = WEtrigger.player;
                    DeSeletUnitEvent.Action(u, pl);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = DeSeletUnitEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //玩家输入消息事件
    export class PlayerChatEvent {
        //定义
        private static action_table: ((
            this: void,
            player: PlayerComponent,
            str: string
        ) => void)[] = [];
        static Action(this: void, player: PlayerComponent, str: string): void {
            let actions = PlayerChatEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](player, str);
            }
        }
        private static ison = false;
        action: (this: void, player: PlayerComponent, str: string) => void;
        /**
         * 玩家输入消息事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发玩家
         */
        constructor(func: (this: void, player: PlayerComponent, str: string) => void) {
            this.action = func;
            PlayerChatEvent.action_table.push(func);
            if (PlayerChatEvent.ison == false) {
                PlayerChatEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerChatEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        "",
                        false
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let pl = WEtrigger.player;
                    let s = jasscj.GetEventPlayerChatString();
                    PlayerChatEvent.Action(pl, s);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = PlayerChatEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //异步按键事件
    export class KeyEvent {
        //action_table[key][按下/松开]
        private static action_table: any[][] = [];
        /**
         * @param mt 1按下,0释放
         * @param player 玩家
         * @param key 按键
         */
        static Action(this: void, mt: number, player: PlayerComponent, key: number): void {
            if (KeyEvent.action_table[key]) {
                if (KeyEvent.action_table[key][mt]) {
                    let actions: ((this: void, player: PlayerComponent) => void)[] =
                        KeyEvent.action_table[key][mt];
                    for (let i = 0; i < actions.length; i++) {
                        actions[i](player);
                    }
                }
            }
        }

        action: (this: void, player: PlayerComponent) => void;
        key: number;
        mt: number;

        /**
         * 按键事件，返回动作，通过Null清楚动作,请用ClickKey
         * @param mt 1按下,0释放
         * @param key 按键
         * @param func 参数：玩家
         */
        constructor(
            mt: number,
            key: number,
            func: (this: void, player: PlayerComponent) => void
        ) {
            if (!KeyEvent.action_table[key]) {
                KeyEvent.action_table[key] = [];
            }
            if (!KeyEvent.action_table[key][mt]) {
                KeyEvent.action_table[key][mt] = [];

                let callback = () => {
                    let pl = WEtrigger.keyplayer;
                    KeyEvent.Action(mt, pl, key);
                };
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                //鼠标左右键，key == 1 || key == 2
                if (key == 1 || key == 2) {
                    let ev = jassjapi.DzTriggerRegisterMouseEventByCode(
                        trigger,
                        key,
                        mt,
                        false,
                        callback
                    );
                    jassdbg.handle_ref(ev);
                } else {
                    let ev = jassjapi.DzTriggerRegisterKeyEventByCode(
                        trigger,
                        key,
                        mt,
                        false,
                        callback
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, null);
                jassdbg.handle_ref(ac);
            }
            this.action = func;
            this.key = key;
            this.mt = mt;
            let t: any[] = KeyEvent.action_table[key][mt];
            t.push(func);
        }
        //
        Null() {
            let t: any[] = KeyEvent.action_table[this.key][this.mt];
            t.splice(t.indexOf(this.action));
        }
    }

    //同步按键事件
    export class SyncKeyEvent {
        //定义
        private static action_table: any[][] = [];
        /**
         * @param this 
         * @param mt 按下/松开
         * @param player 玩家
         * @param key 按键
         */
        static Action(this: void, mt: number, player: PlayerComponent, key: number): void {
            if (SyncKeyEvent.action_table[key]) {
                if (SyncKeyEvent.action_table[key][mt]) {
                    let actions: ((this: void, player: PlayerComponent) => void)[] =
                        SyncKeyEvent.action_table[key][mt];
                    for (let i = 0; i < actions.length; i++) {
                        actions[i](player);
                    }
                }
            }
        }

        action: (this: void, player: PlayerComponent) => void;
        key: number;
        mt: number;

        //游戏初始化时运行顺序0-100
        private static readonly ONINIT_ORDER = 0;
        //游戏初始化时运行
        private static onInit() {
            new War3Event.SyncEvent("SyncKey", (pl, data) => {
                SyncKeyEvent.Action(1, pl, MyMath.S2N(data));
            });
        }
        /**
         * 按键事件，返回动作，通过Null清楚动作,请用ClickKey
         * @param func 参数：按下or释放,玩家,按键
         */
        constructor(key: number, func: (this: void, player: PlayerComponent) => void) {
            if (!SyncKeyEvent.action_table[key]) {
                SyncKeyEvent.action_table[key] = [];
            }
            if (!SyncKeyEvent.action_table[key][1]) {
                SyncKeyEvent.action_table[key][1] = [];

                new KeyEvent(1, key, () => {
                    PlayerComponent.SyncData("SyncKey", key.toString());
                });
            }
            this.action = func;
            this.key = key;
            this.mt = 1;
            let t: any[] = SyncKeyEvent.action_table[key][1];
            t.push(func);
        }
        //
        Null() {
            let t: any[] = SyncKeyEvent.action_table[this.key][this.mt];
            t.splice(t.indexOf(this.action));
        }
    }

    //鼠标滚轮事件
    export class MouseWheelEvent {
        //定义
        private static action_table: ((
            this: void,
            player: PlayerComponent,
            data: number
        ) => void)[] = [];
        static Action(this: void, player: PlayerComponent, data: number): void {
            let actions = MouseWheelEvent.action_table;
            if (actions) {
                for (let i = 0; i < actions.length; i++) {
                    actions[i](player, data);
                }
            }
        }
        private static ison = false;
        action: (this: void, player: PlayerComponent, data: number) => void;
        /**
         * 鼠标滚轮事件，返回动作，通过Null清楚动作，向前+1，向后-1，已经除以120
         * @param func 参数：触发玩家
         */
        constructor(func: (this: void, player: PlayerComponent, data: number) => void) {
            this.action = func;
            MouseWheelEvent.action_table.push(func);
            if (MouseWheelEvent.ison == false) {
                MouseWheelEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                let callback = () => {
                    let pl = WEtrigger.keyplayer;
                    //向前+1，向后-1
                    let wd = WEtrigger.wheeldelta / 120;
                    MouseWheelEvent.Action(pl, wd);
                };
                let ev = jassjapi.DzTriggerRegisterMouseWheelEventByCode(
                    trigger,
                    true,
                    null
                );
                jassdbg.handle_ref(ev);
                let ac = jasscj.TriggerAddAction(trigger, callback);
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = MouseWheelEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //同步事件
    export class SyncEvent {
        //定义
        private static action_table: ((
            this: void,
            player: PlayerComponent,
            data: string
        ) => void)[][] = [];
        static Action(this: void, player: PlayerComponent, str: string): void {
            let wz = str.indexOf("&");
            let index = str.substring(0, wz);
            let data = str.substring(wz + 1, str.length);
            let actions = SyncEvent.action_table[MyMath.StringHash(index)];
            if (actions) {
                for (let i = 0; i < actions.length; i++) {
                    actions[i](player, data);
                }
            }
        }
        private static ison = false;
        action: (this: void, player: PlayerComponent, data: string) => void;
        index: number;
        /**
         * 同步事件，返回动作，通过Null清楚动作
         * @param index 同步标签
         * @param func 触发玩家，同步数据
         */
        constructor(
            index: string,
            func: (this: void, player: PlayerComponent, data: string) => void
        ) {
            let i = MyMath.StringHash(index);
            if (!SyncEvent.action_table[i]) {
                SyncEvent.action_table[i] = [];
            }
            this.action = func;
            this.index = i;
            SyncEvent.action_table[i].push(func);
            if (SyncEvent.ison == false) {
                SyncEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                let ev = jassjapi.DzTriggerRegisterSyncData(trigger, "sync", false);
                jassdbg.handle_ref(ev);
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let pl = PlayerComponent.Id(
                        jasscj.GetPlayerId(jassjapi.DzGetTriggerSyncPlayer()) + 1
                    );
                    let s = jassjapi.DzGetTriggerSyncData();
                    SyncEvent.Action(pl, s);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = SyncEvent.action_table[this.index];
            t.splice(t.indexOf(this.action));
        }
    }

    //帧回调事件
    export class UpdateEvent {
        //定义
        private static action_table: ((this: void) => void)[] = [];
        static Action(this: void): void {
            let actions = UpdateEvent.action_table;
            let length = actions.length;
            for (let i = 0; i < actions.length; i++) {
                actions[i]();
                if (length > actions.length) {
                    length--;
                    i--;
                }
            }
        }
        private static ison = false;
        action: (this: void) => void;
        /**
         * 帧回调事件，返回动作，通过Null清楚动作
         * @param func
         */
        constructor(func: (this: void) => void) {
            this.action = func;
            UpdateEvent.action_table.push(func);
            if (UpdateEvent.ison == false) {
                UpdateEvent.ison = true;
                let Action = UpdateEvent.Action;
                jassjapi.DzFrameSetUpdateCallbackByCode(() => {
                    Action();
                });
            }
        }

        //
        Null() {
            let t = UpdateEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //
    export class PlayerLeaveEvent {
        //定义
        private static action_table: ((this: void, pl: PlayerComponent) => void)[] = [];
        static Action(this: void, pl: PlayerComponent): void {
            let actions: ((this: void, pl: PlayerComponent) => void)[] =
                PlayerLeaveEvent.action_table;
            if (actions) {
                for (let i = 0; i < actions.length; i++) {
                    actions[i](pl);
                }
            }
        }

        private static ison = false;
        action: (this: void, pl: PlayerComponent) => void;
        /**
         * 玩家离开事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，触发物品
         */
        constructor(func: (this: void, pl: PlayerComponent) => void) {
            if (PlayerLeaveEvent.action_table == null) {
                PlayerLeaveEvent.action_table = [];
            }
            this.action = func;
            let t = PlayerLeaveEvent.action_table;
            t.push(func);
            //ArrayInsert(PlayerLeaveEvent.action_table,func)
            if (PlayerLeaveEvent.ison == false) {
                PlayerLeaveEvent.ison = true;
                let trigger = jasscj.CreateTrigger();
                jassdbg.handle_ref(trigger);
                for (let i = 1; i <= 16; i++) {
                    let ev = jasscj.TriggerRegisterPlayerEvent(
                        trigger,
                        jasscj.PlayerComponent(i - 1),
                        jasscj.ConvertPlayerEvent(15)
                    );
                    jassdbg.handle_ref(ev);
                }
                let ac = jasscj.TriggerAddAction(trigger, () => {
                    let pl = WEtrigger.player;
                    PlayerLeaveEvent.Action(pl);
                });
                jassdbg.handle_ref(ac);
            }
        }

        //
        Null() {
            let t = PlayerLeaveEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //-----------------------------------------------
    //自定义伤害事件，不是原生，相当于自定义事件
    export class DamageEvent {
        //定义
        private static action_table: ((this: void, damage: Damage) => Damage)[] =
            [];
        //执行自定义伤害事件
        static Action(this: void, damage: Damage): Damage {
            let actions = DamageEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                damage = actions[i](damage);
            }
            return damage;
        }

        action: (this: void, damage: Damage) => Damage;
        /**
         * 伤害事件，造成实际伤害前触发，返回动作，通过Null清楚动作
         * @param func 参数：伤害来源，被伤害者，伤害值，是否是攻击，是否远程，伤害类型（自己定义字符串）
         */
        constructor(func: (this: void, damage: Damage) => Damage) {
            this.action = func;
            DamageEvent.action_table.push(func);
        }

        //
        Null() {
            let t = DamageEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //自定义事件：单位添加技能
    export class GetAbilityEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            ability: AbilityComponent
        ) => void)[] = [];
        static Action(this: void, unit: UnitComponent, ability: AbilityComponent): void {
            let actions = GetAbilityEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, ability);
            }
        }

        action: (this: void, unit: UnitComponent, ability: AbilityComponent) => void;
        /**
         * 技能添加事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，添加的技能
         */
        constructor(func: (this: void, unit: UnitComponent, ability: AbilityComponent) => void) {
            this.action = func;
            GetAbilityEvent.action_table.push(func);
        }

        //
        Null() {
            let t = GetAbilityEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //自定义事件：单位技能移除
    export class LostAbilityEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            ability: AbilityComponent
        ) => void)[] = [];
        static Action(this: void, unit: UnitComponent, ability: AbilityComponent): void {
            let actions = LostAbilityEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, ability);
            }
        }

        action: (this: void, unit: UnitComponent, ability: AbilityComponent) => void;
        /**
         * 技能移除事件，返回动作，通过Null清楚动作
         * @param func 参数：触发单位，移除的技能
         */
        constructor(func: (this: void, unit: UnitComponent, ability: AbilityComponent) => void) {
            this.action = func;
            LostAbilityEvent.action_table.push(func);
        }

        //
        Null() {
            let t = LostAbilityEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //自定义事件：单位死亡事件
    export class UnitDeathEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            killer: UnitComponent
        ) => void)[] = [];
        static Action(this: void, unit: UnitComponent, killer: UnitComponent): void {
            let actions = UnitDeathEvent.action_table;
            let length = actions.length;
            for (let i = 0; i < length; i++) {
                actions[i](unit, killer);
                if (length > actions.length) {
                    length--;
                    i--;
                }
            }
        }

        action: (this: void, unit: UnitComponent, killer: UnitComponent) => void;
        /**
         * 单位事件，返回动作，通过Null清楚动作
         * @param func 参数：死亡单位，击杀单位（可为空值）
         */
        constructor(func: (this: void, unit: UnitComponent, killer: UnitComponent) => void) {
            this.action = func;
            UnitDeathEvent.action_table.push(func);
        }

        //
        Null() {
            let t = UnitDeathEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //自定义事件：召唤单位事件
    export class SummonUnitEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            unit2: UnitComponent
        ) => void)[] = [];
        /**
         * @param this 
         * @param unit 召唤者
         * @param unit2 召唤单位
         */
        static Action(this: void, unit: UnitComponent, unit2: UnitComponent): void {
            let actions = SummonUnitEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                actions[i](unit, unit2);
            }
        }

        action: (this: void, unit: UnitComponent, unit2: UnitComponent) => void;
        /**
         * 召唤单位事件，返回动作，通过Null清楚动作
         * @param func 参数：召唤者，召唤单位
         */
        constructor(func: (this: void, unit: UnitComponent, unit2: UnitComponent) => void) {
            this.action = func;
            SummonUnitEvent.action_table.push(func);
        }

        //
        Null() {
            let t = SummonUnitEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }

    //自定义事件：单位治疗
    export class HealUnitEvent {
        //定义
        private static action_table: ((
            this: void,
            unit: UnitComponent,
            unit2: UnitComponent,
            n: number
        ) => number)[] = [];
        static Action(this: void, unit: UnitComponent, unit2: UnitComponent, n: number): number {
            let actions = HealUnitEvent.action_table;
            for (let i = 0; i < actions.length; i++) {
                n = actions[i](unit, unit2, n);
            }
            return n;
        }

        action: (this: void, unit: UnitComponent, unit2: UnitComponent, n: number) => number;
        /**
         * 召唤单位事件，返回动作，通过Null清楚动作
         * @param func 参数：治疗单位，被治疗单位，治疗值
         */
        constructor(
            func: (this: void, unit: UnitComponent, unit2: UnitComponent, n: number) => number
        ) {
            this.action = func;
            HealUnitEvent.action_table.push(func);
        }

        //
        Null() {
            let t = HealUnitEvent.action_table;
            t.splice(t.indexOf(this.action));
        }
    }
}
