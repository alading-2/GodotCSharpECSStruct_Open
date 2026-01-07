# Schema 系统 API 参考

## 核心接口

### Schema<T>

游戏对象 Schema 定义接口

```typescript
interface Schema<T> {
  interfaceName: string; // 接口名称
  properties: PropertyDefinition<T>[]; // 属性定义列表
  computed?: ComputedProperty<T>[]; // 计算属性列表（可选）
}
```

### PropertyDefinition<T>

属性定义接口

```typescript
interface PropertyDefinition<T> {
  key: keyof T; // 属性键名
  type: PropertyType; // 属性类型
  defaultValue: any; // 默认值
  description?: string; // 属性描述
  constraints?: PropertyConstraints; // 属性约束
}
```

### ComputedProperty<T>

计算属性定义接口

```typescript
interface ComputedProperty<T> {
  key: keyof T; // 计算属性键名
  dependencies: (keyof T)[]; // 依赖属性列表
  compute: (data: T) => any; // 计算函数
  cache?: boolean; // 是否启用缓存
  description?: string; // 描述信息
}
```

### PropertyConstraints

属性约束接口

```typescript
interface PropertyConstraints {
  min?: number; // 最小值
  max?: number; // 最大值
  minLength?: number; // 最小长度
  maxLength?: number; // 最大长度
  pattern?: string; // 正则表达式
  enum?: any[]; // 枚举值
  required?: boolean; // 是否必需
  custom?: (value: any, data: any) => boolean; // 自定义验证
}
```

### PropertyType

支持的属性类型

```typescript
type PropertyType =
  | "number"
  | "string"
  | "boolean"
  | "object"
  | "array"
  | "any";
```

## SchemaRegistry API

Schema 注册中心，管理所有 Schema 定义

### 静态方法

#### registerSchema<T>(interfaceName: string, schema: Schema<T>): void

注册 Schema 定义

```typescript
SchemaRegistry.registerSchema("Inte_Unit", UNIT_SCHEMA);
```

#### getSchema<T>(interfaceName: string): Schema<T>

获取 Schema 定义

```typescript
const schema = SchemaRegistry.getSchema<Inte_Unit>("Inte_Unit");
```

#### hasSchema(interfaceName: string): boolean

检查 Schema 是否存在

```typescript
if (SchemaRegistry.hasSchema("Inte_Unit")) {
  // Schema存在
}
```

#### getAllSchemas(): Map<string, Schema<any>>

获取所有已注册的 Schema

```typescript
const allSchemas = SchemaRegistry.getAllSchemas();
```

#### removeSchema(interfaceName: string): boolean

移除 Schema 定义

```typescript
const removed = SchemaRegistry.removeSchema("Inte_Unit");
```

#### clearAll(): void

清除所有 Schema（仅用于测试）

```typescript
SchemaRegistry.clearAll();
```

## SchemaRegistry 数据验证 API

SchemaRegistry 现在包含了数据验证功能

### 数据验证方法

#### validateData<T>(schemaName: string, data: T): SchemaValidationResult

验证数据对象是否符合 Schema 定义（主要用于测试和调试）

```typescript
const result = SchemaRegistry.validateData("Inte_Unit", {
  当前生命值: 100,
  等级: 5,
});

if (result.valid) {
  console.log("验证通过");
} else {
  console.log("验证失败:", result.errors);
}
```

**注意**: 此方法主要用于开发和测试阶段的数据验证，生产环境中 Entity 会自动进行属性验证。

#### validateProperty(value: any, definition: PropertyDefinition<any>): ValidationResult

验证单个属性

```typescript
// 单个属性验证现在通过SchemaUtils进行
const isValid = SchemaUtils.validatePropertyType(100, "number");
const errors = SchemaUtils.validatePropertyConstraints(100, {
  min: 0,
  max: 999,
});
```

#### generateTestReport(): string

生成测试报告（主要用于测试和调试）

