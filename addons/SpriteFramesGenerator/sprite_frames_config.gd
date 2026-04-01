@tool
## SpriteFrames 生成器配置文件
## 直接修改常量，保存后自动生效。
class_name SpriteFramesConfig_GDS

# --- 批量扫描路径 ---
# 递归查找这些路径下包含 PNG 序列帧的子文件夹
const BATCH_PATHS: PackedStringArray = [
	"res://assets",
]

# --- 碰撞场景注入开关 ---
# 是否将模板碰撞场景注入到生成的 AnimatedSprite2D/Sprite2D 场景中。
# 设为 true 后，RULES 中配置了 collision_scene_path 的规则才会生效。
const ENABLE_COLLISION_SCENE: bool = true

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

# --- 统一规则表 ---
const RULES: Array = [
	{
		"key": "Player",
		"paths": ["res://assets/Unit/Player"],
		"unified_animation_name": "",
		"collision_scene_path": "res://Data/Data/Collision/Unit/PlayerCollision.tscn",
		# CollisionShape2D 首次生成时使用的默认胶囊体参数（智能更新时自动保留手动调整结果）
		"default_shape_radius": 30.0,
		"default_shape_height": 70.0,
		"default_shape_position": Vector2(-7.0, 29.0),
	},
	{
		"key": "Enemy",
		"paths": ["res://assets/Unit/Enemy"],
		"unified_animation_name": "",
		"collision_scene_path": "res://Data/Data/Collision/Unit/EnemyCollision.tscn",
		# CollisionShape2D 首次生成时使用的默认胶囊体参数
		"default_shape_radius": 20.0,
		"default_shape_height": 40.0,
		"default_shape_position": Vector2(0.0, 0.0),
	},
	{
		"key": "Effect",
		"paths": ["res://assets/Effect"],
		"unified_animation_name": "Effect",
		"collision_scene_path": "res://Data/Data/Collision/Effect/EffectCollision.tscn",
		# CollisionShape2D 首次生成时使用的默认胶囊体参数
		"default_shape_radius": 10.0,
		"default_shape_height": 20.0,
		"default_shape_position": Vector2(0.0, 0.0),
	},
]
