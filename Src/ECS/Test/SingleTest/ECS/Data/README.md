# Data 系统测试场景

## 概述

这是 Data 系统的独立测试场景,用于验证 Data 容器的各项功能是否正常工作。

## 测试内容

### 1. 基础类型测试
- ✅ 字符串类型 (`string`)
- ✅ 整数类型 (`int`)
- ✅ 浮点数类型 (`float`)
- ✅ 布尔类型 (`bool`)

### 2. 数值范围约束
- ✅ 最小值约束 (`MinValue`)
- ✅ 最大值约束 (`MaxValue`)
- ✅ 范围约束 (`MinValue` + `MaxValue`)

### 3. 默认值处理
- ✅ 类型默认值
- ✅ 元数据默认值
- ✅ 未注册数据的默认值

### 4. 百分比显示
- ✅ 百分比标记 (`IsPercentage`)
- ✅ 百分比格式化 (`FormatValue`)

### 5. 选项约束
- ✅ 固定选项列表 (`Options`)
- ✅ 选项名称获取 (`GetOptionName`)
- ✅ 选项验证 (`IsValidOption`)

### 6. 计算属性
- ✅ 简单计算 (加法、乘法)
- ✅ 复杂计算 (组合运算)
- ✅ 依赖追踪和自动更新
- ✅ 缓存机制

### 7. 修改器系统
- ✅ 加法修改器 (`Additive`)
- ✅ 乘法修改器 (`Multiplicative`)
- ✅ 修改器优先级
- ✅ 修改器添加/移除
- ✅ 计算公式: `(基础值 + Σ加法) × Π乘法`

### 8. 事件监听
- ✅ 数据变更检测
- ✅ 值变化验证

## 文件结构

```
Src/Test/ECS/Data/
├── DataTestScene.cs          # 测试场景脚本
├── DataTestScene.tscn        # Godot 场景文件
├── TestCategory.cs           # 测试数据分类枚举
├── DataKey_Test.cs           # 测试数据键定义
├── TestDataRegister.cs       # 测试数据注册器
└── README.md                 # 本文档
```

## 如何运行

### 方法 1: 通过 Godot 编辑器
1. 打开 Godot 编辑器
2. 在文件系统中找到 `Src/Test/ECS/Data/DataTestScene.tscn`
3. 双击打开场景
4. 点击运行当前场景 (F6)

### 方法 2: 设置为主场景
1. 在项目设置中将 `DataTestScene.tscn` 设置为主场景
2. 运行项目 (F5)

## 测试数据定义

### 基础类型数据
- `DataKey.TestString` - 字符串测试
- `DataKey.TestInt` - 整数测试
- `DataKey.TestFloat` - 浮点数测试
- `DataKey.TestBool` - 布尔值测试

### 数值范围数据
- `DataKey.TestMinValue` - 最小值约束 (>= 10)
- `DataKey.TestMaxValue` - 最大值约束 (<= 100)
- `DataKey.TestRange` - 范围约束 (0-1)

### 计算属性数据
- `DataKey.TestBaseA` - 计算基础值 A
- `DataKey.TestBaseB` - 计算基础值 B
- `DataKey.TestComputedAdd` - 加法计算 (A + B)
- `DataKey.TestComputedMultiply` - 乘法计算 (A * B)
- `DataKey.TestComputedComplex` - 复杂计算 ((A + B) * 2)

### 修改器数据
- `DataKey.TestModifierBase` - 修改器基础值

## 测试结果

运行场景后,UI 界面会显示:
- 测试总数
- 通过的测试数
- 失败的测试数
- 每个测试的详细结果

所有测试应该显示为 **绿色 ✓** 表示通过。

## 注意事项

1. **AutoLoad 注册**:  
   `TestDataRegister.cs` 使用 `[ModuleInitializer]` 自动注册到 AutoLoad 系统。
   
2. **数据注册时机**:  
   测试数据在 `TestDataRegister._Ready()` 时注册,确保在测试场景运行前已完成注册。
   
3. **事件监听限制**:  
   由于测试 Data 没有关联 Entity,事件监听功能只能测试基础的变更检测。

## 扩展测试

如需添加新的测试:

1. 在 `DataKey_Test.cs` 中添加新的数据键
2. 在 `TestDataRegister.cs` 中注册对应的 `DataMeta`
3. 在 `DataTestScene.cs` 中添加测试方法
4. 在 `RunAllTests()` 中调用新的测试方法

## 相关文档

- [Data 系统文档](../../../ECS/Data/README.md)
- [DataMeta 元数据](../../../ECS/Data/DataMeta.cs)
- [DataRegistry 注册表](../../../ECS/Data/DataRegistry.cs)
- [Data 容器](../../../ECS/Data/Data.cs)
