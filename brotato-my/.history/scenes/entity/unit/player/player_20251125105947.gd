extends CharacterBody2D

@onready var animation_player: AnimationPlayer = $Animation/AnimationPlayer


func _ready() -> void:
	print(animation_player.get_animation_list())
	animation_player.play("animations/Idle")


func _process(delta: float) -> void:
	pass
