@tool
## SpriteFrames 自动生成插件（GDScript 版）
## 功能：将指定目录下的序列帧图片（PNG）自动转换为 Godot 的 SpriteFrames 资源，并生成配套的 AnimatedSprite2D 场景。
## 配置参数：请编辑同目录下的 sprite_frames_config.gd
extends EditorPlugin

# 预加载配置文件（避免依赖 class_name 在插件未启用时不可用的问题）
const Config = preload("res://addons/SpriteFramesGenerator_GDS/sprite_frames_config.gd")

# 菜单项名称常量
const MENU_ITEM_NAME := "Generate SpriteFrames (Single/Selection)"
const MENU_ITEM_BATCH_NAME := "Generate All SpriteFrames (Batch)"

# Spine 导出格式正则: 前缀-动画名_序号.png
var _regex_complex: RegEx
# 简单格式正则: 动画名_序号.png
var _regex_simple: RegEx
# 右键菜单插件实例
var _context_menu_plugin: FolderContextMenuPlugin


## 插件进入编辑器树时调用（启用插件）
## 配置参数已迁移至 sprite_frames_config.gd，无需注册 ProjectSettings
func _enter_tree() -> void:
	# 编译正则表达式
	_regex_complex = RegEx.new()
	_regex_complex.compile(".*-(.*)_(\\d+)\\.png")
	_regex_simple = RegEx.new()
	_regex_simple.compile("(.*)_(\\d+)\\.png")

	# 1. 在编辑器顶部"工具"菜单中添加批量生成项
	add_tool_menu_item(MENU_ITEM_BATCH_NAME, _generate_all_from_predefined_paths)

	# 2. 注册文件系统面板的右键菜单插件
	_context_menu_plugin = FolderContextMenuPlugin.new(self )
	add_context_menu_plugin(EditorContextMenuPlugin.CONTEXT_SLOT_FILESYSTEM, _context_menu_plugin)


## 插件退出编辑器树时调用（禁用插件）
func _exit_tree() -> void:
	# 清理菜单项
	remove_tool_menu_item(MENU_ITEM_BATCH_NAME)
	# 卸载右键菜单插件
	remove_context_menu_plugin(_context_menu_plugin)


## 处理选中的路径并生成资源
func generate_from_paths(paths: PackedStringArray) -> void:
	if paths.is_empty():
		return

	var path := paths[0]

	# 如果用户选中了文件，则自动获取该文件所在的目录
	if FileAccess.file_exists(path):
		path = path.get_base_dir()

	# 检查文件夹是否存在
	if DirAccess.dir_exists_absolute(path):
		var valid_folders: Array[String] = []
		_scan_folder_recursively(path, valid_folders)

		if valid_folders.size() > 0:
			var total_generated := 0
			for folder in valid_folders:
				_generate_sprite_frames(folder, false)
				total_generated += 1
			EditorInterface.get_resource_filesystem().scan()
			print_rich("[color=cyan]单次/选中生成完成！共处理 %d 个角色目录。[/color]" % total_generated)
		else:
			push_warning("[%s] 未识别到有效的序列帧命名格式 (示例: attack_0.png) 或其子文件夹中未发现资源。" % path)
	else:
		printerr("路径不存在或无法访问: %s" % path)


## 批量处理预设路径下的所有角色文件夹（递归扫描）
func _generate_all_from_predefined_paths() -> void:
	var total_generated := 0
	var batch_paths: PackedStringArray = Config.BATCH_PATHS
	var valid_folders: Array[String] = []

	for base_path in batch_paths:
		if not DirAccess.dir_exists_absolute(base_path):
			print("[SpriteFramesGenerator] 路径不存在: %s" % base_path)
			continue
		# 递归扫描所有子文件夹
		_scan_folder_recursively(base_path, valid_folders)

	if valid_folders.size() > 0:
		for folder in valid_folders:
			# 批量模式下暂不每次都触发资源扫描，最后统一触发
			_generate_sprite_frames(folder, false)
			total_generated += 1
		EditorInterface.get_resource_filesystem().scan()
		print_rich("[color=cyan]批量生成完成！共处理 %d 个角色目录。[/color]" % total_generated)
	else:
		print_rich("[color=yellow]未在预设路径下发现可处理的序列帧资源。[/color]")


