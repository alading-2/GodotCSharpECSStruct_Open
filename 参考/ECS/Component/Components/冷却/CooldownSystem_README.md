# 统一冷却系统设计文档

## 概述

本项目实现了一个基于现代游戏架构设计的统一冷却系统，遵循 ECS(Entity Component System)模式和组件化设计原则。该系统参考了 Unity ECS、Unreal Engine Component 等主流游戏引擎的设计模式。

## 架构设计

### 核心组件

1. **CooldownComponent** - 冷却组件

   - 负责管理 Entity 的所有冷却实例
   - 支持多个并发冷却（如物品冷却、技能冷却等）
   - 提供完整的冷却生命周期管理

2. **CooldownSystem** - 冷却系统

   - 统一处理所有冷却逻辑
   - 批量更新冷却状态
   - 事件驱动的状态通知

3. **EventTypes** - 事件类型定义
   - 定义冷却相关的所有事件常量
   - 提供类型安全的事件数据接口

### 设计原则

- **单一职责原则**：每个组件只负责特定的冷却功能
- **组件化设计**：可被不同类型的组件复用（ItemComponent、AbilityComponent 等）
- **事件驱动**：通过事件系统进行松耦合通信
- **数据与逻辑分离**：组件负责数据管理，系统负责业务逻辑

## 使用方法

### 1. 基本使用

```typescript
// 获取或创建冷却组件
const cooldownComponent =
  entity.getComponent(CooldownComponent) ||
  entity.addComponent(CooldownComponent);

// 创建冷却实例
cooldownComponent.createCooldown("skill_fireball", 5.0, () => {
  console.log("火球术冷却完成");
});

// 开始冷却
cooldownComponent.startCooldownById("skill_fireball");
```

### 2. 在 ItemComponent 中使用

```typescript
// 物品冷却已集成到ItemComponent中
const item = entity.getComponent(ItemComponent);

// 设置冷却时间
item.cool = 3.0;

// 开始冷却
item.cooltime = item.cool;

// 检查冷却状态
if (item.isOnCooldown()) {
  console.log(`剩余冷却时间: ${item.cooltime}`);
}
```

### 3. 在 AbilityComponent 中使用

```typescript
// 技能冷却已集成到AbilityComponent中
const ability = entity.getComponent(AbilityComponent);

// 冷却状态检查
if (ability.isOnCooldown) {
  console.log(`技能冷却中，剩余时间: ${ability.remainingCooldown}`);
}

// 重置冷却
ability.resetCooldown();

// 暂停/恢复冷却
ability.pauseCooldown();
ability.resumeCooldown();
```

### 4. 事件监听

```typescript
// 监听冷却事件
entity.on(EventTypes.COOLDOWN_START, (data) => {
  console.log(`冷却开始: ${data.cooldownId}`);
});

entity.on(EventTypes.COOLDOWN_COMPLETE, (data) => {
  console.log(`冷却完成: ${data.cooldownId}`);
});

entity.on(EventTypes.COOLDOWN_UPDATE, (data) => {
  console.log(`冷却更新: ${data.cooldownId}, 剩余: ${data.remainingTime}`);
});
```

## API 参考

### CooldownComponent

#### 方法

- `createCooldown(id: string, duration: number, onComplete?: () => void): void`

  - 创建新的冷却实例

- `startCooldownById(id: string): boolean`

  - 开始指定 ID 的冷却

- `resetCooldownById(id: string): boolean`

  - 重置指定 ID 的冷却

- `pauseCooldownById(id: string): boolean`

  - 暂停指定 ID 的冷却

- `resumeCooldownById(id: string): boolean`

  - 恢复指定 ID 的冷却

- `isOnCooldown(id: string): boolean`

  - 检查指定 ID 是否在冷却中

- `getRemainingTime(id: string): number`

  - 获取指定 ID 的剩余冷却时间

- `getProgress(id: string): number`
  - 获取指定 ID 的冷却进度(0-1)

### 事件类型

#### EventTypes 冷却事件

- `COOLDOWN_START` - 冷却开始
- `COOLDOWN_UPDATE` - 冷却更新
- `COOLDOWN_COMPLETE` - 冷却完成
- `COOLDOWN_RESET` - 冷却重置
- `COOLDOWN_PAUSE` - 冷却暂停
- `COOLDOWN_RESUME` - 冷却恢复

#### CooldownEvent 数据接口

```typescript
interface CooldownEvent {
  entityId: string; // EntityID
  cooldownId: string; // 冷却实例ID
  duration: number; // 冷却总时长
  remainingTime: number; // 剩余时间
  progress: number; // 进度(0-1)
}
```

## 优势

1. **统一管理**：所有冷却逻辑集中在一个系统中，便于维护和调试
2. **高度复用**：可被任何需要冷却功能的组件使用
3. **性能优化**：批量更新，减少单独计时器的开销
4. **事件驱动**：松耦合的通信机制，便于扩展
5. **类型安全**：完整的 TypeScript 类型定义
6. **现代架构**：符合主流游戏引擎的设计模式

## 迁移指南

### 从旧的冷却系统迁移

1. **ItemComponent**：已自动迁移，保持原有 API 兼容性
2. **AbilityComponent**：已自动迁移，保持原有 API 兼容性
3. **自定义组件**：参考上述使用方法进行迁移

### 注意事项

- 确保在使用前注册 CooldownSystem 到 SystemManager
- 事件监听器需要在组件销毁时正确清理
- 多个冷却实例使用不同的 ID 进行区分

## 扩展性

该系统设计具有良好的扩展性，可以轻松添加：

- 冷却修饰符（加速、减速等）
- 冷却分组管理
- 持久化冷却状态
- 网络同步支持
- 可视化调试工具

## 总结

统一冷却系统为项目提供了一个现代化、高性能、易维护的冷却管理解决方案。它遵循了现代游戏开发的最佳实践，为后续功能扩展奠定了坚实的基础。
