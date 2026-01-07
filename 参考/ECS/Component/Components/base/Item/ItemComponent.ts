/** @noSelfInFile **/

import { Component } from "../../../Component";
import { Logger } from "../../../../../base/object/工具/logger";
import { Position } from "../../../../../base/math/Position";
import { EventTypes } from "../../../../types/EventTypes";
import { Entity } from "../../../..";
import { ItemId } from "../../../../../../output/ts/itemid";
import { xlsx_inte_keys_table_item, xlsx_data_table_item } from "../../../../../../output/ts/xlsx_table_item";
import { ItemTips } from "../../../../../System/界面系统/物品/ItemTips";
import { PlayerComponent, UnitComponent } from "../../..";
import { Color } from "../../../../../base/object/工具/Color";
import { ItemQualityMap, ITEM_SCHEMA_KEYS } from "../../../../Schema/Schemas/ItemSchema";
import { TextTagUI } from "../../../../../base/GUI/UI/TextTagUI";
import { BackSound } from "../../../../../base/object/war3/Sound";
import { ItemType } from "../../../../../base/object/事件/ItemType";
import { SpecialEffect } from "../../../../../base/object/事件/SpecialEffect";
import { ItemUI } from "../../../../../System/界面系统/物品/ItemUI";
import { Setting } from "../../../../../System/界面系统/系统/SettingUI";
import { War3Event } from "../../../../EventSystem/War3Event";
import { CooldownComponent } from "../../冷却/CooldownComponent";


const logger = Logger.createLogger("ItemComponent");

/**
 * 物品组件属性接口
 */
interface ItemComponentProps {
    /** 物品类型 */
    itemType: string;
    /** 物品位置 */
    position: Position,
    /** 物品所属玩家 */
    player?: PlayerComponent,
    /** 物品的模型路径，可选，默认 */
    itemModelPath?: string,
}

/**
 * 物品组件 - 管理War3物品的核心功能
 * 提供物品的基础属性、模型显示、堆叠管理等功能
 */
export class ItemComponent extends Component<ItemComponentProps> {
    // 组件类型名称
    protected static readonly TYPE: string = "ItemComponent";

    // 存ItemComponent，用GetHandleId作为索引
    private static _itemComponents: Map<number, ItemComponent> = new Map<number, ItemComponent>();
    // War3物品句柄
    private item: any;

    //拥有该物品的单位，获取物品时更新
    private unit: UnitComponent;
    //物品所属玩家
    private player: PlayerComponent;
    //物品模型路径
    private itemModelPath: string;
    //用特效模拟物品模型，单位拾取物品，删除特效，丢弃物品，创建特效
    private itemModelEffect: Effect;
    //地面上的物品物品提示UI，显示图标和名称
    private itemTipsUi: ItemTips;

    // 堆叠相关
    private isStackable: boolean;
    private maxStack: number;
    private currentStack: number;

    // 物品状态
    private isOnGround: boolean = true;
    private isInInventory: boolean = false;
    private ownerUnit: Entity | null = null;

    /**
     * 获取组件类型
     */
    static getType(): string {
        return ItemComponent.TYPE;
    }

    /**
     * 构造函数
     */
    constructor(owner: Entity, props?: ItemComponentProps) {
        super(owner, props || {});
    }

    /**
     * 初始化组件
     */
    initialize(): void {
        this.ItemInit()

        if (!this.item) {
            logger.error("ItemComponent initialized without item handle");
            return;
        }

        // 创建模型特效
        this.createModelEffect();

        // 设置事件监听器
        this.setupEventListeners();

        logger.debug(`ItemComponent initialized for item type: ${this.itemType}`);
    }

    /**
     * 更新组件
     */
    update(deltaTime: number): void {

        // 更新特效
        this.updateEffects();

        // 更新物品状态
        this.updateItemStatus();
    }

    /**
     * 销毁组件
     */
    destroy(): void {
        // 清理引用
        this.ownerUnit = null;

        this.position = this.position;

        // TODO删除物品模型特效
        if (this.itemModelEffect) {
            this.itemModelEffect.Null();
            this.itemModelEffect = null;
        }
        //删除地面上的物品物品提示UI
        if (this.itemTipsUi) {
            this.itemTipsUi.Null();
            this.itemTipsUi = null;
        }
        // 从物品组件映射中移除
        ItemComponent._itemComponents.delete(jasscj.GetHandleId(this.item));
        // 减少handle引用计数
        jassdbg.handle_unref(this.item);
        // 从游戏中移除物品
        jasscj.RemoveItem(this.item);
        // 清空物品句柄
        this.item = null;

        logger.debug(`ItemComponent destroyed for item type: ${this.itemType}`);
    }

