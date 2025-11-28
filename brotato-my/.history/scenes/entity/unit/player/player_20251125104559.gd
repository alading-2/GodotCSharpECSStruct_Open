extends CharacterBody2D

@onready var animation: Node2D = $Animation


func _ready() -> void:
	animation.get_child(AnimationPlayer).play("Idle")


func _process(delta: float) -> void:
	pass
