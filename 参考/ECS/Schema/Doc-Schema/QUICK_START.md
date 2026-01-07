# Schema 系统快速入门

## 5 分钟上手 Schema 系统

### 第 1 步：了解基本概念

Schema 系统是 Entity 的数据管理核心，提供：

- 📋 **数据结构定义**：定义对象有哪些属性
- 🔍 **类型安全**：编译时检查数据类型
- ⚡ **计算属性**：自动计算派生属性
- ✅ **数据验证**：确保数据符合规则

### 第 2 步：查看现有示例

```typescript
// 查看单位Schema定义
import { UNIT_SCHEMA } from "./definitions/UnitSchema";

console.log("单位Schema包含的属性:");
UNIT_SCHEMA.properties.forEach((prop) => {
  console.log(`- ${prop.key}: ${prop.type} (默认: ${prop.defaultValue})`);
});
```

### 第 3 步：创建你的第一个 Schema

```typescript
// 1. 定义接口
interface Inte_Weapon {
  名称: string;
  攻击力: number;
  攻击速度: number;
  耐久度: number;
  最大耐久度: number;
  DPS: number; // 计算属性
}

// 2. 创建Schema
export const WEAPON_SCHEMA: Schema<Inte_Weapon> = {
  interfaceName: "Inte_Weapon",
  properties: [
    {
      key: "名称",
      type: "string",
      defaultValue: "未命名武器",
      description: "武器名称",
    },
    {
      key: "攻击力",
      type: "number",
      defaultValue: 10,
      description: "基础攻击力",
      constraints: { min: 1, max: 999 },
    },
    {
      key: "攻击速度",
      type: "number",
      defaultValue: 1.0,
      description: "每秒攻击次数",
      constraints: { min: 0.1, max: 10.0 },
    },
    {
      key: "耐久度",
      type: "number",
      defaultValue: 100,
      description: "当前耐久度",
      constraints: { min: 0 },
    },
    {
      key: "最大耐久度",
      type: "number",
      defaultValue: 100,
      description: "最大耐久度",
      constraints: { min: 1 },
    },
  ],
  computed: [
    {
      key: "DPS",
      dependencies: ["攻击力", "攻击速度"],
      compute: (data) => (data.攻击力 || 0) * (data.攻击速度 || 1),
      cache: true,
      description: "每秒伤害输出",
    },
  ],
};

// 3. 注册Schema
SchemaRegistry.registerSchema("Inte_Weapon", WEAPON_SCHEMA);
```

### 第 4 步：使用 Schema 创建 Entity

```typescript
// 创建武器Entity类
class WeaponEntity extends Entity<Inte_Weapon> {
  constructor(id: string) {
    super("Inte_Weapon", id);
  }

  // 添加业务方法
  repair(amount: number): void {
    const current = this.get("耐久度");
    const max = this.get("最大耐久度");
    this.set("耐久度", Math.min(current + amount, max));
  }

  takeDamage(damage: number): void {
    const current = this.get("耐久度");
    this.set("耐久度", Math.max(current - damage, 0));
  }
}

// 使用管理器注册和创建对象
const manager = EntityManager.getInstance();
manager.registerType("Inte_Weapon", WeaponEntity);
```

### 第 5 步：实际使用

```typescript
// 创建武器对象
const sword = manager.create<Inte_Weapon>("Inte_Weapon", "excalibur");

// 设置属性
sword.set("名称", "王者之剑");
sword.set("攻击力", 50);
sword.set("攻击速度", 1.5);

// 获取属性（包括计算属性）
console.log(`武器名称: ${sword.get("名称")}`);
console.log(`攻击力: ${sword.get("攻击力")}`);
console.log(`DPS: ${sword.get("DPS")}`); // 自动计算：50 * 1.5 = 75

// 批量设置
sword.setMultiple({
  攻击力: 60,
  攻击速度: 2.0,
  耐久度: 80,
});

// 验证数据（自动验证）
const isValid = sword.set("攻击力", -10); // false，违反min约束
console.log(`设置负攻击力: ${isValid}`);

// 手动验证（用于测试）
const validationResult = SchemaRegistry.validateData("Inte_Weapon", {
  名称: "测试武器",
  攻击力: 25,
  攻击速度: 1.2,
});
console.log("手动验证结果:", validationResult.valid);
```

## 常用模式

### 模式 1：枚举约束

