using Godot;

public partial class Player : CharacterBody2D
{
    private static readonly Log _log = new("Player");

    public override void _Ready()
    {
        base._Ready();
        _log.Info("Player initialized with data from resources.");
    }
}
