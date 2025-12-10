extends Node2D
## 测试用的可池化对象
##
## 功能：
## - 显示复用次数（验证对象确实被复用）
## - 颜色渐变（绿→红，表示生命周期）
## - 大小渐变（验证状态重置是否正确）
## - 移动轨迹（基本功能验证）

# 注意：ObjectPool 是 class_name，无需预加载

#region 配置
@export var radius: float = 25.0 # 增大半径，更容易看见
@export var lifetime: float = 3.0 # 延长生命周期，便于观察
@export var speed: float = 200.0 # 降低速度，便于观察
@export var start_color: Color = Color(0.3, 1.0, 0.4) # 鲜艳的绿色
@export var end_color: Color = Color(1.0, 0.3, 0.3) # 鲜艳的红色
#endregion

#region 状态
var velocity: Vector2 = Vector2.ZERO
var _timer: float = 0.0
var _current_color: Color = Color.GREEN
var _current_scale: float = 1.0
var _current_alpha: float = 1.0
## 复用计数（每次从池中取出时+1，用于验证对象确实被复用）
var _reuse_count: int = 0
#endregion

#region 节点引用
@onready var color_rect: ColorRect = $ColorRect
@onready var label: Label = $Label
#endregion


func _process(delta: float) -> void:
	# 移动
	position += velocity * delta
	
	# 更新生命周期
	_timer += delta
	var progress = clampf(_timer / lifetime, 0.0, 1.0)
	
	# 颜色渐变：绿 → 红
	_current_color = start_color.lerp(end_color, progress)
	color_rect.color = _current_color
	
	# 大小渐变：1.0 → 0.5（验证状态重置）
	_current_scale = lerpf(1.0, 0.5, progress)
	color_rect.scale = Vector2.ONE * _current_scale
	
	# 透明度渐变（最后 20% 开始淡出）
	if progress > 0.8:
		_current_alpha = 1.0 - (progress - 0.8) / 0.2
		color_rect.modulate.a = _current_alpha
		label.modulate.a = _current_alpha
	else:
		_current_alpha = 1.0
		color_rect.modulate.a = 1.0
		label.modulate.a = 1.0
	
	# 生命周期结束，归还到池
	if _timer >= lifetime:
		_return_to_pool()


## 初始化方法（由使用者调用）
func init(direction: Vector2, custom_speed: float = -1.0) -> void:
	velocity = direction.normalized() * (custom_speed if custom_speed > 0 else speed)


#region 池化接口
## 从池中取出时调用 - 重置所有状态
## 注意：set_process() 由 ObjectPool 统一管理，这里只处理业务逻辑
func on_pool_acquire() -> void:
	_reuse_count += 1
	velocity = Vector2.ZERO
	_timer = 0.0
	
	# 重置视觉状态
	_current_scale = 1.0
	_current_color = start_color
	_current_alpha = 1.0
	
	color_rect.scale = Vector2.ONE
	color_rect.color = start_color
	color_rect.modulate.a = 1.0
	label.modulate.a = 1.0
	
	# 更新复用计数显示
	label.text = str(_reuse_count)


## 归还到池时调用
## 注意：set_process() 由 ObjectPool 统一管理，这里只处理业务逻辑
func on_pool_release() -> void:
	pass # 当前无额外清理逻辑
#endregion


#region 私有方法
func _return_to_pool() -> void:
	# 使用静态方法归还（推荐）
	# 注意：如果 IDE 报错找不到 ObjectPool，请重启编辑器或重新加载项目
	# ObjectPool 已通过 class_name 全局注册
	if not ObjectPool.return_to_pool(self):
		# 如果归还失败（没有关联的池），则销毁
		queue_free()
#endregion
