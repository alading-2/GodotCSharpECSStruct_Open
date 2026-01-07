/** @noSelfInFile **/

import { Component } from "../../Component";
import { Entity } from "../../../Entity/Entity";
import { DataManager } from "../../../Schema/DataManager";
import {
  Inte_ShieldInstance,
  ShieldType,
  ShieldEventType,
  ShieldEventData,
  ShieldStats,
  Inte_ShieldSchema
} from "../../../Schema/Schemas/护盾/ShieldSchema";
// ShieldUtils 功能已整合到本组件中
import { Logger } from "../../../../base/object/工具/logger";
import { TimerComponent } from "../TimerComponent";
import { SCHEMA_TYPES } from "../../../Schema/SchemaTypes";

const logger = Logger.createLogger("ShieldComponent");

/**
 * 护盾逻辑组件
 * 负责护盾系统的业务逻辑处理
 * 通过依赖注入获取DataManager
 */
export class ShieldComponent extends Component {
  protected static readonly TYPE: string = "ShieldComponent";

  // 依赖的数据管理器
  private shieldData: DataManager<Inte_ShieldSchema>;

  // 依赖声明
  static readonly requiredDataManagers = ["Inte_Shield"];

  // 护盾更新定时器ID
  private updateTimerId: string = "";

  /**
   * 构造函数
   * @param owner 所属Entity
   */
  constructor(owner: Entity) {
    super(owner);
    logger.debug(`护盾逻辑组件已创建，所属对象: ${owner.getId()}`);
  }

  /**
   * 获取组件类型
   */
  static getType(): string {
    return this.TYPE;
  }

  /**
   * 组件初始化
   */
  public initialize(): void {
    // 通过依赖注入获取数据管理器
    this.shieldData = this.owner.getDataManager(SCHEMA_TYPES.SHIELD_DATA);

    if (!this.shieldData) {
      const error = "ShieldComponent requires Inte_Shield DataManager";
      logger.error(error);
      throw new Error(error);
    }

    // 启动护盾更新定时器
    this.startShieldUpdateTimer();

    // 监听护盾添加事件并添加特效
    this.owner.on(ShieldEventType.SHIELD_ADDED, (data: ShieldEventData) => {
      const shield = data.shield;
      if (shield) {
        logger.info(`播放护盾添加特效: ${shield.type}`);
        //TODO: 根据护盾类型播放不同特效
      }
    });

    // 监听护盾破碎事件并播放特效
    this.owner.on(ShieldEventType.SHIELD_BROKEN, (data: ShieldEventData) => {
      const shield = data.shield;
      if (shield) {
        logger.info(`播放护盾破碎特效: ${shield.type}`);
        //TODO: 根据护盾类型播放不同特效
      }
    });

    logger.debug(`护盾逻辑组件初始化完成: ${this.owner.getId()}`);
  }

  /**
   * 组件销毁
   */
  public destroy(): void {
    // 停止定时器
    this.stopShieldUpdateTimer();
    this.clearAllShields()
    logger.debug(`护盾逻辑组件正在销毁: ${this.owner.getId()}`);
  }


  // ===================== 护盾计时器更新护盾状态 ====================
  /**
   * 启动护盾更新定时器
   */
  private startShieldUpdateTimer(): void {
    const timer: TimerComponent = this.owner.getComponent(TimerComponent);
    if (timer) {
      this.updateTimerId = timer.CreateTimer(1.0, () => {
        this.updateShields();
      });
      logger.debug("护盾更新定时器已启动");
    } else {
      logger.warn("未找到TimerComponent，护盾时效性功能可能无法正常工作");
    }
  }