## 递归查找包含有效序列帧的文件夹
func _scan_folder_recursively(dir_path: String, results: Array[String]) -> void:
	# 1. 检查当前文件夹是否包含有效序列帧
	if _has_png_files(dir_path):
		# 预检：如果能提取出动画分组，则认为是一个有效的角色文件夹
		var groups := _find_sprite_sequences(dir_path)
		if groups.size() > 0:
			results.append(dir_path)

	# 2. 遍历子文件夹
	var dir := DirAccess.open(dir_path)
	if dir == null:
		return

	dir.list_dir_begin()
	var sub_dir_name := dir.get_next()
	while sub_dir_name != "":
		if dir.current_is_dir() and not sub_dir_name.begins_with("."):
			# 跳过插件生成的输出目录，避免递归死循环或误判
			if sub_dir_name != "AnimatedSprite2D":
				_scan_folder_recursively(dir_path.path_join(sub_dir_name), results)
		sub_dir_name = dir.get_next()
	dir.list_dir_end()


## 解析文件夹中的序列帧并分组
## 返回: { "动画名": [{ "index": int, "path": String }, ...], ... }
func _find_sprite_sequences(folder_path: String) -> Dictionary:
	var anim_groups: Dictionary = {}

	var dir := DirAccess.open(folder_path)
	if dir == null:
		return anim_groups

	for file_name in dir.get_files():
		if not file_name.ends_with(".png") or file_name.ends_with(".import"):
			continue

		var anim_name := ""
		var frame_index := 0
		var matched := false

		# 尝试匹配 Spine 导出格式: 前缀-动画名_序号.png
		var result := _regex_complex.search(file_name)
		if result:
			anim_name = _normalize_name(result.get_string(1))
			frame_index = int(result.get_string(2))
			matched = true
		else:
			# 尝试匹配简单格式: 动画名_序号.png
			result = _regex_simple.search(file_name)
			if result:
				anim_name = _normalize_name(result.get_string(1))
				frame_index = int(result.get_string(2))
				matched = true

		if matched:
			var full_path := folder_path.path_join(file_name)
			if not anim_groups.has(anim_name):
				anim_groups[anim_name] = []
			anim_groups[anim_name].append({"index": frame_index, "path": full_path})

	return anim_groups


## 动画名称规范化：转换大小写并应用名称映射表
func _normalize_name(raw_name: String) -> String:
	var clean_name := raw_name.to_lower().strip_edges()

	# 从配置文件读取映射表
	if Config.NAME_MAP.has(clean_name):
		return Config.NAME_MAP[clean_name]

	return clean_name


## 检查目录下是否存在 PNG 文件
func _has_png_files(path: String) -> bool:
	var dir := DirAccess.open(path)
	if dir == null:
		return false

	dir.list_dir_begin()
	var file_name := dir.get_next()
	while file_name != "":
		if not dir.current_is_dir() and file_name.ends_with(".png") and not file_name.ends_with(".import"):
			dir.list_dir_end()
			return true
		file_name = dir.get_next()
	dir.list_dir_end()
	return false


## 判断当前文件夹是否处于配置的统一命名路径下，返回匹配的目标动画名（未匹配返回空字符串）
func _get_unified_anim_name(folder_path: String) -> String:
	var unified_name_paths: Dictionary = Config.UNIFIED_NAME_PATHS
	var normalized_folder := folder_path.replace("\\", "/")
	for anim_name in unified_name_paths:
		var paths: Array = unified_name_paths[anim_name]
		for p in paths:
			if p.strip_edges().is_empty():
				continue
			var normalized_path: String = p.replace("\\", "/")
			if normalized_folder.begins_with(normalized_path):
				return anim_name
	return ""


