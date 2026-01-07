# 现代 ECS 护盾系统

基于 Entity-Component-Schema 架构的高性能护盾系统，专为 War3RPG 游戏设计。

## 🎯 设计理念

### 核心原则

- **数据与逻辑分离**: 数据存储与业务逻辑完全分离
- **依赖注入**: 组件间通过依赖注入实现松耦合
- **事件驱动**: 基于事件系统实现组件间通信
- **性能优先**: 优化的数据结构和算法，适合实时游戏

### ECS 架构优势

- **可扩展性**: 易于添加新的护盾类型和功能
- **可维护性**: 清晰的职责分离，便于调试和维护
- **可复用性**: 组件可在不同 Entity 间复用
- **性能优化**: 数据局部性好，缓存友好

## 📁 文件结构

```
护盾/
├── ShieldSchema.ts                    # 护盾数据结构定义（包含SHIELD_INSTANCE_SCHEMA）
# ShieldUtils.ts 已移除，功能整合到 ShieldComponent.ts 中
├── DataManager (Inte_Shield)          # 护盾数据管理器（纯数据存储）
├── ShieldComponent.ts            # 护盾逻辑组件（业务逻辑）
├── ShieldSystemExample.ts             # 使用示例和工厂类
├── ShieldInstanceSchemaExample.ts     # Schema功能使用示例
└── README.md                         # 本文档
```

## 🏗️ 架构设计

### 1. Schema 层 (`ShieldSchema.ts`)

定义护盾系统的数据结构和类型：

- `ShieldType`: 护盾类型枚举
- `ShieldInstance`: 护盾实例接口
- `SHIELD_INSTANCE_SCHEMA`: 护盾实例 Schema 定义（新增）
- `Inte_Shield`: 护盾数据 Schema
- `SHIELD_SCHEMA`: 护盾系统 Schema 定义（增强）
- `ShieldEventType`: 护盾事件类型
- `ShieldComponent`: 护盾逻辑组件（包含原 ShieldUtils 的所有功能）

#### Schema 功能特性

**SHIELD_INSTANCE_SCHEMA 提供：**

- 完整的类型约束和验证
- 属性默认值定义
- 数值范围限制（如护盾值 0-99999，优先级 0-100）
- 枚举值验证（护盾类型）
- 自定义验证器（ID 格式、时间逻辑等）
- 只读属性保护

**SHIELD_SCHEMA 增强：**

- 嵌套 Schema 支持
- 计算属性（自动计算总值）
- 容量限制验证
- 数据一致性检查

### 2. 数据层 (`DataManager` - Inte_Shield)

负责护盾数据的存储和基础操作：

- 继承自 `DataManager`
- 纯数据存储，无业务逻辑
- 提供 CRUD 操作接口
- 数据验证和约束

### 3. 逻辑层 (`ShieldComponent.ts`)

负责护盾系统的业务逻辑：

- 继承自 `Component`
- 通过依赖注入获取数据组件
- 实现护盾管理、伤害处理等核心功能
- 事件触发和生命周期管理

## 🚀 快速开始

### 1. Schema 功能使用（推荐）

#### 护盾管理（推荐）

```typescript
import { ShieldComponent } from "./ShieldComponent";
import { ShieldType } from "./ShieldSchema";

// 获取护盾逻辑组件
const shieldLogic = entity.getComponent(ShieldComponent);

// 添加护盾
const shieldId = shieldLogic.addShield(500, ShieldType.MAGICAL, 30, 5);

// 获取护盾统计信息
const stats = shieldLogic.getShieldStats();
console.log("护盾统计:", stats);

// 处理伤害
const remainingDamage = shieldLogic.processDamage(200);
console.log("剩余伤害:", remainingDamage);
```

### 2. 基础设置

```typescript
import { Entity } from "../../../Entity/Entity";
import { ShieldSystemFactory } from "./ShieldSystemExample";

// 为单位创建护盾系统
const unit: Entity = new Entity("unit_001");
const success = ShieldSystemFactory.createStandardShieldSystem(unit, 5);

if (success) {
  console.log("护盾系统创建成功");
}
```

### 2. 添加护盾

```typescript
import { ShieldComponent } from "./ShieldComponent";
import { ShieldType } from "./ShieldSchema";

const shieldLogic = unit.getComponent(ShieldComponent);

// 添加不同类型的护盾
const physicalShield = shieldLogic.addShield(100, ShieldType.PHYSICAL, 30, 1);
const magicalShield = shieldLogic.addShield(80, ShieldType.MAGICAL, 20, 2);
const universalShield = shieldLogic.addShield(50, ShieldType.UNIVERSAL, -1, 0);
```

### 3. 处理伤害

```typescript
// 护盾会按优先级自动吸收伤害
const remainingDamage = shieldLogic.processDamage(120, "physical");
console.log(`剩余伤害: ${remainingDamage}`);
```

### 4. 监听事件

```typescript
import { ShieldEventType } from "./ShieldSchema";

// 监听护盾事件
unit.on(ShieldEventType.SHIELD_ADDED, (data) => {
  console.log(`护盾添加: ${data.shield?.value}`);
});

unit.on(ShieldEventType.SHIELD_BROKEN, (data) => {
  console.log(`护盾破碎: ${data.shield?.id}`);
  // 播放破碎特效
});
```

## 🔧 核心功能

### 护盾管理

- ✅ 添加/移除护盾
- ✅ 护盾优先级系统
- ✅ 护盾类型分类
- ✅ 护盾时效性管理
- ✅ 护盾容量限制

