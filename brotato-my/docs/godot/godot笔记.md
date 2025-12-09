# Godot 开发笔记

## 注解（Annotation）vs 元数据（Metadata）

### 核心区别

很多人会混淆 GDScript 的 **注解（Annotation）** 和 **元数据（Metadata）**，它们是完全不同的概念：

| 特性         | 注解（Annotation）`@`          | 元数据（Metadata）`set_meta()` |
| ------------ | ------------------------------ | ------------------------------ |
| **语法**     | `@export`, `@onready`, `@tool` | `node.set_meta("key", value)`  |
| **作用时机** | **编译时**                     | **运行时**                     |
| **用途**     | 修改代码行为、编辑器功能       | 存储运行时数据                 |
| **可见性**   | 编辑器可见                     | 仅代码可访问                   |
| **性能**     | 无运行时开销                   | 有内存开销                     |

### 1. 注解（Annotation）- 以 `@` 开头

**定义**：注解是 GDScript 的 **编译时指令**，用于修改代码的行为或提供编辑器功能。

#### Godot 内置注解

```gdscript
# 导出变量到 Inspector
@export var health: int = 100

# 延迟初始化（节点准备好后）
@onready var sprite = $Sprite2D

# 标记为工具脚本（编辑器中运行）
@tool
extends Node

# 警告控制
@warning_ignore("unused_variable")
var unused_var = 0

# 图标设置
@icon("res://icon.svg")
class_name MyClass
```

#### 自定义注解（用于文档或工具）

**重要**：GDScript **不支持自定义注解**！

```gdscript
# ❌ 错误：这只是注释，不是注解
## @autoload
## @autoload_priority 100
extends Node

# 这些 @autoload 只是普通注释，Godot 不会识别
# 需要自己写代码解析这些注释
```

在我们的 `autoload_config_auto.gd.example` 中：

```gdscript
# 这不是真正的注解，只是注释
## @autoload
## @autoload_priority 100

# 需要手动解析注释内容
static func _parse_script_metadata(script_path: String) -> SingletonConfig:
    var file = FileAccess.open(script_path, FileAccess.READ)
    var content = file.get_as_text()

    # 手动搜索 "@autoload" 字符串
    if not content.contains("@autoload"):
        return null

    # 手动解析优先级
    var priority_regex = RegEx.new()
    priority_regex.compile(r"@autoload_priority\s+(\d+)")
    # ...
```

**结论**：我们使用的 `@autoload` 只是 **约定的注释格式**，不是真正的注解。

### 2. 元数据（Metadata）- `set_meta()` / `get_meta()`

**定义**：元数据是 **运行时** 附加到对象上的键值对数据。

#### 基本用法

```gdscript
# 设置元数据
node.set_meta("object_pool", pool_instance)
node.set_meta("spawn_time", Time.get_ticks_msec())
node.set_meta("enemy_type", "zombie")

# 获取元数据
var pool = node.get_meta("object_pool")
var spawn_time = node.get_meta("spawn_time", 0)  # 默认值

# 检查是否存在
if node.has_meta("object_pool"):
    var pool = node.get_meta("object_pool")

# 删除元数据
node.remove_meta("object_pool")

# 获取所有元数据键
var keys = node.get_meta_list()
```

#### 实际应用场景

**场景 1：对象池引用**

```gdscript
# 对象池管理器
class ObjectPool:
    func spawn(scene: PackedScene) -> Node:
        var instance = scene.instantiate()
        instance.set_meta("_pool", self)  # 记住所属对象池
        return instance

    func return_instance(node: Node) -> void:
        # 回收逻辑
        pass

# 实体脚本
extends Node2D

func die():
    # 自动回收到对象池
    if has_meta("_pool"):
        var pool = get_meta("_pool")
        pool.return_instance(self)
```

**场景 2：临时标记**

```gdscript
# 标记敌人已被攻击
func on_hit(enemy: Node):
    if not enemy.has_meta("_hit_this_frame"):
        enemy.set_meta("_hit_this_frame", true)
        apply_damage(enemy)

func _process(delta):
    # 每帧清除标记
    for enemy in enemies:
        enemy.remove_meta("_hit_this_frame")
```

**场景 3：调试信息**