    // =========================================================================================


    /**
     * 物品初始化
     */
    private ItemInit() {
        // 获取物品类型名称
        this.itemType = this.props.itemType || "unknown";
        // 获取物品生成位置
        let position = this.props.position;
        // 根据物品类型创建War3物品
        let handle = jasscj.CreateItem(ItemId[this.props.itemType], position.X, position.Y);
        // 获取自定义物品模型路径(可选)
        let modelPath = this.props?.itemModelPath;
        // 获取物品所属玩家(可选)
        let pl = this.props?.player;
        //物品专属玩家
        if (pl) {
            this.player = pl;
        }

        // 获取物品数据
        let itemdata = xlsx_data_table_item[this.itemType];
        if (!itemdata) {
            logger.error(`创建物品失败，物品类型： “${this.itemType}” 不存在`);
            return;
        }

        //将属性写入
        xlsx_inte_keys_table_item.forEach((key1) => {
            if (ITEM_SCHEMA_KEYS.has(key1)) {
                //@ts-ignore
                this.owner.data.item.set(key1, itemdata[key1]);
            } else {
                logger.info(`Schema Key '${key1}' 在 Inte_ItemSchema 中未找到，跳过。`);
            }
        });

        //将物品写入物品列表
        ItemComponent._itemComponents.set(jasscj.GetHandleId(handle), this);
        //引用计数
        jassdbg.handle_ref(handle);

        //根据品级设置名字颜色
        let n = this.level;
        // 物品描述
        this.tips = ItemComponent.SetNameColor(this.itemType, n);
        //图标，没有默认奶酪图标
        this.icon = xlsx_data_table_item[this.itemType].icon ?? "ReplaceableTextures\\CommandButtons\\BTNCheese.blp";

        //物品模型路径
        if (modelPath) {
            this.itemModelPath = modelPath;
        } else {
            this.itemModelPath = xlsx_data_table_item[this.itemType].model ?? "Objects\\InventoryItems\\TreasureChest\\treasurechest.mdl";
        }
        //创建特效模拟物品模型
        let p = new Position(position.X, position.Y, 0);
        this.itemModelEffect = Effect.New({
            path: this.itemModelPath,
            position: p,
            //size: 0.6,
        });
        this.itemModelEffect.rotateZ = 270;
        // TODO地面上的物品物品提示UI，显示图标和名称
        // this.itemTipsUi = ItemTips.New(this);
    }

    /**
     * 通过物品handleid获取物品对象
     * @param handle 物品handle
     * @returns 物品
     */
    static I(handle: any): ItemComponent | null {
        return ItemComponent._itemComponents.get(jasscj.GetHandleId(handle))
    }

    // ==========================================================================================
    //物品类型
    get itemType(): string {
        return this.owner.data.item.get("itemType");
    }
    set itemType(s: string) {
        this.owner.data.item.set("itemType", s);
    }

    //根据物品等级改变名字的颜色
    static SetNameColor(name: string, level: number) {
        return Color.SetColorByRGB(name, ColorRGB[ItemQualityMap.get(level).rgb]);
    }

    //品级
    set level(n: number) {
        this.owner.data.item.set("品级", n);
        //根据物品等级改变名字的颜色
        this.tips = ItemComponent.SetNameColor(this.itemType, n);
    }
    get level() {
        return this.owner.data.item.get("品级");
    }

    //handleId:I004对应1048980
    get id(): number {
        return jasscj.GetHandleId(this.item);
    }
    //类型ID,I004=1227894836
    get typeid(): number {
        return jasscj.GetItemTypeId(this.item);
    }
    //类型ID字符串'I004'，这个一般没用
    get typeIdStr(): string {
        return ItemId[this.itemType];
    }

    //数量/使用次数
    set nums(n: number) {
        this.owner.data.item.set("数量", n);
        if (n <= 0) {
            this.Null();
        }
    }
    get nums() {
        return this.owner.data.item.get("数量");
    }
    //物品描述，根据物品等级改变物品名称颜色
    set tips(s: string) {
        this.owner.data.item.set("tips", s);
    }
    get tips(): string {
        return this.owner.data.item.get("tips");
    }

