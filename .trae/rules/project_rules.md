# 项目规则

## 1. 项目基本信息

- **项目名称**: 复刻土豆兄弟 (Brotato-like)
- **项目类型**: 2D Rogue-like 独立游戏
- **游戏引擎**: Godot Engine 4.5
- **开发语言**: C# (.NET 8.0)
- **项目版本**: 1.0.0

## 2. 项目结构规范

### 2.1 目录结构

```
brotato-my/
├── assets/              # 游戏资源目录
│   ├── character/       # 角色资源
│   ├── environment/     # 环境资源
│   ├── items/           # 物品资源
│   ├── enemies/         # 敌人资源
│   ├── ui/              # UI资源
│   ├── audio/           # 音频资源
│   └── fonts/           # 字体资源
├── scenes/              # 场景文件目录
│   ├── entity/          # 实体场景
│   │   ├── unit/        # 单位（玩家、敌人）
│   │   ├── items/       # 物品实体
│   │   └── effects/     # 特效
│   ├── ui/              # UI场景
│   ├── levels/          # 关卡场景
│   └── test/            # 测试场景
├── scripts/             # C#脚本文件目录
│   ├── core/            # 核心系统脚本 (如 EventBus, GameState)
│   ├── entity/          # 实体脚本 (Player, Enemy)
│   ├── components/      # 组件脚本 (HealthComponent, HitboxComponent)
│   ├── resources/       # 自定义资源定义 (WeaponData, EnemyData)
│   ├── ui/              # UI脚本
│   └── utils/           # 工具脚本
├── data/                # 游戏数据目录
│   ├── config/          # 配置文件
│   ├── balance/         # 平衡数据 (Resource资源文件)
│   └── localization/    # 本地化数据
└── project.godot        # Godot项目文件
```

### 2.2 文件命名规范

- **场景文件 (.tscn)**: 使用 PascalCase，如 `Player.tscn`, `MainMenu.tscn`
- **C#脚本文件 (.cs)**: 使用 PascalCase，如 `PlayerController.cs`, `EnemySpawner.cs`
- **资源文件**: 使用 snake_case，如 `hero_guangfa.png`, `weapon_data_pistol.tres`
- **目录名称**: 使用 snake_case，如 `character_assets`, `ui_scenes`

## 3. Godot C# 引擎特定规则

### 3.1 C# 脚本编写规范

- **类定义**: 必须使用 `public partial class`，类名必须与文件名一致。
- **命名空间**: 使用 `BrotatoMy` 或相关子命名空间。
- **静态导入**: 推荐使用 `using static Godot.GD;` 以简化 `GD.Print` 等调用。
- **属性导出**: 使用 `[Export]` 特性导出属性，方便在编辑器调整。
- **类型安全**: 优先使用 `GetNode<T>("Path")` 泛型方法，避免显式类型转换。

**标准脚本模板**:

```csharp
using Godot;
using System;
using static Godot.GD;

namespace BrotatoMy
{
    public partial class MyPlayer : CharacterBody2D
    {
        [Export] public float Speed { get; set; } = 400.0f;

        // 使用属性封装逻辑
        public int Hp
        {
            get => _hp;
            set
            {
                _hp = value;
                if (_hp <= 0) Die();
            }
        }
        private int _hp;

        public override void _Ready()
        {
            // 初始化逻辑
        }

        public override void _PhysicsProcess(double delta)
        {
            // 物理逻辑
        }
    }
}
```

### 3.2 信号与事件 (Signal vs Event)

**核心原则**: 默认优先使用 **C# 原生事件 (`event Action`)**，仅在特定场景使用 Godot 信号。

- **C# 原生事件 (推荐)**:

  - **适用场景**: 纯代码逻辑交互、架构解耦、高性能需求。
  - **优点**: 编译期类型检查、性能极高、无字符串依赖。
  - **写法**: `public event Action<int> OnHealthChanged;`
  - **触发**: `OnHealthChanged?.Invoke(currentHp);`
  - **订阅**: `obj.OnHealthChanged += HandleChange;` (务必在 `_ExitTree` 或适当时机 `+=` 解绑，防止内存泄漏)。

- **Godot 信号 (`[Signal]`)**:
  - **适用场景**:
    1. 需要在编辑器 Inspector 面板进行连线。
    2. 与 GDScript 代码交互。
    3. 连接引擎内置信号 (如 `Button.Pressed`, `Area2D.BodyEntered`)。
  - **注意**: 避免使用字符串连接信号，使用 C# 事件风格语法 `button.Pressed += OnPressed;`。

### 3.3 数据驱动与存档

- **静态配置数据 (Resource)**:

  - 使用继承自 `Resource` 的类存储游戏配置（如武器属性、敌人数值）。
  - 必须添加 `[GlobalClass]` 特性以便在编辑器中创建。
  - 文件扩展名为 `.tres`。
  - 示例: `WeaponData.cs` 定义结构，编辑器中创建 `pistol.tres` 配置数值。

