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
    public const string ToggleInventory = "toggle_inventory";
    public const string ToggleProgression = "toggle_progression";

    public static void EnsureDefaults()
    {
        EnsureAction(MoveLeft, Key.A, Key.Left);
        EnsureAction(MoveRight, Key.D, Key.Right);
        EnsureAction(Jump, Key.Space, Key.W, Key.Up);
        ResetAction(Attack);
        ResetAction(Guard);
        EnsureMouseAction(Attack, MouseButton.Left);
        EnsureMouseAction(Guard, MouseButton.Right);
        EnsureAction(SwitchWorld, Key.Tab);
        EnsureAction(ToggleInventory, Key.I);
        EnsureAction(ToggleProgression, Key.P);
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

    private static void EnsureMouseAction(string actionName, MouseButton button)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        var inputEvent = new InputEventMouseButton
        {
            ButtonIndex = button,
        };

        InputMap.ActionAddEvent(actionName, inputEvent);
    }

    private static void ResetAction(string actionName)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
            return;
        }

        foreach (var existingEvent in InputMap.ActionGetEvents(actionName))
        {
            InputMap.ActionEraseEvent(actionName, existingEvent);
        }
    }
}