    //图标
    set icon(s: string) {
        //TODO 图标要模拟
        jassjapi.EXSetItemDataString(this.typeid, 1, s);
        this.owner.data.item.set("图标", s);
    }
    get icon() {
        return this.owner.data.item.get("图标") || jassjapi.EXGetItemDataString(this.typeid, 1);
    }

    //位置
    get position() {
        return new Position(
            jasscj.GetItemX(this.item),
            jasscj.GetItemY(this.item),
            0
        );
    }
    set position(pt: Position) {
        jasscj.SetItemPosition(this.item, pt.X, pt.Y);
        //如果物品没有单位，即放在地上，则移动特效
        if (!this.unit && this.itemModelEffect) {
            // this.itemModelEffect.position = new Position(pt.X, pt.Y, pt.terrianZ);
        }
    }
    //--------
    //获取物编数据
    GetProperty(type: any): string {
        return jassslk.item[jasscj.GetItemTypeId(this.item)][type];
    }
    //引用计数，使用时+1，使用完-1，为0时释放handle
    // 引用计数已移除，现在由ECS框架的EntityManager统一管理
    //删除
    Null() {

        // 物品清理现在由ECS框架的EntityManager统一管理
    }

    /**
   * 获取范围内的物品
   * @param center 中心
   * @param r 范围
   * @returns 物品
   */
    static GetItemsInRange(
        center: Position,
        r: number,
        filter?: (item: ItemComponent) => boolean
    ) {
        // TODO: 使用ECS框架的ComponentManager获取所有ItemComponent实例
        // return ComponentManager.getAllComponents(ItemComponent).filter(item => {
        //     return !item.unit && item.position.DistToPosition(center) <= r && (!filter || filter(item));
        // });

        // 临时返回空数组，需要实现ECS框架的组件查询逻辑
        return [];
    }

