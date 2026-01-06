# ECS 统一测试场景

本目录下包含用于测试和演示 Entity, Data, Timer, ObjectPool 四大核心系统功能的测试场景。

## 文件说明

- `ECSTestScene.tscn`: 测试主场景。
- `ECSTest.cs`: 测试脚本，包含测试逻辑。
- `Entity/TestEntity.cs`: 用于测试的实体类。
- `System/TestComponent.cs`: 用于测试的组件类。

## 如何运行测试

1. 打开 `Src/Test/ECS/ECSTest/ECSTestScene.tscn` 场景。
2. 运行场景 (F6)。
3. 点击界面上的 "Run Tests" 按钮（如果有）或观察输出日志。
4. 控制台将输出 `[PASS]` 或 `[FAIL]` 信息。

## 测试覆盖

1. **ObjectPool**: 测试创建池、获取对象、归还对象、对象重置。
2. **Data**: 测试设置值、获取值、添加/移除修改器、数据运算。
3. **Timer**: 测试定时器创建、取消、回调。
4. **Entity**: 测试实体注册、组件添加、组件获取、数据自动注入。

## 注意事项

- 此测试场景不依赖游戏核心配置，使用本地定义的临时配置。
- 测试过程中的报错会在控制台显示详细堆栈。
