extends CharacterBody2D

@onready var animation: Node2D = $Animation
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
	
	print("\n=== 节点可见性调试 ===")
	print("Animation 节点: ", animation)
	print("Animation 是否可见: ", animation.visible)
	print("Animation 缩放: ", animation.scale)
	print("Animation 位置: ", animation.position)
	print("Animation 子节点数量: ", animation.get_child_count())
	
	# 检查 Skeleton2D 和 Sprite
	if animation.has_node("Skeleton2D"):
		var skeleton = animation.get_node("Skeleton2D")
		print("Skeleton2D 可见: ", skeleton.visible)
		print("Skeleton2D 缩放: ", skeleton.scale)


func _process(delta: float) -> void:
	pass