    //初始化
    static onInit() {
        //获取物品事件
        new War3Event.UnitGetItemEvent((u: UnitComponent, item: ItemComponent) => {
            if (item == null) {
                return;
            }
            //如果物品有专属玩家，而且玩家开启了物品锁定，则不能被其他玩家获取
            if (item.player) {
                if (
                    //TODO Setting.ts
                    Setting.GetPlayerSetting(item.player, "物品锁定") &&
                    (item.player != u.playerComponent)
                ) {
                    item.position = item.position;
                    //TODO Msg单独Component
                    u.playerComponent.Msg("该物品属于玩家" + item.player.id);
                    return;
                }
            }
            //重置物品归属
            item.player = u.playerComponent;
            //设置物品所属单位
            item.unit = u;
            //删除物品模型特效
            if (item.itemModelEffect) {
                item.itemModelEffect.Null();
                item.itemModelEffect = null;
            }
            //更新物品UI
            ItemUI.Update(u.playerComponent);

            //类型为消耗品，数量叠加
            let itemName = item.itemType;
            if (item.owner.data.item.get("类型") == "消耗品" && u.HasItemNum(itemName) == 2) {
                let a = item.nums; //使用次数
                item.Null();  //删除获取物品，并将使用次数加在同类物品上
                let it2 = u.GetItemByName(itemName);
                it2.nums += a;
                return;
            }
            //非消耗品/第一次获取消耗品，直接添加到装备栏
            u.getOwner().addRelationship(item.owner, item.owner.entityRelationshipType.UNIT_TO_ITEM)
            // u.items.push(item);
            item.owner.data.item.set("在装备栏", true);

            //执行获取事件
            item.owner.emit(EventTypes.ITEM_ACQUIRED, {
                entity: item.owner,
                item: item.item,
                acquirer: u
            });
            //TODO 获取装备时为单位添加物品属性.
            // u.AddItemAttribute(item);
            //NPC获得物品，
            if (u.owner.data.unit.get("类别") == "NPC") {
                let price = item.owner.data.item.get("价格");
                if (price > 0) {
                    //售卖物品音效
                    BackSound.PlayFalse(u.playerComponent, BackSound.backsound.购买物品);
                    //删除物品
                    item.Null();
                    //显示售卖物品获得的金币漂浮文字
                    TextTagUI.Create(Color.SetColorByRGB(tostring(price), ColorRGB.橙色), u.position);
                    //TODO 增加金币
                    u.playerComponent.owner.data.player.gold += price;
                } else {
                    //没有价格，掉落物品
                    item.position = item.position;
                }
            }
        });

        //售卖/丢弃物品回调函数
        let func = (u: UnitComponent, it: ItemComponent) => {
            //单位不存在/物品不存在/物品没有所属单位，直接返回
            if (!u || !it || !it.unit) {
                return;
            }
            //物品所属单位null
            it.unit = null;
            //物品不在装备栏
            it.data.item.set("在装备栏", false);
            //从单位装备栏中删除
            u.getOwner().removeRelationship(it.owner, it.owner.entityRelationshipType.UNIT_TO_ITEM)
            // u.items.splice(u.items.indexOf(it));

            //将物品事件从单位上移除
            // SpecialEffect.RemoveEffect(u, it);
            //执行失去物品事件
            it.owner.emit(EventTypes.ITEM_DROPPED, {
                entity: it.owner,
                item: it.item,
                dropper: u
            });

            //TODO 减少属性
            // Item.AddAttri(u, it, false)
            //删除计时器
            if (it.timer) {
                it.timer.Null();
                it.timer = null;
            }
        };

        new War3Event.UnitDropItemEvent(func);//丢弃物品
        new War3Event.UnitSellItemEvent(func);//售卖物品

        //丢弃物品单独执行：创建物品模型特效
        new War3Event.UnitDropItemEvent((u, it) => {
            Timer.RunLater(0, () => {
                if (!u) {//单位不存在
                    return;
                }
                //更新物品UI
                ItemUI.Update(u.playerComponent);
                if (it && it.item) {
                    //如果物品不在装备栏，则创建模拟物品模型特效
                    if (!it.data.item.get("在装备栏")) {
                        let p1 = it.position;
                        let pt = new Position(p1.X, p1.Y, 0);
                        it.itemModelEffect = Effect.New({
                            path: it.itemModelPath,
                            position: pt,
                            //size: 0.6,
                        });
                        it.itemModelEffect.rotateZ = 270;
                    }
                }
            });
        });

        //使用装备
        new War3Event.UnitUseItemEvent((u, it) => {
            it.nums--;
        });

        //装备满时
        new War3Event.OrderTargetEvent((u, it, order) => {
            if (order == "smart" && typeof it == "Item") {
                let item = it as ItemComponent;
                if (u.BagIsFull()) {
                }
            }
        });
    }














    // =========================================================================================

    /**
     * 创建模型特效
     */
    private createModelEffect(): void {
        if (!this.itemModelPath || !this.isOnGround) return;

        try {
            this.itemModelEffect = new Effect(this.itemModelPath, this.position.X, this.position.Y, 0);

            // 获取物品数据组件
            const itemData = this.owner.data.item;
            if (itemData) {
                // 根据品质添加光效
                const quality = itemData.get("物品品质");
                this.createQualityEffect(quality);
            }

        } catch (error) {
            logger.error(`Failed to create item model effect: ${error}`);
        }
    }

    /**
     * 创建品质特效
     */
    private createQualityEffect(quality: string): void {
        let effectPath = "";

        switch (quality) {
            case "优秀":
                effectPath = "Abilities\\Spells\\Other\\Charm\\CharmTarget.mdl";
                break;
            case "稀有":
                effectPath = "Abilities\\Spells\\Human\\ManaFlare\\ManaFlareTarget.mdl";
                break;
            case "史诗":
                effectPath = "Abilities\\Spells\\Other\\Doom\\DoomTarget.mdl";
                break;
        }

        if (effectPath) {
            try {
                this.glowEffect = new Effect(effectPath, this.position.X, this.position.Y, 0);
            } catch (error) {
                logger.error(`Failed to create quality effect: ${error}`);
            }
        }
    }

    /**
     * 更新特效
     */
    private updateEffects(): void {
        if (this.itemModelEffect && this.isOnGround) {
            this.itemModelEffect.setPosition(this.position.X, this.position.Y, 0);
        }

        if (this.glowEffect && this.isOnGround) {
            this.glowEffect.setPosition(this.position.X, this.position.Y, 0);
        }
    }

