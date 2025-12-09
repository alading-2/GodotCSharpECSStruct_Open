## Logger 设计文档

**架构**：组件化 Logger + 全局 LogManager + 完整 UI 面板

---

## 1. 架构设计

### 1.1 三层架构

```
LoggerUI (场景)
  ↑ 读取日志数据
LogManager (全局单例)
  ↑ 收集日志
Logger (组件场景) × N
```

### 1.2 为什么是组件化？

| 方案                  | 优点                                                               | 缺点                                 |
| --------------------- | ------------------------------------------------------------------ | ------------------------------------ |
| **组件化（✅ 当前）** | 自动获取父节点名称、支持实例区分、Inspector 配置、生命周期自动管理 | 需要拖拽场景                         |
| 全局单例              | 代码简洁                                                           | 需手动传 tag、无法区分实例、难以管理 |

### 1.3 实例区分策略

**问题**：Enemy.tscn 被实例化 100 次，如何区分？

**解决**：`源文件名 + instance_id + custom_tag`

```gdscript
# Logger 内部
var _owner_name: String = ""        # 显示名称，如 "Enemy" 或 "Boss"
var _source_file: String = ""       # 源文件，如 "enemy.gd"
var _instance_id: int = 0           # Godot 实例 ID

func _ready():
    var parent = get_parent()
    _source_file = parent.get_script().resource_path.get_file() if parent.get_script() else "Unknown"
    _instance_id = parent.get_instance_id()
    _owner_name = custom_tag if not custom_tag.is_empty() else parent.name

    LogManager.register_logger(self)  # 注册到全局管理器

func _exit_tree():
    LogManager.unregister_logger(self)  # 注销
```

---

## 2. 日志存储策略

### 2.1 混合存储

| 存储方式     | 用途             | 特点                                |
| ------------ | ---------------- | ----------------------------------- |
| **内存字典** | UI 实时显示      | 有上限（500 条/实例），超出自动清理 |
| **本地文件** | 持久化、崩溃分析 | 按日期滚动，保留 7 天               |

### 2.2 数据结构

```gdscript
# LogManager
var _logs_by_source: Dictionary = {}  # { "enemy.gd": { 12345: [LogEntry, ...] } }
var _all_logs: Array[LogEntry] = []   # 时间线视图
var _registered_loggers: Dictionary = {}  # { "enemy.gd": { 12345: Logger } }

class LogEntry:
    var timestamp: String
    var level: int
    var source_file: String
    var instance_id: int
    var display_name: String
    var message: String
    var data: Variant
```

---

## 3. LoggerUI 设计

### 3.1 布局（类似 VSCode）

```
┌─────────────────────────────────────────────────────────────┐
│ [筛选: ▼全部] [等级: ▼DEBUG+] [🔍 搜索...]    [清空] [关闭] │
├──────────────────┬──────────────────────────────────────────┤
│ 📁 源文件列表     │  日志内容区                               │
│ ├─ 🟢 player.gd  │  [12:30:15][INF][Player] 初始化完成       │
│ ├─ 🔴 enemy.gd   │  [12:30:16][DBG][Enemy#101] 敌人生成      │
│ │  ├─ #101 (10) │  [12:30:17][ERR][Enemy#101] 路径计算失败  │
│ │  ├─ #102 (8)  │  ...                                      │
│ │  └─ #103 (7)  │                                           │
│ └─ 🟢 ui.gd      │                                           │
├──────────────────┴──────────────────────────────────────────┤
│ 共 33 条 | 显示 33 条 | 内存: 1.2MB | 活跃实例: 5            │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 功能需求

**左侧：源文件树（Tree）**

- 按源文件分组，展开显示实例
- 日志计数，颜色标记（🔴ERROR/🟡WARN/🟢 正常）
- 点击文件显示所有实例日志，点击实例显示单个日志
- 已销毁实例单独分组

**右侧：日志内容区（RichTextLabel）**

- BBCode 彩色显示
- 自动滚动（可关闭）
- 点击展开详情（data、stack_trace）

**顶部：筛选工具栏**

- 源文件筛选、等级筛选（TRACE+/DEBUG+/INFO+/WARN+/ERROR）
- 搜索框（支持正则）
- 清空按钮

**唤出方式**：F12 快捷键（仅 `OS.is_debug_build()`）

---

## 4. 文件结构

```
scenes/tools/logger/
├── logger.gd              # Logger 组件
├── logger.tscn            # Logger 场景
├── log_manager.gd         # LogManager 全局单例
├── log_entry.gd           # LogEntry 数据类
├── log_writer.gd          # LogWriter 文件写入器
└── logger_ui/
    ├── logger_ui.gd       # UI 主脚本
    ├── logger_ui.tscn     # UI 场景
    ├── source_tree.gd     # 源文件树
    └── log_content.gd     # 日志内容区