- **动态存档数据 (JSON)**:
  - 使用纯 C# 类 (POCO) 定义存档结构。
  - 使用 `System.Text.Json` 进行序列化/反序列化。
  - 不要尝试序列化 Godot 节点或 Resource 为存档，仅保存数据状态。

### 3.4 常用数据结构选择

- **List<T>**: 通用列表，但查找慢。
- **Dictionary<TKey, TValue>**: 需要按 ID 快速查找物品/技能时使用 (O(1) 复杂度)。
- **HashSet<T>**: 需要快速判断“是否存在”或去重时使用 (如已解锁成就列表)。
- **Queue<T>**: 先进先出 (对话系统、输入缓冲)。
- **Stack<T>**: 后进先出 (UI 窗口层级管理)。

## 4. 资源使用规则

### 4.1 解包游戏资源使用

- **解包游戏路径**: `E:\Godot\Games\steam解包游戏\土豆兄弟`
- **资源引用方式**:
  1. 仅允许引用解包资源作为参考和学习
  2. 禁止直接复制解包资源到项目中
  3. 所有最终使用的资源必须进行修改或重新创建，确保不侵犯原游戏版权
  4. 解包资源仅用于：
     - 了解游戏机制和设计思路
     - 参考美术风格和动画效果
     - 分析游戏平衡数据

### 4.2 资源创建规范

- 美术资源：优先使用原创或开源资源
- 音频资源：使用免费或购买的版权音乐和音效
- UI 资源：保持与游戏风格一致的设计

## 5. 代码规范与架构

### 5.1 架构模式

- **组件化 (Component-based)**:

  - 优先使用组合而非继承。
  - 功能封装为独立 Node 组件 (如 `HealthComponent`, `VelocityComponent`)。
  - 实体 (Player/Enemy) 仅作为容器组装组件。

- **有限状态机 (FSM)**:

  - 复杂逻辑 (如玩家/Boss 行为) 必须使用状态机。
  - **推荐实现**: 使用纯 C# 类实现 `IState` 接口，而非为每个状态创建一个 Node。
  - 状态类不继承 Node，由宿主对象在 `_Process` 中驱动 `CurrentState.Update()`。

- **事件总线 (Event Bus)**:

  - 使用 **静态类 + 静态 C# 事件** 实现全局解耦。
  - 示例: `public static class EventBus { public static event Action OnPlayerDied; }`
  - 注意: 静态事件必须在接收者销毁时 (`_ExitTree`) 手动解绑，否则会导致严重内存泄漏。

- **对象池 (Object Pooling)**:
  - 高频生成的对象 (子弹、伤害数字、特效) **必须** 使用对象池。
  - 禁止在战斗中频繁 `Instantiate` 和 `QueueFree`。
  - 使用 `Hide()`/`Show()` 和重置状态代替销毁/创建。

### 5.2 性能优化 (GC 意识)

C# 是托管语言，垃圾回收 (GC) 会导致游戏卡顿。

- **热路径禁忌 (`_Process` / `_PhysicsProcess`)**:
  - **禁止** `new` 引用类型对象 (Class, List, Array, Delegate)。
  - **禁止** 字符串拼接 (`"Score: " + score`)，使用 `StringBuilder` 或仅在变化时更新。
  - **禁止** 滥用 LINQ (`Where`, `Select`)，这会产生大量临时垃圾。
- **结构体 (Struct)**:
  - Godot 的 `Vector2`, `Rect2`, `Color` 是结构体，在栈上分配，零 GC，可放心使用。
- **集合复用**:
  - 尽量作为成员变量缓存 List，使用 `Clear()` 复用，而不是每帧 `new List()`。

## 6. 版本控制规则

### 6.1 Git 提交规范

- 提交信息使用中文，格式：`[类型] 描述`
- 类型包括：`feat`（新功能）、`fix`（修复）、`refactor`（重构）、`docs`（文档）、`style`（格式）、`test`（测试）
- C# 项目必须确保无编译错误和警告

### 6.2 忽略文件

- 忽略 Godot 临时文件 (`.godot/`)
- 忽略 C# 编译产物 (`bin/`, `obj/`)
- 忽略 IDE 配置 (`.vs/`, `.idea/`)

## 7. 调试与日志

- 使用 `GD.Print()` 进行普通日志。
- 使用 `GD.PushWarning()` 和 `GD.PushError()` 突出重要问题。
- 异常处理: 关键逻辑使用 `try-catch`，避免游戏直接崩溃。

## 8. 学习资源

- **Docs/C Sharp**: 项目内含的 Godot C# 本地知识库 (强烈推荐阅读)。
- **Godot 官方文档**: https://docs.godotengine.org/zh-cn/4.5/tutorials/scripting/c_sharp/index.html
