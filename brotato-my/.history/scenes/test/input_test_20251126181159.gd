extends Node2D

## 输入映射测试脚本
## 用于测试所有手柄和键盘输入是否正常工作

@onready var label: Label = $CanvasLayer/Label
@onready var vibration_label: Label = $CanvasLayer/VibrationLabel

var test_log: Array[String] = []
var max_log_lines: int = 20
var vibration_timer: float = 0.0

func _ready() -> void:
	add_log("=== 输入映射测试 ===")
	add_log("开始测试所有输入...")
	add_log("请按任意按钮测试")
	add_log("")
	update_vibration_status(0, 0, 0)


func _process(delta: float) -> void:
	if vibration_timer > 0:
		vibration_timer -= delta
		if vibration_timer <= 0:
			update_vibration_status(0, 0, 0)

	# 测试移动输入（使用 get_vector 获取模拟输入）
	var move_dir = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	if move_dir.length() > 0.1:
		add_log("移动: %.2f, %.2f" % [move_dir.x, move_dir.y])
	
	# 测试右摇杆
	var stick_right = Input.get_vector("stick_right_left", "stick_right_right", "stick_right_up", "stick_right_down")
	if stick_right.length() > 0.1:
		add_log("右摇杆: %.2f, %.2f" % [stick_right.x, stick_right.y])


func _input(event: InputEvent) -> void:
	# 测试 UI 导航
	if event.is_action_pressed("ui_up"):
		add_log("✓ UI 上")
	if event.is_action_pressed("ui_down"):
		add_log("✓ UI 下")
	if event.is_action_pressed("ui_left"):
		add_log("✓ UI 左")
	if event.is_action_pressed("ui_right"):
		add_log("✓ UI 右")
	
	# 测试面部按钮 (ABXY)
	if event.is_action_pressed("btn_a"):
		add_log("✓ A 键：确认/跳跃 (轻震动)")
		# 轻微震动：低频 0.5，持续 0.2秒
		start_vibration(0.5, 0.0, 0.2, "A键: 轻震动")
		
	if event.is_action_pressed("btn_b"):
		add_log("✓ B 键：取消/后退 (重震动)")
		# 强烈震动：高频 1.0，持续 0.5秒
		start_vibration(0.0, 1.0, 0.5, "B键: 重震动")
		
	if event.is_action_pressed("btn_x"):
		add_log("✓ X 键：攻击/菜单 (混合震动)")
		# 混合震动：低频 0.5 + 高频 0.5
		start_vibration(0.5, 0.5, 0.3, "X键: 混合震动")
		
	if event.is_action_pressed("btn_y"):
		add_log("✓ Y 键：特殊动作/物品栏")
	
	# 测试肩键
	if event.is_action_pressed("btn_lb"):
		add_log("✓ LB：左肩键 (心跳震动)")
		# 心跳震动模拟：手动调用多次震动比较复杂，这里演示长震动
		start_vibration(0.8, 0.0, 0.1, "LB: 心跳(砰)")
		
	if event.is_action_pressed("btn_rb"):
		add_log("✓ RB：右肩键 (高频震动)")
		start_vibration(0.0, 1.0, 0.1, "RB: 高频点震")
	
	# 测试扳机键
	if event.is_action_pressed("btn_lt"):
		var strength = Input.get_action_strength("btn_lt")
		add_log("✓ LT：左扳机 (力度: %.2f)" % strength)
		
	if event.is_action_pressed("btn_rt"):
		var strength = Input.get_action_strength("btn_rt")
		add_log("✓ RT：右扳机 (力度: %.2f)" % strength)
		
		# 模拟油门震动：根据扳机力度震动
		if strength > 0.1:
			start_vibration(strength * 0.5, strength, 0.1, "RT: 扳机反馈")
	
	# 测试系统按钮
	if event.is_action_pressed("btn_start"):
		add_log("✓ Start：暂停/菜单")
	if event.is_action_pressed("btn_select"):
		add_log("✓ Select：地图/信息")


func add_log(message: String) -> void:
	# 添加新消息到数组开头（最新消息在顶部）
	test_log.push_front(message)
	
	# 限制日志行数，删除最旧的（最后一行）
	if test_log.size() > max_log_lines:
		test_log.pop_back()
	
	# 更新显示
	update_label()


func update_label() -> void:
	if label:
		label.text = "\n".join(test_log)


func _notification(what: int) -> void:
	# 检测手柄连接状态
	if what == NOTIFICATION_WM_WINDOW_FOCUS_IN:
		check_joypad_connection()


func check_joypad_connection() -> void:
	var joypads = Input.get_connected_joypads()
	if joypads.size() > 0:
		for joypad_id in joypads:
			var joypad_name = Input.get_joy_name(joypad_id)
			add_log("手柄已连接: %s (ID: %d)" % [joypad_name, joypad_id])
	else:
		add_log("未检测到手柄")


func start_vibration(weak: float, strong: float, duration: float, desc: String = "") -> void:
	Input.start_joy_vibration(0, weak, strong, duration)
	vibration_timer = duration
	update_vibration_status(weak, strong, duration, desc)


func update_vibration_status(weak: float, strong: float, duration: float, desc: String = "") -> void:
	if vibration_label:
		if duration > 0:
			vibration_label.text = "震动状态: %s\n低频强度: %.2f\n高频强度: %.2f\n剩余时间: %.2f s" % [desc, weak, strong, duration]
			vibration_label.add_theme_color_override("font_color", Color(1, 0.2, 0.2)) # 红色表示震动中
		else:
			vibration_label.text = "震动状态: 无\n低频强度: 0.00\n高频强度: 0.00\n等待触发..."
			vibration_label.add_theme_color_override("font_color", Color(1, 0.5, 0)) # 橙色表示待机
