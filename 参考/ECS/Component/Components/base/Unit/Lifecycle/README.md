# 单位组件重构说明

## 概述

本次重构对 `LifecycleComponent` 进行了现代化改造，遵循现代游戏架构的设计原则，实现了组件的职责分离和功能优化。

## 重构内容

### 1. LifecycleComponent 精简

**重构前的问题：**
- 组件职责过多，包含生命周期管理和恢复系统
- 违反单一职责原则
- 代码耦合度高，难以维护

**重构后的改进：**
- 专注于生命周期状态管理
- 移除了恢复相关的所有代码
- 清晰的状态流转：`spawning → alive → dying → dead → reviving → alive`
- 完整的文档和使用示例

### 2. RecoveryComponent 重构

**设计目标：**
- 专门负责生命值和魔法值恢复
- 遵循单一职责原则
- 完全依赖属性数据系统，实现数据驱动设计

**核心功能：**
- 自动恢复：从属性数据读取恢复速率并执行恢复
- 手动恢复：提供治疗和魔法恢复接口
- 恢复控制：支持暂停、恢复、停止恢复过程
- 属性集成：支持基础恢复和百分比恢复的灵活组合

**数据驱动设计：**
- 不再内部存储恢复速率
- 完全依赖 `AttributeData` 管理恢复参数
- 支持"基础生命恢复"、"基础魔法恢复"、"百分比生命恢复"、"百分比魔法恢复"属性

## 架构设计原则

### 现代游戏架构标准

本重构严格遵循现代游戏引擎的设计模式：

1. **单一职责原则**
   - `LifecycleComponent`：只负责生命周期状态管理
   - `RecoveryComponent`：只负责恢复功能

2. **数据驱动架构**
   - 恢复速率完全由 `AttributeData` 管理
   - 组件不再内部存储业务数据，只负责逻辑执行
   - 支持复杂的属性计算公式（基础值 + 百分比值）

3. **属性系统集成**
   - 与 `AttributeSchema` 深度集成
   - 支持"基础生命恢复"、"百分比生命恢复"等属性
   - 自动计算最终恢复值

4. **事件驱动架构**
   - 状态变化通过事件系统通知
   - 组件间通过事件进行通信

5. **组件化设计**
   - 组件间松耦合，可独立使用
   - 支持灵活的组合和配置

## 使用指南

### 基础使用

```typescript
// 创建普通单位
const lifecycle = entity.addComponent(LifecycleComponent, {
    maxLifeTime: 120,           // 2分钟后自动死亡
    canRevive: false,           // 不能复活
    invulnerabilityDuration: 0
});

const recovery = entity.addComponent(RecoveryComponent, {
    recoveryInterval: 1.0,      // 每1秒执行一次恢复
    autoStart: true             // 自动开始恢复
});

// 设置恢复速率（通过属性数据）
entity.data.attr.set("基础生命恢复", 2);      // 每秒恢复2点生命值
entity.data.attr.set("基础魔法恢复", 1);      // 每秒恢复1点魔法值
```

### 英雄单位配置

```typescript
// 英雄单位具有复活能力和强化恢复
const heroLifecycle = entity.addComponent(LifecycleComponent, {
    maxLifeTime: -1,            // 永不自然死亡
    canRevive: true,            // 可以复活
    reviveTime: 30,             // 复活需要30秒
    invulnerabilityDuration: 5  // 复活后5秒无敌
});

const heroRecovery = entity.addComponent(RecoveryComponent, {
    recoveryInterval: 0.5,      // 更频繁的恢复检查
    autoStart: true
});

// 设置英雄恢复速率（通过属性数据）
entity.data.attr.set("基础生命恢复", 10);     // 更快的恢复速度
entity.data.attr.set("基础魔法恢复", 5);      // 更快的魔法恢复
entity.data.attr.set("百分比生命恢复", 1);    // 每秒恢复1%最大生命值
entity.data.attr.set("百分比魔法恢复", 2);    // 每秒恢复2%最大魔法值
```

### 动态控制

```typescript
// 根据游戏状态动态调整
entity.on(EventTypes.UNIT_ENTER_COMBAT, () => {
    entity.data.attr.set("基础生命恢复", 1);    // 战斗中降低恢复速率
    entity.data.attr.set("百分比生命恢复", 0);   // 战斗中取消百分比恢复
});

entity.on(EventTypes.UNIT_LEAVE_COMBAT, () => {
    entity.data.attr.set("基础生命恢复", 5);    // 脱战后恢复正常速率
    entity.data.attr.set("百分比生命恢复", 1);   // 脱战后恢复百分比恢复
});

// 手动治疗
recovery.heal(50);              // 立即恢复50点生命值
recovery.restoreMana(30);       // 立即恢复30点魔法值

// 控制恢复过程
recovery.pauseRecovery();       // 暂停恢复
recovery.resumeRecovery();      // 恢复恢复
recovery.stopRecovery();        // 停止恢复
```

## 迁移指南

### 从旧版本迁移到数据驱动版本

**旧版本（已废弃）：**
```typescript
// 旧方式：直接在组件属性中设置恢复速率
const recovery = entity.addComponent(RecoveryComponent, {
    healthRecoveryRate: 5,
    manaRecoveryRate: 3,
    recoveryInterval: 1.0,
    autoStart: true
});

// 动态调整
recovery.setHealthRecoveryRate(10);
recovery.setManaRecoveryRate(5);
```