  /**
   * 更新护盾状态（处理时效性）
   */
  private updateShields(): void {
    const shields = this.shieldData.get("shields") || [];
    let hasExpired = false; //是否已经过期
    const expiredShields: Inte_ShieldInstance[] = [];

    for (const shield of shields) {
      if (shield.remainingTime > 0) {
        shield.remainingTime <= 1 ? shield.remainingTime = 0 : shield.remainingTime -= 1;

        if (shield.remainingTime == 0) {
          shield.isActive = false;
          hasExpired = true;
          expiredShields.push(shield);
          this.emitShieldEvent(ShieldEventType.SHIELD_EXPIRED, { shield });
          logger.debug(`护盾过期: ${shield.name}`);
        }
      }
    }

    if (hasExpired) {
      // 移除过期的护盾
      const activeShields = shields.filter(s => s.isActive || s.remainingTime === -1);
      this.shieldData.set("shields", activeShields);
      logger.debug(`护盾更新完成，移除了 ${expiredShields.length} 个过期护盾`);
    }
  }

  /**
   * 停止护盾更新定时器
   */
  private stopShieldUpdateTimer(): void {
    if (this.updateTimerId) {
      const timer: TimerComponent = this.owner.getComponent(TimerComponent);
      if (timer) {
        timer.removeTimer(this.updateTimerId);
        logger.debug("护盾更新定时器已停止");
      }
      this.updateTimerId = "";
    }
  }


  // ==================== 护盾管理API ====================

  /**
   * 创建护盾实例
   * @param shieldName - 护盾名
   * @param value - 护盾值
   * @param type - 护盾类型,默认为通用类型
   * @param duration - 持续时间,默认为-1(永久)
   * @param priority - 优先级,默认为0
   * @returns 创建的护盾实例
   */
  private createShieldInstance(
    shieldName: string,
    value: number,
    type: ShieldType = ShieldType.UNIVERSAL,
    duration: number = -1,
    priority: number = 0
  ): Inte_ShieldInstance {
    return {
      name: shieldName,
      value: Math.max(0, value),
      type: type,
      priority: Math.max(0, priority),
      remainingTime: duration,
      createTime: os.time(),
      isActive: true
    };
  }

  /**
   * 触发护盾事件
   * @param eventType 事件类型
   * @param eventData 事件数据
   */
  private emitShieldEvent(eventType: ShieldEventType, eventData: ShieldEventData): void {
    this.owner.emit(eventType, eventData);
  }
  /**
   * 添加护盾
   * @param shieldName 护盾名
   * @param value 护盾值
   * @param type 护盾类型
   * @param duration 持续时间（秒，-1表示永久）
   * @param priority 优先级
   * @returns 护盾ID，失败返回空字符串
   */
  addShield(
    shieldName: string,
    value: number,
    type: ShieldType = ShieldType.UNIVERSAL,
    duration: number = -1,
    priority: number = 0
  ): string {
    if (value <= 0) {
      logger.warn(`尝试添加无效护盾值: ${value}`);
      return "";
    }

    // 检查容量限制
    const currentShields = this.shieldData.get("shields");  //当前护盾
    const maxCapacity = this.shieldData.get("maxCapacity"); //护盾最大容量
    if (currentShields.length >= maxCapacity) {
      logger.warn("护盾容量已满，无法添加新护盾");
      return "";
    }

    // 创建护盾实例
    const shield = this.createShieldInstance(
      shieldName,
      value,
      type,
      duration,
      priority
    );

    // 添加到数据管理器
    const newShields = [...currentShields, shield];
    const success = this.shieldData.set("shields", newShields);

    if (success) {
      // 触发护盾添加事件
      this.emitShieldEvent(ShieldEventType.SHIELD_ADDED, { shield });
      logger.debug(`护盾添加成功: ${shield.name}, 值: ${value}, 类型: ${type}, 持续时间: ${duration}, 优先级: ${priority}`);
      return shield.name;
    }

    logger.error(`护盾添加失败: 值: ${value}, 类型: ${type}, 持续时间: ${duration}, 优先级: ${priority}`);
    return "";
  }

