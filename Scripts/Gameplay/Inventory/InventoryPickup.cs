using EchoSpace.Player;
using Godot;

namespace EchoSpace.Gameplay.Inventory;

public partial class InventoryPickup : Area2D
{
	[Export] public string ItemId { get; set; } = "field_supply";
	[Export] public string DisplayName { get; set; } = "野外补给";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "拾取后加入背包的原型物品。";
	[Export] public ItemCategory Category { get; set; } = ItemCategory.Consumable;
	[Export] public int Quantity { get; set; } = 1;
	[Export] public int MaxStack { get; set; } = 9;
	[Export] public bool IsUnique { get; set; }
	[ExportGroup("Use Effect")]
	[Export] public int HealthRestore { get; set; } = 1;
	[Export] public float StaminaRestore { get; set; } = 25f;
	[Export] public int ProgressionPointsGranted { get; set; }

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _ExitTree()
	{
		BodyEntered -= OnBodyEntered;
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