    /**
     * 更新物品状态
     */
    private updateItemStatus(): void {
        if (!this.item) return;

        // 检查物品是否被拾取
        const currentOwner = jasscj.GetItemPlayer(this.item);
        const wasOnGround = this.isOnGround;

        this.isOnGround = (currentOwner === null);
        this.isInInventory = !this.isOnGround;

        // 状态变化处理
        if (wasOnGround && !this.isOnGround) {
            // 物品被拾取
            this.onItemPickedUp(currentOwner);
        } else if (!wasOnGround && this.isOnGround) {
            // 物品被丢弃
            this.onItemDropped();
        }

        // 获取物品数据组件
        const itemData = this.owner.data.item;
        if (itemData) {
            // 更新数据组件属性
            itemData.set("在地面上", this.isOnGround);
            itemData.set("在背包中", this.isInInventory);
        }
    }

    /**
     * 设置事件监听器
     */
    private setupEventListeners(): void {
        // 监听物品使用事件
        this.owner.on(EventTypes.ITEM_USED, (data) => {
            this.onItemUsed(data);
        });

        // 监听堆叠变化事件
        this.owner.on(EventTypes.ITEM_STACK_CHANGED, (data) => {
            this.onStackChanged(data);
        });

        // 监听属性变化
        this.owner.on(EventTypes.DATA_PROPERTY_CHANGED, (data) => {
            this.onPropertyChanged(data);
        });
    }

    /**
     * 物品被拾取处理
     */
    private onItemPickedUp(playerHandle: any): void {
        // 隐藏地面特效
        if (this.itemModelEffect) {
            this.itemModelEffect.setVisible(false);
        }
        if (this.glowEffect) {
            this.glowEffect.setVisible(false);
        }

        this.owner.emit(EventTypes.ITEM_ACQUIRED, {
            entity: this.owner,
            item: this.item,
            acquirer: playerHandle
        });

        logger.debug(`Item picked up: ${this.itemType}`);
    }

    /**
     * 物品被丢弃处理
     */
    private onItemDropped(): void {
        // 显示地面特效
        if (this.itemModelEffect) {
            this.itemModelEffect.setVisible(true);
        }
        if (this.glowEffect) {
            this.glowEffect.setVisible(true);
        }

        this.owner.emit(EventTypes.ITEM_DROPPED, {
            entity: this.owner,
            item: this.item,
            position: this.position
        });

        logger.debug(`Item dropped: ${this.itemType}`);
    }

    /**
     * 物品使用处理
     */
    private onItemUsed(data: any): void {
        // 获取物品数据组件
        const itemData = this.owner.data.item;
        if (!itemData) return;

        // 如果是消耗品，减少堆叠数量
        const isConsumable = itemData.get("是否消耗品") || false;
        if (isConsumable && this.currentStack > 0) {
            this.currentStack--;
            itemData.set("当前堆叠", this.currentStack);

            if (this.currentStack <= 0) {
                // 物品用完，销毁
                this.owner.emit(EventTypes.ITEM_DESTROYED, {
                    entity: this.owner,
                    item: this.item
                });
                this.destroy();
            }
        }

        logger.debug(`Item used: ${this.itemType}`);
    }

    /**
     * 堆叠变化处理
     */
    private onStackChanged(data: any): void {
        this.currentStack = data.newStack;

        // 获取物品数据组件
        const itemData = this.owner.data.item;
        if (itemData) {
            itemData.set("当前堆叠", this.currentStack);
        }

        logger.debug(`Item stack changed: ${this.itemType}, new stack: ${this.currentStack}`);
    }

    /**
     * 属性变化处理
     */
    private onPropertyChanged(data: any): void {
        switch (data.key) {
            case "物品品质":
                // 品质变化时更新特效
                this.destroyEffects();
                if (this.isOnGround) {
                    this.createModelEffect();
                }
                break;
        }
    }

    // ==================== 公共API方法 ====================

    /**
     * 获取War3物品句柄
     */
    getItemHandle(): any {
        return this.item;
    }

    /**
     * 获取物品位置
     */
    getPosition(): Position {
        return new Position(this.position.X, this.position.Y);
    }

    /**
     * 设置物品位置
     */
    setPosition(x: number, y: number): void {
        if (!this.item) return;

        jasscj.SetItemPosition(this.item, x, y);
        this.position.X = x;
        this.position.Y = y;

        this.owner.emit(EventTypes.ITEM_POSITION_CHANGED, { x, y, item: this.owner });
    }