```

---

## 5. 日志等级

| 等级  | 标签 | 颜色 | 输出方式       |
| ----- | ---- | ---- | -------------- |
| TRACE | TRC  | 灰色 | print_rich()   |
| DEBUG | DBG  | 青色 | print_rich()   |
| INFO  | INF  | 绿色 | print_rich()   |
| WARN  | WRN  | 黄色 | push_warning() |
| ERROR | ERR  | 红色 | push_error()   |

---

## 6. 配置项

### 6.1 Logger 组件（Inspector）

```gdscript
@export var enable_logging: bool = true
@export var min_level: Level = Level.DEBUG
@export var custom_tag: String = ""  # 自定义标签，如 "Boss"
```

### 6.2 LogManager 全局配置

```gdscript
@export var enable_file_logging: bool = true
@export var log_file_path: String = "user://logs/"
@export var max_logs_per_instance: int = 500
@export var max_total_logs: int = 5000
@export var enable_ui_panel: bool = true
@export var ui_panel_hotkey: Key = KEY_F12
```

---

## 7. API 设计

### 7.1 Logger 组件

```gdscript
func log_trace(message: String, data: Variant = null)
func log_debug(message: String, data: Variant = null)
func log_info(message: String, data: Variant = null)
func log_warn(message: String, data: Variant = null)
func log_error(message: String, data: Variant = null)

func set_enabled(enabled: bool)
func set_min_level(level: Level)
```

### 7.2 LogManager

```gdscript
func register_logger(logger: Logger)
func unregister_logger(logger: Logger)
func add_log(entry: LogEntry)

func get_all_logs() -> Array[LogEntry]
func get_logs_by_source(source_file: String) -> Array[LogEntry]
func get_logs_by_instance(source_file: String, instance_id: int) -> Array[LogEntry]

func show_ui()
func hide_ui()
func toggle_ui()

signal log_added(entry: LogEntry)
signal logger_registered(logger: Logger)
signal logger_unregistered(source_file: String, instance_id: int)
```

---

## 8. 使用示例

### 8.1 基础使用

```gdscript
# enemy.gd
@onready var logger: Logger = $Logger

func _ready():
    logger.log_info("敌人初始化", {"hp": hp})

func take_damage(amount: int):
    logger.log_debug("受到伤害", {"amount": amount})
    if hp <= 0:
        logger.log_info("敌人死亡")
```

### 8.2 自定义标签

```gdscript
# boss.gd
# 在 Inspector 中设置 Logger 的 custom_tag = "Boss"
@onready var logger: Logger = $Logger

func _ready():
    logger.log_info("Boss 登场")  # 输出: [INF][Boss] Boss 登场
```

---

## 9. 性能优化

- 日志方法内部先检查 `enable_logging` 和 `min_level`，不满足直接返回
- 避免在 `_process()` 中每帧打印日志
- 内存有上限，超出自动清理最旧日志
- 文件写入通过 LogWriter 统一管理，避免频繁 I/O

---

## 10. 发布配置

| 环境         | 控制台      | 文件      | UI 面板 | 等级   |
| ------------ | ----------- | --------- | ------- | ------ |
| **编辑器**   | ✅ 主要     | ⚠️ 可选   | ❌ 多余 | ALL    |
| **打包自测** | ⚠️ 需命令行 | ✅ 必须   | ✅ F12  | DEBUG+ |
| **正式发布** | ⚠️ 技术玩家 | ✅ 仅错误 | ❌ 禁用 | WARN+  |

**正式发布时**：

- 设置 `min_level = Level.WARN`
- 设置 `enable_ui_panel = false`
- 或通过导出预设完全禁用

---

## 11. 核心原则

1. **混合模式**：DEBUG/INFO 用 `print_rich()`，WARN 用 `push_warning()`，ERROR 用 `push_error()`
2. **组件化**：每个节点一个 Logger，自动获取父节点名称
3. **实例区分**：通过 `instance_id` 区分同一场景的多个实例
4. **UI 管理**：LogManager 收集所有日志，LoggerUI 统一显示
5. **性能优先**：内部短路返回，避免不必要的字符串操作
