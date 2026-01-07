/** @noSelfInFile **/

import { EntityPrefab } from "../EntityPrefab";
import { Entity } from "../Entity";
import { UnitComponent } from "../../Component/Components/base/Unit/UnitComponent";
import { PlayerComponent } from "../../Component/Components/PlayerComponent";
import { Position } from "../../../base/math/Position";
import { Logger } from "../../../base/object/工具/logger";
import { EntityRelationshipManager } from "../EntityRelationshipManager";
import { SCHEMA_TYPES } from "../../Schema/SchemaTypes";

const logger = Logger.createLogger("UnitTool");

/**
 * 单位创建参数接口
 */
export interface UnitCreationParams {
  /** 单位类型 */
  unitType: string;
  /** 单位所属玩家组件 */
  playerComp: PlayerComponent;
  /** 单位位置 */
  position: Position;
  /** 单位朝向角度，默认为0 */
  face?: number;
  /** 自定义单位数据 */
  customData?: { [key: string]: any };
  /** 自定义属性数据 */
  customAttributes?: { [key: string]: any };
}

/**
 * 英雄创建参数接口
 */
export interface HeroCreationParams extends UnitCreationParams {
  /** 英雄等级，默认为1 */
  level?: number;
  /** 初始经验值，默认为0 */
  experience?: number;
  /** 技能点数，默认为0 */
  skillPoints?: number;
}

/**
 * 召唤单位创建参数接口
 */
export interface SummonCreationParams extends UnitCreationParams {
  /** 召唤者Entity */
  summoner: Entity;
  /** 召唤持续时间，-1表示永久，默认为60秒 */
  duration?: number;
}

/**
 * UnitTool - 单位创建工具类
 * 
 * 现代游戏设计框架下的单位管理工具，提供统一的单位创建接口
 * 基于ECS架构，通过EntityPrefab.create创建单位实体
 * 
 * 核心职责：
 * 1. 提供普通单位的创建方法
 * 2. 提供英雄单位的创建方法
 * 3. 提供召唤单位的创建方法
 * 4. 自动管理单位与玩家的关系
 * 5. 统一的单位生命周期管理
 * 
 * 设计原则：
 * - 简洁API：提供直观的创建方法
 * - 类型安全：完整的TypeScript类型支持
 * - 关系管理：自动建立单位与玩家、召唤者的关系
 * - 数据注入：支持自定义数据和属性注入
 */
export class UnitTool {

  /**
   * 创建普通单位
   * @param params 单位创建参数
   * @returns 创建的单位Entity
   */
  static createUnit(params: UnitCreationParams): Entity {
    try {
      // 准备单位数据
      const unitData = {
        单位类型: params.unitType,
        ...params.customData
      };

      // 准备属性数据
      const attributeData = {
        ...params.customAttributes
      };

      // 创建单位Entity
      const unitEntity = EntityPrefab.create("UnitEntity", {
        data: [
          {
            schemaName: SCHEMA_TYPES.UNIT_DATA,
            initialData: unitData,
            isPrimary: true
          },
          {
            schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
            initialData: attributeData,
            isPrimary: false
          }
        ],
        components: [{
          type: UnitComponent,
          props: {
            unitType: params.unitType,
            playerComp: params.playerComp,
            position: params.position,
            face: params.face || 0
          }
        }]
      });

      // 建立单位与玩家的关系
      this.establishPlayerUnitRelationship(params.playerComp.getOwner(), unitEntity);

      logger.debug(`已创建单位: ${params.unitType} at (${params.position.X}, ${params.position.Y})`);
      return unitEntity;

    } catch (error) {
      logger.error(`创建单位失败: ${error}`);
      throw error;
    }
  }

  /**
   * 创建英雄单位
   * @param params 英雄创建参数
   * @returns 创建的英雄Entity
   */
  static createHero(params: HeroCreationParams): Entity {
    try {
      // 准备英雄数据
      const heroData = {
        单位类型: params.unitType,
        是否英雄: true,
        等级: params.level || 1,
        经验值: params.experience || 0,
        技能点数: params.skillPoints || 0,
        ...params.customData
      };

      // 准备属性数据
      const attributeData = {
        ...params.customAttributes
      };

      // 创建英雄Entity
      const heroEntity = EntityPrefab.create("HeroEntity", {
        data: [
          {
            schemaName: SCHEMA_TYPES.UNIT_DATA,
            initialData: heroData,
            isPrimary: true
          },
          {
            schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
            initialData: attributeData,
            isPrimary: false
          }
        ],
        components: [{
          type: UnitComponent,
          props: {
            unitType: params.unitType,
            playerComp: params.playerComp,
            position: params.position,
            face: params.face || 0
          }
        }]
      });

      // 建立英雄与玩家的关系
      this.establishPlayerUnitRelationship(params.playerComp.getOwner(), heroEntity, "PlayerHero");

      logger.debug(`已创建英雄: ${params.unitType} (Level ${params.level || 1})`);
      return heroEntity;

    } catch (error) {
      logger.error(`创建英雄失败: ${error}`);
      throw error;
    }
  }

