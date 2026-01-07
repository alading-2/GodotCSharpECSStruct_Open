/** @noSelfInFile **/

import { Logger } from "../../base/object/工具/logger";

const logger = Logger.createLogger("EntityRelationshipManager");

/**
 * Entity关系数据接口
 */
interface RelationshipData {
  [key: string]: any;
}

/**
 * 关系记录接口
 */
interface RelationshipRecord {
  /** 父Entity ID */
  parentEntityId?: string;
  /** 子Entity ID */
  childEntityId?: string;
  /** 关系类型 */
  relationType?: string;
  /** 关系附加数据 */
  data?: RelationshipData;
}

/**
 * Entity关系管理器
 * 
 * 负责管理Entity之间的关系，这是现代ECS架构中的重要组成部分
 * 使用单层Map优化性能和维护性
 */
export class EntityRelationshipManager {
  // 单例模式
  private static instance: EntityRelationshipManager;

  // 关系存储：relationshipId -> RelationshipRecord
  private relationshipsMap: Map<string, RelationshipRecord> = new Map();

  // 父Entity索引：parentEntityId -> Set<relationshipId>
  private parentIndexMap: Map<string, Set<string>> = new Map();

  // 子Entity索引：childEntityId -> Set<relationshipId>
  private childIndexMap: Map<string, Set<string>> = new Map();

  // 关系类型索引：relationType -> Set<relationshipId>
  private typeIndexMap: Map<string, Set<string>> = new Map();

  private constructor() {
    // logger.debug("EntityRelationshipManager created");
  }

  public static getInstance(): EntityRelationshipManager {
    if (!EntityRelationshipManager.instance) {
      EntityRelationshipManager.instance = new EntityRelationshipManager();
    }
    return EntityRelationshipManager.instance;
  }

  // ====================== 关系管理 ======================
  /**
   * 生成关系ID
   */
  private generateRelationshipId(parentEntityId: string, childEntityId: string, relationType: string): string {
    return `${parentEntityId}:${childEntityId}:${relationType}`;
  }

  /**
   * 添加Entity关系【增】
   * @param parentEntityId 父EntityId
   * @param childEntityId 子EntityId
   * @param relationType 关系类型，可选，如果不提供则根据两个Entity的类型自动生成
   * @param data 关系数据
   * @returns 是否成功添加
   */
  addRelationship(parentEntityId: string, childEntityId: string, relationType?: string, data?: RelationshipData): boolean {
    if (!parentEntityId || !childEntityId) {
      logger.warn("无效的关系参数：缺少Entity ID");
      return false;
    }

    // 如果没有提供关系类型，则自动生成
    if (!relationType) {
      relationType = `${parentEntityId}_${childEntityId}`;
    }

    const relationshipId = this.generateRelationshipId(parentEntityId, childEntityId, relationType);

    // 检查关系是否已存在
    if (this.relationshipsMap.has(relationshipId)) {
      logger.warn(`关系已存在: ${parentEntityId} -> ${childEntityId} (${relationType})`);
      return false;
    }
    // 子Entity的关系不能重复，父Entity的关系可以重复，比如父Entity是玩家，子Entity是单位，一个玩家可以有多个单位，但是一个单位只能属于一个玩家
    if (this.getRelationshipsByChildAndType(childEntityId, relationType).length > 0) {
      logger.warn(`子Entity已存在关系: ${childEntityId} (${relationType})`);
      return false;
    }

    const now = Date.now();
    const record: RelationshipRecord = {
      parentEntityId,
      childEntityId,
      relationType,
      data: data || {},
    };

    // 添加到主存储
    this.relationshipsMap.set(relationshipId, record);

    // 更新父Entity索引
    if (!this.parentIndexMap.has(parentEntityId)) {
      this.parentIndexMap.set(parentEntityId, new Set());
    }
    this.parentIndexMap.get(parentEntityId)!.add(relationshipId);

    // 更新子Entity索引
    if (!this.childIndexMap.has(childEntityId)) {
      this.childIndexMap.set(childEntityId, new Set());
    }
    this.childIndexMap.get(childEntityId)!.add(relationshipId);

    // 更新类型索引
    if (!this.typeIndexMap.has(relationType)) {
      this.typeIndexMap.set(relationType, new Set());
    }
    this.typeIndexMap.get(relationType)!.add(relationshipId);

    logger.debug(`已添加关系: ${parentEntityId} -> ${childEntityId} (${relationType})`);
    return true;
  }

