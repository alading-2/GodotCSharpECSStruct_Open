extends CharacterBody2D

@onready var animation: Node2D = $Animation



# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	animation.get_child(AnimationPlayer).play


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
