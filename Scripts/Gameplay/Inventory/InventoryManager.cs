using System;
using System.Collections.Generic;
using Godot;

namespace EchoSpace.Gameplay.Inventory;

public partial class InventoryManager : Node
{
    public static InventoryManager? Instance { get; private set; }

    public event Action? InventoryChanged;
    public event Action<int, InventorySlot>? SlotChanged;
    public event Action<ItemDefinition, int>? ItemAdded;
    public event Action<ItemDefinition, int>? ItemRemoved;

    [Export] public int Capacity { get; set; } = 24;
    [Export] public bool SeedDebugItems { get; set; } = true;

    private readonly List<InventorySlot> _slots = new();
    private bool _isInitialized;

    public IReadOnlyList<InventorySlot> Slots => _slots;

    public override void _Ready()
    {
        Instance = this;
        EnsureInitialized();
        SeedPrototypeItemsIfNeeded();
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
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

    public void ResetToDefaults()
    {
        EnsureInitialized();
        ClearInventory();
        SeedPrototypeItemsIfNeeded();
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

    private void SeedPrototypeItemsIfNeeded()
    {
        if (!SeedDebugItems)
        {
            return;
        }

        var hasItems = false;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty)
            {
                hasItems = true;
                break;
            }
        }

        if (hasItems)
        {
            return;
        }

        AddItem(CreatePrototypeItem("healing_flask", "治疗药瓶", "恢复用的原型道具。", ItemCategory.Consumable, 9), 3);
        AddItem(CreatePrototypeItem("spirit_ore", "灵魂矿石", "后续可用于强化或交换。", ItemCategory.Material, 99), 12);
        AddItem(CreatePrototypeItem("old_key", "旧钥匙", "用于测试背包与关键道具显示。", ItemCategory.KeyItem, 1, true), 1);
    }

    private static ItemDefinition CreatePrototypeItem(
        string itemId,
        string displayName,
        string description,
        ItemCategory category,
        int maxStack,
        bool isUnique = false)
    {
        return new ItemDefinition
        {
            ItemId = itemId,
            DisplayName = displayName,
            Description = description,
            Category = category,
            MaxStack = maxStack,
            IsUnique = isUnique,
        };
    }

}
