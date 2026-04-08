using Godot;

namespace EchoSpace.Core.Input;

public static class GameInputActions
{
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Jump = "jump";
    public const string Attack = "attack";
    public const string Guard = "guard";
    public const string SwitchWorld = "switch_world";

    public static void EnsureDefaults()
    {
        EnsureAction(MoveLeft, Key.A, Key.Left);
        EnsureAction(MoveRight, Key.D, Key.Right);
        EnsureAction(Jump, Key.Space, Key.W, Key.Up);
        EnsureAction(Attack, Key.J, Key.K);
        EnsureAction(Guard, Key.L, Key.Shift);
        EnsureAction(SwitchWorld, Key.Tab);
    }

    private static void EnsureAction(string actionName, params Key[] keys)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        if (InputMap.ActionGetEvents(actionName).Count > 0)
        {
            return;
        }

        foreach (var key in keys)
        {
            var inputEvent = new InputEventKey
            {
                PhysicalKeycode = key,
            };

            InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }
}
