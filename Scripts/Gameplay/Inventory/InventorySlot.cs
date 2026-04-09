using System;

namespace EchoSpace.Gameplay.Inventory;

public sealed class InventorySlot
{
    public ItemDefinition? Item { get; private set; }
    public int Quantity { get; private set; }

    public bool IsEmpty => Item == null || Quantity <= 0;

    public bool CanAccept(ItemDefinition item)
    {
        if (IsEmpty)
        {
            return true;
        }

        return Matches(item) && Quantity < item.SafeMaxStack;
    }

    public int Add(ItemDefinition item, int quantity)
    {
        if (quantity <= 0)
        {
            return 0;
        }

        if (!CanAccept(item))
        {
            return quantity;
        }

        Item ??= item;
        var capacity = item.SafeMaxStack - Quantity;
        var accepted = Math.Min(capacity, quantity);
        Quantity += accepted;
        return quantity - accepted;
    }

    public int Remove(int quantity)
    {
        if (IsEmpty || quantity <= 0)
        {
            return 0;
        }

        var removed = Math.Min(Quantity, quantity);
        Quantity -= removed;

        if (Quantity <= 0)
        {
            Clear();
        }

        return removed;
    }

    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }

    public bool Matches(ItemDefinition item)
    {
        if (Item == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Item.ItemId) && !string.IsNullOrWhiteSpace(item.ItemId))
        {
            return string.Equals(Item.ItemId, item.ItemId, StringComparison.Ordinal);
        }

        return ReferenceEquals(Item, item);
    }
}
