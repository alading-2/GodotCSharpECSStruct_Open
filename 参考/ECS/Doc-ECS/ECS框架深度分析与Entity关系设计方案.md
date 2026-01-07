# ECS 框架深度分析与 Entity 关系设计方案

## 🎯 核心问题分析

### 用户理解验证

**✅ 用户的理解是正确的！**

作为资深游戏设计师，基于对 Unity ECS、Unreal Engine Component 等现代游戏架构的深度理解，用户的架构思路完全符合现代游戏设计标准：

1. **Item 应该作为独立的 ItemEntity** - 正确！
2. **Player 应该作为独立的 PlayerEntity** - 正确！
3. **Ability、Buff 也应该作为独立 Entity** - 正确！
4. **Entity 之间需要关系管理系统** - 正确！

## 📋 问题 1：Entity 之间的关系与 Entity-Component 关系

### 1.1 现代游戏架构标准

#### Unity ECS 模式参考：

```typescript
// Entity: 纯容器，只负责ID和组件管理
// Component: 纯数据，不包含逻辑
// System: 纯逻辑，通过查询访问数据
```

#### Unreal Engine Component 模式参考：

```typescript
// Actor: 容器和生命周期管理
// ActorComponent: 功能模块化
// 数据与表现分离
```

### 1.2 正确的 Entity 层次结构

```typescript
// 基础Entity层次
GameEntity (抽象基类)
├── UnitEntity (单位Entity)
├── ItemEntity (物品Entity)
├── PlayerEntity (玩家Entity)
├── AbilityEntity (技能Entity)
├── BuffEntity (BuffEntity)
└── ProjectileEntity (投射物Entity)
```

### 1.3 Entity 与 Component 的正确关系

```typescript
// ✅ 正确的设计
class UnitEntity extends GameEntity {
  constructor(unitType: string) {
    super("UnitEntity", generateId());

    this.addComponent(EffectComponent); // 视觉表现（原RenderComponent已并入EffectComponent）
    this.addComponent(LifecycleComponent); // 生命周期

    // 添加数据管理器
    this.addDataManager("unit", { unitType });
    this.addDataManager("attr", { 当前生命值: 100 });
  }
}

class ItemEntity extends GameEntity {
  constructor(itemType: string) {
    super("ItemEntity", generateId());

    this.addComponent(EffectComponent); // 视觉表现（原RenderComponent已并入EffectComponent）
    this.addComponent(StackComponent); // 堆叠管理

    // 添加数据管理器
    this.addDataManager("item", { itemType });
  }
}
```

### 1.4 Entity 关系管理系统

```typescript
// Entity关系管理器
class EntityRelationshipManager {
  private relationships: Map<string, Set<string>> = new Map();

  // 建立关系
  addRelationship(
    parentId: string,
    childId: string,
    relationType: string
  ): void {
    const key = `${parentId}:${relationType}`;
    if (!this.relationships.has(key)) {
      this.relationships.set(key, new Set());
    }
    this.relationships.get(key)!.add(childId);
  }

  // 获取关系
  getRelatedEntities(entityId: string, relationType: string): string[] {
    const key = `${entityId}:${relationType}`;
    return Array.from(this.relationships.get(key) || []);
  }

  // 移除关系
  removeRelationship(
    parentId: string,
    childId: string,
    relationType: string
  ): void {
    const key = `${parentId}:${relationType}`;
    this.relationships.get(key)?.delete(childId);
  }
}

// 使用示例
const relationshipManager = new EntityRelationshipManager();

// 单位拾取物品
relationshipManager.addRelationship(
  unitEntity.getId(),
  itemEntity.getId(),
  "inventory"
);

// 玩家拥有单位
relationshipManager.addRelationship(
  playerEntity.getId(),
  unitEntity.getId(),
  "owns"
);

// 单位拥有Buff
relationshipManager.addRelationship(
  unitEntity.getId(),
  buffEntity.getId(),
  "buffs"
);
```

