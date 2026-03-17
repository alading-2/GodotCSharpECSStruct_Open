@tool
extends Control

const TablesPluginSettingsClass := preload("res://addons/resources_spreadsheet_view/settings_grid.gd")
const ColumnLabelsZh := preload("res://addons/resources_spreadsheet_view/column_labels_zh.gd")
const GroupHeaderScene := preload("res://addons/resources_spreadsheet_view/main_screen/group_header.tscn")

@export var table_header_scene: PackedScene

@onready var editor_view: Control = $"../../../.."
@onready var hide_columns_button: MenuButton = $"../../MenuStrip/VisibleCols"
@onready var grid: Container = $"../../../MarginContainer/FooterContentSplit/Panel/Scroll/MarginContainer/TableGrid"
@onready var group_row: Control = $"../GroupRow"


var hidden_columns := {}:
	get:
		var result := {}
		for k_path in column_properties:
			var result_one_path := {}
			result[k_path] = result_one_path
			for k_column in column_properties[k_path]:
				for k_property in column_properties[k_path][k_column]:
					if k_property == &"visibility" && [k_property]:
						result[k_path][k_column] = true

		return result
var column_properties := {}
var group_headers := []
var columns := []:
	set(v):
		columns = v
		for x in get_children():
			remove_child(x)
			x.queue_free()

		var new_node: Control
		for x in v:
			new_node = table_header_scene.instantiate()
			new_node.manager = self
			add_child(new_node)
			new_node.set_label(x)
			new_node.get_node("Button").pressed.connect(editor_view._set_sorting.bind(x))
			_update_column_sizes()
		
		_build_group_headers()


func _ready():
	hide_columns_button \
		.get_popup() \
		.id_pressed \
		.connect(_on_visible_cols_id_pressed)
	$"../../../MarginContainer/FooterContentSplit/Panel/Scroll" \
		.get_h_scroll_bar() \
		.value_changed \
		.connect(_on_h_scroll_changed)


func update():
	_update_hidden_columns()
	_update_column_sizes()


func hide_column(column_index: int):
	set_column_property(column_index, &"visibility", 0)
	editor_view.save_data()
	update()


func set_column_property(column_index: int, property_key: StringName, property_value):
	var dict := column_properties
	if !dict.has(editor_view.current_path):
		dict[editor_view.current_path] = {}

	dict = dict[editor_view.current_path]
	if !dict.has(columns[column_index]):
		dict[columns[column_index]] = {}

	dict = dict[columns[column_index]]
	dict[property_key] = property_value


func get_column_property(column_index: int, property_key: StringName, property_default = null):
	var dict := column_properties
	if !dict.has(editor_view.current_path):
		return property_default

	dict = dict[editor_view.current_path]
	if !dict.has(columns[column_index]):
		return property_default

	dict = dict[columns[column_index]]
	return dict.get(property_key, property_default)


func select_column(column_index: int):
	editor_view.select_column(column_index)


func _update_column_sizes():
	if grid.get_child_count() == 0:
		return
		
	await get_tree().process_frame
	var column_headers := get_children()

	if grid.get_child_count() < column_headers.size(): return
	if column_headers.size() != columns.size():
		editor_view.refresh()
		return
	
	var clip_text: bool = ProjectSettings.get_setting(TablesPluginSettingsClass.PREFIX + "clip_headers")
	var visible_column_minsizes: Array[float] = []
	for i in column_headers.size():
		var header = column_headers[i]
		if header.visible:
			header.get_child(0).clip_text = clip_text
			visible_column_minsizes.append(header.get_combined_minimum_size().x)

	grid.visible_column_minsizes = visible_column_minsizes
	await get_tree().process_frame

	# Abort if the node has been deleted since.
	if !is_instance_valid(column_headers[0]):
		return

	get_parent().custom_minimum_size.y = column_headers[0].get_combined_minimum_size().y + (28 if group_headers.size() > 0 else 0)
	var column_positions: Array = grid.visible_column_positions
	var i := 0
	for x in column_headers:
		if !x.visible:
			continue

		var pos: float = column_positions[i]
		x.position.x = pos
		x.size.x = column_positions[i + 1] - pos
		i += 1
	
	_update_group_header_positions()


func _update_hidden_columns():
	var current_path: String = editor_view.current_path
	var rows_shown: int = editor_view.last_row - editor_view.first_row

	if !column_properties.has(current_path):
		column_properties[current_path] = {
			"resource_local_to_scene": {&"visibility": 0},
			"resource_name": {&"visibility": 0},
			"metadata/_custom_type_script": {&"visibility": 0},
		}
		editor_view.save_data()

	var visible_column_count := 0
	for i in columns.size():
		var column_visible: bool = get_column_property(i, &"visibility", 1) != 0
		get_child(i).visible = column_visible
		for j in rows_shown:
			grid.get_child(j * columns.size() + i).visible = column_visible

		if column_visible:
			visible_column_count += 1
	
	# 同步分组头部的折叠状态
	_sync_group_collapsed_states()


func _sync_group_collapsed_states():
	var column_headers := get_children()
	for group_header in group_headers:
		var start_idx = group_header.column_start
		var end_idx = start_idx + group_header.column_count - 1
		
		# 检查分组内是否有任何可见列
		var has_visible := false
		for i in range(start_idx, min(end_idx + 1, column_headers.size())):
			if i < column_headers.size() and column_headers[i].visible:
				has_visible = true
				break
		
		# 根据可见性更新折叠状态
		group_header.is_collapsed = not has_visible
		group_header._update_label()


