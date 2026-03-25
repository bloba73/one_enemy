using Godot;

public partial class HUD : CanvasLayer
{
	private Label _label;
	private Player _player;

	public override void _Ready()
	{
		_label = GetNode<Label>("Control/Label");
		_player = GetParent<Player>();
	}

	public override void _Process(double delta)
	{
		_label.Text = _player.Health.ToString();
	}
}