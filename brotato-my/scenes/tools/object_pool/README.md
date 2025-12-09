# ObjectPool 对象池

复用 Node 实例，减少 `instantiate()` 和 `queue_free()` 开销。

## 快速开始

```gdscript
class_name Weapon extends Node2D

@export var bullet_scene: PackedScene
var _bullet_pool: ObjectPool

func _ready() -> void:
    # 方式1：字典配置（推荐）
    _bullet_pool = ObjectPool.new(bullet_scene, {
        "max_size": 50,
        "initial_size": 20,
        "name": "Bullets"
    })

    # 方式2：配置对象
    var config = ObjectPool.PoolConfig.new()
    config.max_size = 50
    config.initial_size = 20
    config.name = "Bullets"
    _bullet_pool = ObjectPool.new(bullet_scene, config)

func shoot() -> void:
    var bullet = _bullet_pool.acquire(get_parent())
    bullet.global_position = global_position

func _exit_tree() -> void:
    _bullet_pool.clear()
```

## API

### 构造函数

```gdscript
# 方式1：字典配置（推荐）
var pool = ObjectPool.new(bullet_scene, {
    "max_size": 50,
    "initial_size": 20,
    "name": "Bullets"
})

# 方式2：配置对象
var config = ObjectPool.PoolConfig.new()
config.max_size = 50
config.initial_size = 20
config.name = "Bullets"
var pool = ObjectPool.new(bullet_scene, config)

# 方式3：默认配置
var pool = ObjectPool.new(bullet_scene)
```

### 配置选项

**字典配置**：

```gdscript
{
    "max_size": 50,        # 最大容量，-1 无限制（默认 50）
    "enable_stats": true,  # 启用统计（默认 true）
    "initial_size": 20,    # 初始预热数量（默认 0）
    "name": "Bullets"      # 池名称（默认 "ObjectPool"）
}
```

**配置对象**：

```gdscript
var config = ObjectPool.PoolConfig.new()
config.max_size = 50
config.enable_stats = true
config.initial_size = 20
config.name = "Bullets"
```

### 核心方法

```gdscript
pool.warmup(count, container?)  # 预热（如果 initial_size > 0 会自动预热）
pool.acquire(parent) -> Node    # 获取实例
pool.release(instance)          # 归还实例（池满时自动销毁多余对象）
pool.clear()                    # 清空池中所有缓存对象
pool.cleanup(retain_count)      # 清理多余对象，保留指定数量
```

### 静态方法

```gdscript
ObjectPool.return_to_pool(instance)  # 便捷归还
```

### 属性

```gdscript
pool.available_count  # 可用数量
pool.active_count     # 活跃数量
pool.reuse_rate       # 对象复用率（0.0-1.0）
pool.is_empty         # 是否为空
pool.is_full          # 是否已满
```

### 信号

```gdscript
instance_acquired(instance)  # 获取后
instance_released(instance)  # 归还后
pool_exhausted()             # 池空时
pool_cleared()               # 清空时
```

## 被池化对象

实现以下方法（可选）：

```gdscript
func on_pool_acquire() -> void:
    # 重置状态
    velocity = Vector2.ZERO

func on_pool_release() -> void:
    # 清理资源
    pass
```

归还到池：

```gdscript
func _return_to_pool() -> void:
    ObjectPool.return_to_pool(self)
```

## 示例

### 敌人生成器

```gdscript
var _enemy_pool: ObjectPool

func _ready() -> void:
    _enemy_pool = ObjectPool.new(enemy_scene, {
        "max_size": 20,
        "initial_size": 5,
        "name": "Enemies"
    })

func spawn() -> void:
    var enemy = _enemy_pool.acquire(get_parent())
    enemy.global_position = spawn_pos
    enemy.died.connect(func(): _enemy_pool.release(enemy))
```

### 特效管理

```gdscript
var _vfx_pool: ObjectPool

func _ready() -> void:
    var config = ObjectPool.PoolConfig.new()
    config.max_size = 30
    config.initial_size = 10
    config.name = "VFX"
    _vfx_pool = ObjectPool.new(explosion_scene, config)

func play_explosion(pos: Vector2) -> void:
    var vfx = _vfx_pool.acquire(self)
    vfx.global_position = pos
    vfx.finished.connect(func(): _vfx_pool.release(vfx))
```

### 多个池

```gdscript
func _ready() -> void:
    _bullet_pool = ObjectPool.new(bullet_scene, {
        "max_size": 50,
        "initial_size": 20,
        "name": "Bullets"
    })

    _vfx_pool = ObjectPool.new(vfx_scene, {
        "max_size": 30,
        "initial_size": 10,
        "name": "VFX"
    })

    _enemy_pool = ObjectPool.new(enemy_scene, {
        "max_size": 20,
        "initial_size": 5,
        "name": "Enemies"
    })
```

## 性能建议

- **max_size**：子弹 30-50，敌人 20-30，特效 20-40
- **initial_size**：max_size 的 20-40%
- **定期清理**：波次结束时调用 `cleanup(retain_count)`
- **监控复用率**：> 80% 为佳

```gdscript
print(pool.get_stats_string())
# [Bullets] 总:25(活:10/闲:15) | 峰:20 | 创:25 | 获:100 | 还:90 | 弃:0 | 复用:75.0%
```
