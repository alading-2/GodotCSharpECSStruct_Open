/** @noSelfInFile **/

import { EntityPrefab } from "../EntityPrefab";
import { UnitComponent } from "../../Component/Components/base/Unit/UnitComponent";
import { AttributeComponent } from "../../Component/Components/base/Attribute/AttributeComponent";
import { SCHEMA_TYPES } from "../../Schema/SchemaTypes";
import { Logger } from "../../../base/object/工具/logger";
import { TimerComponent, LifecycleComponent, ExpComponent, ShieldComponent } from "../../Component";
import { DeathType } from "../../Component/Components/base/Unit/Lifecycle/LifecycleComponent";
import { RecoveryComponent } from "../../Component/Components/base/Unit/RecoveryComponent";

const logger = Logger.createLogger("UnitEntity");

/**
 * UnitEntity 模板注册类
 * 现代 ECS 架构下，不再继承 Entity，而是通过 EntityPrefab 注册预制模板
 * 
 * 核心职责：
 * 1. 注册标准单位 Entity 配置模板
 * 2. 定义单位的标准组件组合（UnitComponent + AttributeComponent + 生命周期等）
 * 3. 提供可复用的单位创建配置
 * 
 * 使用方式：
 * ```typescript
 * // 创建单位实体
 * const unit = EntityPrefab.create("UnitEntity", {
 *     data: [{ schemaName: SCHEMA_TYPES.UNIT_DATA, initialData: { 单位类型: "圣骑士" } }],
 *     components: [{ type: UnitComponent, props: { unitType: "圣骑士", position: pos } }]
 * });
 * ```
 */
export class UnitEntity {

  /**
   * 初始化方法 - 注册 EntityPrefab 模板
   * 在系统启动时自动调用，注册各种单位预制配置
   */
  static onInit(): void {
    try {
      // 注册基础单位模板
      EntityPrefab.register("普通单位", {
        entityType: "UnitEntity",
        data: [
          {
            schemaName: SCHEMA_TYPES.UNIT_DATA,
            isPrimary: true
          },
          {
            schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
            isPrimary: false
          }
        ],
        components: [
          { type: UnitComponent },// 单位组件
          { type: AttributeComponent },// 属性组件
          { type: TimerComponent },// 定时器组件
          { type: ExpComponent },// 经验组件
          {// 生命周期组件
            type: LifecycleComponent, props: {
              deathType: DeathType.NORMAL,
              canRevive: false,
            }
          },
          { type: RecoveryComponent },// 恢复组件
          { type: ShieldComponent },// 护盾组件
        ]
      });

      // 派生英雄单位模板
      EntityPrefab.derive("普通单位", "英雄", {
        entityType: "UnitEntity",
        data: [
          {
            schemaName: SCHEMA_TYPES.ATTRIBUTE_DATA,
            initialData: {//英雄初始属性
              "物理闪避几率": 5,
            },
            isPrimary: true
          },
        ],
        components: [
          {//英雄能复活、死亡类型是英雄
            type: LifecycleComponent, props: {
              deathType: DeathType.HERO,
              canRevive: true,
            }
          },
          { type: ExpComponent },//英雄才需要计算Exp
        ]
      });

      // 派生召唤单位模板，需要设置LifecycleComponent持续时间
      EntityPrefab.derive("普通单位", "召唤物", {
        entityType: "UnitEntity",
        data: [
        ],
        components: [
        ]
      });

      logger.debug("UnitEntity 预制模板已注册完成");

    } catch (error) {
      logger.error(`UnitEntity 预制模板注册失败: ${error}`);
    }
  }
}