## 🔧 问题 2：ECS 中 System 的作用与示例

### 2.1 System 的核心作用

在现代 ECS 架构中，System 负责：

1. **纯逻辑处理** - 不包含数据，只处理业务逻辑
2. **跨 Entity 操作** - 处理多个 Entity 之间的交互
3. **性能优化** - 批量处理相同类型的操作
4. **解耦组件** - 避免组件间直接依赖

### 2.2 System 基类设计

```typescript
/**
 * System基类 - 现代ECS架构的核心
 */
export abstract class System {
  protected name: string;
  protected isEnabled: boolean = true;
  protected priority: number = 0;

  constructor(name: string, priority: number = 0) {
    this.name = name;
    this.priority = priority;
  }

  // 系统初始化
  abstract initialize(): void;

  // 系统更新 - 每帧调用
  abstract update(deltaTime: number): void;

  // 系统销毁
  abstract destroy(): void;

  // 获取系统名称
  getName(): string {
    return this.name;
  }

  // 启用/禁用系统
  setEnabled(enabled: boolean): void {
    this.isEnabled = enabled;
  }

  isSystemEnabled(): boolean {
    return this.isEnabled;
  }
}
```

### 2.3 具体 System 示例

#### 2.3.1 物品拾取系统

```typescript
/**
 * 物品拾取系统 - 处理单位与物品的交互
 */
export class ItemPickupSystem extends System {
  private entityManager: EntityManager;
  private relationshipManager: EntityRelationshipManager;

  constructor(
    entityManager: EntityManager,
    relationshipManager: EntityRelationshipManager
  ) {
    super("ItemPickupSystem", 100);
    this.entityManager = entityManager;
    this.relationshipManager = relationshipManager;
  }

  initialize(): void {
    // 监听物品拾取事件
    EventBus.on("Item.PickupRequested", this.handleItemPickup.bind(this));
  }

  update(deltaTime: number): void {
    if (!this.isEnabled) return;

    // 检查范围内的物品拾取
    this.checkAutoPickup();
  }

  private handleItemPickup(data: { unitId: string; itemId: string }): void {
    const unit = EntityManager.get(data.unitId) as UnitEntity;
    const item = EntityManager.get(data.itemId) as ItemEntity;

    if (!unit || !item) return;

    // 检查是否可以拾取
    if (this.canPickupItem(unit, item)) {
      this.performPickup(unit, item);
    }
  }

  private canPickupItem(unit: UnitEntity, item: ItemEntity): boolean {
    // 检查背包空间
    const inventory = this.relationshipManager.getRelatedEntities(
      unit.getId(),
      "inventory"
    );
    if (inventory.length >= 6) return false;

    // 检查距离

    if (!unitPos || !itemPos) return false;

    return unitPos.distanceTo(itemPos) <= 150;
  }

  private performPickup(unit: UnitEntity, item: ItemEntity): void {
    // 建立拾取关系
    this.relationshipManager.addRelationship(
      unit.getId(),
      item.getId(),
      "inventory"
    );

    // 隐藏物品（通过特效/显示控制）
    item.getComponent(EffectComponent)?.setVisible(false);

    // 发射拾取成功事件
    EventBus.emit("Item.PickedUp", {
      unitId: unit.getId(),
      itemId: item.getId(),
    });
  }

  private checkAutoPickup(): void {
    // 获取所有单位和地面物品
    const units = EntityManager.getByType("UnitEntity");
    const groundItems = EntityManager.getByType("ItemEntity").filter(
      (item) => !this.isItemInInventory(item.getId())
    );

    // 检查自动拾取
    for (const unit of units) {
      for (const item of groundItems) {
        if (this.canPickupItem(unit as UnitEntity, item as ItemEntity)) {
          this.performPickup(unit as UnitEntity, item as ItemEntity);
        }
      }
    }
  }

  private isItemInInventory(itemId: string): boolean {
    // 检查物品是否已在某个单位的背包中
    const allUnits = EntityManager.getByType("UnitEntity");
    for (const unit of allUnits) {
      const inventory = this.relationshipManager.getRelatedEntities(
        unit.getId(),
        "inventory"
      );
      if (inventory.includes(itemId)) return true;
    }
    return false;
  }

  destroy(): void {
    EventBus.off("Item.PickupRequested", this.handleItemPickup.bind(this));
  }
}
```

