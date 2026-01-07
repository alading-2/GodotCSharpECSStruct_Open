# ExpComponent - 经验组件

## 概述

`ExpComponent` 是一个专门用于管理单位经验值和等级系统的 ECS 组件。它从原有的 `UnitComponent` 中分离出来，遵循单一职责原则，专注于经验相关的功能。

## 设计理念

### 现代游戏设计原则

- **组件化设计**: 将经验系统独立为单独组件，提高代码的可维护性和可扩展性
- **事件驱动**: 通过事件系统通知其他组件经验和等级的变化
- **配置灵活**: 支持自定义经验计算公式和等级上限
- **数据驱动**: 经验值和等级数据存储在实体的数据系统中

### ECS 架构优势

- **解耦合**: 经验系统与单位系统分离，降低耦合度
- **可复用**: 可以轻松应用到不同类型的实体上
- **易测试**: 独立的组件更容易进行单元测试
- **易扩展**: 可以轻松添加新的经验相关功能

## 核心功能

### 1. 经验管理

- 获得经验值 (`gainExp`)
- 设置当前经验值 (`setCurrentExp`)
- 获取当前经验值 (`currentExp`)
- 获取总经验值 (`totalExp`)

### 2. 等级系统

- 自动升级检测
- 手动升级 (`levelUp`)
- 直接设置等级 (`setLevel`)
- 等级上限控制

### 3. 经验计算

- 自定义经验公式支持
- 默认线性增长公式
- 升级所需经验计算
- 总经验值计算

### 4. 事件系统

- `UNIT_EXP_GAINED`: 获得经验事件
- `UNIT_LEVEL_UP`: 升级事件
- `UNIT_LEVEL_CHANGED`: 等级变更事件

## 使用方法

### 基本用法

```typescript
// 创建实体并添加经验组件
const entity = new Entity();
const expComponent = entity.addComponent(ExpComponent, {
  initialLevel: 1,
  initialExp: 0,
  maxLevel: 100,
});

// 获得经验
expComponent.gainExp(50, "击杀怪物");

// 检查是否可以升级
if (expComponent.canLevelUp()) {
  expComponent.levelUp();
}
```

### 自定义经验公式

```typescript
const expComponent = entity.addComponent(ExpComponent, {
  initialLevel: 1,
  initialExp: 0,
  maxLevel: 100,
  // 自定义经验公式: 每级所需经验 = 等级 * 100 + 50
  expFormula: (level: number) => level * 100 + 50,
});
```

### 事件监听

```typescript
// 监听经验获得事件
entity.on(
  EventTypes.UNIT_EXP_GAINED,
  (data: EventTypes.UnitExpGainedEventData) => {
    console.log(`获得经验: +${data.expAmount}`);
  }
);

// 监听升级事件
entity.on(EventTypes.UNIT_LEVEL_UP, (data: EventTypes.UnitLevelUpEventData) => {
  console.log(`升级! ${data.oldLevel} -> ${data.newLevel}`);
});
```

## 配置选项

### ExpComponentProps 接口

```typescript
interface ExpComponentProps {
  /** 初始等级 */
  initialLevel?: number;
  /** 初始经验值 */
  initialExp?: number;
  /** 最大等级限制 */
  maxLevel?: number;
  /** 自定义经验计算公式 */
  expFormula?: (level: number) => number;
}
```

## API 参考

### 属性

- `level: number` - 当前等级
- `currentExp: number` - 当前经验值
- `totalExp: number` - 总经验值
- `maxLevel: number` - 最大等级
- `expRequiredForNextLevel: number` - 升级所需经验值

### 方法

- `gainExp(amount: number, source?: string): void` - 获得经验
- `setCurrentExp(exp: number): void` - 设置当前经验值
- `levelUp(): boolean` - 升级
- `setLevel(level: number): void` - 设置等级
- `canLevelUp(): boolean` - 检查是否可以升级
- `getExpForLevel(level: number): number` - 获取指定等级所需经验
- `getTotalExpForLevel(level: number): number` - 获取达到指定等级的总经验

## 与其他组件的集成

### UnitComponent 集成

经验组件与单位组件协同工作，通过实体的数据系统共享等级信息。

### AttributeComponent 集成

等级变化时可以触发属性重新计算，实现等级对属性的影响。

### 事件系统集成

通过事件系统与其他组件通信，实现松耦合的系统设计。

## 最佳实践

1. **合理设置经验公式**: 根据游戏平衡性需求设计经验增长曲线
2. **使用事件监听**: 通过事件系统响应经验和等级变化
3. **数据持久化**: 确保经验数据能够正确保存和加载
4. **性能优化**: 避免频繁的经验计算，可以考虑缓存机制
5. **错误处理**: 对异常情况进行适当的错误处理和日志记录

## 扩展建议

1. **经验加成系统**: 添加经验倍率和加成效果
2. **经验衰减**: 实现死亡经验损失等机制
3. **经验分享**: 支持队伍经验分享功能
4. **经验池**: 实现经验暂存和批量获得功能
5. **成就系统**: 基于经验获得触发成就解锁

## 注意事项

- 确保在使用前正确初始化组件
- 注意等级上限的边界检查
- 合理处理经验溢出情况
- 保持与数据系统的同步
- 适当的日志记录有助于调试
