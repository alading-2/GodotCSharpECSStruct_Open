/**
 * 技能初始化类
 * 负责处理技能和物品技能的初始化和使用逻辑
 */
import { ItemComponent, UnitComponent } from "../../..";
import { ItemAbilId } from "../../../../../../output/ts/itemabil";
import { xlsx_data_table_ability } from "../../../../../../output/ts/xlsx_table_ability";
import { TextTagUI } from "../../../../../base/GUI/UI/TextTagUI";
import { MyMath } from "../../../../../base/math/MyMath";
import { AbilityType } from "../../../../../base/object/事件/Abilitytype";
import { ItemType } from "../../../../../base/object/事件/ItemType";
import { SpecialEffect } from "../../../../../base/object/事件/SpecialEffect";
import { Ability } from "../../../../../base/object/技能/Ability";
import { War3Event } from "../../../../EventSystem/War3Event";
import { AbilityComponent } from "./AbilityComponent";


class AbilityInit {
    // 单例
    private static instance: AbilityInit;

    // 事件数据存储
    private eventData: {
        abilId: string,  // 技能ID
        unitComp: UnitComponent,  // 单位组件
    };

    private constructor() { }

    /**
     * 获取单例实例
     */
    public static getInstance(): AbilityInit {
        if (!AbilityInit.instance) {
            AbilityInit.instance = new AbilityInit();
        }
        return AbilityInit.instance;
    }
    // 第一位a：AbilityMethodType[abilitydata[name].method]	物品技能：'A'，被动: 'X', 无目标: 'X', 点: 'Z', 单位: 'Y'
    // 第二位b：(this.player.id - 1)(0 - 11)
    // 第三位c：技能按钮坐标x(0 - 3)
    // 第四位d：技能按钮坐标y(0 - 2)

    /**
     * 初始化技能系统
     * 注册技能施放事件监听
     */
    static onInit() {
        //施放技能
        new War3Event.UnitSpellEvent((unitComp) => {
            let abilInit = AbilityInit.getInstance();
            let abilId = abilInit.eventData.abilId;
            // 更新事件数据
            abilInit.eventData = {
                abilId: abilId,
                unitComp: unitComp,
            }
            let playComp = unitComp.playerComponent;
            unitComp = playComp.selectUnit;
            // 检查单位状态
            if (!unitComp || unitComp.pause) {
                return;
            }
            let s = abilId.substring(0, 1);
            if (s == "A") {
                // 物品技能
                abilInit.ItemAbilityInit();
            } else {
                // 普通技能
                abilInit.AbilityInit();

            }
        });
    }

    /**
     * 物品技能初始化和处理
     */
    ItemAbilityInit() {
        let abilId = this.eventData.abilId;
        let unitComp = this.eventData.unitComp;
        //物品主动技能
        //数据——目标类型DataB1：单位/点/无目标，无目标：0，单位：1，点：2，单位/点：3
        let targettype = MyMath.S2N(jassslk.ability[abilId]["DataB1"]);
        let itemName = ItemAbilId[abilId];

        // 获取目标
        let target;
        if (targettype == 1) {
            //单位目标
            if (War3Event.WEtrigger.targetitem) {
                target = War3Event.WEtrigger.targetitem;
            } else if (War3Event.WEtrigger.spelltarget) {
                target = War3Event.WEtrigger.spelltarget;
            } else {
                return;
            }
        } else {
            //点目标
            target = War3Event.WEtrigger.spellposition;
        }

        // 物品技能使用处理
        let itemComp: ItemComponent = unitComp.GetItemByName(itemName);

        if (itemComp) {
            // 检查冷却
            //@ts-ignore
            if (itemComp.owner.component.cooldown.isOnCooldown) {
                unitComp.playerComponent.Msg("物品冷却中！");
                return;
            }
            // itemComp.cooltime = itemComp.cool;
            unitComp.Stop();

            // 执行物品效果
            // if (ItemType.Data[itemName]) {
            //     if (targettype == 1) {
            //         //单位目标效果
            //         if (ItemType.Data[itemName].UseEffectTarget) {
            //             ItemType.Data[itemName].UseEffectTarget(unitComp, target, itemComp);
            //         }
            //     } else if (targettype == 2) {
            //         //点目标效果
            //         if (ItemType.Data[itemName].UseEffectLoc) {
            //             ItemType.Data[itemName].UseEffectLoc(unitComp, target, itemComp);
            //         }
            //     } else {
            //         //无目标效果
            //         if (ItemType.Data[itemName].UseEffect) {
            //             ItemType.Data[itemName].UseEffect(unitComp, itemComp);
            //         }
            //     }
            // }
        }
    }

