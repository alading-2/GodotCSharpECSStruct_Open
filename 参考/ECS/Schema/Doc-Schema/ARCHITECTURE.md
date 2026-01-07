# Schema 系统架构设计

## 设计理念

Schema 系统基于以下核心设计理念：

- 🎯 **数据与逻辑分离**：数据结构定义与业务逻辑完全分离
- 🔒 **类型安全**：编译时类型检查，运行时数据验证
- ⚡ **高性能**：智能缓存，批量操作，最小化计算开销
- 🔧 **易扩展**：模块化设计，支持自定义扩展
- 🧪 **可测试**：完整的测试框架，易于调试和验证

## 系统架构

### 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                    Schema系统架构                            │
├─────────────────────────────────────────────────────────────┤
│  应用层 (Application Layer)                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Entity  │  │ Component   │  │ GameLogic   │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
├─────────────────────────────────────────────────────────────┤
│  接口层 (Interface Layer)                                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ get/set API │  │ Validation  │  │ Events      │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
├─────────────────────────────────────────────────────────────┤
│  核心层 (Core Layer)                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │SchemaRegistry│  │DataValidation │ │ComputedEngine│        │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
├─────────────────────────────────────────────────────────────┤
│  数据层 (Data Layer)                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │ Schema定义  │  │ 数据存储    │  │ 缓存管理    │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
└─────────────────────────────────────────────────────────────┘
```

### 核心组件关系

```
Schema ←─── SchemaRegistry ←─── Entity
       │                    │                │
       ↓                    ↓                ↓
PropertyDefinition    DataValidation     DataAccess
       │                    │                │
       ↓                    ↓                ↓
ComputedProperty      ValidationResult   CacheManager
```

## 核心组件设计

### 1. SchemaRegistry (Schema 注册中心)

**职责**：

- 管理所有 Schema 定义
- 提供 Schema 查询和缓存
- 确保 Schema 的唯一性和完整性

**设计模式**：单例模式

```typescript
class SchemaRegistry {
  private static schemas = new Map<string, Schema<any>>();
  private static initialized = false;

  // 单例访问，全局唯一
  static registerSchema<T>(name: string, schema: Schema<T>): void;
  static getSchema<T>(name: string): Schema<T>;
  static hasSchema(name: string): boolean;
}
```

**关键特性**：

- 🔒 线程安全的单例实现
- 💾 内存缓存，避免重复解析
- ✅ Schema 完整性验证
- 🔍 快速查询接口

### 2. DataValidation (数据验证模块)

**职责**：

- 数据类型验证
- 约束条件检查
- 自定义验证规则
- 错误信息收集

**设计模式**：策略模式

**实现位置**：已集成到 SchemaRegistry 中

```typescript
class SchemaRegistry {
  // 数据验证功能已集成到注册中心
  static validateData<T>(schemaName: string, data: T): SchemaValidationResult;
  static generateTestReport(): string;
}
```

**验证策略**：

- 📊 类型验证：number, string, boolean, object, array, any
- 📏 约束验证：min, max, length, pattern, enum
- 🎯 自定义验证：支持自定义验证函数
- 🔗 依赖验证：检查计算属性依赖关系

### 3. ComputedEngine (计算属性引擎)

**职责**：

- 计算属性值计算
- 依赖关系管理
- 缓存策略实现
- 循环依赖检测

**设计模式**：观察者模式 + 缓存模式

```typescript
class ComputedEngine {
  private cache = new Map<string, any>();
  private dependencies = new Map<string, Set<string>>();

  // 观察者模式，监听依赖变更
  compute<T>(schema: Schema<T>, data: T, key: keyof T): any;
  invalidateCache(key: string): void;
  detectCircularDependency<T>(schema: Schema<T>): string[];
}
```

**缓存策略**：

- 🎯 按需计算：只在访问时计算
- 💾 智能缓存：缓存计算结果
- 🔄 自动失效：依赖变更时自动清除缓存
- 📊 批量失效：一次变更影响多个缓存

### 4. DataAccessLayer (数据访问层)

**职责**：

- 统一的数据访问接口
- 数据变更通知
- 批量操作优化
- 事务性操作支持

**设计模式**：门面模式 + 观察者模式

```typescript
class DataAccessLayer {
  private data: Record<string, any> = {};
  private listeners = new Map<string, Function[]>();

  // 门面模式，统一数据访问接口
  get<T>(key: keyof T): T[keyof T];
  set<T>(key: keyof T, value: T[keyof T]): boolean;
  setMultiple<T>(values: Partial<T>): boolean;

  // 观察者模式，数据变更通知
  onPropertyChanged<T>(key: keyof T, listener: Function): void;
}
```

## 性能优化设计

### 1. 缓存策略

```
计算属性缓存架构：

┌─────────────┐    依赖变更    ┌─────────────┐
│  属性变更   │ ──────────→   │ 缓存失效    │
└─────────────┘              └─────────────┘
       │                            │
       ↓                            ↓
