/** @noSelfInFile **/

import { EntityPrefab } from "../EntityPrefab";
import { Entity } from "../Entity";
import { EffectComponent } from "../../Component/Components/base/Effect/EffectComponent";
import { UnitComponent } from "../../Component/Components/base/Unit/UnitComponent";
import { PlayerComponent } from "../../Component/Components/PlayerComponent";
import { Position } from "../../../base/math/Position";
import { Logger } from "../../../base/object/工具/logger";
import { EntityRelationshipManager } from "../EntityRelationshipManager";

const logger = Logger.createLogger("EffectTool");

/**
 * 附着特效创建参数接口
 */
export interface AttachedEffectParams {
  /** 特效模型路径 */
  path: string;
  /** 附着的单位Entity */
  unitEntity: Entity;
  /** 附着点名称，默认为"origin" */
  attach?: string;
  /** 缩放大小，默认为1 */
  size?: number;
  /** 持续时间，-1表示永久，默认为-1 */
  life?: number;
  /** 玩家组件（用于特效显示控制） */
  playerComp?: PlayerComponent;
  /** 播放速度，默认为1 */
  speed?: number;
}

/**
 * 位置特效创建参数接口
 */
export interface PositionEffectParams {
  /** 特效模型路径 */
  path: string;
  /** 特效位置 */
  position: Position;
  /** 缩放大小，默认为1 */
  size?: number;
  /** 持续时间，-1表示永久，默认为3秒 */
  life?: number;
  /** 玩家组件（用于特效显示控制） */
  playerComp?: PlayerComponent;
  /** 播放速度，默认为1 */
  speed?: number;
}

/**
 * EffectTool - 特效创建工具类
 * 
 * 现代游戏设计框架下的特效管理工具，提供统一的特效创建接口
 * 基于ECS架构，通过EntityPrefab.create创建特效实体
 * 
 * 核心职责：
 * 1. 提供附着单位特效的创建方法
 * 2. 提供位置特效的创建方法
 * 3. 自动管理特效与单位的关系
 * 4. 统一的特效生命周期管理
 * 
 * 设计原则：
 * - 简洁API：提供直观的创建方法
 * - 类型安全：完整的TypeScript类型支持
 * - 关系管理：自动建立特效与单位的关系
 * - 生命周期：支持临时和永久特效
 */
export class EffectTool {

  /**
   * 创建附着到单位的特效
   * @param params 附着特效参数
   * @returns 创建的特效Entity
   */
  static createAttachedEffect(params: AttachedEffectParams): Entity {
    try {
      // 获取单位的UnitComponent
      const unitComponent = params.unitEntity.getComponent(UnitComponent);
      if (!unitComponent) {
        throw new Error("目标单位缺少UnitComponent");
      }

      // 创建特效Entity
      const effectEntity = EntityPrefab.create("AttachedEffectEntity", {
        components: [{
          type: EffectComponent,
          props: {
            path: params.path,
            unitComp: unitComponent,
            attach: params.attach || "origin",
            size: params.size || 1,
            life: params.life || -1,
            playerComp: params.playerComp,
            speed: params.speed || 1
          }
        }]
      });

      // 建立特效与单位的关系
      const relationshipManager = EntityRelationshipManager.getInstance();
      relationshipManager.addRelationship(
        params.unitEntity.getId(),
        effectEntity.getId(),
        "AttachedEffect",
        {
          attachPoint: params.attach || "origin",
          effectPath: params.path
        }
      );

      logger.debug(`已创建附着特效: ${params.path} -> ${params.unitEntity.getId()}`);
      return effectEntity;

    } catch (error) {
      logger.error(`创建附着特效失败: ${error}`);
      throw error;
    }
  }

  /**
   * 创建位置特效
   * @param params 位置特效参数
   * @returns 创建的特效Entity
   */
  static createPositionEffect(params: PositionEffectParams): Entity {
    try {
      // 创建特效Entity
      const effectEntity = EntityPrefab.create("TempEffectEntity", {
        components: [{
          type: EffectComponent,
          props: {
            path: params.path,
            position: params.position,
            size: params.size || 1,
            life: params.life !== undefined ? params.life : 3.0, // 默认3秒
            playerComp: params.playerComp,
            speed: params.speed || 1
          }
        }]
      });

      logger.debug(`已创建位置特效: ${params.path} at (${params.position.X}, ${params.position.Y})`);
      return effectEntity;

    } catch (error) {
      logger.error(`创建位置特效失败: ${error}`);
      throw error;
    }
  }

  /**
   * 创建永久位置特效
   * @param params 位置特效参数（life参数会被忽略）
   * @returns 创建的特效Entity
   */
  static createPermanentPositionEffect(params: Omit<PositionEffectParams, 'life'>): Entity {
    return this.createPositionEffect({
      ...params,
      life: -1 // 永久特效
    });
  }

  /**
   * 批量创建附着特效
   * @param unitEntities 单位Entity数组
   * @param effectPath 特效路径
   * @param options 可选参数
   * @returns 创建的特效Entity数组
   */
  static createBatchAttachedEffects(
    unitEntities: Entity[],
    effectPath: string,
    options?: Partial<Omit<AttachedEffectParams, 'path' | 'unitEntity'>>
  ): Entity[] {
    const effects: Entity[] = [];
    
    for (const unitEntity of unitEntities) {
      try {
        const effect = this.createAttachedEffect({
          path: effectPath,
          unitEntity,
          ...options
        });
        effects.push(effect);
      } catch (error) {
        logger.warn(`批量创建特效时跳过单位 ${unitEntity.getId()}: ${error}`);
      }
    }

    logger.debug(`批量创建特效完成: ${effects.length}/${unitEntities.length}`);
    return effects;
  }

  /**
   * 移除单位的所有附着特效
   * @param unitEntity 单位Entity
   */
  static removeAllAttachedEffects(unitEntity: Entity): void {
    try {
      const relationshipManager = EntityRelationshipManager.getInstance();
      const relationships = relationshipManager.getRelationshipsByParentAndType(
        unitEntity.getId(),
        "AttachedEffect"
      );

      for (const relationship of relationships) {
        if (relationship.childEntityId) {
          // 销毁特效Entity
          const effectEntity = EntityManager.getEntity(relationship.childEntityId);
          if (effectEntity) {
            effectEntity.destroy();
          }
          
          // 移除关系
          relationshipManager.removeRelationship(
            unitEntity.getId(),
            relationship.childEntityId,
            "AttachedEffect"
          );
        }
      }

      logger.debug(`已移除单位 ${unitEntity.getId()} 的所有附着特效`);

    } catch (error) {
      logger.error(`移除附着特效失败: ${error}`);
    }
  }
}