  /**
   * 移除关系【删】
   * @param parentEntityId 父EntityId
   * @param childEntityId 子EntityId
   * @param relationType 关系类型
   * @returns 是否成功移除
   */
  removeRelationship(parentEntityId: string, childEntityId: string, relationType: string): boolean {
    if (!parentEntityId || !childEntityId || !relationType) {
      logger.warn("无效的关系参数");
      return false;
    }

    const relationshipId = this.generateRelationshipId(parentEntityId, childEntityId, relationType);

    // 检查关系是否存在
    if (!this.relationshipsMap.has(relationshipId)) {
      logger.warn(`关系不存在: ${parentEntityId} -> ${childEntityId} (${relationType})`);
      return false;
    }

    // 从主存储移除
    this.relationshipsMap.delete(relationshipId);

    // 从父Entity索引移除
    const parentSet = this.parentIndexMap.get(parentEntityId);
    if (parentSet) {
      parentSet.delete(relationshipId);
      if (parentSet.size === 0) {
        this.parentIndexMap.delete(parentEntityId);
      }
    }

    // 从子Entity索引移除
    const childSet = this.childIndexMap.get(childEntityId);
    if (childSet) {
      childSet.delete(relationshipId);
      if (childSet.size === 0) {
        this.childIndexMap.delete(childEntityId);
      }
    }

    // 从类型索引移除
    const typeSet = this.typeIndexMap.get(relationType);
    if (typeSet) {
      typeSet.delete(relationshipId);
      if (typeSet.size === 0) {
        this.typeIndexMap.delete(relationType);
      }
    }

    logger.debug(`已移除关系: ${parentEntityId} -> ${childEntityId} (${relationType})`);
    return true;
  }

  /**
   * 设置关系数据【改】
   * @param parentEntityId 父EntityId
   * @param childEntityId 子EntityId
   * @param relationType 关系类型
   * @param data 关系数据
   * @returns 是否成功设置
   */
  setRelationshipData(parentEntityId: string, childEntityId: string, relationType: string, data: RelationshipData): boolean {
    const relationshipId = this.generateRelationshipId(parentEntityId, childEntityId, relationType);
    const record = this.relationshipsMap.get(relationshipId);
    if (!record) {
      logger.warn(`关系不存在: ${parentEntityId} -> ${childEntityId} (${relationType})`);
      return false;
    }
    record.data = { ...record.data, ...data };

    logger.debug(`已更新关系数据: ${parentEntityId} -> ${childEntityId} (${relationType})`);
    return true;
  }

  /**
   * 检查关系是否存在【查】
   * @param parentEntityId 父EntityId
   * @param childEntityId 子EntityId
   * @param relationType 关系类型
   * @returns 是否存在
   */
  hasRelationship(parentEntityId: string, childEntityId: string, relationType: string): boolean {
    const relationshipId = this.generateRelationshipId(parentEntityId, childEntityId, relationType);
    const has = this.relationshipsMap.has(relationshipId);
    if (!has) {
      logger.warn(`关系不存在: ${parentEntityId} -> ${childEntityId} (${relationType})`);
    }
    return has;
  }

  /**
   * 获取指定关系数据【查】
   * @param parentEntityId 父EntityId
   * @param childEntityId 子EntityId
   * @param relationType 关系类型 
   * @returns 关系数据
   */
  getRelationshipData(parentEntityId: string, childEntityId: string, relationType: string): RelationshipData | null {
    const relationshipId = this.generateRelationshipId(parentEntityId, childEntityId, relationType);
    const record = this.relationshipsMap.get(relationshipId);
    return record ? record.data || {} : null;
  }

  /**
   * 根据关系ID集合获取关系记录列表
   * @param relationshipIds 关系ID集合
   * @returns 关系记录列表
   */
  getRelationshipRecords(relationshipIds: Set<string>): RelationshipRecord[] {
    const records: RelationshipRecord[] = [];
    for (const id of relationshipIds) {
      const record = this.relationshipsMap.get(id);
      if (record) {
        records.push(record);
      }
    }
    return records;
  }
  /**
   * 获取父Entity的所有关系
   */
  getRelationshipsByParent(parentEntityId: string): RelationshipRecord[] {
    const relationshipIds = this.parentIndexMap.get(parentEntityId);
    if (!relationshipIds) return [];
    return this.getRelationshipRecords(relationshipIds);
  }

