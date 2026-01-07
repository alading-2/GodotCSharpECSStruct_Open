/** @noSelfInFile **/

import { Entity } from "../../../Entity/Entity";
import { ShieldComponent } from "./ShieldComponent";
import {
  ShieldType,
  ShieldEventType,
  ShieldEventData,
  Inte_ShieldSchema
} from "../../../Schema/Schemas/护盾/ShieldSchema";
import { Logger } from "../../../../base/object/工具/logger";

const logger = Logger.createLogger("ShieldSystemExample");

/**
 * 护盾系统使用示例
 * 展示如何在实际游戏中使用现代ECS护盾系统
 */
export class ShieldSystemExample {

  /**
   * 示例1: 为单位添加护盾系统
   * @param unit 游戏单位Entity
   */
  static setupShieldForUnit(unit: Entity): void {
    logger.info(`为单位 ${unit.getId()} 设置护盾系统`);

    // 1. 添加护盾数据管理器
    unit.addDataManager("ShieldData", {
      shields: [],
      maxCapacity: 5,
      totalValue: 0,
      isActive: true
    });
    const shieldData = unit.getDataManager("ShieldData");
    if (!shieldData) {
      logger.error("护盾数据管理器添加失败");
      return;
    }

    // 2. 添加护盾逻辑组件
    const shieldLogic = unit.addComponent(ShieldComponent);
    if (!shieldLogic) {
      logger.error("护盾逻辑组件添加失败");
      return;
    }

    // 3. 设置护盾容量
    shieldLogic.setCapacity(5);

    // 4. 监听护盾事件
    unit.on(ShieldEventType.SHIELD_ADDED, (data: ShieldEventData) => {
      logger.info(`护盾添加: ${data.shield?.name}, 值: ${data.shield?.value}`);
    });

    unit.on(ShieldEventType.SHIELD_BROKEN, (data: ShieldEventData) => {
      logger.info(`护盾破碎: ${data.shield?.name}`);
      // 可以在这里播放破碎特效
    });

    unit.on(ShieldEventType.SHIELD_DAMAGED, (data: ShieldEventData) => {
      logger.info(`护盾受损: 吸收伤害 ${data.absorbed}, 剩余伤害 ${data.remaining}`);
    });

    logger.info(`单位 ${unit.getId()} 护盾系统设置完成`);
  }

  /**
   * 示例2: 基础护盾操作
   * @param unit 已设置护盾系统的单位
   */
  static basicShieldOperations(unit: Entity): void {
    const shieldLogic = unit.getComponent(ShieldComponent);
    if (!shieldLogic) {
      logger.error("单位没有护盾逻辑组件");
      return;
    }

    logger.info("开始基础护盾操作演示");

    // 添加不同类型的护盾
    const physicalShieldId = shieldLogic.addShield(100, ShieldType.PHYSICAL, 30, 1);
    const magicalShieldId = shieldLogic.addShield(80, ShieldType.MAGICAL, 20, 2);
    const universalShieldId = shieldLogic.addShield(50, ShieldType.UNIVERSAL, -1, 0);

    logger.info(`添加护盾: 物理=${physicalShieldId}, 魔法=${magicalShieldId}, 通用=${universalShieldId}`);

    // 查询护盾状态
    const totalValue = shieldLogic.getTotalShieldValue();
    const activeCount = shieldLogic.getActiveShieldCount();
    const stats = shieldLogic.getShieldStats();

    logger.info(`护盾状态: 总值=${totalValue}, 数量=${activeCount}`);
    logger.info(`护盾统计: 物理=${stats.physicalShields}, 魔法=${stats.magicalShields}, 通用=${stats.universalShields}`);

    // 模拟伤害
    const remainingDamage1 = shieldLogic.processDamage(60, "physical");
    logger.info(`物理伤害60处理完成，剩余伤害: ${remainingDamage1}`);

    const remainingDamage2 = shieldLogic.processDamage(120, "magical");
    logger.info(`魔法伤害120处理完成，剩余伤害: ${remainingDamage2}`);

    // 查看最终状态
    const finalStats = shieldLogic.getShieldStats();
    logger.info(`最终护盾状态: 总值=${shieldLogic.getTotalShieldValue()}`);
  }

