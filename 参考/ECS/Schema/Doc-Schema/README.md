# Schema 系统说明文档

## 概述

Schema 系统是新一代 Entity 数据管理的核心，提供类型安全、高性能的数据定义、验证和计算功能，实现了真正的数据与逻辑分离。

## 系统架构

### 核心组件

```
Schema系统架构
├── Schema.ts     # Schema定义接口和工具类
├── SchemaRegistry.ts       # Schema注册中心
├── SchemaValidator.ts      # Schema验证器
├── SchemaTest.ts          # Schema测试工具
├── definitions/           # Schema定义文件
│   └── UnitSchema.ts      # 单位Schema定义
└── index.ts              # 统一导出
```

### 数据流向

```
Schema定义 → SchemaRegistry → Entity → 数据访问
    ↓              ↓              ↓           ↓
  结构定义      注册管理        数据存储    类型安全访问
    ↓              ↓              ↓           ↓
  验证规则      缓存管理        计算属性    性能优化
```

## 核心特性

### 1. 类型安全的 Schema 定义

```typescript
export const UNIT_SCHEMA: Schema<Inte_Unit> = {
  interfaceName: "Inte_Unit",
  properties: [
    {
      key: "当前生命值",
      type: "number",
      defaultValue: 100,
      description: "单位当前生命值",
      constraints: {
        min: 0,
        max: 9999,
      },
    },
  ],
  computed: [
    {
      key: "最终力量",
      dependencies: ["基础力量", "力量加成", "等级"],
      compute: (data) => {
        const base = data.基础力量 || 0;
        const bonus = (data.力量加成 || 0) / 100;
        const level = data.等级 || 1;
        return Math.floor(base * (1 + bonus) + level * 2);
      },
      cache: true,
      description: "计算后的最终力量值",
    },
  ],
};
```

### 2. 智能计算属性系统

- **依赖追踪**：自动追踪属性依赖关系
- **智能缓存**：只在依赖变更时重新计算
- **批量失效**：一次变更清除所有相关缓存
- **性能优化**：避免重复计算，提升性能

### 3. 完整的数据验证

```typescript
// 类型验证
type: 'number' | 'string' | 'boolean' | 'object' | 'array' | 'any'

// 约束验证
constraints: {
    min?: number;           // 最小值
    max?: number;           // 最大值
    minLength?: number;     // 最小长度
    maxLength?: number;     // 最大长度
    pattern?: string;       // 正则表达式
    enum?: any[];          // 枚举值
    required?: boolean;     // 是否必需
}
```

### 4. 高性能注册中心

```typescript
// 单例模式，全局唯一
SchemaRegistry.registerSchema("Inte_Unit", UNIT_SCHEMA);
const schema = SchemaRegistry.getSchema<Inte_Unit>("Inte_Unit");

// 自动缓存，避免重复解析
// 类型安全，编译时检查
```

## 使用指南

### 1. 定义 Schema

```typescript
// 1. 定义接口类型
interface MyInterface {
  name: string;
  level: number;
  power: number;
  finalPower: number; // 计算属性
}

// 2. 创建Schema定义
export const MY_SCHEMA: Schema<MyInterface> = {
  interfaceName: "MyInterface",
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
      key: "power",
      type: "number",
      defaultValue: 10,
      description: "基础力量",
    },
  ],
  computed: [
    {
      key: "finalPower",
      dependencies: ["power", "level"],
      compute: (data) => data.power * data.level,
      cache: true,
      description: "最终力量",
    },
  ],
};

// 3. 注册Schema
SchemaRegistry.registerSchema("MyInterface", MY_SCHEMA);
```

### 2. 在 Entity 中使用

```typescript
// Entity会自动使用Schema
class MyEntity extends Entity<MyInterface> {
  constructor(id: string) {
    super("MyInterface", id); // 自动加载Schema
  }
}

// 创建对象
const obj = new MyEntity("test-001");

// 类型安全的数据访问
obj.set("name", "测试对象"); // ✅ 类型正确
obj.set("level", 5); // ✅ 类型正确
obj.set("level", "5"); // ❌ 类型错误，编译时报错

// 自动计算属性
const finalPower = obj.get("finalPower"); // 自动计算 power * level
```

### 3. 数据验证