#### 2.3.2 Buff 管理系统

```typescript
/**
 * Buff管理系统 - 处理所有Buff的生命周期和效果
 */
export class BuffManagementSystem extends System {
  private entityManager: EntityManager;
  private relationshipManager: EntityRelationshipManager;

  constructor(
    entityManager: EntityManager,
    relationshipManager: EntityRelationshipManager
  ) {
    super("BuffManagementSystem", 200);
    this.entityManager = entityManager;
    this.relationshipManager = relationshipManager;
  }

  initialize(): void {
    EventBus.on("Buff.Applied", this.handleBuffApplied.bind(this));
    EventBus.on("Buff.Removed", this.handleBuffRemoved.bind(this));
  }

  update(deltaTime: number): void {
    if (!this.isEnabled) return;

    // 更新所有Buff的持续时间
    this.updateBuffDurations(deltaTime);

    // 处理Buff效果
    this.processBuffEffects(deltaTime);
  }

  private updateBuffDurations(deltaTime: number): void {
    const allBuffs = EntityManager.getByType("BuffEntity");

    for (const buff of allBuffs) {
      const buffData = buff.getDataManager("buff");
      if (!buffData) continue;

      const currentDuration = buffData.get("剩余时间");
      const newDuration = currentDuration - deltaTime;

      if (newDuration <= 0) {
        // Buff到期，移除
        this.removeBuff(buff as BuffEntity);
      } else {
        buffData.set("剩余时间", newDuration);
      }
    }
  }

  private processBuffEffects(deltaTime: number): void {
    const allUnits = EntityManager.getByType("UnitEntity");

    for (const unit of allUnits) {
      const buffIds = this.relationshipManager.getRelatedEntities(
        unit.getId(),
        "buffs"
      );

      for (const buffId of buffIds) {
        const buff = EntityManager.get(buffId) as BuffEntity;
        if (!buff) continue;

        this.applyBuffEffect(unit as UnitEntity, buff, deltaTime);
      }
    }
  }

  private applyBuffEffect(
    unit: UnitEntity,
    buff: BuffEntity,
    deltaTime: number
  ): void {
    const buffData = buff.getDataManager("buff");
    const unitAttr = unit.getDataManager("attr");

    if (!buffData || !unitAttr) return;

    const buffType = buffData.get("类型");
    const effectValue = buffData.get("效果值");

    switch (buffType) {
      case "持续治疗":
        const currentHp = unitAttr.get("当前生命值");
        const maxHp = unitAttr.get("最大生命值");
        const healAmount = effectValue * deltaTime;
        unitAttr.set("当前生命值", Math.min(currentHp + healAmount, maxHp));
        break;

      case "持续伤害":
        const damage = effectValue * deltaTime;
        EventBus.emit("Unit.TakeDamage", { unitId: unit.getId(), damage });
        break;

      case "属性加成":
        // 属性加成在Buff添加/移除时处理，这里不需要持续处理
        break;
    }
  }

  private handleBuffApplied(data: { unitId: string; buffId: string }): void {
    this.relationshipManager.addRelationship(data.unitId, data.buffId, "buffs");
  }

  private handleBuffRemoved(data: { unitId: string; buffId: string }): void {
    this.relationshipManager.removeRelationship(
      data.unitId,
      data.buffId,
      "buffs"
    );
  }

  private removeBuff(buff: BuffEntity): void {
    // 找到拥有此Buff的单位
    const allUnits = EntityManager.getByType("UnitEntity");
    for (const unit of allUnits) {
      const buffIds = this.relationshipManager.getRelatedEntities(
        unit.getId(),
        "buffs"
      );
      if (buffIds.includes(buff.getId())) {
        EventBus.emit("Buff.Removed", {
          unitId: unit.getId(),
          buffId: buff.getId(),
        });
        break;
      }
    }

    // 销毁BuffEntity
    EntityManager.destroy(buff.getId());
  }

  destroy(): void {
    EventBus.off("Buff.Applied", this.handleBuffApplied.bind(this));
    EventBus.off("Buff.Removed", this.handleBuffRemoved.bind(this));
  }
}
```

