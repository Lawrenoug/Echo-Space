using System;
using System.Collections.Generic;
using Godot;

namespace EchoSpace.Gameplay.Inventory;

public partial class InventoryManager : Node
{
    public event Action? InventoryChanged;
    public event Action<int, InventorySlot>? SlotChanged;
    public event Action<ItemDefinition, int>? ItemAdded;
    public event Action<ItemDefinition, int>? ItemRemoved;

    [Export] public int Capacity { get; set; } = 24;

    private readonly List<InventorySlot> _slots = new();
    private bool _isInitialized;

    public IReadOnlyList<InventorySlot> Slots => _slots;

    public override void _Ready()
    {
        EnsureInitialized();
    }

    public bool TryAddItem(ItemDefinition item, int quantity = 1)
    {
        return AddItem(item, quantity) == 0;
    }

    public int AddItem(ItemDefinition item, int quantity = 1)
    {
        EnsureInitialized();

        if (item == null || quantity <= 0)
        {
            return quantity;
        }

        var remaining = quantity;

        for (var index = 0; index < _slots.Count && remaining > 0; index++)
        {
            var slot = _slots[index];
            if (!slot.CanAccept(item) || slot.IsEmpty)
            {
                continue;
            }

            var before = slot.Quantity;
            remaining = slot.Add(item, remaining);

            if (slot.Quantity != before)
            {
                SlotChanged?.Invoke(index, slot);
            }
        }

        for (var index = 0; index < _slots.Count && remaining > 0; index++)
        {
            var slot = _slots[index];
            if (!slot.IsEmpty)
            {
                continue;
            }

            remaining = slot.Add(item, remaining);
            SlotChanged?.Invoke(index, slot);
        }

        var added = quantity - remaining;
        if (added > 0)
        {
            ItemAdded?.Invoke(item, added);
            InventoryChanged?.Invoke();
        }

        return remaining;
    }

    public bool RemoveItem(ItemDefinition item, int quantity = 1)
    {
        EnsureInitialized();

        if (item == null || quantity <= 0 || GetItemCount(item) < quantity)
        {
            return false;
        }

        var remaining = quantity;

        for (var index = 0; index < _slots.Count && remaining > 0; index++)
        {
            var slot = _slots[index];
            if (slot.IsEmpty || !slot.Matches(item))
            {
                continue;
            }

            var removed = slot.Remove(remaining);
            remaining -= removed;
            SlotChanged?.Invoke(index, slot);
        }

        ItemRemoved?.Invoke(item, quantity);
        InventoryChanged?.Invoke();
        return true;
    }

    public int GetItemCount(ItemDefinition item)
    {
        EnsureInitialized();

        if (item == null)
        {
            return 0;
        }

        var total = 0;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.Matches(item))
            {
                total += slot.Quantity;
            }
        }

        return total;
    }

    public bool HasItem(ItemDefinition item, int quantity = 1)
    {
        return GetItemCount(item) >= quantity;
    }

    public void ClearInventory()
    {
        EnsureInitialized();

        for (var index = 0; index < _slots.Count; index++)
        {
            _slots[index].Clear();
            SlotChanged?.Invoke(index, _slots[index]);
        }

        InventoryChanged?.Invoke();
    }

    public Dictionary<string, int> BuildCountSnapshot()
    {
        EnsureInitialized();

        var snapshot = new Dictionary<string, int>();

        foreach (var slot in _slots)
        {
            if (slot.IsEmpty || slot.Item == null)
            {
                continue;
            }

            var key = string.IsNullOrWhiteSpace(slot.Item.ItemId)
                ? slot.Item.ResourcePath
                : slot.Item.ItemId;

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            snapshot.TryGetValue(key, out var current);
            snapshot[key] = current + slot.Quantity;
        }

        return snapshot;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _slots.Clear();
        for (var index = 0; index < Mathf.Max(1, Capacity); index++)
        {
            _slots.Add(new InventorySlot());
        }

        _isInitialized = true;
    }
}
