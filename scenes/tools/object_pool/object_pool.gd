class_name ObjectPool extends RefCounted
## 对象池 - 复用 Node 实例，减少 instantiate() 和 queue_free() 开销
##
## 推荐用法（字典配置）：
## [codeblock]
## var pool = ObjectPool.new(bullet_scene, {
##     "max_size": 50,
##     "initial_size": 20,
##     "name": "Bullets"
## })
## var bullet = pool.acquire(get_parent())
## ObjectPool.return_to_pool(bullet)  # 在对象中调用
## [/codeblock]


#region 信号
## 实例被获取时触发
signal instance_acquired(instance: Node)
## 实例被归还时触发
signal instance_released(instance: Node)
## 池空需要创建新实例时触发
signal pool_exhausted()
## 池被清空时触发
signal pool_cleared()
#endregion

#region 私有变量
## 池化的场景资源
var _scene: PackedScene
## 池中缓存的实例数组（空闲对象）
var _instances: Array[Node] = []
## 池名称
var _name: String = ""

## 配置 - 池的最大容量（-1 表示无限制）
var _max_size: int = 50
## 配置 - 是否启用统计
var _enable_stats: bool = true

## 统计信息 - 当前活跃对象数量（已获取未归还）
var _active_count: int = 0
## 统计信息 - 预热创建的数量（用于复用率计算）
var _initial_created: int = 0
## 统计信息 - 历史峰值活跃数量
var _peak_active: int = 0
## 统计信息 - 总创建数量
var _total_created: int = 0
## 统计信息 - 总获取数量
var _total_acquired: int = 0
## 统计信息 - 总归还数量
var _total_released: int = 0
## 统计信息 - 总丢弃数量（因池满而销毁）
var _total_discarded: int = 0
#endregion

#region 公开属性（只读）
## 池化的场景资源
var scene: PackedScene:
	get: return _scene

## 池名称（唯一标识）
var pool_name: String:
	get: return _name

## 池中可用实例数量（空闲）
var available_count: int:
	get: return _instances.size()

## 当前活跃实例数量（已借出）
var active_count: int:
	get: return _active_count

## 对象总数（活跃 + 空闲）
var total_count: int:
	get: return _active_count + _instances.size()

## 历史峰值活跃数量
var peak_active: int:
	get: return _peak_active

## 总创建数量
var total_created: int:
	get: return _total_created

## 总获取数量
var total_acquired: int:
	get: return _total_acquired

## 总归还数量
var total_released: int:
	get: return _total_released

## 池是否为空
var is_empty: bool:
	get: return _instances.is_empty()

## 池是否已满
var is_full: bool:
	get: return _max_size > 0 and _instances.size() >= _max_size

## 对象复用率（0.0 - 1.0）
var reuse_rate: float:
	get:
		if _total_acquired == 0:
			return 0.0
		# 复用次数 = 总获取 - 运行时新创建数
		var reused = _total_acquired - (_total_created - _initial_created)
		return clampf(float(reused) / float(_total_acquired), 0.0, 1.0)
#endregion

#region 内部类
## 池配置类（可选使用）
class PoolConfig extends RefCounted:
	var max_size: int = 50 ## 最大容量，-1 无限制
	var enable_stats: bool = true ## 是否启用统计
	var initial_size: int = 0 ## 初始预热数量
	var name: String = "" ## 池名称（用于调试）
#endregion

#region 构造函数
## 创建对象池
## [param p_scene] 要池化的 PackedScene 资源（必须）
## [param p_config] 池配置（可选）：
##   - PoolConfig 对象：类型安全，IDE 自动补全
##   - Dictionary：快速配置，如 {"max_size": 50, "initial_size": 20, "name": "Bullets"}
##   - null：使用默认配置
func _init(p_scene: PackedScene, p_config: Variant = null) -> void:
	assert(p_scene != null, "ObjectPool: scene 不能为 null")
	
	_scene = p_scene
	
	# 解析配置
	if p_config is PoolConfig:
		_max_size = p_config.max_size
		_enable_stats = p_config.enable_stats
		_name = p_config.name if p_config.name.strip_edges() != "" else "ObjectPool"
		# 自动预热
		if p_config.initial_size > 0:
			warmup(p_config.initial_size)
	elif p_config is Dictionary:
		_max_size = p_config.get("max_size", 50)
		_enable_stats = p_config.get("enable_stats", true)
		_name = p_config.get("name", "ObjectPool")
		# 自动预热
		var initial = p_config.get("initial_size", 0)
		if initial > 0:
			warmup(initial)
	else:
		_name = "ObjectPool"
#endregion

#region 配置方法（链式调用）
## 设置池的最大容量
## [param size] 最大容量，-1 表示无限制
## [returns] 返回自身，支持链式调用
func set_max_size(size: int) -> ObjectPool:
	_max_size = size
	return self


## 设置是否启用统计
## [param enabled] 是否启用
## [returns] 返回自身，支持链式调用
func set_stats_enabled(enabled: bool) -> ObjectPool:
	_enable_stats = enabled
	return self
#endregion

#region 核心方法
## 预热池：提前创建指定数量的对象
## [param count] 要创建的数量
## [param container] 可选的临时容器节点，用于触发对象的 _ready()
func warmup(count: int, container: Node = null) -> void:
	for i in range(count):
		var instance = _create_instance()
		_initial_created += 1 # 记录预热数量
		
		# 如果提供了容器，临时加入场景树以触发 _ready
		if container:
			container.add_child(instance)
			await instance.ready # 等待 _ready 完成
			container.remove_child(instance)
		
		# 隐藏并存入池
		if instance is CanvasItem:
			instance.visible = false
		
		instance.set_meta("_in_pool", true)
		_instances.append(instance)


