using EchoSpace.Gameplay.Inventory;

namespace EchoSpace.Gameplay.Equipment;

public sealed class EquippedItemState
{
    public EquippedItemState(EquipmentSlotType slotType, ItemDefinition item)
    {
        SlotType = slotType;
        Item = item;
    }

    public EquipmentSlotType SlotType { get; }
    public ItemDefinition Item { get; }
}