  /**
   * 批量添加护盾
   * @param shieldConfigs 护盾配置数组
   */
  addMultipleShields(shieldConfigs: Array<{
    name?: string;
    value: number;
    type?: ShieldType;
    duration?: number;
    priority?: number;
  }>): string[] {
    const addedShieldIds: string[] = [];

    for (const config of shieldConfigs) {
      const shieldId = this.addShield(
        config.name,
        config.value,
        config.type || ShieldType.UNIVERSAL,
        config.duration || -1,
        config.priority || 0
      );

      if (shieldId) {
        addedShieldIds.push(shieldId);
      }
    }

    logger.debug(`批量添加护盾完成，成功添加 ${addedShieldIds.length}/${shieldConfigs.length} 个护盾`);
    return addedShieldIds;
  }
  /**
   * 移除护盾
   * @param shieldName 护盾名
   * @returns 是否移除成功
   */
  removeShield(shieldName: string): boolean {
    const currentShields = this.shieldData.get("shields");
    const shield = currentShields.find(s => s.name === shieldName);
    if (!shield) {
      logger.warn(`尝试移除不存在的护盾: ${shieldName}`);
      return false;
    }

    const newShields = currentShields.filter(s => s.name !== shield.name);
    const success = this.shieldData.set("shields", newShields);

    if (success) {
      // 触发护盾移除事件
      this.emitShieldEvent(ShieldEventType.SHIELD_REMOVED, { shield });
      logger.debug(`护盾移除成功: ${shield.name}`);
    }

    return success;
  }

  /**
   * 移除指定类型的所有护盾
   * @param type 护盾类型
   */
  removeShieldsByType(type: ShieldType): number {
    const shieldsToRemove = this.getShieldsByType(type);
    let removedCount = 0;

    for (const shield of shieldsToRemove) {
      if (this.removeShield(shield.name)) {
        removedCount++;
      }
    }

    logger.debug(`移除 ${type} 类型护盾完成，共移除 ${removedCount} 个护盾`);
    return removedCount;
  }
  /**
   * 获取护盾
   * @param shieldName 护盾名称
   */
  getShieldInfo(shieldName: string): Inte_ShieldInstance | null {
    const shields = this.shieldData.get("shields") || [];
    return shields.find(s => s.name === shieldName) || null;
  }

  /**
   * 获取指定类型的护盾
   * @param type 护盾类型
   */
  getShieldsByType(type: ShieldType): Inte_ShieldInstance[] {
    const shields = this.shieldData.get("shields") || [];
    return shields.filter(s => s.isActive && s.type === type);
  }

  /**
   * 处理伤害
   * @param damage 伤害值
   * @returns 实际造成的伤害（扣除护盾吸收后）
   */
  processDamage(damage: number): number {
    if (damage <= 0) return 0;

    let remainingDamage = damage;
    const shields = this.shieldData.get("shields");
    const activeShields = shields.filter(s => s.isActive);

    // 按优先级排序处理护盾
    const sortedShields = this.sortShieldsByPriority(activeShields);

    for (const shield of sortedShields) {
      if (remainingDamage <= 0) break;

      const absorbed = Math.min(remainingDamage, shield.value);
      shield.value -= absorbed;
      remainingDamage -= absorbed;

      // 触发护盾受损事件
      this.emitShieldEvent(ShieldEventType.SHIELD_DAMAGED, {
        shield,
        damage: absorbed,
        remaining: remainingDamage,
      });

      // 如果护盾被完全消耗，移除它
      if (shield.value <= 0) {
        this.removeShield(shield.name);
        this.emitShieldEvent(ShieldEventType.SHIELD_BROKEN, { shield });
      }
    }

    // 更新护盾数据
    this.shieldData.set("shields", shields);

    const totalAbsorbed = damage - remainingDamage;
    logger.debug(`伤害处理完成: 总伤害=${damage}, 吸收=${totalAbsorbed}, 剩余=${remainingDamage}`);

    return remainingDamage;
  }

  /**
   * 清空所有护盾
   */
  clearAllShields(): void {
    const shields = this.shieldData.get("shields");

    // 触发清空事件
    if (shields.length > 0) {
      this.emitShieldEvent(ShieldEventType.SHIELD_CLEARED, {
        shield: shields[0], // 使用第一个护盾作为主要护盾
        clearedShields: shields
      });
    }

    this.shieldData.set("shields", []);
    logger.debug("所有护盾已清空");
  }

