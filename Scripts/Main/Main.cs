using EchoSpace.Core.Input;
using EchoSpace.Core.Settings;
using Godot;

namespace EchoSpace.Main;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		GameInputActions.EnsureDefaults();
		GameSettingsManager.Instance?.ApplyAll();
	}
}