```typescript
const report = SchemaRegistry.generateTestReport();
console.log(report);
```

**注意**: 此方法主要用于开发阶段生成 Schema 系统的详细报告，包含统计信息和 Schema 详情。

### SchemaValidationResult

Schema 验证结果接口

```typescript
interface SchemaValidationResult {
  valid: boolean; // 是否验证通过
  errors: string[]; // 错误信息列表
  warnings: string[]; // 警告信息列表
}
```

## SchemaTest API

Schema 测试工具，提供测试和调试功能

### 静态方法

#### testSchema(interfaceName: string): boolean

测试指定 Schema

```typescript
const passed = SchemaTest.testSchema("Inte_Unit");
```

#### testAllSchemas(): { [interfaceName: string]: boolean }

测试所有 Schema

```typescript
const results = SchemaTest.testAllSchemas();
```

#### performanceTest(interfaceName: string, iterations: number): PerformanceResult

性能测试

```typescript
const result = SchemaTest.performanceTest("Inte_Unit", 1000);
console.log(`平均耗时: ${result.averageTime}ms`);
```

#### runFullTestSuite(): TestSuiteResult

运行完整测试套件

```typescript
const result = SchemaTest.runFullTestSuite();
```

### PerformanceResult

性能测试结果

```typescript
interface PerformanceResult {
  totalTime: number; // 总耗时
  averageTime: number; // 平均耗时
  iterations: number; // 迭代次数
  operationsPerSecond: number; // 每秒操作数
}
```

### TestSuiteResult

测试套件结果

```typescript
interface TestSuiteResult {
  totalTests: number; // 总测试数
  passedTests: number; // 通过测试数
  failedTests: number; // 失败测试数
  results: { [test: string]: boolean }; // 详细结果
}
```

## SchemaUtils API

Schema 工具类，提供实用工具方法

### 静态方法

#### validatePropertyType(value: any, expectedType: PropertyType): boolean

验证属性类型

```typescript
const isValid = SchemaUtils.validatePropertyType(123, "number"); // true
```

#### validatePropertyConstraints(value: any, constraints: PropertyConstraints): string[]

验证属性约束

```typescript
const errors = SchemaUtils.validatePropertyConstraints(150, {
  min: 0,
  max: 100,
}); // ['Value 150 exceeds maximum 100']
```

#### getDefaultValue<T>(schema: Schema<T>, key: keyof T): any

获取属性默认值

```typescript
const defaultHP = SchemaUtils.getDefaultValue(UNIT_SCHEMA, "当前生命值");
```

#### computeProperty<T>(schema: Schema<T>, data: T, key: keyof T): any

计算属性值

```typescript
const finalStr = SchemaUtils.computeProperty(UNIT_SCHEMA, unitData, "最终力量");
```

#### getDependencies<T>(schema: Schema<T>, key: keyof T): (keyof T)[]

获取属性依赖

```typescript
const deps = SchemaUtils.getDependencies(UNIT_SCHEMA, "最终攻击力");
```

#### validateCircularDependency<T>(schema: Schema<T>): string[]

检测循环依赖

```typescript
const errors = SchemaUtils.validateCircularDependency(UNIT_SCHEMA);
```

## SchemaSystem API

Schema 系统管理器，提供系统级功能

### 静态方法

#### initialize(): void

初始化 Schema 系统

```typescript
SchemaSystem.initialize();
```

#### isInitialized(): boolean

检查系统是否已初始化

```typescript
if (!SchemaSystem.isInitialized()) {
  SchemaSystem.initialize();
}
```

#### getStatus(): SystemStatus

获取系统状态

```typescript
const status = SchemaSystem.getStatus();
console.log(`已注册Schema数量: ${status.schemaCount}`);
```

#### enablePerformanceMonitoring(): void

启用性能监控

```typescript
SchemaSystem.enablePerformanceMonitoring();
```

#### getPerformanceStats(): PerformanceStats

获取性能统计

