extends Node2D
## 对象池测试场景
##
## 功能：
## - 可视化测试对象池的复用效果
## - 验证对象池的性能和正确性
## - 提供交互式调试界面
##
## 测试重点：
## 1. 复用计数：每个对象显示被复用的次数
## 2. 视觉反馈：颜色渐变、大小变化、透明度变化
## 3. 性能统计：实时显示池的运行状态

#region 配置
@export var pooled_object_scene: PackedScene
## 要池化的场景资源
## 如果为空，会自动加载默认的测试对象
#endregion

#region 状态变量
## 对象池实例
var _pool: ObjectPool
## 自动生成计时器（用于压力测试）
var _spawn_timer: float = 0.0
## 是否启用自动生成（压力测试模式）
var _auto_spawn: bool = false
#endregion

#region UI 引用
@onready var stats_label: Label = $CanvasLayer/StatsLabel
## 显示实时统计信息（左上角）

@onready var auto_spawn_button: Button = $CanvasLayer/ControlPanel/AutoSpawnButton
## 自动生成开关按钮（右侧控制面板）
#endregion


func _ready() -> void:
	# 加载默认测试场景（如果未手动指定）
	# 测试对象包含：复用计数显示、颜色渐变、大小变化等功能
	if not pooled_object_scene:
		pooled_object_scene = load("res://scenes/test/object_pool/test_pooled_object.tscn")
	
	# 创建对象池，使用字典内联配置（推荐方式）
	# 配置说明：
	# - max_size: 30 - 最多缓存30个空闲对象，超过的会被销毁
	# - initial_size: 10 - 预热10个对象，避免首次获取时的延迟
	# - name: "测试对象池" - 用于调试和统计显示
	_pool = ObjectPool.new(pooled_object_scene, {
		"max_size": 30,
		"initial_size": 10,
		"name": "测试对象池"
	})
	
	# 监听池耗尽信号（用于调试）
	# 当池中没有可用对象需要创建新实例时触发
	_pool.pool_exhausted.connect(_on_pool_exhausted)
	
	print("对象池测试场景已启动")
	print("配置：max_size=30, initial_size=10")
	print("使用右侧控制面板进行测试，或点击空白区域生成对象")


func _process(delta: float) -> void:
	# 压力测试模式：自动生成对象
	# 用于测试对象池在高频率创建/销毁场景下的性能
	if _auto_spawn:
		_spawn_timer += delta
		if _spawn_timer >= 0.05: # 每 0.05 秒生成一个（约 20 个/秒）
			_spawn_timer = 0.0
			_spawn_object()
	
	# 实时更新统计信息（左上角显示）
	# 格式：[池名] 总:50(活:20/闲:30) | 峰:25 | 命中:85.2% | 弃:5
	# - 总数：池中对象总数（活跃+空闲）
	# - 活跃：当前使用中的对象数量
	# - 空闲：池中缓存的可用对象数量
	# - 峰值：历史最高活跃数量
	# - 命中：从池中获取的比例（越高越好）
	# - 弃：因池满被销毁的对象数量
	stats_label.text = _pool.get_stats_string()


func _input(event: InputEvent) -> void:
	# 鼠标点击生成对象（交互式测试）
	# 排除控制面板区域，避免点击按钮时也生成对象
	if event is InputEventMouseButton:
		if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			# 检查点击位置是否在控制面板外
			var panel = $CanvasLayer/ControlPanel as Control
			if panel and not panel.get_global_rect().has_point(event.position):
				_spawn_at_position(event.position)


#region 按钮回调
## 自动生成开关切换
## 用于压力测试，验证对象池在高频率操作下的稳定性
func _on_auto_spawn_toggled(toggled_on: bool) -> void:
	_auto_spawn = toggled_on
	_spawn_timer = 0.0 # 重置计时器，避免立即生成
	# 更新按钮文本，直观显示当前状态
	auto_spawn_button.text = "▶ 自动生成: 开启" if toggled_on else "⏸ 自动生成: 关闭"


## 手动生成一个对象
## 用于测试基本的获取/归还流程
func _on_spawn_one_pressed() -> void:
	_spawn_object()


## 清空对象池
## 销毁所有缓存的空闲对象，测试清理功能
func _on_clear_pool_pressed() -> void:
	_pool.clear()
	print("池已清空 - 所有空闲对象已被销毁")


## 打印详细统计信息
## 将统计信息输出到控制台，便于复制和分析
func _on_print_stats_pressed() -> void:
	print("=== 对象池统计 ===")
	print(_pool.get_stats_string())
	print("================")
#endregion


#region 对象生成方法
## 生成一个随机方向的对象
## 从屏幕中心向随机方向发射，测试基本的对象生命周期
func _spawn_object() -> void:
	var screen_center = get_viewport_rect().size / 2
	# 生成 0-2π 的随机角度（TAU = 2π）
	var angle = randf() * TAU
	var direction = Vector2(cos(angle), sin(angle))
	
	# 从池中获取对象（如果池空会自动创建新实例）
	var obj = _pool.acquire(self)
	obj.global_position = screen_center
	obj.init(direction)


## 根据鼠标点击位置生成对象
## 从屏幕中心向点击位置发射，提供交互式测试体验
func _spawn_at_position(pos: Vector2) -> void:
	var screen_center = get_viewport_rect().size / 2
	# 计算从中心到点击位置的方向向量
	var direction = (pos - screen_center).normalized()
	
	var obj = _pool.acquire(self)
	obj.global_position = screen_center
	obj.init(direction)
#endregion


#region 信号处理和清理
## 池耗尽信号回调
## 当池中没有可用对象需要创建新实例时触发
## 可以在这里添加性能监控或警告
func _on_pool_exhausted() -> void:
	# 可以添加性能警告或日志
	# print("警告：池耗尽，正在创建新实例")
	pass


## 场景退出时的清理
## 打印最终统计并清空池，避免内存泄漏
func _exit_tree() -> void:
	print("\n=== 测试结束 ===")
	print("最终统计:")
	print(_pool.get_stats_string())
	print("================")
	
	# 清理对象池，销毁所有缓存的实例
	_pool.clear()
#endregion
