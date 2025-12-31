# Data 数据模块

本目录存放游戏的所有静态数据配置（Resource）和相关定义。

## 目录结构

- **Resources/**: 存放具体的资源配置文件 (.tres) 和资源类定义 (.cs)
  - **Unit/**: 单位相关资源 (Player, Enemy)
- **Spawn/**: 刷怪规则配置
- **DataResourceIndex.cs**: 资源索引系统，负责资源的统一加载和管理

## 核心资源类

### UnitResource (单位资源)

所有单位（玩家、敌人）的基类。

- **VisualScene** (`PackedScene`): 单位的显示场景。
  - **约定**: 必须包含 `AnimatedSprite2D` 或其他显示对象。
  - **自动加载**: `EntityManager.Spawn` 会自动实例化此场景并挂载到 Entity 下，节点名为 `VisualRoot`。
  - **ZIndex**: 自动设置为 `10`，确保显示在顶层。
- **基础属性**: MaxHp, Speed, Damage 等。

### 扩展指南

#### 新增资源类型

1. 在 `Data/Resources` 下新建目录。
2. 创建继承自 `Resource` 的 C# 类。
3. 如果需要自动加载显示对象，请添加 `VisualScene` 属性：
   ```csharp
   [Export] public PackedScene VisualScene { get; set; }
   ```
   `EntityManager` 会通过反射自动识别并加载该场景。

#### 命名规范

- 资源类名以 `Resource` 结尾 (e.g., `WeaponResource.cs`).
- 属性使用 PascalCase.
