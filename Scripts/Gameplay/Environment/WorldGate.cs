using System.Collections.Generic;
using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class WorldGate : Node2D, IWorldActivationTarget
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public NodePath? CollisionShapePath { get; set; } = new("StaticBody2D/CollisionShape2D");
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public float GateWidth { get; set; } = 72f;
    [Export] public float GateHeight { get; set; } = 168f;
    [Export] public float OpenOffsetY { get; set; } = -108f;
    [Export] public bool StartsOpenInReality { get; set; }
    [Export] public bool StartsOpenInSoul { get; set; }
    [Export] public Color ClosedColor { get; set; } = new(0.72f, 0.32f, 0.22f, 1f);
    [Export] public Color OpenColor { get; set; } = new(0.46f, 0.84f, 0.62f, 0.78f);
    [ExportGroup("World State")]
    [Export] public bool ExistsInReality { get; set; } = true;
    [Export] public bool ExistsInSoul { get; set; } = true;

    private readonly Dictionary<WorldType, bool> _openStates = new();

    private Polygon2D? _visual;
    private CollisionShape2D? _collisionShape;
    private Core.World.DualWorldObject? _dualWorldObject;
    private Vector2 _visualBasePosition;

    public override void _Ready()
    {
        _visual = VisualPath != null && !VisualPath.IsEmpty ? GetNodeOrNull<Polygon2D>(VisualPath) : null;
        _collisionShape = CollisionShapePath != null && !CollisionShapePath.IsEmpty ? GetNodeOrNull<CollisionShape2D>(CollisionShapePath) : null;
        _dualWorldObject = DualWorldObjectPath != null && !DualWorldObjectPath.IsEmpty
            ? GetNodeOrNull<Core.World.DualWorldObject>(DualWorldObjectPath)
            : null;

        ApplyWorldDefaults();
        _openStates[WorldType.Reality] = StartsOpenInReality;
        _openStates[WorldType.Soul] = StartsOpenInSoul;
        _visualBasePosition = _visual?.Position ?? Vector2.Zero;

        ApplyGeometry();

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            ApplyState(WorldManager.Instance.CurrentWorld);
        }
        else
        {
            ApplyState(WorldType.Reality);
        }
    }

    public override void _ExitTree()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= OnWorldChanged;
        }
    }

    public void SetActivatedForWorld(bool isActivated, WorldType worldType)
    {
        _openStates[worldType] = isActivated;

        if ((WorldManager.Instance?.CurrentWorld ?? WorldType.Reality) == worldType)
        {
            ApplyState(worldType);
        }
    }

    private void OnWorldChanged(WorldType worldType)
    {
        ApplyState(worldType);
    }

    private void ApplyGeometry()
    {
        var width = Mathf.Max(32f, GateWidth);
        var height = Mathf.Max(72f, GateHeight);
        var halfWidth = width * 0.5f;
        var halfHeight = height * 0.5f;

        if (_visual != null)
        {
            _visual.Polygon = new[]
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(-halfWidth, halfHeight),
            };
        }

        if (_collisionShape?.Shape is not RectangleShape2D rectangleShape)
        {
            rectangleShape = new RectangleShape2D();
            if (_collisionShape != null)
            {
                _collisionShape.Shape = rectangleShape;
            }
        }

        if (_collisionShape?.Shape is RectangleShape2D shape)
        {
            shape.Size = new Vector2(width, height);
        }
    }

    private void ApplyWorldDefaults()
    {
        if (_dualWorldObject == null)
        {
            return;
        }

        _dualWorldObject.ExistsInReality = ExistsInReality;
        _dualWorldObject.ExistsInSoul = ExistsInSoul;
    }

    private void ApplyState(WorldType worldType)
    {
        var isOpen = _openStates.TryGetValue(worldType, out var openState) && openState;

        if (_dualWorldObject != null)
        {
            _dualWorldObject.SetActive(worldType, !isOpen);
        }

        if (_visual != null)
        {
            _visual.Position = _visualBasePosition + (isOpen ? new Vector2(0f, OpenOffsetY) : Vector2.Zero);
            _visual.Color = isOpen ? OpenColor : ClosedColor;
        }
    }
}
