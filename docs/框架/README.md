# Brotato 复刻项目 - 框架文档索引

## 📚 文档列表

### 1. [项目框架.md](./项目框架.md) - 主文档

**核心内容**:

- 架构概览（伪 ECS 混合架构）
- 已完成模块（Log, ObjectPool, Data, NodeExtensions）
- 待实现系统（Component, Resource, Event, Manager, System）
- 目录结构规划
- 设计模式与最佳实践
- 开发路线图

**适用人群**: 所有开发者

---

### 2. [事件系统更新说明.md](./事件系统更新说明.md) - 重要更新

**核心内容**:

- 从 Godot Signal 迁移到 C# Event 的原因
- 性能对比（7-9 倍提升）
- 完整的迁移指南
- 代码模板和示例
- 注意事项（内存泄漏、解绑）

**适用人群**: 所有开发者（必读）

---

## 🎯 快速导航

### 我想了解...

#### 架构设计

→ 阅读 [项目框架.md - 第 1 章：架构概览](./项目框架.md#1-架构概览)

#### 已完成的模块

→ 阅读 [项目框架.md - 第 2 章：已完成模块](./项目框架.md#2-已完成模块)

#### 如何使用事件系统

→ 阅读 [事件系统更新说明.md](./事件系统更新说明.md)

#### 组件如何通信

→ 阅读 [项目框架.md - 5.2 组件通信模式](./项目框架.md#52-组件通信模式)

#### 对象池如何使用

→ 阅读 [项目框架.md - 2.2 ObjectPool](./项目框架.md#-22-objectpool-对象池)

#### 开发路线图

→ 阅读 [项目框架.md - 第 6 章：开发路线图](./项目框架.md#6-开发路线图)

---

## ⚡ 核心原则速查

### 事件系统

```
✅ 优先使用 C# Event（性能提升7-9倍）
❌ 避免使用 Godot Signal（除非必要）
⚠️ 必须在 _ExitTree() 中解绑事件
```

### 组件通信

```
1. C# Event（组件间通信）⭐⭐⭐⭐⭐
2. 直接调用（父子关系）⭐⭐⭐⭐
3. 全局事件总线（跨场景）⭐⭐⭐⭐
4. Godot Signal（仅编辑器连线）⭐⭐
```

### 数据管理

```
Resource: 静态配置（设计时确定）
Data: 动态数据（运行时变化）
```

### 性能优化

```
✅ 必须使用对象池（子弹、敌人、掉落物）
✅ 缓存 GetNode 引用
✅ 使用 Data 而非 Metadata
```

---

## 📊 项目状态

### 已完成 ✅

- [x] Log（日志系统）
- [x] ObjectPool（对象池）
- [x] Data（动态数据容器）
- [x] NodeExtensions（节点扩展）
- [x] InputManager（输入管理器）
- [x] AutoLoad（全局引导器）
- [x] AttributeComponent（属性组件）
- [x] VelocityComponent（移动组件）

### 进行中 🔄

- [ ] Component System（剩余组件）

### 待实现 🔲

- [ ] Component System（Health, Hitbox, Hurtbox, Follow, Pickup）
- [ ] Resource System（4 个数据类）
- [ ] Event System（全局事件总线）
- [ ] Manager System（4 个管理器）
- [ ] System Layer（2 个系统）

---

## 🔗 相关资源

### 官方文档

- [Godot C# 文档](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)
- [C# Events (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/)

### 项目文档

- [C# 学习笔记](../C%20Sharp/)
- [Godot 开发速查](../C%20Sharp/Godot/)

---

## 📝 更新日志

### 2024-12-21

- ✅ 完成项目框架主文档
- ✅ 完成事件系统更新说明
- ✅ 从 Godot Signal 迁移到 C# Event
- ✅ 更新组件通信模式
- ✅ 添加性能对比数据

---

## 💡 贡献指南

### 文档更新流程

1. 修改对应的 .md 文件
2. 更新本 README.md 的更新日志
3. 提交 commit

### 文档规范

- 使用中文编写
- 代码示例使用 C#
- 标注优先级（⭐）
- 区分推荐/不推荐（✅/❌）

---

**维护者**: 项目团队  
**最后更新**: 2024-12-21