## 从池中获取一个实例
## [param parent] 实例将被添加到的父节点（必须提供）
## [returns] Node 实例
func acquire(parent: Node) -> Node:
	assert(parent != null, "ObjectPool[%s].acquire: parent 不能为 null" % _name)
	
	var instance: Node
	
	# 尝试从池中获取有效实例
	while not _instances.is_empty():
		var pooled_instance = _instances.pop_back()
		if is_instance_valid(pooled_instance):
			instance = pooled_instance
			break
		else:
			# 发现无效实例（可能被外部意外销毁），记录警告
			push_warning("ObjectPool[%s]: 发现无效实例，已跳过" % _name)
	
	# 如果池空或没找到有效实例，创建新实例
	if not instance:
		instance = _create_instance()
		pool_exhausted.emit()
	
	# 更新状态
	instance.set_meta("_in_pool", false)
	_active_count += 1
	if _active_count > _peak_active:
		_peak_active = _active_count
	
	if _enable_stats:
		_total_acquired += 1
	
	# 如果实例已有父节点，先移除（防御性编程）
	if instance.get_parent():
		instance.get_parent().remove_child(instance)
	
	# 添加到指定父节点
	parent.add_child(instance)
	
	# 显示（如果是 CanvasItem）
	if instance is CanvasItem:
		instance.visible = true
	
	# 启用处理（统一管理，被池化对象无需手动处理）
	instance.set_process(true)
	instance.set_physics_process(true)
	
	# 调用对象的激活方法（业务逻辑）
	if instance.has_method("on_pool_acquire"):
		instance.on_pool_acquire()
	
	instance_acquired.emit(instance)
	return instance


## 归还实例到池中
## [param instance] 要归还的实例
## [returns] 是否成功归还（true=成功，false=拒绝）
func release(instance: Node) -> bool:
	if not is_instance_valid(instance):
		push_warning("ObjectPool[%s].release: 尝试归还无效实例" % _name)
		return false
	
	# 防止重复归还
	if instance.get_meta("_in_pool", false):
		push_warning("ObjectPool[%s].release: 实例已在池中" % _name)
		return true # 已在池中，视为成功
	
	# 禁用处理（统一管理，被池化对象无需手动处理）
	instance.set_process(false)
	instance.set_physics_process(false)
	
	# 调用对象的停用方法（业务逻辑）
	if instance.has_method("on_pool_release"):
		instance.on_pool_release()
	
	# 从场景树移除
	if instance.get_parent():
		instance.get_parent().remove_child(instance)
	
	# 隐藏
	if instance is CanvasItem:
		instance.visible = false
	
	_active_count -= 1
	if _active_count < 0:
		_active_count = 0
	
	if _enable_stats:
		_total_released += 1
	
	# 检查池容量
	if _max_size > 0 and _instances.size() >= _max_size:
		# 池满，销毁多余对象
		if _enable_stats:
			_total_discarded += 1
		instance.queue_free()
	else:
		# 归还到池
		instance.set_meta("_in_pool", true)
		_instances.append(instance)
	
	instance_released.emit(instance)
	return true


## 清空池，销毁所有缓存的实例
## 注意：不会影响当前活跃的实例
func clear() -> void:
	for instance in _instances:
		if is_instance_valid(instance):
			instance.queue_free()
	_instances.clear()
	pool_cleared.emit()


## 清理池中多余的对象，保留指定数量
## [param retain_count] 要保留的数量
func cleanup(retain_count: int) -> void:
	if retain_count < 0:
		retain_count = 0
	
	# 从最旧的开始删除（FIFO）
	while _instances.size() > retain_count:
		var instance = _instances.pop_front() # 删除最旧的
		if is_instance_valid(instance):
			instance.queue_free()
			if _enable_stats:
				_total_discarded += 1


## 获取统计信息字符串
func get_stats_string() -> String:
	return "[%s] 总:%d(活:%d/闲:%d) | 峰:%d | 创:%d | 获:%d | 还:%d | 弃:%d | 复用:%.1f%%" % [
		_name,
		total_count,
		_active_count,
		_instances.size(),
		_peak_active,
		_total_created,
		_total_acquired,
		_total_released,
		_total_discarded,
		reuse_rate * 100.0
	]


## 批量获取实例
## [param parent] 父节点
## [param count] 获取数量
## [returns] 实例数组
func acquire_batch(parent: Node, count: int) -> Array[Node]:
	var result: Array[Node] = []
	for i in range(count):
		result.append(acquire(parent))
	return result


## 批量归还实例
## [param instances] 要归还的实例数组
func release_batch(instances: Array) -> void:
	for instance in instances:
		release(instance)


## 静态方法：从实例归还到其所属的池
## [param instance] 要归还的实例
## [returns] 是否成功归还
static func return_to_pool(instance: Node) -> bool:
	if not is_instance_valid(instance):
		return false
	
	var pool = instance.get_meta("_object_pool", null) as ObjectPool
	if pool:
		return pool.release(instance)
	else:
		push_warning("ObjectPool.return_to_pool: 实例没有关联的对象池")
		return false
#endregion

#region 私有方法
func _create_instance() -> Node:
	var instance = _scene.instantiate()
	instance.set_meta("_object_pool", self) # 标记所属池
	instance.set_meta("_in_pool", false) # 初始状态不在池中
	
	if _enable_stats:
		_total_created += 1
	
	return instance


#endregion
