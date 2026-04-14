using System;
using System.Collections.Generic;
using EchoSpace.Gameplay.Progression;
using EchoSpace.Player;
using Godot;

namespace EchoSpace.Gameplay.Inventory;

public partial class InventoryManager : Node
{
    public static InventoryManager? Instance { get; private set; }

    public event Action? InventoryChanged;
    public event Action<int, InventorySlot>? SlotChanged;
    public event Action<ItemDefinition, int>? ItemAdded;
    public event Action<ItemDefinition, int>? ItemRemoved;
    public event Action<ItemDefinition>? ItemUsed;
    public event Action<ItemDefinition, int>? ItemDropped;

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

    public bool HasKeyItem(string itemId)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        foreach (var slot in _slots)
        {
            if (slot.Item == null || slot.Item.Category != ItemCategory.KeyItem)
            {
                continue;
            }

            if (string.Equals(slot.Item.ItemId, itemId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryUseSlot(int slotIndex, PlayerController? player)
    {
        EnsureInitialized();

        if (!TryGetOccupiedSlot(slotIndex, out var slot) || slot.Item == null || !slot.Item.HasUseEffect)
        {
            return false;
        }

        var item = slot.Item;
        var hadEffect = ApplyItemEffect(item, player);
        if (!hadEffect)
        {
            return false;
        }

        slot.Remove(1);
        SlotChanged?.Invoke(slotIndex, slot);
        ItemUsed?.Invoke(item);
        ItemRemoved?.Invoke(item, 1);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryUseFirstUsable(PlayerController? player)
    {
        EnsureInitialized();

        for (var index = 0; index < _slots.Count; index++)
        {
            if (_slots[index].Item?.HasUseEffect == true && TryUseSlot(index, player))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryDropSlot(int slotIndex, int quantity = 1)
    {
        EnsureInitialized();

        if (!TryGetOccupiedSlot(slotIndex, out var slot) || slot.Item == null || quantity <= 0)
        {
            return false;
        }

        var item = slot.Item;
        if (item.IsProtectedKeyItem)
        {
            return false;
        }

        var removed = slot.Remove(quantity);
        if (removed <= 0)
        {
            return false;
        }

        SlotChanged?.Invoke(slotIndex, slot);
        ItemDropped?.Invoke(item, removed);
        ItemRemoved?.Invoke(item, removed);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryDropFirstDroppable(int quantity = 1)
    {
        EnsureInitialized();

        for (var index = 0; index < _slots.Count; index++)
        {
            if (_slots[index].Item?.IsProtectedKeyItem == false && TryDropSlot(index, quantity))
            {
                return true;
            }
        }

        return false;
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

        AddItem(CreatePrototypeItem("healing_flask", "治疗药瓶", "恢复 2 点生命和 35 点耐力的原型道具。", ItemCategory.Consumable, 9, healthRestore: 2, staminaRestore: 35f), 3);
        AddItem(CreatePrototypeItem("spirit_ore", "灵魂矿石", "后续可用于强化或交换。", ItemCategory.Material, 99), 12);
        AddItem(CreatePrototypeItem("old_key", "旧钥匙", "用于测试背包与关键道具显示。", ItemCategory.KeyItem, 1, true), 1);
    }

    private static ItemDefinition CreatePrototypeItem(
        string itemId,
        string displayName,
        string description,
        ItemCategory category,
        int maxStack,
        bool isUnique = false,
        int healthRestore = 0,
        float staminaRestore = 0f,
        int progressionPointsGranted = 0)
    {
        return new ItemDefinition
        {
            ItemId = itemId,
            DisplayName = displayName,
            Description = description,
            Category = category,
            MaxStack = maxStack,
            IsUnique = isUnique,
            IsUsable = healthRestore > 0 || staminaRestore > 0f || progressionPointsGranted > 0,
            HealthRestore = healthRestore,
            StaminaRestore = staminaRestore,
            ProgressionPointsGranted = progressionPointsGranted,
        };
    }

    private bool TryGetOccupiedSlot(int slotIndex, out InventorySlot slot)
    {
        slot = null!;

        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return false;
        }

        slot = _slots[slotIndex];
        return !slot.IsEmpty;
    }

    private static bool ApplyItemEffect(ItemDefinition item, PlayerController? player)
    {
        var hadEffect = false;

        if (player != null)
        {
            if (item.HealthRestore > 0)
            {
                hadEffect |= player.RestoreHealth(item.HealthRestore);
            }

            if (item.StaminaRestore > 0f)
            {
                hadEffect |= player.RestoreStamina(item.StaminaRestore);
            }
        }

        if (item.ProgressionPointsGranted > 0)
        {
            ProgressionManager.Instance?.GrantPoints(item.ProgressionPointsGranted);
            hadEffect = true;
        }

        return hadEffect;
    }
}
