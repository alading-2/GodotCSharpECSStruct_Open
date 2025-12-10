extends Node
## Bootstrap - 项目启动引导器
## 
## 这是项目中唯一的 Autoload 单例，负责：
## 1. 从配置文件（autoload_config.gd）读取需要加载的单例列表
## 2. 按优先级排序并检查依赖关系
## 3. 自动实例化并注册所有单例
## 4. 提供统一的全局访问接口
##
## 配置方法：
## 1. 在 Project Settings → Autoload 中添加此脚本
##    - Path: res://scenes/autoload/bootstrap.gd
##    - Node Name: Bootstrap
## 2. 在 autoload_config.gd 中添加单例配置
## 3. 无需修改此文件，Bootstrap 会自动加载
##
## 访问方式：
## - Bootstrap.audio_manager.play_sound("shoot")
## - Bootstrap.get_singleton("AudioManager")
## - var audio = Bootstrap.audio_manager  # 本地缓存（推荐）
##
## 注意：Logger 已改为组件化，不再是全局单例
## 使用方式：@onready var logger: Logger = $Logger
##
## 调试方法：
## - Bootstrap.list_singletons()  # 列出所有已加载的单例

# ============================================
# 预加载配置类（解决类型识别问题）
# ============================================
const AutoloadConfigScript = preload("res://scenes/autoload/autoload_config.gd")

# ============================================
# 单例容器（动态填充）
# ============================================
var _singletons: Dictionary = {} # {name: Node}

# ============================================
# 启动流程
# ============================================

func _ready() -> void:
	print("[Bootstrap] 开始加载单例...")
	
	# 从配置文件读取单例列表
	var configs = AutoloadConfigScript.get_singletons()
	
	# 按优先级排序
	configs.sort_custom(_sort_by_priority)
	
	# 依次加载
	for config in configs:
		if config.enabled:
			_load_singleton_from_config(config)
	
	# 加载完成
	_on_bootstrap_complete()


## 按加载顺序排序（数字越小越先加载，类似 Godot 的 process_priority）
func _sort_by_priority(a, b) -> bool:
	return a.priority < b.priority


## 从配置加载单例
func _load_singleton_from_config(config) -> void:
	# 1. 检查依赖是否已加载
	for dep_name in config.dependencies:
		if not _singletons.has(dep_name):
			push_error("[Bootstrap] 单例 '%s' 依赖 '%s'，但后者尚未加载！请检查优先级设置。" % [config.name, dep_name])
			return
	
	# 2. 加载资源（支持脚本 .gd 和场景 .tscn）
	var resource = load(config.script_path)
	if resource == null:
		push_error("[Bootstrap] 无法加载资源: %s（请检查路径是否正确）" % config.script_path)
		return
	
	# 3. 实例化（根据类型选择方式）
	var instance: Node = null
	if resource is PackedScene:
		# 场景文件：使用 instantiate()
		instance = resource.instantiate()
	elif resource is GDScript:
		# 脚本文件：使用 new()
		instance = resource.new()
	else:
		push_error("[Bootstrap] 不支持的资源类型: %s（仅支持 .gd 和 .tscn）" % config.script_path)
		return
	
	if instance == null:
		push_error("[Bootstrap] 无法实例化单例: %s" % config.name)
		return
	
	instance.name = config.name
	add_child(instance)
	
	# 4. 注册到容器
	_singletons[config.name] = instance
	
	# 5. 日志输出
	_log_singleton_loaded(config)


## 日志输出（统一处理）
func _log_singleton_loaded(config) -> void:
	var msg = "单例已加载: %s (顺序: %d)" % [config.name, config.priority]
	
	if config.dependencies.size() > 0:
		msg += " [依赖: %s]" % ", ".join(config.dependencies)
	
	print("[Bootstrap] " + msg)


## 启动完成回调
func _on_bootstrap_complete() -> void:
	var msg = "✓ 所有单例加载完成（共 %d 个）" % _singletons.size()
	print("[Bootstrap] " + msg)
	
	# 输出加载的单例列表（调试用）
	if OS.is_debug_build():
		print("[Bootstrap] 已加载的单例: %s" % ", ".join(list_singletons()))


# ============================================
# 全局访问接口
# ============================================

## 通用获取方法
## 
## 示例：
##   var logger = Bootstrap.get_singleton("Logger")
##   if logger: logger.log_info("测试")
func get_singleton(singleton_name: String) -> Node:
	if _singletons.has(singleton_name):
		return _singletons[singleton_name]
	else:
		push_error("[Bootstrap] 单例 '%s' 不存在！可用的单例: %s" % [singleton_name, ", ".join(list_singletons())])
		return null


## 便捷属性访问（通过 _get() 实现动态属性）
## 
## 允许通过 Bootstrap.xxx 访问单例（自动转换为 PascalCase）
## 
## 示例：
##   Bootstrap.audio_manager.play()    # 等同于 Bootstrap.get_singleton("AudioManager")
##   Bootstrap.game_manager.start()    # 等同于 Bootstrap.get_singleton("GameManager")
func _get(property: StringName) -> Variant:
	var prop_str = String(property)
	
	# 转换为 PascalCase（首字母大写）
	# logger -> Logger
	# game_manager -> GameManager
	var singleton_name = prop_str.capitalize().replace(" ", "")
	
	if _singletons.has(singleton_name):
		return _singletons[singleton_name]
	
	# 返回 null 而不是报错，避免干扰正常的属性访问
	return null


## 列出所有已加载的单例
## 
## 返回：单例名称数组（按字母顺序排序）
func list_singletons() -> Array[String]:
	var names: Array[String] = []
	names.assign(_singletons.keys())
	names.sort()
	return names


## 检查单例是否已加载
## 
## 示例：
##   if Bootstrap.has_singleton("AudioManager"):
##       Bootstrap.audio_manager.play_sound("shoot")
func has_singleton(singleton_name: String) -> bool:
	return _singletons.has(singleton_name)