  /**
   * 示例3: 高级护盾功能
   * @param unit 已设置护盾系统的单位
   */
  static advancedShieldFeatures(unit: Entity): void {
    const shieldLogic = unit.getComponent(ShieldComponent);
    if (!shieldLogic) {
      logger.error("单位没有护盾逻辑组件");
      return;
    }

    logger.info("开始高级护盾功能演示");

    // 批量添加护盾
    const shieldConfigs = [
      { value: 100, type: ShieldType.PHYSICAL, duration: 30, priority: 3 },
      { value: 80, type: ShieldType.MAGICAL, duration: 25, priority: 2 },
      { value: 60, type: ShieldType.UNIVERSAL, duration: -1, priority: 1 }
    ];

    const addedShields = shieldLogic.addMultipleShields(shieldConfigs);
    logger.info(`批量添加护盾完成，成功添加 ${addedShields.length} 个护盾`);

    // 按优先级插入护盾
    const priorityShieldId = shieldLogic.addShieldWithPriority(150, ShieldType.UNIVERSAL, 60, 2);
    logger.info(`优先级护盾添加: ${priorityShieldId}`);

    // 查看按优先级排序的护盾
    const sortedShields = shieldLogic.getActiveShieldsByPriority();
    logger.info("护盾优先级排序:");
    sortedShields.forEach((shield, index) => {
      logger.info(`  ${index + 1}. ID: ${shield.id}, 值: ${shield.value}, 优先级: ${shield.priority}, 类型: ${shield.type}`);
    });

    // 移除特定类型的护盾
    const removedCount = shieldLogic.removeShieldsByType(ShieldType.PHYSICAL);
    logger.info(`移除物理护盾完成，共移除 ${removedCount} 个`);

    // 获取调试信息
    const debugInfo = shieldLogic.getDebugInfo();
    logger.info("护盾系统调试信息:", debugInfo);
  }

  /**
   * 示例4: 与伤害系统集成
   * @param unit 游戏单位Entity
   * @param damageValue 伤害值
   * @param damageType 伤害类型
   */
  static integrateWithDamageSystem(unit: Entity, damageValue: number, damageType: string): number {
    const shieldLogic = unit.getComponent(ShieldComponent);

    if (!shieldLogic || !shieldLogic.hasShields()) {
      // 没有护盾，直接返回原始伤害
      logger.info(`单位 ${unit.getId()} 没有护盾，伤害直接作用`);
      return damageValue;
    }

    logger.info(`单位 ${unit.getId()} 开始处理伤害: ${damageValue} (${damageType})`);

    // 护盾吸收伤害
    const remainingDamage = shieldLogic.processDamage(damageValue, damageType);

    const absorbedDamage = damageValue - remainingDamage;
    logger.info(`护盾吸收伤害: ${absorbedDamage}, 剩余伤害: ${remainingDamage}`);

    // 如果还有剩余伤害，应用到单位生命值
    if (remainingDamage > 0) {
      logger.info(`对单位 ${unit.getId()} 造成 ${remainingDamage} 点实际伤害`);
      // 这里可以调用单位的生命值组件来处理剩余伤害
      // const healthComponent = unit.getComponent(HealthComponent);
      // healthComponent?.takeDamage(remainingDamage);
    }

    return remainingDamage;
  }

  /**
   * 示例5: 护盾效果和特效
   * @param unit 游戏单位Entity
   */
  static shieldEffectsExample(unit: Entity): void {
    const shieldLogic = unit.getComponent(ShieldComponent);
    if (!shieldLogic) {
      logger.error("单位没有护盾逻辑组件");
      return;
    }

    logger.info("护盾效果演示");

    // 监听护盾事件并播放相应特效
    unit.on(ShieldEventType.SHIELD_ADDED, (data: ShieldEventData) => {
      const shield = data.shield;
      if (shield) {
        logger.info(`播放护盾添加特效: ${shield.type}`);
        // 根据护盾类型播放不同特效
        switch (shield.type) {
          case ShieldType.PHYSICAL:
            // PlayEffect("PhysicalShieldEffect", unit.getPosition());
            logger.info("播放物理护盾特效");
            break;
          case ShieldType.MAGICAL:
            // PlayEffect("MagicalShieldEffect", unit.getPosition());
            logger.info("播放魔法护盾特效");
            break;
          case ShieldType.UNIVERSAL:
            // PlayEffect("UniversalShieldEffect", unit.getPosition());
            logger.info("播放通用护盾特效");
            break;
        }
      }
    });

    unit.on(ShieldEventType.SHIELD_BROKEN, (data: ShieldEventData) => {
      const shield = data.shield;
      if (shield) {
        logger.info(`播放护盾破碎特效: ${shield.type}`);
        // PlayEffect("ShieldBreakEffect", unit.getPosition());
        // PlaySound("ShieldBreakSound");
      }
    });

    unit.on(ShieldEventType.SHIELD_DAMAGED, (data: ShieldEventData) => {
      if (data.absorbed && data.absorbed > 0) {
        logger.info(`播放护盾受损特效，吸收伤害: ${data.absorbed}`);
        // PlayEffect("ShieldHitEffect", unit.getPosition());
        // 可以根据吸收的伤害量调整特效强度
      }
    });

    // 添加一些护盾来触发特效
    shieldLogic.addShield(100, ShieldType.PHYSICAL, 30);
    shieldLogic.addShield(80, ShieldType.MAGICAL, 25);

    // 模拟伤害来触发受损和破碎特效
    shieldLogic.processDamage(50, "physical");
    shieldLogic.processDamage(150, "magical");
  }