### 伤害处理

- ✅ 按优先级消耗护盾
- ✅ 类型匹配伤害吸收
- ✅ 剩余伤害计算
- ✅ 护盾破碎检测

### 查询功能

- ✅ 护盾统计信息
- ✅ 按类型查询护盾
- ✅ 护盾详细信息
- ✅ 调试信息输出

### 高级功能

- ✅ 批量护盾操作
- ✅ 优先级插入
- ✅ 类型批量移除
- ✅ 事件驱动架构

## 📊 性能特性

### 数据结构优化

- 使用数组存储护盾，支持快速遍历
- 缓存计算结果，避免重复计算
- 惰性更新，只在需要时更新数据

### 算法优化

- O(n)时间复杂度的伤害处理
- 优先级排序缓存
- 事件批处理机制

### 内存管理

- 对象池复用护盾实例
- 及时清理过期护盾
- 避免内存泄漏

## 🎮 游戏集成

### 与伤害系统集成

```typescript
// 在伤害系统中集成护盾
function dealDamage(
  target: Entity,
  damage: number,
  damageType: string
): number {
  const shieldLogic = target.getComponent(ShieldComponent);

  if (shieldLogic && shieldLogic.hasShields()) {
    // 护盾先吸收伤害
    const remainingDamage = shieldLogic.processDamage(damage, damageType);

    // 剩余伤害作用于生命值
    if (remainingDamage > 0) {
      const healthComponent = target.getComponent(HealthComponent);
      healthComponent?.takeDamage(remainingDamage);
    }

    return remainingDamage;
  }

  // 没有护盾，直接造成伤害
  const healthComponent = target.getComponent(HealthComponent);
  healthComponent?.takeDamage(damage);
  return damage;
}
```

### 与技能系统集成

```typescript
// 护盾技能示例
class ShieldSpell {
  cast(caster: Entity, target: Entity): void {
    const shieldLogic = target.getComponent(ShieldComponent);

    if (shieldLogic) {
      // 根据施法者属性计算护盾值
      const shieldValue = this.calculateShieldValue(caster);
      const duration = this.getSpellDuration();

      shieldLogic.addShield(shieldValue, ShieldType.MAGICAL, duration, 2);
    }
  }
}
```

## 🔍 调试和监控

### 调试信息

```typescript
// 获取详细调试信息
const debugInfo = shieldLogic.getDebugInfo();
console.log("护盾系统状态:", debugInfo);
```

### 性能监控

```typescript
// 监控护盾系统性能
unit.on(ShieldEventType.SHIELD_DAMAGED, (data) => {
  const processingTime = performance.now() - startTime;
  if (processingTime > 1.0) {
    // 超过1ms
    console.warn(`护盾处理耗时过长: ${processingTime}ms`);
  }
});
```

## 🧪 测试示例

完整的测试示例请参考 `ShieldSystemExample.ts` 文件，包含：

1. **基础功能测试**: 护盾添加、移除、查询
2. **伤害处理测试**: 不同类型伤害的处理
3. **高级功能测试**: 批量操作、优先级管理
4. **集成测试**: 与其他系统的集成
5. **性能测试**: 大量护盾的性能表现
6. **边界测试**: 异常情况处理

## 🔧 配置和扩展

### 添加新的护盾类型

1. 在 `ShieldSchema.ts` 中扩展 `ShieldType` 枚举
2. 更新 `ShieldComponent.canAbsorbDamage` 方法（内部方法）
3. 添加相应的事件处理逻辑

### 自定义护盾行为

```typescript
// 扩展护盾逻辑组件
class CustomShieldComponent extends ShieldComponent {
  // 重写伤害处理逻辑
  processDamage(damage: number, damageType: string): number {
    // 自定义逻辑
    return super.processDamage(damage, damageType);
  }
}
```

## 📈 最佳实践

### 1. 护盾配置

- 合理设置护盾容量（建议 3-5 个）
- 平衡护盾数值和持续时间
- 使用优先级系统管理护盾层次

### 2. 性能优化

- 避免频繁的护盾添加/移除
- 使用批量操作处理多个护盾
- 及时清理过期护盾

### 3. 事件处理

- 合理使用事件监听，避免过多监听器
- 在组件销毁时清理事件监听
- 使用事件批处理减少性能开销

### 4. 错误处理

- 检查组件依赖关系
- 验证输入参数
- 提供详细的错误日志

## 🐛 常见问题

### Q: 护盾不生效？

A: 检查是否正确添加了 `DataManager` (Inte_Shield) 和 `ShieldComponent`

### Q: 伤害处理不正确？

A: 确认护盾类型与伤害类型的匹配规则

### Q: 性能问题？

A: 检查护盾数量是否过多，考虑使用批量操作

### Q: 事件不触发？

A: 确认事件监听器是否正确注册，检查事件类型是否匹配

## 📝 更新日志

### v1.0.0 (当前版本)

- ✅ 基础护盾系统实现
- ✅ ECS 架构集成
- ✅ 事件驱动机制
- ✅ 完整的 API 接口
- ✅ 性能优化
- ✅ 详细文档和示例

## 🤝 贡献指南

1. 遵循现有的代码风格和架构
2. 添加适当的单元测试
3. 更新相关文档
4. 确保性能不受影响

## 📄 许可证

本护盾系统遵循项目的整体许可证。

---

**现代 ECS 护盾系统** - 为 War3RPG 游戏提供高性能、可扩展的护盾解决方案。
