using Godot;

public partial class Hud : CanvasLayer
{
	private Label _label;
	private Player _player;

	public override void _Ready()
	{
		_label = GetNode<Label>("Control/ColorRect/Label");
		_player = GetParent<Player>();
	}

	public override void _Process(double delta)
	{
		_label.Text = _player.Health.ToString();
	}
}