┌─────────────┐    重新计算    ┌─────────────┐
│ 触发计算    │ ←──────────   │ 缓存更新    │
└─────────────┘              └─────────────┘
```

**缓存特性**：

- 🎯 **按需缓存**：只缓存启用缓存的计算属性
- 🔄 **智能失效**：依赖变更时自动失效相关缓存
- 📊 **批量失效**：一次变更可能影响多个缓存
- 💾 **内存优化**：及时清理无用缓存

### 2. 批量操作优化

```typescript
// 优化前：多次单独操作
obj.set("力量", 20); // 触发计算和通知
obj.set("敏捷", 15); // 触发计算和通知
obj.set("智力", 25); // 触发计算和通知

// 优化后：批量操作
obj.setMultiple({
  // 一次性设置，批量计算和通知
  力量: 20,
  敏捷: 15,
  智力: 25,
});
```

### 3. 依赖图优化

```
依赖关系图：

基础力量 ──┐
          ├──→ 最终力量 ──┐
力量加成 ──┘              ├──→ 最终攻击力
                         │
基础敏捷 ──┐              │
          ├──→ 最终敏捷 ──┘
敏捷加成 ──┘
```

**优化策略**：

- 📊 **依赖分析**：构建完整的依赖关系图
- 🎯 **最小计算**：只计算真正需要的属性
- 🔄 **拓扑排序**：按依赖顺序计算，避免重复
- ⚡ **并行计算**：无依赖关系的属性可并行计算

## 扩展性设计

### 1. 插件架构

```typescript
// 支持自定义验证器（通过SchemaUtils扩展）
SchemaUtils.addCustomValidator("email", (value: string) => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
});

// 支持自定义计算函数
SchemaUtils.registerComputeFunction("complex", (data, params) => {
  return complexCalculation(data, params);
});
```

### 2. 类型扩展

```typescript
// 支持新的属性类型
type ExtendedPropertyType = PropertyType | "date" | "vector" | "color";

// 支持自定义约束
interface ExtendedConstraints extends PropertyConstraints {
  dateRange?: [Date, Date];
  vectorLength?: number;
  colorFormat?: "hex" | "rgb" | "hsl";
}
```

### 3. 事件扩展

```typescript
// 支持自定义事件
interface CustomEvents {
  "schema:property:beforeChange": PropertyChangeEvent;
  "schema:property:afterChange": PropertyChangeEvent;
  "schema:validation:failed": ValidationEvent;
  "schema:computation:error": ComputationEvent;
}
```

## 错误处理设计

### 1. 错误分类

```typescript
enum SchemaErrorType {
  SCHEMA_NOT_FOUND = "SCHEMA_NOT_FOUND",
  TYPE_MISMATCH = "TYPE_MISMATCH",
  CONSTRAINT_VIOLATION = "CONSTRAINT_VIOLATION",
  CIRCULAR_DEPENDENCY = "CIRCULAR_DEPENDENCY",
  COMPUTATION_ERROR = "COMPUTATION_ERROR",
}
```

### 2. 错误恢复策略

```
错误处理流程：

┌─────────────┐    验证失败    ┌─────────────┐
│  数据设置   │ ──────────→   │ 错误捕获    │
└─────────────┘              └─────────────┘
       │                            │
       ↓                            ↓
┌─────────────┐    记录日志    ┌─────────────┐
│ 保持原值    │ ←──────────   │ 错误处理    │
└─────────────┘              └─────────────┘
```

### 3. 调试支持

```typescript
// 调试模式
SchemaSystem.enableDebugMode();

// 详细日志
SchemaSystem.setLogLevel("debug");

// 性能监控
SchemaSystem.enablePerformanceMonitoring();
```

## 测试架构

### 1. 测试分层

```
测试架构：

┌─────────────────────────────────────────┐
│  集成测试 (Integration Tests)            │
├─────────────────────────────────────────┤
│  组件测试 (Component Tests)              │
├─────────────────────────────────────────┤
│  单元测试 (Unit Tests)                   │
└─────────────────────────────────────────┘
```

### 2. 测试工具

```typescript
// Schema定义测试
SchemaTest.testSchema("Inte_Unit");

// 性能测试
SchemaTest.performanceTest("Inte_Unit", 1000);

// 完整测试套件
SchemaTest.runFullTestSuite();
```

## 总结

Schema 系统采用了现代软件架构的最佳实践：

- 🏗️ **分层架构**：清晰的职责分离
- 🔧 **模块化设计**：高内聚，低耦合
- ⚡ **性能优化**：智能缓存，批量操作
- 🔒 **类型安全**：编译时和运行时双重保障
- 🧪 **可测试性**：完整的测试框架
- 📈 **可扩展性**：插件化架构支持

这个架构为 Entity 系统提供了强大、灵活、高性能的数据管理能力，是现代游戏开发的理想选择。
