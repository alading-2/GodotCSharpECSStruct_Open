@tool
## SpriteFrames 生成器配置文件
## 直接修改常量，保存后自动生效。
class_name SpriteFramesConfig_GDS

# --- 批量扫描路径 ---
# 递归查找这些路径下包含 PNG 序列帧的子文件夹
const BATCH_PATHS: PackedStringArray = [
	"res://assets",
]

# --- 碰撞形状注入开关 ---
# 是否将 CollisionShape2D 注入到生成的 AnimatedSprite2D/Sprite2D 场景中。
# 注入的节点仅携带形状参数（用于运行时同步到 Entity 根节点），不再注入碰撞模板场景。
# Entity 的 collision_layer / collision_mask 直接在其 .tscn 根节点设置，不通过此处传递。
const ENABLE_COLLISION_SHAPE: bool = true

# --- 默认帧率 (FPS) ---
const DEFAULT_FPS: float = 10.0

# --- 默认循环播放 ---
# 是否默认循环。白名单中的动画（Idle,Run）会强制循环。
const DEFAULT_LOOP: bool = true

# --- 名称映射表 ---
# 将美术资源中的各种命名统一为标准名称。Key(小写) -> Value(标准名)
const NAME_MAP: Dictionary = {
	"movement": "run",
	"deaded": "dead",
	"death": "dead",
	"die": "dead",
}

# --- 循环播放白名单 ---
# 强制循环播放的动画名（忽略 DEFAULT_LOOP）。"idle" 开头的动画默认自动循环。
const LOOP_ANIMATIONS: PackedStringArray = [
	"idle",
	"run",
]

# --- 默认碰撞形状参数 ---
# [仅首次生成有效] 智能更新时自动保留用户手动调整的形状。
# RULES 中各规则可通过 collision_shape 字典覆盖对应字段，未填写的字段回退到此处的值。
const DEFAULT_COLLISION_SHAPE: Dictionary = {
	"radius": 30.0,
	"height": 70.0,
	"position": Vector2.ZERO,
}

# --- 统一规则表 ---
# 规则字典字段说明：
# - key: String, 规则标识（仅供辨识）
# - paths: Array, 该规则适用的资源路径列表
# - unified_animation_name: String, 统一动画名称（非空则将该目录下所有动画按顺序重命名，如: Effect, Effect1...）
# - collision_shape: Dictionary（可选），覆盖默认形状参数，支持字段：radius / height / position
const RULES: Array = [
	{
		"key": "Player",
		"paths": ["res://assets/Unit/Player"],
		"unified_animation_name": "",
		# "collision_shape": {"radius": 30.0, "height": 70.0, "position": Vector2(-7.0, 29.0)},
	},
	{
		"key": "Enemy",
		"paths": ["res://assets/Unit/Enemy"],
		"unified_animation_name": "",
	},
	{
		"key": "Effect",
		"paths": ["res://assets/Effect"],
		"unified_animation_name": "Effect",
	},
]