  /**
   * 获取子Entity的所有关系
   * @param childEntityId 子Entity的ID
   */
  getRelationshipsByChild(childEntityId: string): RelationshipRecord[] {
    const relationshipIds = this.childIndexMap.get(childEntityId);
    if (!relationshipIds) return [];
    return this.getRelationshipRecords(relationshipIds);
  }

  /**
   * 获取指定类型的所有关系
   * @param relationType 关系类型
   */
  getRelationshipsByType(relationType: string): RelationshipRecord[] {
    const relationshipIds = this.typeIndexMap.get(relationType);
    if (!relationshipIds) return [];
    return this.getRelationshipRecords(relationshipIds);
  }

  /**
   * 获取父Entity的所有相关Entity（指定关系类型），常用，比如获取UnitEntity的多个物品ItemEntity
   * @param parentEntityId 父EntityId
   * @param relationType 关系类型
   * @returns 子EntityId列表
   */
  getRelationshipsByParentAndType(parentEntityId: string, relationType: string): string[] {
    const relationshipIds = this.parentIndexMap.get(parentEntityId);
    if (!relationshipIds) return [];

    const childEntityIds: string[] = [];
    for (const id of relationshipIds) {
      const record = this.relationshipsMap.get(id);
      if (record && record.relationType === relationType && record.childEntityId) {
        childEntityIds.push(record.childEntityId);
      }
    }
    return childEntityIds;
  }

  /**
   * 获取子Entity的所有相关Entity（指定关系类型），常用，比如获取ItemEntity的所有拥有者UnitEntity
   * @param childEntityId 子EntityId
   * @param relationType 关系类型
   * @returns 父EntityId列表
   */
  getRelationshipsByChildAndType(childEntityId: string, relationType: string): string[] {
    const relationshipIds = this.childIndexMap.get(childEntityId);
    if (!relationshipIds) return [];

    const parentEntityIds: string[] = [];
    for (const id of relationshipIds) {
      const record = this.relationshipsMap.get(id);
      if (record && record.relationType === relationType && record.parentEntityId) {
        parentEntityIds.push(record.parentEntityId);
      }
    }
    return parentEntityIds;
  }

  // ====================== 批量操作 ======================

  /**
   * 移除指定类型的所有关系
   */
  removeRelationshipsByType(relationType: string): void {
    const relationshipIds = this.typeIndexMap.get(relationType);
    if (!relationshipIds) return;

    // 复制ID列表，避免在迭代过程中修改
    const idsToRemove = [...relationshipIds];

    for (const id of idsToRemove) {
      const record = this.relationshipsMap.get(id);
      if (record && record.parentEntityId && record.childEntityId && record.relationType) {
        this.removeRelationship(record.parentEntityId, record.childEntityId, record.relationType);
      }
    }

    logger.debug(`已移除所有类型为 ${relationType} 的关系`);
  }

  /**
   * 移除Entity的所有关系，Entity执行destroy时运行，将与自己有关的关系全部销毁
   * @param entityId Entity.getId()
   */
  removeAllRelationships(entityId: string): void {
    // 移除作为父Entity的所有关系
    const parentRelationshipIds = this.parentIndexMap.get(entityId);
    if (parentRelationshipIds) {
      const idsToRemove = [...parentRelationshipIds];
      for (const id of idsToRemove) {
        const record = this.relationshipsMap.get(id);
        if (record && record.parentEntityId && record.childEntityId && record.relationType) {
          this.removeRelationship(record.parentEntityId, record.childEntityId, record.relationType);
        }
      }
    }

    // 移除作为子Entity的所有关系
    const childRelationshipIds = this.childIndexMap.get(entityId);
    if (childRelationshipIds) {
      const idsToRemove = [...childRelationshipIds];
      for (const id of idsToRemove) {
        const record = this.relationshipsMap.get(id);
        if (record && record.parentEntityId && record.childEntityId && record.relationType) {
          this.removeRelationship(record.parentEntityId, record.childEntityId, record.relationType);
        }
      }
    }

    logger.debug(`已移除Entity的所有关系: ${entityId}`);
  }

  // ====================== 工具方法 ======================
  /**
   * 清理所有数据
   */
  clear(): void {
    this.relationshipsMap.clear();
    this.parentIndexMap.clear();
    this.childIndexMap.clear();
    this.typeIndexMap.clear();

    logger.info("EntityRelationshipManager 已清理所有数据");
  }

  /**
   * 销毁管理器
   */
  destroy(): void {
    this.clear();
    logger.info("EntityRelationshipManager 已销毁");
  }
}