  /**
   * 设置护盾最大容量，即最多保留多少个护盾
   * @param capacity 新的容量值
   */
  setCapacity(capacity: number): void {
    this.shieldData.set("maxCapacity", capacity);
    logger.debug(`护盾容量设置为: ${capacity}`);
  }

  // ==================== 查询API ====================

  /**
   * 按优先级排序护盾
   * @param shields - 护盾实例数组
   * @returns 按优先级排序后的护盾数组(仅包含激活状态的护盾)
   */
  private sortShieldsByPriority(shields: Inte_ShieldInstance[]): Inte_ShieldInstance[] {
    return shields
      .filter(s => s.isActive)
      .sort((a, b) => a.priority - b.priority);
  }

  /**
   * 检查是否有护盾
   */
  hasShields(): boolean {
    const shields = this.shieldData.get("shields") || [];
    return shields.some(s => s.isActive);
  }

  /**
   * 检查是否有指定类型的护盾
   * @param type 护盾类型
   */
  hasShieldOfType(type: ShieldType): boolean {
    return this.getShieldsByType(type).length > 0;
  }

  /**
   * 计算护盾总值
   * @param shields - 护盾实例数组
   * @returns 所有激活状态护盾的值总和
   */
  private calculateTotalValue(shields: Inte_ShieldInstance[]): number {
    return shields
      .filter(s => s.isActive)
      .reduce((sum, shield) => sum + shield.value, 0);
  }
  /**
   * 获取护盾总值
   */
  getTotalShieldValue(): number {
    const shields = this.shieldData.get("shields") || [];
    return this.calculateTotalValue(shields);
  }

  /**
   * 获取激活护盾数量
   */
  getActiveShieldCount(): number {
    const shields = this.shieldData.get("shields") || [];
    return shields.filter(s => s.isActive).length;
  }

  /**
   * 获取所有护盾信息
   */
  getAllShields(): Inte_ShieldInstance[] {
    return this.shieldData.get("shields") || [];
  }

  // ==================== 内部工具方法 ====================
  // /**
  //  * 生成护盾ID
  //  * @param entityId - EntityID
  //  * @returns 生成的护盾ID字符串,格式为 shield_EntityID_时间戳
  //  */
  // private generateShieldId(entityId: string): string {
  //   return `shield_${entityId}_${os.time()}`;
  // }

  // ======================护盾统计信息=======================================================

  /**
   * 按类型分组护盾
   * @param shields - 护盾实例数组
   * @returns 按类型分组的护盾值总和
   */
  private groupShieldsByType(shields: Inte_ShieldInstance[]): Record<string, number> {
    return shields
      .filter(s => s.isActive)
      .reduce((acc, shield) => {
        acc[shield.type] = (acc[shield.type] || 0) + shield.value;
        return acc;
      }, {} as Record<string, number>);
  }

  /**
   * 获取护盾统计信息（内部方法）
   * @param shields - 护盾实例数组
   * @returns 护盾统计信息
   */
  private getShieldStatsInternal(shields: Inte_ShieldInstance[]): ShieldStats {
    const activeShields = shields.filter(s => s.isActive);

    return {
      total: shields.length,
      active: activeShields.length,
      totalValue: this.calculateTotalValue(shields),
      byType: this.groupShieldsByType(shields)
    };
  }
  /**
   * 获取护盾统计信息
   */
  getShieldStats(): ShieldStats {
    const shields = this.shieldData.get("shields") || [];
    return this.getShieldStatsInternal(shields);
  }

  /**
   * 获取调试信息
   */
  public getDebugInfo(): Record<string, any> {
    const stats = this.getShieldStats();

    return {
      componentType: this.getType(),
      entityId: this.owner.getId(),
      stats: stats,
      updateTimerId: this.updateTimerId,
      hasDataManager: !!this.shieldData,
      shields: this.getAllShields().map(s => ({
        id: s.name,
        value: s.value,
        type: s.type,
        priority: s.priority,
        isActive: s.isActive,
        remainingTime: s.remainingTime,
        createTime: s.createTime
      }))
    };
  }


}