  /**
   * 示例6: 护盾配置和平衡
   * @param unit 游戏单位Entity
   */
  static shieldBalancingExample(unit: Entity): void {
    const shieldLogic = unit.getComponent(ShieldComponent);
    if (!shieldLogic) {
      logger.error("单位没有护盾逻辑组件");
      return;
    }

    logger.info("护盾平衡性演示");

    // 设置合理的护盾容量限制
    shieldLogic.setMaxCapacity(3); // 最多3个护盾

    // 添加平衡的护盾配置
    const configs = [
      // 高优先级，短时间，高数值
      { value: 200, type: ShieldType.UNIVERSAL, duration: 10, priority: 3 },
      // 中优先级，中等时间，中等数值
      { value: 100, type: ShieldType.PHYSICAL, duration: 30, priority: 2 },
      // 低优先级，长时间，低数值
      { value: 50, type: ShieldType.MAGICAL, duration: 60, priority: 1 }
    ];

    const addedShields = shieldLogic.addMultipleShields(configs);
    logger.info(`平衡护盾配置添加完成: ${addedShields.length} 个护盾`);

    // 尝试添加超出容量的护盾（应该失败）
    const overCapacityShield = shieldLogic.addShield(75, ShieldType.UNIVERSAL, 20);
    if (!overCapacityShield) {
      logger.info("护盾容量限制正常工作，超出容量的护盾添加失败");
    }

    // 测试不同类型伤害的处理
    const testDamages = [
      { damage: 80, type: "physical" },
      { damage: 60, type: "magical" },
      { damage: 100, type: "universal" }
    ];

    testDamages.forEach(test => {
      const remaining = shieldLogic.processDamage(test.damage, test.type);
      logger.info(`${test.type} 伤害 ${test.damage} 处理结果: 剩余伤害 ${remaining}`);
    });

    // 显示最终状态
    const finalStats = shieldLogic.getShieldStats();
    logger.info("最终护盾统计:", finalStats);
  }

  /**
   * 完整的护盾系统演示
   * @param unit 游戏单位Entity
   */
  static fullShieldSystemDemo(unit: Entity): void {
    logger.info("=== 完整护盾系统演示开始 ===");

    // 1. 设置护盾系统
    this.setupShieldForUnit(unit);

    // 2. 基础操作
    this.basicShieldOperations(unit);

    // 3. 高级功能
    this.advancedShieldFeatures(unit);

    // 4. 效果演示
    this.shieldEffectsExample(unit);

    // 5. 平衡性测试
    this.shieldBalancingExample(unit);

    // 6. 与伤害系统集成测试
    this.integrateWithDamageSystem(unit, 150, "universal");

    logger.info("=== 完整护盾系统演示结束 ===");
  }
}

/**
 * 护盾系统工厂类
 * 提供便捷的护盾系统创建方法
 */
export class ShieldSystemFactory {

  /**
   * 为Entity创建标准护盾系统
   * @param entity 目标Entity
   * @param maxCapacity 最大护盾容量
   * @returns 是否创建成功
   */
  static createStandardShieldSystem(entity: Entity, maxCapacity: number = 5): boolean {
    try {
      // 添加护盾数据管理器
      const shieldData = entity.addDataManager("Inte_Shield", {
        shields: [],
        maxCapacity: maxCapacity,
        totalValue: 0,
        isActive: true
      });
      if (!shieldData) {
        logger.error("护盾数据管理器创建失败");
        return false;
      }

      // 添加逻辑组件
      const logicComponent = entity.addComponent(ShieldComponent);
      if (!logicComponent) {
        logger.error("护盾逻辑组件创建失败");
        return false;
      }

      // 设置容量
      logicComponent.setCapacity(maxCapacity);

      logger.info(`标准护盾系统创建成功: Entity ${entity.getId()}, 容量: ${maxCapacity}`);
      return true;
    } catch (error) {
      logger.error("护盾系统创建失败:", error);
      return false;
    }
  }

  /**
   * 检查Entity是否已有护盾系统
   * @param entity 目标Entity
   */
  static hasShieldSystem(entity: Entity): boolean {
    const shieldData = entity.getDataManager("Inte_Shield");
    const logicComponent = entity.getComponent(ShieldComponent);
    return !!(shieldData && logicComponent);
  }

  /**
   * 移除Entity的护盾系统
   * @param entity 目标Entity
   */
  static removeShieldSystem(entity: Entity): boolean {
    try {
      const logicComponent = entity.getComponent(ShieldComponent);
      const shieldData = entity.getDataManager("Inte_Shield");

      if (logicComponent) {
        entity.removeComponent(ShieldComponent);
      }

      if (shieldData) {
        entity.removeDataManager("Inte_Shield");
      }

      logger.info(`护盾系统已从 Entity ${entity.getId()} 移除`);
      return true;
    } catch (error) {
      logger.error("护盾系统移除失败:", error);
      return false;
    }
  }
}