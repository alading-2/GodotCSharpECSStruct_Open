/** @noSelfInFile **/

import { EntityPrefab } from "../EntityPrefab";
import { EffectComponent } from "../../Component/Components/base/Effect/EffectComponent";
import { SCHEMA_TYPES } from "../../Schema/SchemaTypes";
import { Logger } from "../../../base/object/工具/logger";
import { TimerComponent, LifecycleComponent } from "../../Component";

const logger = Logger.createLogger("EffectEntity");

/**
 * EffectEntity 模板注册类
 * 现代 ECS 架构下，通过 EntityPrefab 注册特效实体预制模板
 * 
 * 核心职责：
 * 1. 注册标准特效 Entity 配置模板
 * 2. 定义特效的标准组件组合（EffectComponent + 生命周期等）
 * 3. 提供可复用的特效创建配置
 * 
 * 使用方式：
 * ```typescript
 * // 创建附着单位的特效实体
 * const effect = EntityPrefab.create("EffectEntity", {
 *     components: [{ 
 *         type: EffectComponent, 
 *         props: { 
 *             path: "Abilities\\Spells\\Human\\HolyBolt\\HolyBoltSpecialArt.mdl",
 *             unitComp: targetUnit.getComponent(UnitComponent),
 *             attach: "chest"
 *         } 
 *     }]
 * });
 * 
 * // 创建位置特效实体
 * const effect = EntityPrefab.create("EffectEntity", {
 *     components: [{ 
 *         type: EffectComponent, 
 *         props: { 
 *             path: "Abilities\\Spells\\Human\\HolyBolt\\HolyBoltSpecialArt.mdl",
 *             position: new Position(100, 100, 0),
 *             life: 3.0
 *         } 
 *     }]
 * });
 * ```
 */
export class EffectEntity {

  /**
   * 初始化方法 - 注册 EntityPrefab 模板
   * 在系统启动时自动调用，注册各种特效预制配置
   */
  static onInit(): void {
    try {
      // 注册基础特效模板
      EntityPrefab.register("EffectEntity", {
        entityType: "EffectEntity",
        data: [
          {
            schemaName: SCHEMA_TYPES.EFFECT_DATA,
            isPrimary: true
          }
        ],
        components: [
          { type: EffectComponent }, // EffectComponent的参数在create时填写
          { type: TimerComponent },
          { type: LifecycleComponent }
        ]
      });

      // 注册临时特效模板（有生命周期限制）
      EntityPrefab.register("TempEffectEntity", {
        entityType: "EffectEntity",
        data: [
          {
            schemaName: SCHEMA_TYPES.EFFECT_DATA,
            isPrimary: true
          }
        ],
        components: [
          { type: EffectComponent }, // EffectComponent的参数在create时填写
          { type: TimerComponent },
          { type: LifecycleComponent }
        ]
      });

      // 注册附着特效模板（附着到单位）
      EntityPrefab.register("AttachedEffectEntity", {
        entityType: "EffectEntity",
        data: [
          {
            schemaName: SCHEMA_TYPES.EFFECT_DATA,
            isPrimary: true
          }
        ],
        components: [
          { type: EffectComponent }, // EffectComponent的参数在create时填写
          { type: TimerComponent },
          { type: LifecycleComponent }
        ]
      });

      logger.debug("EffectEntity 预制模板已注册完成");

    } catch (error) {
      logger.error(`EffectEntity 预制模板注册失败: ${error}`);
    }
  }
}