  /**
   * 创建召唤单位
   * @param params 召唤单位创建参数
   * @returns 创建的召唤单位Entity
   */
  static createSummon(params: SummonCreationParams): Entity {
    try {
      // 准备召唤单位数据
      const summonData = {
        单位类型: params.unitType,
        是否召唤物: true,
        召唤持续时间: params.duration !== undefined ? params.duration : 60,
        ...params.customData
      };

      // 准备属性数据
      const attributeData = {
        ...params.customAttributes
      };

      // 创建召唤单位Entity
      const summonEntity = EntityPrefab.create("SummonEntity", {
        data: [
          {
            schemaName: SCHEMA_TYPES.UNIT_DATA,
            initialData: summonData,
            isPrimary: true
          },
          {
            schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
            initialData: attributeData,
            isPrimary: false
          }
        ],
        components: [{
          type: UnitComponent,
          props: {
            unitType: params.unitType,
            playerComp: params.playerComp,
            position: params.position,
            face: params.face || 0
          }
        }]
      });

      // 建立召唤单位与玩家的关系
      this.establishPlayerUnitRelationship(params.playerComp.getOwner(), summonEntity, "PlayerSummon");

      // 建立召唤单位与召唤者的关系
      this.establishSummonRelationship(params.summoner, summonEntity, params.duration);

      logger.debug(`已创建召唤单位: ${params.unitType} (Duration: ${params.duration || 60}s)`);
      return summonEntity;

    } catch (error) {
      logger.error(`创建召唤单位失败: ${error}`);
      throw error;
    }
  }

  /**
   * 批量创建单位
   * @param params 单位创建参数数组
   * @returns 创建的单位Entity数组
   */
  static createBatchUnits(params: UnitCreationParams[]): Entity[] {
    const units: Entity[] = [];
    
    for (const param of params) {
      try {
        const unit = this.createUnit(param);
        units.push(unit);
      } catch (error) {
        logger.warn(`批量创建单位时跳过: ${param.unitType} - ${error}`);
      }
    }

    logger.debug(`批量创建单位完成: ${units.length}/${params.length}`);
    return units;
  }

  /**
   * 建立玩家与单位的关系
   * @param playerEntity 玩家Entity
   * @param unitEntity 单位Entity
   * @param relationType 关系类型，默认为"PlayerUnit"
   */
  private static establishPlayerUnitRelationship(
    playerEntity: Entity,
    unitEntity: Entity,
    relationType: string = "PlayerUnit"
  ): void {
    try {
      const relationshipManager = EntityRelationshipManager.getInstance();
      relationshipManager.addRelationship(
        playerEntity.getId(),
        unitEntity.getId(),
        relationType,
        {
          unitType: unitEntity.data.unit.get("单位类型"),
          createdAt: Date.now()
        }
      );
    } catch (error) {
      logger.warn(`建立玩家单位关系失败: ${error}`);
    }
  }

  /**
   * 建立召唤者与召唤单位的关系
   * @param summonerEntity 召唤者Entity
   * @param summonEntity 召唤单位Entity
   * @param duration 召唤持续时间
   */
  private static establishSummonRelationship(
    summonerEntity: Entity,
    summonEntity: Entity,
    duration?: number
  ): void {
    try {
      const relationshipManager = EntityRelationshipManager.getInstance();
      relationshipManager.addRelationship(
        summonerEntity.getId(),
        summonEntity.getId(),
        "SummonedUnit",
        {
          summonDuration: duration || 60,
          summonedAt: Date.now()
        }
      );
    } catch (error) {
      logger.warn(`建立召唤关系失败: ${error}`);
    }
  }

  /**
   * 获取玩家的所有单位
   * @param playerEntity 玩家Entity
   * @returns 单位Entity数组
   */
  static getPlayerUnits(playerEntity: Entity): Entity[] {
    try {
      const relationshipManager = EntityRelationshipManager.getInstance();
      const relationships = relationshipManager.getRelationshipsByParentAndType(
        playerEntity.getId(),
        "PlayerUnit"
      );

      const units: Entity[] = [];
      for (const relationship of relationships) {
        if (relationship.childEntityId) {
          const unitEntity = EntityManager.getEntity(relationship.childEntityId);
          if (unitEntity) {
            units.push(unitEntity);
          }
        }
      }

      return units;
    } catch (error) {
      logger.error(`获取玩家单位失败: ${error}`);
      return [];
    }
  }

  /**
   * 获取召唤者的所有召唤单位
   * @param summonerEntity 召唤者Entity
   * @returns 召唤单位Entity数组
   */
  static getSummonedUnits(summonerEntity: Entity): Entity[] {
    try {
      const relationshipManager = EntityRelationshipManager.getInstance();
      const relationships = relationshipManager.getRelationshipsByParentAndType(
        summonerEntity.getId(),
        "SummonedUnit"
      );

      const summons: Entity[] = [];
      for (const relationship of relationships) {
        if (relationship.childEntityId) {
          const summonEntity = EntityManager.getEntity(relationship.childEntityId);
          if (summonEntity) {
            summons.push(summonEntity);
          }
        }
      }

      return summons;
    } catch (error) {
      logger.error(`获取召唤单位失败: ${error}`);
      return [];
    }
  }
}