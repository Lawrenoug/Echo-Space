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
        EnsureAction(Attack);
        EnsureAction(Guard);
        EnsureMouseActionIfEmpty(Attack, MouseButton.Left);
        EnsureMouseActionIfEmpty(Guard, MouseButton.Right);
        EnsureAction(SwitchWorld, Key.Tab);
        EnsureAction(ToggleInventory, Key.I);
        EnsureAction(ToggleProgression, Key.P);
    }

    public static void ApplyBindingPreset(bool includeKeyboardCombatAlternative)
    {
        ResetAction(MoveLeft);
        AddKeyAction(MoveLeft, Key.A);
        AddKeyAction(MoveLeft, Key.Left);

        ResetAction(MoveRight);
        AddKeyAction(MoveRight, Key.D);
        AddKeyAction(MoveRight, Key.Right);

        ResetAction(Jump);
        AddKeyAction(Jump, Key.Space);
        AddKeyAction(Jump, Key.W);
        AddKeyAction(Jump, Key.Up);

        ResetAction(Attack);
        AddMouseAction(Attack, MouseButton.Left);

        ResetAction(Guard);
        AddMouseAction(Guard, MouseButton.Right);

        if (includeKeyboardCombatAlternative)
        {
            AddKeyAction(Attack, Key.J);
            AddKeyAction(Guard, Key.K);
        }

        ResetAction(SwitchWorld);
        AddKeyAction(SwitchWorld, Key.Tab);

        ResetAction(ToggleInventory);
        AddKeyAction(ToggleInventory, Key.I);

        ResetAction(ToggleProgression);
        AddKeyAction(ToggleProgression, Key.P);
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

    private static void EnsureMouseActionIfEmpty(string actionName, MouseButton button)
    {
        if (InputMap.ActionGetEvents(actionName).Count > 0)
        {
            return;
        }

        AddMouseAction(actionName, button);
    }

    private static void AddKeyAction(string actionName, Key key)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        var inputEvent = new InputEventKey
        {
            PhysicalKeycode = key,
        };

        InputMap.ActionAddEvent(actionName, inputEvent);
    }

    private static void AddMouseAction(string actionName, MouseButton button)
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
