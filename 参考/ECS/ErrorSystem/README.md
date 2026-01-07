# 错误处理系统详细说明

## 概述

错误处理系统是现代游戏对象架构的关键部分，提供了统一的方式来处理、报告和恢复各种错误。该系统旨在提高代码的健壮性，防止错误导致游戏崩溃，并提供有用的调试信息。

## 核心组件

### ErrorTypes

`ErrorTypes`定义了系统使用的自定义错误类型，分为三大类：

```typescript
// 基础游戏对象错误
export class EntityError extends Error {
  /* ... */
}
export class EntityNotFoundError extends EntityError {
  /* ... */
}
export class EntityAlreadyDestroyedError extends EntityError {
  /* ... */
}
export class InvalidPropertyError extends EntityError {
  /* ... */
}

// 组件相关错误
export class ComponentError extends Error {
  /* ... */
}
export class ComponentNotFoundError extends ComponentError {
  /* ... */
}
export class ComponentAlreadyAttachedError extends ComponentError {
  /* ... */
}
export class ComponentDependencyError extends ComponentError {
  /* ... */
}
export class ComponentStateError extends ComponentError {
  /* ... */
}

// 事件系统错误
export class EventSystemError extends Error {
  /* ... */
}
export class EventHandlerError extends EventSystemError {
  /* ... */
}
export class InvalidEventTypeError extends EventSystemError {
  /* ... */
}
export class EventBusOverflowError extends EventSystemError {
  /* ... */
}
```

### ErrorHandler

`ErrorHandler`是一个静态工具类，提供集中式的错误处理功能：

```typescript
export class ErrorHandler {
  // 处理错误
  static handleError(error: Error, context?: Entity): void;

  // 记录错误
  private static logError(error: Error): void;

  // 根据错误类型处理
  private static handleEntityError(error: EntityError, context?: Entity): void;
  private static handleComponentError(
    error: ComponentError,
    context?: Entity
  ): void;
  private static handleEventSystemError(
    error: EventSystemError,
    context?: Entity
  ): void;

  // 报告警告
  static reportWarning(message: string, context?: Entity): void;

  // 断言
  static assert(condition: boolean, message: string, context?: Entity): void;

  // 安全执行函数
  static safeExecute<T>(fn: () => T, context?: Entity, fallback?: T): T;
}
```

## 错误类型详解

### 游戏对象错误

1. **EntityError**: 所有游戏对象错误的基类
2. **EntityNotFoundError**: 找不到指定 ID 的游戏对象
3. **EntityAlreadyDestroyedError**: 尝试操作已销毁的游戏对象
4. **InvalidPropertyError**: 访问或修改无效属性

### 组件错误

1. **ComponentError**: 所有组件错误的基类
2. **ComponentNotFoundError**: 尝试访问不存在的组件
3. **ComponentAlreadyAttachedError**: 尝试添加已存在的组件
4. **ComponentDependencyError**: 组件依赖不满足
5. **ComponentStateError**: 组件状态不正确（例如在未初始化组件上调用方法）

### 事件系统错误

1. **EventSystemError**: 所有事件系统错误的基类
2. **EventHandlerError**: 事件处理器执行出错
3. **InvalidEventTypeError**: 使用未注册的事件类型
4. **EventBusOverflowError**: 事件队列溢出

## 工作原理

### 错误处理流程

1. 代码中抛出特定类型的错误
2. `ErrorHandler.handleError()` 接收错误并根据类型处理
3. 错误被记录到日志
4. 系统尝试恢复或降级处理
5. 错误信息被发送到事件系统作为`GameEvents.ERROR`事件

### 自定义错误创建

```typescript
// 创建并抛出自定义错误
if (!Entity) {
  throw new EntityNotFoundError("无法找到ID为: " + id + "的游戏对象");
}

if (Entity.isObjectDestroyed()) {
  throw new EntityAlreadyDestroyedError(
    "尝试操作已销毁的游戏对象: " + Entity.getId()
  );
}

if (!component) {
  throw new ComponentNotFoundError("组件未找到: " + componentType.getType());
}
```

### 错误处理

```typescript
// 使用错误处理器处理错误
try {
  const component = Entity.getComponent(AttributeComponent);
  component.calculateAttribute("someAttribute");
} catch (error) {
  ErrorHandler.handleError(error, Entity);
}

// 或使用安全执行方法
const result = ErrorHandler.safeExecute(
  () => {
    const component = Entity.getComponent(AttributeComponent);
    return component.calculateAttribute("someAttribute");
  },
  Entity,
  defaultValue
);
```

## 主要功能

### 错误日志

错误处理系统会将所有错误详细记录到日志，包括：

- 错误类型
- 错误消息
- 错误堆栈
- 上下文信息（如相关游戏对象）

```typescript
// 记录错误
private static logError(error: Error): void {
    logger.error(`[ErrorSystem] ${error.name}: ${error.message}`);
    logger.error(`Stack: ${error.stack}`);
}
```

### 错误恢复

系统会尝试根据错误类型进行恢复：

```typescript
// 组件依赖错误恢复
private static handleComponentDependencyError(error: ComponentDependencyError, context?: Entity): void {
    if (!context) return;

    const missingDependency = error.dependencyType;
    logger.warn(`尝试自动添加缺失的组件依赖: ${missingDependency.getType()}`);

    try {
        context.addComponent(missingDependency);
        logger.info(`成功添加依赖组件: ${missingDependency.getType()}`);
    } catch (addError) {
        logger.error(`无法添加依赖组件: ${addError.message}`);
    }
}
```