```typescript
const stats = SchemaSystem.getPerformanceStats();
```

### SystemStatus

系统状态接口

```typescript
interface SystemStatus {
  initialized: boolean; // 是否已初始化
  schemaCount: number; // Schema数量
  schemas: string[]; // Schema名称列表
  performanceMonitoring: boolean; // 是否启用性能监控
}
```

### PerformanceStats

性能统计接口

```typescript
interface PerformanceStats {
  totalOperations: number; // 总操作数
  averageOperationTime: number; // 平均操作时间
  cacheHitRate: number; // 缓存命中率
  memoryUsage: number; // 内存使用量
}
```

## 事件接口

### PropertyChangeEvent

属性变更事件

```typescript
interface PropertyChangeEvent<T> {
  key: keyof T; // 变更的属性键
  oldValue: any; // 旧值
  newValue: any; // 新值
  source: Entity<T>; // 事件源对象
}
```

### ObjectResetEvent

对象重置事件

```typescript
interface ObjectResetEvent<T> {
  oldData: T; // 重置前数据
  newData: T; // 重置后数据
  source: Entity<T>; // 事件源对象
}
```

## 使用示例

### 完整的 Schema 定义示例

```typescript
export const EXAMPLE_SCHEMA: Schema<ExampleInterface> = {
  interfaceName: "ExampleInterface",
  properties: [
    {
      key: "name",
      type: "string",
      defaultValue: "",
      description: "名称",
      constraints: {
        required: true,
        minLength: 1,
        maxLength: 50,
        pattern: "^[a-zA-Z\\u4e00-\\u9fa5]+$",
      },
    },
    {
      key: "level",
      type: "number",
      defaultValue: 1,
      description: "等级",
      constraints: {
        min: 1,
        max: 100,
      },
    },
    {
      key: "category",
      type: "string",
      defaultValue: "普通",
      description: "类别",
      constraints: {
        enum: ["普通", "精英", "英雄", "传说"],
      },
    },
  ],
  computed: [
    {
      key: "displayName",
      dependencies: ["name", "level", "category"],
      compute: (data) => `[${data.category}] ${data.name} (Lv.${data.level})`,
      cache: true,
      description: "显示名称",
    },
  ],
};
```

### 完整的使用流程

```typescript
// 1. 注册Schema
SchemaRegistry.registerSchema("ExampleInterface", EXAMPLE_SCHEMA);

// 2. 创建Entity
const obj = EntityFactory.create<ExampleInterface>("ExampleInterface");

// 3. 设置数据
obj.setMultiple({
  name: "测试对象",
  level: 10,
  category: "英雄",
});

// 4. 获取数据
const displayName = obj.get("displayName"); // "[英雄] 测试对象 (Lv.10)"

// 5. 监听变更
obj.onPropertyChanged("level", (oldValue, newValue) => {
  console.log(`等级变更: ${oldValue} -> ${newValue}`);
});

// 6. 验证数据
const result = SchemaRegistry.validateData("ExampleInterface", {
  name: "有效名称",
  level: 50,
  category: "精英",
});
```

## 错误处理

### 常见错误类型

```typescript
// Schema未找到
SchemaNotFoundError: "Schema not found: InterfaceName";

// 类型不匹配
TypeMismatchError: "Expected number, got string for property: propertyName";

// 约束违反
ConstraintViolationError: "Value violates constraint: min=0, got -5";

// 循环依赖
CircularDependencyError: "Circular dependency detected: A -> B -> A";

// 计算错误
ComputationError: "Failed to compute property: propertyName";
```

### 错误处理最佳实践

```typescript
try {
  const result = obj.set("level", newValue);
  if (!result) {
    console.warn("属性设置失败，可能违反了约束条件");
  }
} catch (error) {
  console.error("设置属性时发生错误:", error.message);
}
```

这个 API 参考涵盖了 Schema 系统的所有核心功能和接口，可以作为开发时的快速查询手册。
