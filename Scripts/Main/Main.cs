using EchoSpace.Core.Input;
using Godot;

namespace EchoSpace.Main;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		GameInputActions.EnsureDefaults();
	}
}