### 断言

提供断言功能，用于验证条件并在失败时提供有用的错误消息：

```typescript
// 使用断言
ErrorHandler.assert(health > 0, "生命值必须大于0", unit);

// 断言实现
static assert(condition: boolean, message: string, context?: Entity): void {
    if (!condition) {
        logger.error(`[ErrorSystem] 断言失败: ${message}`);
        this.reportWarning(message, context);

        // 可选：在开发模式下抛出错误
        if (IS_DEV_MODE) {
            throw new Error(`断言失败: ${message}`);
        }
    }
}
```

### 安全执行

提供`safeExecute`方法，用于安全地执行可能抛出错误的函数：

```typescript
// 使用安全执行
const damage = ErrorHandler.safeExecute(() => {
    return damageCalculator.calculate(attacker, defender);
}, attacker, 0); // 失败时返回0伤害

// 安全执行实现
static safeExecute<T>(fn: () => T, context?: Entity, fallback?: T): T {
    try {
        return fn();
    } catch (error) {
        this.handleError(error, context);
        return fallback as T;
    }
}
```

## 与事件系统集成

错误处理系统与事件系统集成，将错误作为事件发出：

```typescript
// 发送错误事件
if (context && context.emit) {
  context.emit(GameEvents.ERROR, {
    error: error,
    name: error.name,
    message: error.message,
  });
}
```

其他系统可以监听这些错误事件：

```typescript
// 监听错误事件
Entity.on(GameEvents.ERROR, (data) => {
  console.log(`捕获到错误: ${data.name} - ${data.message}`);

  // 执行特定于对象的错误恢复
  if (data.name === "ComponentNotFoundError") {
    // 尝试创建缺失的组件
  }
});
```

## 使用示例

### 处理游戏对象错误

```typescript
// 创建游戏对象
const unit = new ModernUnit("hero1");

try {
  // 尝试操作已销毁的单位
  unit.destroy();
  unit.setPosition(100, 200); // 这将抛出EntityAlreadyDestroyedError
} catch (error) {
  if (error instanceof EntityAlreadyDestroyedError) {
    console.log("尝试操作已销毁的单位");
    // 创建新单位替代
    const newUnit = new ModernUnit("hero1_replacement");
    newUnit.setPosition(100, 200);
  } else {
    // 处理其他错误
    ErrorHandler.handleError(error);
  }
}
```

### 处理组件错误

```typescript
// 尝试获取组件
try {
  const healthComponent = unit.getComponent(HealthComponent);
  healthComponent.setHealth(100);
} catch (error) {
  if (error instanceof ComponentNotFoundError) {
    // 组件不存在，创建它
    console.log("健康组件不存在，添加它");
    const newComponent = unit.addComponent(HealthComponent);
    newComponent.setHealth(100);
  } else {
    // 处理其他错误
    ErrorHandler.handleError(error, unit);
  }
}
```

### 使用断言

```typescript
function takeDamage(unit, amount) {
  // 确保参数有效
  ErrorHandler.assert(unit != null, "单位不能为空");
  ErrorHandler.assert(amount >= 0, "伤害值不能为负数", unit);

  // 确保单位状态正确
  ErrorHandler.assert(!unit.isDead(), "无法对已死亡单位造成伤害", unit);

  // 处理伤害逻辑
  const currentHealth = unit.get("当前生命值");
  unit.set("当前生命值", Math.max(0, currentHealth - amount));
}
```

### 使用安全执行

```typescript
// 安全地执行复杂计算
function calculateCriticalDamage(attacker, defender) {
  return ErrorHandler.safeExecute(
    () => {
      const baseDamage = attacker.get("攻击力");
      const critMultiplier = attacker.get("暴击伤害");
      const defenderArmor = defender.get("防御");

      // 可能的错误: 属性不存在、防御为负等
      return Math.max(1, baseDamage * critMultiplier - defenderArmor);
    },
    attacker,
    1
  ); // 失败时返回1点伤害
}
```

## 最佳实践

1. **使用特定错误类型**：抛出特定于错误情况的错误类型，而不是通用 Error
2. **提供上下文**：错误处理时包含相关的游戏对象上下文
3. **有意义的错误消息**：错误消息应包含有用的信息，帮助调试
4. **优雅降级**：错误处理器应尽可能恢复或降级，而不是简单地中止操作
5. **日志级别**：根据错误严重性使用适当的日志级别
6. **断言关键条件**：使用断言验证关键假设和前置条件
7. **避免过度捕获**：只捕获预期的特定错误，让系统错误冒泡到更高级别处理

```typescript
// 过度捕获 (不推荐)
try {
  // 大量代码...
} catch (error) {
  ErrorHandler.handleError(error);
}

// 特定捕获 (推荐)
try {
  const component = Entity.getComponent(SpecificComponent);
  component.doSomething();
} catch (error) {
  if (error instanceof ComponentNotFoundError) {
    // 特定处理
  } else if (error instanceof ComponentStateError) {
    // 特定处理
  } else {
    // 重新抛出未预期的错误
    throw error;
  }
}
```

8. **测试错误处理**：编写测试确保错误处理逻辑正确运行