```typescript
// 自动验证（推荐方式）
obj.set("level", 150); // ❌ 超出最大值100，验证失败
obj.set("name", ""); // ❌ 违反required约束，验证失败
obj.set("level", -1); // ❌ 小于最小值1，验证失败

// 手动验证（主要用于测试和调试）
const result = SchemaRegistry.validateData("MyInterface", {
  name: "测试",
  level: 50,
  power: 20,
});

if (result.valid) {
  console.log("数据验证通过");
} else {
  console.log("验证错误:", result.errors);
}
```

**推荐做法**: 在生产环境中，直接使用 Entity 的`set()`方法，它会自动进行验证。手动调用`validateData()`主要用于开发和测试阶段。

### 4. 测试和调试

```typescript
// 运行Schema测试
SchemaTest.testSchema("MyInterface");

// 性能测试
SchemaTest.performanceTest("MyInterface", 1000);

// 生成测试报告
const report = SchemaValidator.generateTestReport();
console.log(report);
```

## 最佳实践

### 1. Schema 设计原则

- **单一职责**：每个 Schema 只定义一种对象类型
- **完整性**：包含所有必要的属性和约束
- **一致性**：命名和类型保持一致
- **可扩展性**：预留扩展空间

### 2. 计算属性设计

- **明确依赖**：准确列出所有依赖属性
- **纯函数**：计算函数应该是纯函数，无副作用
- **性能考虑**：复杂计算启用缓存
- **错误处理**：处理依赖属性缺失的情况

### 3. 约束设计

- **合理范围**：设置合理的数值范围
- **必要验证**：只添加必要的约束
- **用户友好**：提供清晰的错误信息
- **性能平衡**：避免过于复杂的验证逻辑

### 4. 性能优化

- **缓存策略**：合理使用计算属性缓存
- **批量操作**：使用批量设置减少计算次数
- **依赖优化**：减少不必要的依赖关系
- **内存管理**：及时清理无用缓存

## 迁移指南

### 从 InterfaceDefinitionManager 迁移

```typescript
// 旧方式
const manager = InterfaceManagerFactory.getManager("Inte_Unit");
const unitData = manager.createObject();
manager.setValue(unitData, "名字", "测试单位");
const name = manager.getValue(unitData, "名字");

// 新方式
const unit = EntityFactory.create<Inte_Unit>("Inte_Unit");
unit.set("名字", "测试单位");
const name = unit.get("名字");
```

### 迁移步骤

1. **创建 Schema 定义**：将原有 Definition 转换为 Schema
2. **注册 Schema**：在 SchemaRegistry 中注册
3. **更新代码**：使用 Entity 的 get/set 方法
4. **测试验证**：运行测试确保功能正常
5. **性能优化**：根据需要调整缓存策略

## 故障排除

### 常见问题

1. **Schema 未注册**

   ```
   错误：Schema not found: MyInterface
   解决：确保调用了 SchemaRegistry.registerSchema()
   ```

2. **类型不匹配**

   ```
   错误：Type mismatch for property 'level'
   解决：检查属性类型定义和赋值类型
   ```

3. **约束验证失败**

   ```
   错误：Constraint validation failed
   解决：检查属性值是否符合约束条件
   ```

4. **计算属性错误**

   ```
   错误：Computed property calculation failed
   解决：检查依赖属性是否存在，计算函数是否正确
   ```

### 调试技巧

- 使用 `SchemaTest.testSchema()`测试 Schema 定义
- 使用 `SchemaRegistry.validateData()`验证数据
- 检查浏览器控制台的详细错误信息
- 使用 `Entity.getDataSnapshot()`查看当前数据状态

## 扩展开发

### 添加新的 Schema 类型

1. 定义接口类型
2. 创建 Schema 定义文件
3. 在 SchemaRegistry 中注册
4. 创建对应的 Entity 类
5. 编写测试用例

### 自定义验证器

```typescript
// 扩展SchemaUtils添加自定义验证
SchemaUtils.addCustomValidator("email", (value: string) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(value);
});
```

### 性能监控

```typescript
// 添加性能监控
SchemaSystem.enablePerformanceMonitoring();
const stats = SchemaSystem.getPerformanceStats();
```

## 总结

Schema 系统提供了：

- ✅ **类型安全**：编译时类型检查
- ✅ **高性能**：智能缓存和批量操作
- ✅ **易维护**：清晰的数据结构定义
- ✅ **可扩展**：灵活的扩展机制
- ✅ **完整测试**：全面的测试覆盖

这是一个现代化、高效的数据管理系统，为游戏对象提供了强大的数据处理能力。