```typescript
{
    key: '品质',
    type: 'string',
    defaultValue: '普通',
    constraints: {
        enum: ['普通', '精良', '稀有', '史诗', '传说']
    }
}
```

### 模式 2：复杂计算属性

```typescript
{
    key: '综合评分',
    dependencies: ['攻击力', '攻击速度', '耐久度', '品质'],
    compute: (data) => {
        const base = (data.攻击力 || 0) * (data.攻击速度 || 1);
        const durability = (data.耐久度 || 0) / 100;
        const qualityMultiplier = {
            '普通': 1.0,
            '精良': 1.2,
            '稀有': 1.5,
            '史诗': 2.0,
            '传说': 3.0
        }[data.品质 || '普通'] || 1.0;

        return Math.floor(base * durability * qualityMultiplier);
    },
    cache: true
}
```

### 模式 3：条件验证

```typescript
{
    key: '耐久度',
    type: 'number',
    defaultValue: 100,
    constraints: {
        min: 0,
        // 自定义验证：耐久度不能超过最大耐久度
        custom: (value, data) => {
            const max = data.最大耐久度 || 100;
            return value <= max;
        }
    }
}
```

### 模式 4：属性监听

```typescript
// 监听属性变更
sword.onPropertyChanged("耐久度", (oldValue, newValue) => {
  if (newValue <= 0) {
    console.log("武器已损坏！");
  } else if (newValue < oldValue * 0.2) {
    console.log("武器耐久度过低，建议修理");
  }
});
```

## 调试技巧

### 1. 查看 Schema 信息

```typescript
// 获取Schema
const schema = SchemaRegistry.getSchema("Inte_Weapon");
console.log("Schema信息:", schema);

// 查看所有属性
const keys = sword.getAllPropertyKeys();
console.log("所有属性:", keys);
```

### 2. 数据快照

```typescript
// 获取当前数据快照
const snapshot = sword.getDataSnapshot();
console.log("当前数据:", snapshot);
```

### 3. 验证测试

```typescript
// 测试Schema定义
SchemaTest.testSchema("Inte_Weapon");

// 验证特定数据（用于测试）
const result = SchemaRegistry.validateData("Inte_Weapon", {
  名称: "测试武器",
  攻击力: 25,
  攻击速度: 1.2,
});

console.log("验证结果:", result);

// 生成测试报告（用于调试）
const report = SchemaRegistry.generateTestReport();
console.log("系统报告长度:", report.length);
```

## 性能提示

### 1. 合理使用缓存

```typescript
// 频繁访问的计算属性启用缓存
{
    key: '复杂计算属性',
    dependencies: ['多个', '依赖', '属性'],
    compute: (data) => {
        // 复杂计算逻辑
        return expensiveCalculation(data);
    },
    cache: true // 启用缓存
}
```

### 2. 批量操作

```typescript
// 推荐：批量设置
sword.setMultiple({
  攻击力: 60,
  攻击速度: 2.0,
  耐久度: 80,
});

// 避免：逐个设置
sword.set("攻击力", 60);
sword.set("攻击速度", 2.0);
sword.set("耐久度", 80);
```

### 3. 减少依赖

```typescript
// 好：最小依赖
{
    key: 'DPS',
    dependencies: ['攻击力', '攻击速度'], // 只依赖必要属性
    compute: (data) => data.攻击力 * data.攻击速度
}

// 避免：过多依赖
{
    key: 'DPS',
    dependencies: ['攻击力', '攻击速度', '名称', '品质'], // 不必要的依赖
    compute: (data) => data.攻击力 * data.攻击速度
}
```

## 下一步

1. 📖 阅读完整的[README.md](./README.md)文档
2. 🔍 查看[UnitSchema.ts](./definitions/UnitSchema.ts)的完整示例
3. 🧪 运行[test-schema.ts](./test-schema.ts)进行测试
4. 🚀 开始创建你自己的 Schema 定义

## 常见问题

**Q: 如何添加新的数据类型？**
A: 在 Schema.ts 中扩展 PropertyType 类型定义。

**Q: 计算属性什么时候重新计算？**
A: 当任何依赖属性发生变更时，计算属性的缓存会自动失效并重新计算。

**Q: 如何处理循环依赖？**
A: Schema 系统会检测循环依赖并抛出错误，需要重新设计依赖关系。

**Q: 可以动态修改 Schema 吗？**
A: 不建议在运行时修改 Schema，应该在初始化时定义好所有 Schema。

开始你的 Schema 之旅吧！🚀