#### 2.3.3 系统管理器

```typescript
/**
 * 系统管理器 - 管理所有System的生命周期
 */
export class SystemManager {
  private systems: Map<string, System> = new Map();
  private systemOrder: string[] = [];

  // 注册系统
  registerSystem(system: System): void {
    this.systems.set(system.getName(), system);
    this.systemOrder.push(system.getName());

    // 按优先级排序
    this.systemOrder.sort((a, b) => {
      const systemA = this.systems.get(a)!;
      const systemB = this.systems.get(b)!;
      return (systemA as any).priority - (systemB as any).priority;
    });

    system.initialize();
  }

  // 更新所有系统
  update(deltaTime: number): void {
    for (const systemName of this.systemOrder) {
      const system = this.systems.get(systemName);
      if (system && system.isSystemEnabled()) {
        system.update(deltaTime);
      }
    }
  }

  // 获取系统
  getSystem<T extends System>(systemName: string): T | null {
    return (this.systems.get(systemName) as T) || null;
  }

  // 移除系统
  removeSystem(systemName: string): void {
    const system = this.systems.get(systemName);
    if (system) {
      system.destroy();
      this.systems.delete(systemName);
      this.systemOrder = this.systemOrder.filter((name) => name !== systemName);
    }
  }

  // 销毁所有系统
  destroy(): void {
    for (const system of this.systems.values()) {
      system.destroy();
    }
    this.systems.clear();
    this.systemOrder = [];
  }
}
```

## 🏗️ 问题 3：创建基础 Entity 类

### 3.1 GameEntity 基类

```typescript
/**
 * 游戏Entity基类 - 所有游戏对象的统一基类
 */
export abstract class GameEntity extends Entity {
  protected entityType: string;
  protected relationshipManager: EntityRelationshipManager;

  constructor(entityType: string, id: string) {
    super(entityType, id);
    this.entityType = entityType;
    this.relationshipManager = EntityRelationshipManager.getInstance();
  }

  // 获取Entity类型
  getEntityType(): string {
    return this.entityType;
  }

  // 建立关系
  addRelationship(targetEntityId: string, relationType: string): void {
    this.relationshipManager.addRelationship(
      this.getId(),
      targetEntityId,
      relationType
    );
  }

  // 获取相关Entity
  getRelatedEntities(relationType: string): string[] {
    return this.relationshipManager.getRelatedEntities(
      this.getId(),
      relationType
    );
  }

  // 移除关系
  removeRelationship(targetEntityId: string, relationType: string): void {
    this.relationshipManager.removeRelationship(
      this.getId(),
      targetEntityId,
      relationType
    );
  }

  // 销毁时清理所有关系
  destroy(): void {
    this.relationshipManager.removeAllRelationships(this.getId());
    super.destroy();
  }
}
```

### 3.2 UnitEntity