## 核心逻辑：扫描文件夹并生成 SpriteFrames 资源和场景
func _generate_sprite_frames(folder_path: String, trigger_scan: bool = true) -> void:
	# 复用解析逻辑
	var anim_groups := _find_sprite_sequences(folder_path)

	# 如果该文件夹匹配统一命名路径，对所有动画按字母序统一重命名为 Key, Key1, Key2...
	var unified_name := _get_unified_anim_name(folder_path)
	if anim_groups.size() > 0 and not unified_name.is_empty():
		var new_groups: Dictionary = {}
		var sorted_keys: Array = anim_groups.keys()
		sorted_keys.sort()
		var idx := 0
		for key in sorted_keys:
			var new_name: String = unified_name if idx == 0 else "%s%d" % [unified_name, idx]
			new_groups[new_name] = anim_groups[key]
			idx += 1
		anim_groups = new_groups

	if anim_groups.size() == 0:
		if trigger_scan:
			push_warning("[%s] 未识别到有效的序列帧命名格式 (示例: attack_0.png)" % folder_path)
		return

	print("正在处理文件夹: %s" % folder_path)

	# 获取当前文件夹名称，用作资源名和节点名
	# 例如 .../Unit/Enemy/豺狼人 -> 豺狼人
	# 先规范化路径，移除末尾的斜杠（避免 get_file() 返回空字符串）
	var normalized_path := folder_path.rstrip("/").rstrip("\\")
	var folder_name := normalized_path.get_file()
	if folder_name.is_empty():
		folder_name = "AnimatedSprite2D" # 兜底

	# --- 读取配置 ---
	var default_fps: float = Config.DEFAULT_FPS

	# --- 阶段 1: 构建 SpriteFrames 资源 ---
	var sprite_frames := SpriteFrames.new()
	if sprite_frames.has_animation("default"):
		sprite_frames.remove_animation("default")

	for anim_name in anim_groups:
		# 按帧序号升序排列
		var frames: Array = anim_groups[anim_name]
		frames.sort_custom(func(a, b): return a["index"] < b["index"])

		sprite_frames.add_animation(anim_name) # 添加动画
		sprite_frames.set_animation_speed(anim_name, default_fps) # 使用设置中的帧率
		# 只有白名单中的动画循环播放，或者是以 idle 开头的命名，其他动画播放一次
		var should_loop: bool = anim_name in Config.LOOP_ANIMATIONS or anim_name.begins_with("idle")
		sprite_frames.set_animation_loop(anim_name, should_loop)

		for frame_data in frames:
			# 加载纹理并添加到动画帧
			var texture: Texture2D = load(frame_data["path"])
			if texture != null:
				sprite_frames.add_frame(anim_name, texture)

	# --- 阶段 2: 保存 SpriteFrames 资源 (.tres) ---
	# 在当前目录下创建 "AnimatedSprite2D" 子文件夹存放生成结果
	var sub_folder := "AnimatedSprite2D"
	var sub_folder_path := folder_path.path_join(sub_folder)
	if not DirAccess.dir_exists_absolute(sub_folder_path):
		var make_dir_err := DirAccess.make_dir_absolute(sub_folder_path)
		if make_dir_err != OK:
			printerr("创建文件夹失败: %s, 错误: %s" % [sub_folder_path, error_string(make_dir_err)])
			return

	# 使用文件夹名作为资源文件名: "豺狼人.tres"
	var res_path := sub_folder_path.path_join("%s.tres" % folder_name)
	sprite_frames.resource_path = res_path # 设置资源路径，确保序列化时引用正确
	var res_err := ResourceSaver.save(sprite_frames, res_path)

	if res_err != OK:
		printerr("资源保存失败: %s" % error_string(res_err))
		return

	# --- 阶段 3: 创建并保存 AnimatedSprite2D 场景 (.tscn) ---
	# 目标路径：在子文件夹中生成 "豺狼人.tscn"
	var scene_path := sub_folder_path.path_join("%s.tscn" % folder_name)
	var sprite_node: AnimatedSprite2D = null
	var is_new_scene := true

	# -------------------------------------------------------------------------
	# 智能更新逻辑 (Smart Update Logic)
	# -------------------------------------------------------------------------
	if FileAccess.file_exists(scene_path):
		var existing_scene: PackedScene = load(scene_path)
		if existing_scene != null:
			var instance := existing_scene.instantiate()
			if instance is AnimatedSprite2D:
				sprite_node = instance
				is_new_scene = false
				print_rich("[color=cyan][Generator] 智能更新现有场景: %s (属性已保留)[/color]" % folder_path)
			else:
				instance.free()

	# -------------------------------------------------------------------------
	# 场景创建逻辑 (New Scene Creation)
	# -------------------------------------------------------------------------
	if sprite_node == null:
		sprite_node = AnimatedSprite2D.new()
		# 设置节点名称为文件夹名，方便调试 (如 "豺狼人")
		sprite_node.name = folder_name
		sprite_node.transform = Transform2D.IDENTITY
		sprite_node.centered = true
		print("[Generator] 创建全新场景: %s" % folder_path)
	else:
		# 即使是旧场景，也强制更新节点名称，确保一致性
		if sprite_node.name != folder_name:
			sprite_node.name = folder_name

	# -------------------------------------------------------------------------
	# 统一更新逻辑 (Unified Update)
	# -------------------------------------------------------------------------
	sprite_node.sprite_frames = sprite_frames

	# 检查当前设置的动画名是否依然有效
	if not sprite_frames.has_animation(sprite_node.animation):
		if sprite_frames.has_animation("Effect"):
			sprite_node.animation = "Effect"
		elif sprite_frames.has_animation("idle"):
			sprite_node.animation = "idle"
		elif sprite_frames.get_animation_names().size() > 0:
			sprite_node.animation = sprite_frames.get_animation_names()[0]

	# -------------------------------------------------------------------------
	# 保存场景 (Save Scene)
	# -------------------------------------------------------------------------
	var packed_scene := PackedScene.new()
	var pack_err := packed_scene.pack(sprite_node)

	if pack_err == OK:
		var scene_save_err := ResourceSaver.save(packed_scene, scene_path)
		if scene_save_err == OK:
			var action := "创建" if is_new_scene else "更新"
			print_rich("[color=green]成功%s场景: %s[/color]" % [action, scene_path])
			print_rich("[color=green]成功生成资源文件: %s[/color]" % res_path)
		else:
			printerr("场景保存失败: %s" % error_string(scene_save_err))
	else:
		printerr("场景打包失败: %s" % error_string(pack_err))

	# 释放节点内存
	sprite_node.queue_free()

	# 触发资源扫描
	if trigger_scan:
		EditorInterface.get_resource_filesystem().scan()


# =============================================================================
# 内部类：用于扩展文件系统面板的右键菜单
# =============================================================================
class FolderContextMenuPlugin extends EditorContextMenuPlugin:
	# 内部类无法访问外层脚本的常量，需在此重新声明
	const _MENU_ITEM_NAME := "Generate SpriteFrames (Single/Selection)"
	var _plugin: EditorPlugin

	func _init(plugin: EditorPlugin = null) -> void:
		_plugin = plugin

	## 当用户在文件系统右键点击时触发
	func _popup_menu(paths: PackedStringArray) -> void:
		# 遍历选中项，如果包含目录，则显示生成菜单
		for path in paths:
			if DirAccess.dir_exists_absolute(path):
				# 捕获 paths 副本，避免 lambda 闭包持有原始引用的问题
				var captured_paths := paths.duplicate()
				# 注意：add_context_menu_item 的回调被调用时会传入一个 Array 参数
				# lambda 必须接受该参数（即使不使用），否则 Godot 会报错导致回调失败
				add_context_menu_item(_MENU_ITEM_NAME, func(_args): _plugin.generate_from_paths(captured_paths))
				break