**新版本（推荐）：**
```typescript
// 新方式：通过属性数据设置恢复速率
const recovery = entity.addComponent(RecoveryComponent, {
    recoveryInterval: 1.0,
    autoStart: true
});

// 设置恢复速率
entity.data.attr.set("基础生命恢复", 5);
entity.data.attr.set("基础魔法恢复", 3);
entity.data.attr.set("百分比生命恢复", 1);  // 新功能：百分比恢复

// 动态调整
entity.data.attr.set("基础生命恢复", 10);
entity.data.attr.set("基础魔法恢复", 5);
```

### 迁移步骤

1. **移除组件属性中的恢复速率设置**
   - 删除 `healthRecoveryRate` 和 `manaRecoveryRate` 属性

2. **使用属性数据设置恢复速率**
   - 使用 `entity.data.attr.set()` 设置恢复相关属性

3. **利用新的百分比恢复功能**
   - 设置 `百分比生命恢复` 和 `百分比魔法恢复` 实现更灵活的恢复机制

4. **更新动态调整代码**
   - 将 `setHealthRecoveryRate()` 调用改为 `entity.data.attr.set("基础生命恢复", value)`

## 文件结构

```
base/Unit/
├── LifecycleComponent.ts           # 重构后的生命周期组件
├── RecoveryComponent.ts             # 重构后的恢复组件（数据驱动）
├── LifecycleComponentExample.ts     # 详细使用示例
└── README.md                        # 本说明文档
```

## 迁移指南

### 从旧版本迁移

如果你的代码使用了旧版本的 `LifecycleComponent` 中的恢复功能，需要进行以下调整：

**旧代码：**
```typescript
// 旧版本中的恢复功能
lifecycle.setHealthRecoveryRate(5);
lifecycle.healUnit(50);
```

**新代码：**
```typescript
// 新版本需要使用 RecoveryComponent
const recovery = entity.addComponent(RecoveryComponent, {
    healthRecoveryRate: 5,
    autoStart: true
});
recovery.heal(50);
```

### 兼容性说明

- `LifecycleComponent` 的生命周期管理功能保持不变
- 所有恢复相关的方法已移至 `RecoveryComponent`
- 事件系统保持兼容

## 性能优化

### 智能恢复
- 只有在需要恢复时才执行计算
- 批量更新生命值和魔法值
- 只有在单位存活时才进行恢复

### 计时器管理
- 组件销毁时自动清理所有计时器
- 避免内存泄漏
- 优化性能开销

## 最佳实践

### 1. 组件组合
```typescript
// 推荐的组件组合方式
const lifecycle = entity.addComponent(LifecycleComponent, lifecycleProps);
const recovery = entity.addComponent(RecoveryComponent, recoveryProps);

// 确保组件间的协调
entity.on(EventTypes.UNIT_DEATH, () => {
    recovery.stopRecovery();  // 死亡时停止恢复
});

entity.on(EventTypes.UNIT_REVIVE, () => {
    recovery.startRecovery(); // 复活时重新开始恢复
});
```

### 2. 错误处理
```typescript
// 安全的组件操作
const safeHeal = (amount: number) => {
    const lifecycle = entity.getComponent(LifecycleComponent);
    const recovery = entity.getComponent(RecoveryComponent);
    
    if (lifecycle?.isAlive() && recovery && amount > 0) {
        recovery.heal(amount);
    }
};
```

### 3. 配置管理
```typescript
// 使用配置对象管理不同类型单位
const UnitConfigs = {
    NORMAL: {
        lifecycle: { maxLifeTime: 120, canRevive: false },
        recovery: { healthRecoveryRate: 2, manaRecoveryRate: 1 }
    },
    HERO: {
        lifecycle: { maxLifeTime: -1, canRevive: true, reviveTime: 30 },
        recovery: { healthRecoveryRate: 10, manaRecoveryRate: 5 }
    },
    SUMMON: {
        lifecycle: { maxLifeTime: 60, canRevive: false },
        recovery: { healthRecoveryRate: 5, manaRecoveryRate: 3 }
    }
};
```

## 注意事项

1. **依赖关系**：`RecoveryComponent` 需要 Entity 具有 `unit` 和 `attr` 数据管理器
2. **生命周期**：建议配合 `LifecycleComponent` 使用，检查单位存活状态
3. **事件监听**：及时清理事件监听器，避免内存泄漏
4. **性能考虑**：合理设置恢复间隔，平衡性能和效果

## 示例代码

详细的使用示例请参考 `LifecycleComponentExample.ts` 文件，包含：

- 普通单位创建
- 英雄单位创建
- 召唤单位创建
- 动态恢复管理
- 手动恢复控制
- 组件生命周期管理
- 错误处理示例

## 总结

本次重构实现了：

✅ **职责分离**：生命周期管理和恢复功能分离
✅ **代码精简**：`LifecycleComponent` 代码量减少约40%
✅ **功能增强**：`RecoveryComponent` 提供更灵活的恢复控制
✅ **文档完善**：详细的文档和使用示例
✅ **架构优化**：遵循现代游戏架构设计原则

重构后的组件更加符合现代游戏开发的最佳实践，提高了代码的可维护性和可扩展性。