```typescript
/**
 * 单位Entity - 管理War3单位
 */
export class UnitEntity extends GameEntity {
  private unitHandle: any;

  constructor(unitType: string, unitHandle?: any) {
    super("UnitEntity", generateId());
    this.unitHandle = unitHandle;

    this.setupComponents(unitType);
    this.setupDataManagers(unitType);
  }

  initialize(): void {
    this.performInitialize();

    // 设置War3单位事件监听
    this.setupWar3Events();

    logger.debug(`UnitEntity initialized: ${this.getId()}`);
  }

  private setupComponents(unitType: string): void {
    // 核心组件
    this.addComponent(EffectComponent);
    this.addComponent(LifecycleComponent);

    // 根据单位类型添加特定组件
    if (this.isHero(unitType)) {
      this.addComponent(HeroComponent);
      this.addComponent(ExperienceComponent);
    }

    if (this.hasAbilities(unitType)) {
      this.addComponent(AbilityManagerComponent);
    }
  }

  private setupDataManagers(unitType: string): void {
    // 单位基础数据
    this.addDataManager("unit", {
      单位类型: unitType,
      单位名称: this.getUnitName(unitType),
      单位等级: 1,
    });

    // 属性数据
    const baseStats = this.getBaseStats(unitType);
    this.addDataManager("attr", baseStats);

    // 设置主数据管理器
    this.setPrimaryDataManager(this.getDataManager("unit")!);
  }

  private setupWar3Events(): void {
    if (!this.unitHandle) return;

    // 监听单位死亡
    War3Event.onUnitDeath(this.unitHandle, () => {
      this.emit("Unit.Death", { unitId: this.getId() });
    });

    // 监听单位受伤
    War3Event.onUnitDamaged(this.unitHandle, (damage: number) => {
      this.emit("Unit.TakeDamage", { unitId: this.getId(), damage });
    });
  }

  // 获取War3单位句柄
  getUnitHandle(): any {
    return this.unitHandle;
  }

  // 设置War3单位句柄
  setUnitHandle(handle: any): void {
    this.unitHandle = handle;
    this.setupWar3Events();
  }

  // 添加物品到背包
  addItemToInventory(itemEntity: ItemEntity): boolean {
    const inventory = this.getRelatedEntities("inventory");
    if (inventory.length >= 6) return false;

    this.addRelationship(itemEntity.getId(), "inventory");
    this.emit("Unit.ItemAdded", {
      unitId: this.getId(),
      itemId: itemEntity.getId(),
    });
    return true;
  }

  // 从背包移除物品
  removeItemFromInventory(itemEntity: ItemEntity): boolean {
    this.removeRelationship(itemEntity.getId(), "inventory");
    this.emit("Unit.ItemRemoved", {
      unitId: this.getId(),
      itemId: itemEntity.getId(),
    });
    return true;
  }

  // 获取背包物品
  getInventoryItems(): ItemEntity[] {
    const itemIds = this.getRelatedEntities("inventory");
    return itemIds
      .map((id) => EntityManager.get(id) as ItemEntity)
      .filter((item) => item !== null);
  }

  // 添加Buff
  addBuff(buffEntity: BuffEntity): void {
    this.addRelationship(buffEntity.getId(), "buffs");
    this.emit("Unit.BuffAdded", {
      unitId: this.getId(),
      buffId: buffEntity.getId(),
    });
  }

  // 移除Buff
  removeBuff(buffEntity: BuffEntity): void {
    this.removeRelationship(buffEntity.getId(), "buffs");
    this.emit("Unit.BuffRemoved", {
      unitId: this.getId(),
      buffId: buffEntity.getId(),
    });
  }

  // 获取所有Buff
  getBuffs(): BuffEntity[] {
    const buffIds = this.getRelatedEntities("buffs");
    return buffIds
      .map((id) => EntityManager.get(id) as BuffEntity)
      .filter((buff) => buff !== null);
  }

  private isHero(unitType: string): boolean {
    // 检查是否为英雄单位
    return unitType.startsWith("Hero") || unitType.includes("英雄");
  }

  private hasAbilities(unitType: string): boolean {
    // 检查是否有技能
    return true; // 大部分单位都有技能
  }

  private getUnitName(unitType: string): string {
    // 从配置表获取单位名称
    return `单位_${unitType}`;
  }

  private getBaseStats(unitType: string): any {
    // 从配置表获取基础属性
    return {
      当前生命值: 100,
      最大生命值: 100,
      当前魔法值: 50,
      最大魔法值: 50,
      攻击力: 10,
      护甲: 0,
      移动速度: 270,
    };
  }
}
```

### 3.3 ItemEntity

