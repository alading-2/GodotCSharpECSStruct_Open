extends CharacterBody2D

@onready var animation_player: AnimationPlayer = $Animation/AnimationPlayer


func _ready() -> void:
	print("=== 动画调试信息 ===")
	print("AnimationPlayer 节点: ", animation_player)
	print("可用的动画列表: ", animation_player.get_animation_list())
	print("当前播放的动画: ", animation_player.current_animation)
	print("是否正在播放: ", animation_player.is_playing())
	
	animation_player.play("animations/Idle")
	
	print("--- 播放后 ---")
	print("当前播放的动画: ", animation_player.current_animation)
	print("是否正在播放: ", animation_player.is_playing())
	print("动画长度: ", animation_player.current_animation_length)
	print("动画位置: ", animation_player.current_animation_position)


func _process(delta: float) -> void:
	pass
