using EchoSpace.Gameplay.Inventory;
using Godot;

namespace EchoSpace.Gameplay.Equipment;

public partial class WeaponModelAnchor : Node2D
{
    [Export] public EquipmentSlotType SlotType { get; set; } = EquipmentSlotType.Weapon;
    [Export] public bool AutoFollowParentFacing { get; set; } = true;

    private Node2D? _spawnedModel;
    private CanvasItem? _parentVisual;
    private bool _lastFlipH;

    public override void _Ready()
    {
        _parentVisual = ResolveFacingSource();

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.SlotChanged += OnEquipmentSlotChanged;
        }

        RefreshFromEquipment();
    }

    public override void _ExitTree()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.SlotChanged -= OnEquipmentSlotChanged;
        }
    }

    public override void _Process(double delta)
    {
        if (!AutoFollowParentFacing || _parentVisual == null)
        {
            return;
        }

        var parentFlipH = ResolveParentFlip();
        if (parentFlipH == _lastFlipH)
        {
            return;
        }

        _lastFlipH = parentFlipH;
        Scale = new Vector2(Mathf.Abs(Scale.X) * (parentFlipH ? -1f : 1f), Scale.Y);
    }

    private void RefreshFromEquipment()
    {
        var equippedItem = EquipmentManager.Instance?.GetEquippedItem(SlotType);
        ApplyItemModel(equippedItem);
    }

    private void OnEquipmentSlotChanged(EquipmentSlotType slotType, ItemDefinition? item)
    {
        if (slotType != SlotType)
        {
            return;
        }

        ApplyItemModel(item);
    }

    private void ApplyItemModel(ItemDefinition? item)
    {
        _spawnedModel?.QueueFree();
        _spawnedModel = null;

        if (item?.EquippedModelScene == null)
        {
            Visible = false;
            return;
        }

        var modelInstance = item.EquippedModelScene.Instantiate<Node2D>();
        AddChild(modelInstance);
        _spawnedModel = modelInstance;
        Visible = true;

        _lastFlipH = ResolveParentFlip();
        Scale = new Vector2(Mathf.Abs(Scale.X) * (_lastFlipH ? -1f : 1f), Scale.Y);
    }

    private bool ResolveParentFlip()
    {
        return _parentVisual is Sprite2D sprite && sprite.FlipH
            || _parentVisual is AnimatedSprite2D animatedSprite && animatedSprite.FlipH;
    }

    private CanvasItem? ResolveFacingSource()
    {
        var current = GetParent();
        while (current != null)
        {
            if (current is AnimatedSprite2D animatedSprite)
            {
                return animatedSprite;
            }

            if (current is Sprite2D sprite)
            {
                return sprite;
            }

            current = current.GetParent();
        }

        var owner = GetOwner();
        if (owner != null)
        {
            return owner.FindChild("AnimatedSprite", true, false) as CanvasItem
                ?? owner.FindChild("Body", true, false) as CanvasItem;
        }

        return null;
    }
}