```typescript
/**
 * 物品Entity - 管理War3物品
 */
export class ItemEntity extends GameEntity {
  private itemHandle: any;

  constructor(itemType: string, itemHandle?: any) {
    super("ItemEntity", generateId());
    this.itemHandle = itemHandle;

    this.setupComponents(itemType);
    this.setupDataManagers(itemType);
  }

  initialize(): void {
    this.performInitialize();

    // 设置War3物品事件监听
    this.setupWar3Events();

    logger.debug(`ItemEntity initialized: ${this.getId()}`);
  }

  private setupComponents(itemType: string): void {
    // 核心组件
    this.addComponent(EffectComponent);
    this.addComponent(LifecycleComponent);

    // 物品特定组件
    if (this.isStackable(itemType)) {
      this.addComponent(StackComponent);
    }

    if (this.hasActiveAbility(itemType)) {
      this.addComponent(ActiveAbilityComponent);
    }

    if (this.hasPassiveEffect(itemType)) {
      this.addComponent(PassiveEffectComponent);
    }
  }

  private setupDataManagers(itemType: string): void {
    // 物品基础数据
    this.addDataManager("item", {
      物品类型: itemType,
      物品名称: this.getItemName(itemType),
      物品品级: this.getItemQuality(itemType),
      堆叠数量: 1,
      最大堆叠: this.getMaxStack(itemType),
    });

    // 设置主数据管理器
    this.setPrimaryDataManager(this.getDataManager("item")!);
  }

  private setupWar3Events(): void {
    if (!this.itemHandle) return;

    // 监听物品被拾取
    War3Event.onItemPickup(this.itemHandle, (unit: any) => {
      this.emit("Item.PickedUp", { itemId: this.getId(), unitHandle: unit });
    });

    // 监听物品被丢弃
    War3Event.onItemDrop(this.itemHandle, () => {
      this.emit("Item.Dropped", { itemId: this.getId() });
    });

    // 监听物品被使用
    War3Event.onItemUse(this.itemHandle, (unit: any) => {
      this.emit("Item.Used", { itemId: this.getId(), unitHandle: unit });
    });
  }

  // 获取War3物品句柄
  getItemHandle(): any {
    return this.itemHandle;
  }

  // 设置War3物品句柄
  setItemHandle(handle: any): void {
    this.itemHandle = handle;
    this.setupWar3Events();
  }

  // 检查是否可以与其他物品堆叠
  canStackWith(otherItem: ItemEntity): boolean {
    const myData = this.getDataManager("item");
    const otherData = otherItem.getDataManager("item");

    if (!myData || !otherData) return false;

    return (
      myData.get("物品类型") === otherData.get("物品类型") &&
      myData.get("堆叠数量") < myData.get("最大堆叠")
    );
  }

  // 堆叠物品
  stackWith(otherItem: ItemEntity, amount: number): boolean {
    if (!this.canStackWith(otherItem)) return false;

    const myData = this.getDataManager("item")!;
    const currentStack = myData.get("堆叠数量");
    const maxStack = myData.get("最大堆叠");

    const newStack = Math.min(currentStack + amount, maxStack);
    myData.set("堆叠数量", newStack);

    this.emit("Item.StackChanged", { itemId: this.getId(), newStack });
    return true;
  }

  // 分离堆叠
  splitStack(amount: number): ItemEntity | null {
    const itemData = this.getDataManager("item")!;
    const currentStack = itemData.get("堆叠数量");

    if (amount >= currentStack) return null;

    // 创建新物品
    const newItem = new ItemEntity(itemData.get("物品类型"));
    const newItemData = newItem.getDataManager("item")!;
    newItemData.set("堆叠数量", amount);

    // 减少当前堆叠
    itemData.set("堆叠数量", currentStack - amount);

    this.emit("Item.StackSplit", {
      originalId: this.getId(),
      newId: newItem.getId(),
    });
    return newItem;
  }

  // 使用物品
  useItem(target?: UnitEntity): boolean {
    if (!this.hasActiveAbility(this.getDataManager("item")!.get("物品类型"))) {
      return false;
    }

    this.emit("Item.UseRequested", {
      itemId: this.getId(),
      targetId: target?.getId(),
    });

    return true;
  }

  private isStackable(itemType: string): boolean {
    // 检查物品是否可堆叠
    return this.getMaxStack(itemType) > 1;
  }

  private hasActiveAbility(itemType: string): boolean {
    // 检查物品是否有主动技能
    return itemType.includes("药水") || itemType.includes("卷轴");
  }

  private hasPassiveEffect(itemType: string): boolean {
    // 检查物品是否有被动效果
    return itemType.includes("装备") || itemType.includes("饰品");
  }

  private getItemName(itemType: string): string {
    // 从配置表获取物品名称
    return `物品_${itemType}`;
  }

  private getItemQuality(itemType: string): string {
    // 从配置表获取物品品级
    return "普通";
  }

  private getMaxStack(itemType: string): number {
    // 从配置表获取最大堆叠数
    if (itemType.includes("药水")) return 5;
    if (itemType.includes("材料")) return 10;
    return 1;
  }
}
```

