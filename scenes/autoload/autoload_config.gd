extends Resource
class_name AutoloadConfig
## Autoload 配置资源
## 
## 定义项目中需要自动加载的所有单例
## 
## 功能特性：
## - 加载顺序控制（通过 LoadOrder 枚举 + 微调，数字越小越先加载）
## - 条件加载（通过 enabled，可根据环境动态启用/禁用）
## - 依赖声明（通过 dependencies，自动检查依赖是否已加载）
## 
## 使用方法：
## 1. 在 get_singletons() 中添加单例配置
## 2. 使用 LoadOrder 枚举指定层级，用 +/- 微调顺序
## 3. Bootstrap 会自动按加载顺序和依赖关系加载
## 4. 通过 Bootstrap.单例名 访问（如 Bootstrap.logger）
## 
## 示例：
##   # 基础工具层（支持脚本和场景）
##   SingletonConfig.new("LogWriter", "res://scenes/tools/logger/log_writer.gd", LoadOrder.TOOL)  # 全局日志写入器
##   SingletonConfig.new("AudioManager", "res://...", LoadOrder.MANAGER)  # 音频管理器
##   
##   # 管理器层
##   SingletonConfig.new("AudioManager", "res://...", LoadOrder.MANAGER)
##   SingletonConfig.new("ResourceManager", "res://...", LoadOrder.MANAGER + 10)
##   
##   # 业务逻辑层
##   SingletonConfig.new("GameManager", "res://...", LoadOrder.LOGIC)

## 加载顺序枚举（数字越小越先加载，类似 Godot 的 process_priority）
enum LoadOrder {
	TOOL = 0, ## 基础工具层（LogWriter 等全局工具）
	MANAGER = 100, ## 管理器层（ResourceManager, AudioManager 等）
	LOGIC = 200, ## 业务逻辑层（GameManager, BattleManager 等）
	DEBUG = 900, ## 调试工具（DebugTools 等，仅开发环境）
}

## 单例配置项
class SingletonConfig:
	## 单例名称（PascalCase，如 "Logger", "GameManager"）
	var name: String
	
	## 资源路径（支持 .gd 脚本或 .tscn 场景，必须以 res:// 开头）
	var script_path: String
	
	## 加载顺序（数字越小越先加载，默认 100）
	## 注：类似 Godot 的 process_priority，0 表示最先加载
	var priority: int = 100
	
	## 是否启用（可用于条件加载，默认 true）
	var enabled: bool = true
	
	## 依赖的其他单例名称（必须在此单例之前加载）
	var dependencies: Array[String] = []
	
	func _init(p_name: String, p_script_path: String, p_priority: int = 100, p_enabled: bool = true):
		name = p_name
		script_path = p_script_path
		priority = p_priority
		enabled = p_enabled
	
	## 添加依赖（链式调用）
	func depends_on(dep_names: Array[String]) -> SingletonConfig:
		dependencies = dep_names
		return self
	
	## 设置加载顺序（链式调用）
	## 数字越小越先加载，类似 Godot 的 process_priority
	func with_priority(p: int) -> SingletonConfig:
		priority = p
		return self


## 获取所有单例配置
static func get_singletons() -> Array[SingletonConfig]:
	var singletons: Array[SingletonConfig] = []
	
	# ============================================
	# 基础工具层（优先级 0-99）
	# ============================================
	
	# LogWriter - 全局日志文件写入器（可选）
	# 注意：Logger 已改为组件化，每个节点独立实例
	# LogWriter 负责统一的文件写入
	singletons.append(SingletonConfig.new(
		"LogWriter",
		"res://scenes/tools/logger/log_writer.gd",
		LoadOrder.TOOL # 0
	))
	
	# 注意：ObjectPool 已改为按需创建的 RefCounted 类
	# 不再作为全局单例，在需要的地方直接 ObjectPool.new() 创建
	
	# ============================================
	# 管理器层（优先级 100-199）
	# 注意：以下管理器需要创建对应的脚本文件才能启用
	# ============================================
	
	# 资源管理器 - 加载和管理游戏配置数据
	# singletons.append(SingletonConfig.new(
	# 	"ResourceManager",
	# 	"res://scenes/managers/resource_manager.gd",
	# 	LoadOrder.MANAGER  # 100
	# ))
	
	# 音频管理器 - 管理音效和音乐（依赖 Logger）
	# singletons.append(
	# 	SingletonConfig.new("AudioManager", "res://scenes/managers/audio_manager.gd", LoadOrder.MANAGER + 10)  # 110
	# 		.depends_on(["Logger"])
	# )
	
	# 游戏管理器 - 管理游戏流程（依赖其他管理器）
	# singletons.append(
	# 	SingletonConfig.new("GameManager", "res://scenes/managers/game_manager.gd", LoadOrder.LOGIC)  # 200
	# 		.depends_on(["Logger", "AudioManager", "ResourceManager"])
	# )
	
	# ============================================
	# 调试工具（优先级 900+，仅开发环境）
	# ============================================
	
	# 调试工具 - 仅在调试构建中加载
	# if OS.is_debug_build():
	# 	singletons.append(SingletonConfig.new(
	# 		"DebugTools",
	# 		"res://scenes/tools/debug/debug_tools.gd",
	# 		LoadOrder.DEBUG  # 900
	# 	))
	
	return singletons