```gdscript
# 记录创建信息
func spawn_enemy(type: String):
    var enemy = enemy_scene.instantiate()
    enemy.set_meta("_debug_spawn_time", Time.get_ticks_msec())
    enemy.set_meta("_debug_spawn_type", type)
    enemy.set_meta("_debug_spawn_wave", current_wave)
    add_child(enemy)

# 调试时查看
func debug_print_enemy_info(enemy: Node):
    print("敌人信息:")
    print("  类型: ", enemy.get_meta("_debug_spawn_type", "unknown"))
    print("  波次: ", enemy.get_meta("_debug_spawn_wave", 0))
    print("  存活时间: ", Time.get_ticks_msec() - enemy.get_meta("_debug_spawn_time", 0))
```

### 3. 对比总结

#### 注解（Annotation）

```gdscript
# ✅ 编译时生效
@export var speed: float = 100.0

# ✅ 编辑器可见
@onready var sprite = $Sprite2D

# ❌ 不能自定义
@my_custom_annotation  # 语法错误！
```

#### 元数据（Metadata）

```gdscript
# ✅ 运行时设置
node.set_meta("speed", 100.0)

# ✅ 完全自定义
node.set_meta("any_key_you_want", any_value)

# ❌ 编辑器不可见
# ❌ 有内存开销
```

#### 我们的 `@autoload` 方案

```gdscript
# 这只是注释，不是注解
## @autoload
## @autoload_priority 100

# 需要手动解析：
# 1. 读取文件内容
# 2. 用正则表达式搜索 "@autoload"
# 3. 提取优先级数字
# 4. 手动创建配置

# 这就是为什么推荐用配置文件：
singletons.append(SingletonConfig.new("Manager", "path", 100))
# 更清晰、更可靠、无需解析
```

### 4. 最佳实践

#### 何时用注解

- ✅ 导出变量到 Inspector：`@export`
- ✅ 延迟初始化节点：`@onready`
- ✅ 工具脚本：`@tool`
- ✅ 警告控制：`@warning_ignore`

#### 何时用元数据

- ✅ 对象池引用（避免循环引用）
- ✅ 临时标记（如"本帧已处理"）
- ✅ 调试信息（如创建时间、来源）
- ✅ 运行时动态数据

#### 何时用配置文件

- ✅ 单例加载配置
- ✅ 游戏数据（武器、敌人属性）
- ✅ 关卡配置
- ✅ 任何需要清晰架构的地方

---

## 性能注意事项

### 元数据的开销

```gdscript
# 每个元数据都会占用内存
node.set_meta("key1", value1)  # 额外内存
node.set_meta("key2", value2)  # 额外内存

# 高频调用时注意性能
func _process(delta):
    # ❌ 不好：每帧设置元数据
    for enemy in enemies:
        enemy.set_meta("last_update", Time.get_ticks_msec())

    # ✅ 更好：用成员变量
    for enemy in enemies:
        enemy.last_update = Time.get_ticks_msec()
```

### 注解无开销

```gdscript
# ✅ 注解在编译时处理，运行时无开销
@export var health: int = 100
@onready var sprite = $Sprite2D
```

---

## 常见误区

### 误区 1：以为 `@` 开头的注释是注解

```gdscript
# ❌ 错误理解
## @my_custom_tag
# 这只是注释，Godot 不会识别

# ✅ 正确理解
# 这是普通注释，需要自己写代码解析
```

### 误区 2：混淆元数据和成员变量

```gdscript
# 元数据
node.set_meta("health", 100)  # 运行时附加，任何对象都可以

# 成员变量
class Enemy:
    var health: int = 100  # 编译时定义，只有 Enemy 类有
```

### 误区 3：过度使用元数据

```gdscript
# ❌ 不好：应该用成员变量
class Enemy:
    func _ready():
        set_meta("health", 100)
        set_meta("speed", 50)
        set_meta("damage", 10)

# ✅ 更好：用成员变量
class Enemy:
    var health: int = 100
    var speed: float = 50.0
    var damage: int = 10
```

---

## 参考资源

- [GDScript 注解官方文档](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_basics.html#annotations)
- [Object.set_meta() 官方文档](https://docs.godotengine.org/en/stable/classes/class_object.html#class-object-method-set-meta)