### 3.4 PlayerEntity

```typescript
/**
 * 玩家Entity - 管理War3玩家
 */
export class PlayerEntity extends GameEntity {
  private playerHandle: any;
  private playerId: number;

  constructor(playerId: number, playerHandle?: any) {
    super("PlayerEntity", generateId());
    this.playerId = playerId;
    this.playerHandle = playerHandle;

    this.setupComponents();
    this.setupDataManagers();
  }

  initialize(): void {
    this.performInitialize();

    // 设置War3玩家事件监听
    this.setupWar3Events();

    logger.debug(
      `PlayerEntity initialized: ${this.getId()} (Player ${this.playerId})`
    );
  }

  private setupComponents(): void {
    // 核心组件
    this.addComponent(LifecycleComponent);

    // 玩家特定组件
    this.addComponent(ResourceManagerComponent);
    this.addComponent(CameraControlComponent);
    this.addComponent(UIManagerComponent);
    this.addComponent(InputHandlerComponent);
  }

  private setupDataManagers(): void {
    // 玩家基础数据
    this.addDataManager("player", {
      玩家ID: this.playerId,
      玩家名称: this.getPlayerName(),
      是否本地玩家: this.isLocalPlayer(),
      玩家状态: "在线",
      队伍: this.getPlayerTeam(),
    });

    // 资源数据
    this.addDataManager("resource", {
      黄金: 500,
      木材: 150,
      人口: 0,
      人口上限: 100,
    });

    // 设置主数据管理器
    this.setPrimaryDataManager(this.getDataManager("player")!);
  }

  private setupWar3Events(): void {
    if (!this.playerHandle) return;

    // 监听玩家离开游戏
    War3Event.onPlayerLeave(this.playerHandle, () => {
      this.emit("Player.Left", { playerId: this.playerId });
    });

    // 监听资源变化
    War3Event.onPlayerResourceChange(
      this.playerHandle,
      (resourceType: string, amount: number) => {
        this.emit("Player.ResourceChanged", {
          playerId: this.playerId,
          resourceType,
          amount,
        });
      }
    );
  }

  // 获取War3玩家句柄
  getPlayerHandle(): any {
    return this.playerHandle;
  }

  // 获取玩家ID
  getPlayerId(): number {
    return this.playerId;
  }

  // 添加拥有的单位
  addOwnedUnit(unitEntity: UnitEntity): void {
    this.addRelationship(unitEntity.getId(), "owns");
    this.emit("Player.UnitAdded", {
      playerId: this.playerId,
      unitId: unitEntity.getId(),
    });
  }

  // 移除拥有的单位
  removeOwnedUnit(unitEntity: UnitEntity): void {
    this.removeRelationship(unitEntity.getId(), "owns");
    this.emit("Player.UnitRemoved", {
      playerId: this.playerId,
      unitId: unitEntity.getId(),
    });
  }

  // 获取拥有的所有单位
  getOwnedUnits(): UnitEntity[] {
    const unitIds = this.getRelatedEntities("owns");
    return unitIds
      .map((id) => EntityManager.get(id) as UnitEntity)
      .filter((unit) => unit !== null);
  }

  // 添加资源
  addResource(resourceType: string, amount: number): void {
    const resourceData = this.getDataManager("resource")!;
    const currentAmount = resourceData.get(resourceType) || 0;
    resourceData.set(resourceType, currentAmount + amount);

    this.emit("Player.ResourceAdded", {
      playerId: this.playerId,
      resourceType,
      amount: currentAmount + amount,
    });
  }

  // 消耗资源
  consumeResource(resourceType: string, amount: number): boolean {
    const resourceData = this.getDataManager("resource")!;
    const currentAmount = resourceData.get(resourceType) || 0;

    if (currentAmount < amount) return false;

    resourceData.set(resourceType, currentAmount - amount);

    this.emit("Player.ResourceConsumed", {
      playerId: this.playerId,
      resourceType,
      amount: currentAmount - amount,
    });

    return true;
  }

  // 检查资源是否足够
  hasResource(resourceType: string, amount: number): boolean {
    const resourceData = this.getDataManager("resource")!;
    const currentAmount = resourceData.get(resourceType) || 0;
    return currentAmount >= amount;
  }

  // 发送消息给玩家
  sendMessage(message: string, duration: number = 10): void {
    if (this.isLocalPlayer()) {
      // 显示消息给本地玩家
      this.emit("Player.MessageReceived", { message, duration });
    }
  }

  // 设置玩家状态
  setPlayerStatus(status: string): void {
    const playerData = this.getDataManager("player")!;
    playerData.set("玩家状态", status);

    this.emit("Player.StatusChanged", { playerId: this.playerId, status });
  }

  private getPlayerName(): string {
    if (this.playerHandle) {
      return GetPlayerName(this.playerHandle);
    }
    return `Player ${this.playerId}`;
  }

  private isLocalPlayer(): boolean {
    if (this.playerHandle) {
      return GetLocalPlayer() === this.playerHandle;
    }
    return false;
  }

  private getPlayerTeam(): number {
    // 从游戏配置获取队伍信息
    return this.playerId <= 6 ? 1 : 2;
  }
}
```

