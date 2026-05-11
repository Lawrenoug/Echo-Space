using EchoSpace.Player;
using Godot;

namespace EchoSpace.Gameplay.Inventory;

public partial class InventoryPickup : Area2D
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public string ItemId { get; set; } = "field_supply";
    [Export] public string DisplayName { get; set; } = "Field Supply";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "Prototype pickup item.";
    [Export] public ItemCategory Category { get; set; } = ItemCategory.Consumable;
    [Export] public int Quantity { get; set; } = 1;
    [Export] public int MaxStack { get; set; } = 9;
    [Export] public bool IsUnique { get; set; }
    [Export] public Color PickupColor { get; set; } = new(0.6f, 0.9f, 1f, 1f);
    [Export] public float BobAmplitude { get; set; } = 4f;
    [Export] public float BobSpeed { get; set; } = 3.4f;

    [ExportGroup("Use Effect")]
    [Export] public int HealthRestore { get; set; } = 1;
    [Export] public float StaminaRestore { get; set; } = 25f;
    [Export] public int ProgressionPointsGranted { get; set; }

    private Polygon2D? _visual;
    private Vector2 _visualBasePosition;

    public override void _Ready()
    {
        _visual = VisualPath != null && !VisualPath.IsEmpty ? GetNodeOrNull<Polygon2D>(VisualPath) : null;
        _visualBasePosition = _visual?.Position ?? Vector2.Zero;

        if (_visual != null)
        {
            _visual.Color = PickupColor;
        }

        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        if (_visual == null)
        {
            return;
        }

        var bob = Mathf.Sin((float)(Time.GetTicksMsec() / 1000.0 * BobSpeed)) * BobAmplitude;
        _visual.Position = _visualBasePosition + new Vector2(0f, bob);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController || InventoryManager.Instance == null)
        {
            return;
        }

        var remaining = InventoryManager.Instance.AddItem(CreateItemDefinition(), Mathf.Max(1, Quantity));
        if (remaining <= 0)
        {
            QueueFree();
        }
    }

    private ItemDefinition CreateItemDefinition()
    {
        return new ItemDefinition
        {
            ItemId = ItemId,
            DisplayName = DisplayName,
            Description = Description,
            Category = Category,
            MaxStack = MaxStack,
            IsUnique = IsUnique,
            IsUsable = HealthRestore > 0 || StaminaRestore > 0f || ProgressionPointsGranted > 0,
            HealthRestore = HealthRestore,
            StaminaRestore = StaminaRestore,
            ProgressionPointsGranted = ProgressionPointsGranted,
        };
    }
}
