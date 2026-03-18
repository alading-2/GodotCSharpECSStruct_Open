@tool
## SpriteFrames 生成器配置文件
## 直接修改常量，保存后自动生效。
class_name SpriteFramesConfig_GDS

# --- 批量扫描路径 ---
# 递归查找这些路径下包含 PNG 序列帧的子文件夹
const BATCH_PATHS: PackedStringArray = [
	"res://assets",
]

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

# --- 统一动画命名路径字典 ---
# Key: 目标动画名（字符串），Value: 路径列表（PackedStringArray）
# 位于对应路径下的所有动画，将按字母序统一重命名为 Key, Key1, Key2...
# 比如res://assets/Effect路径下的动画名全部改成effect, effect1, effect2...
const UNIFIED_NAME_PATHS: Dictionary = {
	"effect": ["res://assets/Effect"],
}