## 📋 实施建议

### 阶段 1：核心 Entity 重构

1. **创建基础 Entity 类**

   - 实现 GameEntity 基类
   - 创建 EntityRelationshipManager
   - 建立 Entity 工厂模式

2. **重构现有组件**

   - 将 UnitComponent 改为 UnitEntity
   - 将 ItemComponent 改为 ItemEntity
   - 将 PlayerComponent 改为 PlayerEntity

### 阶段 2：System 架构实现

1. **创建 System 基础架构**

   - 实现 System 基类
   - 创建 SystemManager
   - 建立事件驱动机制

2. **实现核心 System**

   - ItemPickupSystem
   - BuffManagementSystem
   - CombatSystem
   - MovementSystem

### 阶段 3：关系管理优化

1. **完善关系管理**

   - 实现关系查询优化
   - 添加关系验证机制
   - 建立关系持久化

2. **性能优化**

   - 实现 Entity 池化
   - 优化 System 更新顺序
   - 添加性能监控

## 🎯 总结

用户的架构理解完全正确，符合现代游戏设计标准：

1. **Entity 独立性** - Item、Player、Ability、Buff 都应该是独立的 Entity
2. **关系管理** - 通过 EntityRelationshipManager 管理 Entity 间的关系
3. **System 架构** - 通过 System 处理跨 Entity 的业务逻辑
4. **数据与逻辑分离** - Entity 负责数据，System 负责逻辑

这种设计完全符合 Unity ECS、Unreal Engine Component 等现代游戏架构标准，将大大提升代码的可维护性、可扩展性和性能。