    /**
     * 检查是否可以堆叠
     */
    canStackWith(otherItem: Entity): boolean {
        if (!this.isStackable) return false;

        const otherItemData = otherItem.data.item;
        if (!otherItemData) return false;

        const otherType = otherItemData.get("物品类型");
        const otherStackable = otherItemData.get("可堆叠");

        return otherStackable && otherType === this.itemType;
    }

    /**
     * 堆叠物品
     */
    stackWith(otherItem: Entity, amount: number): boolean {
        if (!this.canStackWith(otherItem)) return false;

        const newStack = this.currentStack + amount;
        if (newStack > this.maxStack) return false;

        this.currentStack = newStack;

        // 获取物品数据组件
        const itemData = this.owner.data.item;
        if (itemData) {
            itemData.set("当前堆叠", this.currentStack);
        }

        this.owner.emit(EventTypes.ITEM_STACK_CHANGED, {
            oldStack: this.currentStack - amount,
            newStack: this.currentStack,
            item: this.owner
        });

        return true;
    }

    /**
     * 分离堆叠
     */
    splitStack(amount: number): Entity | null {
        if (!this.isStackable || amount >= this.currentStack) return null;

        // 减少当前堆叠
        this.currentStack -= amount;

        // 获取物品数据组件
        const itemData = this.owner.data.item;
        if (itemData) {
            itemData.set("当前堆叠", this.currentStack);
        }

        // 创建新的物品对象（这里需要具体的实现）
        // 返回新创建的物品Entity

        this.owner.emit(EventTypes.ITEM_STACK_SPLIT, {
            originalStack: this.currentStack + amount,
            remainingStack: this.currentStack,
            splitAmount: amount,
            item: this.owner
        });

        return null; // 需要具体实现
    }

    /**
     * 设置物品可见性
     */
    setVisible(visible: boolean): void {
        if (!this.item) return;

        jasscj.SetItemVisible(this.item, visible);

        // 同时控制特效可见性
        if (this.itemModelEffect) {
            this.itemModelEffect.setVisible(visible);
        }
        if (this.glowEffect) {
            this.glowEffect.setVisible(visible);
        }
    }

    /**
     * 检查物品是否在地面上
     */
    isItemOnGround(): boolean {
        return this.isOnGround;
    }

    /**
     * 检查物品是否在背包中
     */
    isItemInInventory(): boolean {
        return this.isInInventory;
    }
    /**
     * 使用物品
     */
    useItem(target?: Entity): void {
        this.owner.emit(EventTypes.ITEM_USED, {
            target,
            item: this.owner
        });
    }

    // ========================= 冷却系统相关方法 =========================

    /**
     * 获取冷却组件
     * @returns 冷却组件实例或null
     */
    private getCooldownComponent(): CooldownComponent | null {
        return this.owner.getComponent(CooldownComponent);
    }

    /**
     * 检查物品是否在冷却中
     * @returns 是否在冷却中
     */
    isOnCooldown(): boolean {
        const cooldownComponent = this.getCooldownComponent();
        return cooldownComponent ? cooldownComponent.isOnCooldownById("item_skill") : false;
    }

    /**
     * 获取冷却进度（0-1）
     * @returns 冷却进度
     */
    getCooldownProgress(): number {
        const cooldownComponent = this.getCooldownComponent();
        return cooldownComponent ? cooldownComponent.getCooldownProgressById("item_skill") : 0;
    }

    /**
     * 重置物品冷却
     */
    resetCooldown(): void {
        const cooldownComponent = this.getCooldownComponent();
        if (cooldownComponent) {
            cooldownComponent.resetCooldownById("item_skill");
        }

        // 同步到数据层（保持兼容性）
        this.owner.data.item.set("剩余冷却时间", 0);
        this.owner.data.item.set("冷却计时器Id", null);
    }

    /**
     * 暂停物品冷却
     */
    pauseCooldown(): void {
        const cooldownComponent = this.getCooldownComponent();
        if (cooldownComponent) {
            cooldownComponent.pauseCooldownById("item_skill");
        }
    }

    /**
     * 恢复物品冷却
     */
    resumeCooldown(): void {
        const cooldownComponent = this.getCooldownComponent();
        if (cooldownComponent) {
            cooldownComponent.resumeCooldownById("item_skill");
        }
    }
}
