using Godot;

namespace EchoSpace.Gameplay.Inventory;

[GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string ItemId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public ItemCategory Category { get; set; } = ItemCategory.Material;
    [Export] public int MaxStack { get; set; } = 1;
    [Export] public bool IsUnique { get; set; }
    [Export] public Texture2D? Icon { get; set; }
    [ExportGroup("Use Effect")]
    [Export] public bool IsUsable { get; set; }
    [Export] public int HealthRestore { get; set; }
    [Export] public float StaminaRestore { get; set; }
    [Export] public int ProgressionPointsGranted { get; set; }

    public int SafeMaxStack => Mathf.Max(1, MaxStack);
    public bool IsProtectedKeyItem => Category is ItemCategory.KeyItem or ItemCategory.Quest;
    public bool HasUseEffect => IsUsable || HealthRestore > 0 || StaminaRestore > 0f || ProgressionPointsGranted > 0;
}
