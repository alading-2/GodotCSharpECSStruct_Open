@tool
extends Control

const ColumnLabelsZh := preload("res://addons/resources_spreadsheet_view/column_labels_zh.gd")

var group_name: String = ""
var column_start: int = 0
var column_count: int = 0
var is_collapsed: bool = false
var column_names: Array[String] = [] # 存储分组包含的列名

var manager: Control
var label: Label


func _ready():
	label = $HBoxContainer/Label
	# 整个头部可点击
	gui_input.connect(_on_gui_input)
	mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND


func setup(p_group_name: String, p_column_start: int, p_column_count: int):
	group_name = p_group_name
	column_start = p_column_start
	column_count = p_column_count
	
	_update_label()


func set_column_names(names: Array[String]):
	column_names = names
	_update_tooltip()


func _update_label():
	if not label:
		return
	
	var display_name := ColumnLabelsZh.get_label(group_name) if group_name != "" else "其他"
	var arrow := "▶" if is_collapsed else "▼"
	label.text = "%s %s (%d)" % [arrow, display_name, column_count]


func _update_tooltip():
	if not label:
		return
	
	if column_names.size() > 0:
		var names_str := ""
		for col_name in column_names:
			names_str += "\n  • " + ColumnLabelsZh.get_label(col_name)
		label.tooltip_text = "包含 %d 列:%s" % [column_count, names_str]
	else:
		label.tooltip_text = "包含 %d 列" % column_count


func _on_gui_input(event: InputEvent):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_toggle()
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			_show_context_menu()


func _toggle():
	is_collapsed = not is_collapsed
	_update_label()
	
	if manager:
		manager.toggle_group_visibility(column_start, column_count, not is_collapsed)


func _show_context_menu():
	var popup := PopupMenu.new()
	add_child(popup)
	popup.add_item("仅显示此组", 0)
	popup.add_item("隐藏此组", 1)
	popup.add_separator()
	popup.add_item("全部展开", 2)
	popup.id_pressed.connect(_on_context_menu_id_pressed.bind(popup))
	popup.position = get_global_mouse_position()
	popup.popup()


func _on_context_menu_id_pressed(id: int, popup: PopupMenu):
	match id:
		0:
			if manager:
				manager.show_only_group(column_start, column_count)
		1:
			if manager:
				manager.toggle_group_visibility(column_start, column_count, false)
				is_collapsed = true
				_update_label()
		2:
			if manager:
				manager.expand_all_groups()
	
	popup.queue_free()