func _on_h_scroll_changed(value):
	position.x = - value
	if group_row:
		group_row.position.x = - value


func _on_visible_cols_about_to_popup():
	var popup := hide_columns_button.get_popup()
	popup.clear()
	popup.hide_on_checkable_item_selection = false
	for i in columns.size():
		popup.add_check_item(ColumnLabelsZh.get_label(columns[i]), i)
		popup.set_item_checked(i, get_column_property(i, &"visibility", 1) != 0)


func _on_visible_cols_id_pressed(id: int):
	var popup := hide_columns_button.get_popup()
	if popup.is_item_checked(id):
		popup.set_item_checked(id, false)
		set_column_property(id, &"visibility", 0)

	else:
		popup.set_item_checked(id, true)
		set_column_property(id, &"visibility", 1)

	editor_view.save_data()
	update()


func _build_group_headers():
	if not group_row:
		return
	
	for child in group_row.get_children():
		child.queue_free()
	
	group_headers.clear()
	
	if editor_view.column_groups.size() != columns.size():
		return
	
	var groups := []
	var current_group_name := ""
	var current_start := 0
	var current_count := 0
	
	for i in columns.size():
		var group_name: String = editor_view.column_groups[i]
		
		if group_name != current_group_name:
			if current_count > 0:
				if current_group_name != "":
					groups.append({"name": current_group_name, "start": current_start, "count": current_count})
			current_group_name = group_name
			current_start = i
			current_count = 1
		else:
			current_count += 1
	
	if current_count > 0:
		if current_group_name != "":
			groups.append({"name": current_group_name, "start": current_start, "count": current_count})
	
	for group_info in groups:
		# 跳过 Resource 相关分组（Godot 内置属性，对用户配置无意义）
		if group_info["name"].to_lower() == "resource":
			# 同时隐藏 Resource 分组的所有列
			for i in range(group_info["start"], group_info["start"] + group_info["count"]):
				if i < columns.size():
					set_column_property(i, &"visibility", 0)
			continue
		
		var group_header = GroupHeaderScene.instantiate()
		group_header.manager = self
		group_row.add_child(group_header)
		group_header.setup(group_info["name"], group_info["start"], group_info["count"])
		
		# 传递列名信息用于 tooltip
		var col_names: Array[String] = []
		for i in range(group_info["start"], group_info["start"] + group_info["count"]):
			if i < columns.size():
				col_names.append(columns[i])
		group_header.set_column_names(col_names)
		
		group_headers.append(group_header)
	
	await get_tree().process_frame
	_update_group_header_positions()


func _update_group_header_positions():
	if not group_row or group_headers.size() == 0:
		return
	
	var column_headers := get_children()
	if column_headers.size() == 0:
		return
	
	# 跟踪下一个折叠分组应该放置的位置
	var next_collapsed_pos := 0.0
	var collapsed_width := 80.0 # 折叠分组的固定宽度
	
	for group_header in group_headers:
		var start_idx = group_header.column_start
		var end_idx = start_idx + group_header.column_count - 1
		
		if start_idx >= column_headers.size():
			group_header.visible = false
			continue
		
		# 找到分组内第一个可见列
		var first_visible_idx := -1
		var last_visible_idx := -1
		for i in range(start_idx, min(end_idx + 1, column_headers.size())):
			if column_headers[i].visible:
				if first_visible_idx == -1:
					first_visible_idx = i
				last_visible_idx = i
		
		group_header.visible = true
		
		# 如果分组内没有可见列，显示折叠状态的紧凑分组头部
		if first_visible_idx == -1:
			group_header.position.x = next_collapsed_pos
			group_header.size.x = collapsed_width
			next_collapsed_pos += collapsed_width + 2 # 2px 间距
			continue
		
		# 展开状态：根据可见列计算位置
		var start_pos = column_headers[first_visible_idx].position.x
		var end_pos = column_headers[last_visible_idx].position.x + column_headers[last_visible_idx].size.x
		
		# 如果有折叠的分组在前面，需要偏移
		if next_collapsed_pos > start_pos:
			# 折叠分组占用了空间，展开分组需要在折叠分组之后
			group_header.position.x = next_collapsed_pos
		else:
			group_header.position.x = start_pos
		
		group_header.size.x = max(end_pos - start_pos, 50)
		# 更新下一个折叠分组的位置为当前展开分组的结束位置
		next_collapsed_pos = group_header.position.x + group_header.size.x + 2


func toggle_group_visibility(column_start: int, column_count: int, visible: bool):
	for i in column_count:
		var col_idx = column_start + i
		if col_idx < columns.size():
			set_column_property(col_idx, &"visibility", 1 if visible else 0)
	
	# 同步对应分组头部的折叠状态
	for group_header in group_headers:
		if group_header.column_start == column_start:
			group_header.is_collapsed = not visible
			group_header._update_label()
			break
	
	editor_view.save_data()
	update()


func show_only_group(column_start: int, column_count: int):
	for i in columns.size():
		var visible = i >= column_start and i < column_start + column_count
		set_column_property(i, &"visibility", 1 if visible else 0)
	
	for group_header in group_headers:
		group_header.is_collapsed = group_header.column_start != column_start
		group_header._update_label()
	
	editor_view.save_data()
	update()


func expand_all_groups():
	for i in columns.size():
		set_column_property(i, &"visibility", 1)
	
	for group_header in group_headers:
		group_header.is_collapsed = false
		group_header._update_label()
	
	editor_view.save_data()
	update()