    AbilityInit() {
        let abilId = this.eventData.abilId;
        let unitComp = this.eventData.unitComp;
        // 处理普通技能
        let x = MyMath.S2N(abilId.substring(2, 3));
        let y = MyMath.S2N(abilId.substring(3, 4));
        let p = Ability.Loc2Num(x, y);
        //TODO 根据unitEntity获取abilityEntity
        let abilEntity: AbilityComponent = unitComp.abilButton[p];
        // 技能验证检查
        if (!abilEntity) {
            return;
        }

        // 检查技能可用性
        let success = abilEntity.checkAvailability();
        if (!success) {
            TextTagUI.Create(`技能释放失败：${abilEntity.getUnavailableReason()}`, unitComp.position);
            return;
        }

        // 目标获取和验证
        let target;
        //目标类型：单位
        if (abilEntity.dataB == 1) {
            //目标物品
            if (War3Event.WEtrigger.targetitem) {
                if (abilEntity.targetType != "物品") {
                    TextTagUI.Create("不能对物品使用！", unitComp.position);
                    return;
                }
                target = War3Event.WEtrigger.targetitem;
            }
            //施法目标单位
            else if (War3Event.WEtrigger.spelltarget) {
                target = War3Event.WEtrigger.spelltarget;
                // 目标类型检查
                if (abilEntity.targetType == "敌人" && !unitComp.IsEnemy(target.player)) {
                    TextTagUI.Create("不能对友军使用！", unitComp.position);
                    return;
                }
                if (abilEntity.targetType == "友军" && unitComp.IsEnemy(target.player)) {
                    TextTagUI.Create("不能对敌人使用！", unitComp.position);
                    return;
                }
                if (abilEntity.targetType == "物品") {
                    TextTagUI.Create("不能对单位使用！", unitComp.position);
                    return;
                }
            } else {
                return;
            }
        }
        //目标类型：点
        else if (abilEntity.dataB == 2) {
            target = War3Event.WEtrigger.spellposition;
        }
        //目标类型：单位/点
        else if (abilEntity.dataB == 3) {
            if (War3Event.WEtrigger.spellposition) {
                target = War3Event.WEtrigger.spellposition;
            } else if (War3Event.WEtrigger.targetitem) {
                target = War3Event.WEtrigger.targetitem.position;
            } else if (War3Event.WEtrigger.spelltarget) {
                target = War3Event.WEtrigger.spelltarget;
            }
        }

        // 释放技能
        abilEntity.castAbility();
        unitComp.Stop();

        // 处理施法动作
        if (abilEntity.action && abilEntity.action != "") {
            unitComp.Action(abilEntity.action);
            unitComp.AddAction("stand");
        }

        // 执行技能效果
        // let abiltp: AbilityType = AbilityType.Data[abilEntity.name];
        // if (abiltp) {
        //     if (abilEntity.dataB == 1) {
        //         //单位目标技能效果
        //         if (abiltp.UseEffectTarget) {
        //             abiltp.UseEffectTarget(unitComp, target, abilEntity);
        //         }
        //     } else if (abilEntity.dataB == 2) {
        //         //点目标技能效果
        //         if (abiltp.UseEffectLoc) {
        //             abiltp.UseEffectLoc(unitComp, target, abilEntity);
        //         }
        //     } else if (abilEntity.dataB == 3) {
        //         //单位/点目标技能效果
        //         if (abiltp.UseEffectLoc) {
        //             abiltp.UseEffectLoc(unitComp, target, abilEntity);
        //         }
        //     } else {
        //         //无目标技能效果
        //         if (abiltp.UseEffect) {
        //             abiltp.UseEffect(unitComp, abilEntity);
        //         }
        //     }
        // }
    }

}
