using System;
using System.Collections.Generic;
using EchoSpace.Gameplay.Inventory;
using Godot;

namespace EchoSpace.Gameplay.Equipment;

public partial class EquipmentManager : Node
{
    public static EquipmentManager? Instance { get; private set; }

    public event Action? EquipmentChanged;
    public event Action<EquipmentSlotType, ItemDefinition?>? SlotChanged;

    private readonly Dictionary<EquipmentSlotType, EquippedItemState> _equippedItems = new();

    public override void _Ready()
    {
        Instance = this;
        ResetToDefaults();
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public IReadOnlyDictionary<EquipmentSlotType, EquippedItemState> EquippedItems => _equippedItems;

    public ItemDefinition? GetEquippedItem(EquipmentSlotType slotType)
    {
        return _equippedItems.TryGetValue(slotType, out var state) ? state.Item : null;
    }

    public bool IsEquipped(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        foreach (var state in _equippedItems.Values)
        {
            if (string.Equals(state.Item.ItemId, itemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool CanEquip(ItemDefinition? item)
    {
        return item != null && item.Category == ItemCategory.Equipment && item.EquipmentSlot != EquipmentSlotType.None;
    }

    public bool TryEquip(ItemDefinition? item)
    {
        if (!CanEquip(item) || item == null)
        {
            return false;
        }

        _equippedItems[item.EquipmentSlot] = new EquippedItemState(item.EquipmentSlot, item);
        SlotChanged?.Invoke(item.EquipmentSlot, item);
        EquipmentChanged?.Invoke();
        return true;
    }

    public bool TryEquipByItemId(string? itemId)
    {
        if (InventoryManager.Instance == null || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        var item = InventoryManager.Instance.FindFirstItemById(itemId);
        return TryEquip(item);
    }

    public bool Unequip(EquipmentSlotType slotType)
    {
        if (!_equippedItems.Remove(slotType))
        {
            return false;
        }

        SlotChanged?.Invoke(slotType, null);
        EquipmentChanged?.Invoke();
        return true;
    }

    public void ClearAll()
    {
        if (_equippedItems.Count == 0)
        {
            return;
        }

        var affectedSlots = new List<EquipmentSlotType>(_equippedItems.Keys);
        _equippedItems.Clear();
        foreach (var slotType in affectedSlots)
        {
            SlotChanged?.Invoke(slotType, null);
        }

        EquipmentChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        ClearAll();
        TryEquipByItemId("training_blade");
    }
}
