extends CharacterBody2D

@onready var animation_player: AnimationPlayer = $Animation/AnimationPlayer


func _ready() -> void:
	animation_player.play("Idle")


func _process(delta: float) -> void:
	pass
