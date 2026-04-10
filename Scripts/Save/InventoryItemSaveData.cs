using EchoSpace.Gameplay.Inventory;

namespace EchoSpace.Save;

public sealed class InventoryItemSaveData
{
    public string ItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemCategory Category { get; set; } = ItemCategory.Material;
    public int MaxStack { get; set; } = 1;
    public bool IsUnique { get; set; }
    public int Quantity { get; set; }
}
