using System;
using System.Collections.Generic;
using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class WorldButtonSwitch : Node2D
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public NodePath? DetectionAreaPath { get; set; } = new("Area2D");
    [Export] public NodePath? CollisionShapePath { get; set; } = new("Area2D/CollisionShape2D");
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public NodePath[] TargetPaths { get; set; } = Array.Empty<NodePath>();
    [Export] public bool ActivatesInReality { get; set; } = true;
    [Export] public bool ActivatesInSoul { get; set; } = true;
    [Export] public bool LatchActivation { get; set; } = true;
    [Export] public float PlateWidth { get; set; } = 58f;
    [Export] public float PlateHeight { get; set; } = 14f;
    [Export] public float PressDepth { get; set; } = 5f;
    [Export] public Color IdleRealityColor { get; set; } = new(0.9f, 0.6f, 0.26f, 1f);
    [Export] public Color ActiveRealityColor { get; set; } = new(1f, 0.82f, 0.42f, 1f);
    [Export] public Color IdleSoulColor { get; set; } = new(0.36f, 0.72f, 1f, 1f);
    [Export] public Color ActiveSoulColor { get; set; } = new(0.68f, 0.92f, 1f, 1f);
    [ExportGroup("World State")]
    [Export] public bool ExistsInReality { get; set; } = true;
    [Export] public bool ExistsInSoul { get; set; } = true;

    private readonly Dictionary<WorldType, bool> _latchedStates = new();
    private readonly List<Node> _resolvedTargets = new();

    private Polygon2D? _visual;
    private Area2D? _detectionArea;
    private CollisionShape2D? _collisionShape;
    private DualWorldObject? _dualWorldObject;
    private Vector2 _visualBasePosition;
    private int _pressingPlayerCount;

    public override void _Ready()
    {
        _visual = VisualPath != null && !VisualPath.IsEmpty ? GetNodeOrNull<Polygon2D>(VisualPath) : null;
        _detectionArea = DetectionAreaPath != null && !DetectionAreaPath.IsEmpty ? GetNodeOrNull<Area2D>(DetectionAreaPath) : null;
        _collisionShape = CollisionShapePath != null && !CollisionShapePath.IsEmpty ? GetNodeOrNull<CollisionShape2D>(CollisionShapePath) : null;
        _dualWorldObject = DualWorldObjectPath != null && !DualWorldObjectPath.IsEmpty ? GetNodeOrNull<DualWorldObject>(DualWorldObjectPath) : null;
        _visualBasePosition = _visual?.Position ?? Vector2.Zero;

        _latchedStates[WorldType.Reality] = false;
        _latchedStates[WorldType.Soul] = false;

        ApplyWorldDefaults();
        ResolveTargets();
        ApplyGeometry();

        if (_detectionArea != null)
        {
            _detectionArea.BodyEntered += OnBodyEntered;
            _detectionArea.BodyExited += OnBodyExited;
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            ApplyVisualState(WorldManager.Instance.CurrentWorld);
        }
        else
        {
            ApplyVisualState(WorldType.Reality);
        }
    }

    public override void _ExitTree()
    {
        if (_detectionArea != null)
        {
            _detectionArea.BodyEntered -= OnBodyEntered;
            _detectionArea.BodyExited -= OnBodyExited;
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= OnWorldChanged;
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (!IsPlayerBody(body))
        {
            return;
        }

        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        if (!CanActivateInWorld(currentWorld))
        {
            return;
        }

        _pressingPlayerCount += 1;

        if (LatchActivation)
        {
            if (!_latchedStates[currentWorld])
            {
                _latchedStates[currentWorld] = true;
                SetTargets(true, currentWorld);
            }
        }
        else if (_pressingPlayerCount == 1)
        {
            SetTargets(true, currentWorld);
        }

        ApplyVisualState(currentWorld);
    }

    private void OnBodyExited(Node body)
    {
        if (!IsPlayerBody(body))
        {
            return;
        }

        _pressingPlayerCount = Mathf.Max(0, _pressingPlayerCount - 1);
        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;

        if (!LatchActivation && _pressingPlayerCount == 0 && CanActivateInWorld(currentWorld))
        {
            SetTargets(false, currentWorld);
        }

        ApplyVisualState(currentWorld);
    }

    private void ResolveTargets()
    {
        _resolvedTargets.Clear();

        foreach (var targetPath in TargetPaths)
        {
            if (targetPath == null || targetPath.IsEmpty)
            {
                continue;
            }

            var node = GetNodeOrNull(targetPath);
            if (node != null)
            {
                _resolvedTargets.Add(node);
            }
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

    private void SetTargets(bool isActivated, WorldType worldType)
    {
        foreach (var targetNode in _resolvedTargets)
        {
            if (targetNode is IWorldActivationTarget activationTarget)
            {
                activationTarget.SetActivatedForWorld(isActivated, worldType);
            }
        }
    }

    private void ApplyGeometry()
    {
        var width = Mathf.Max(36f, PlateWidth);
        var height = Mathf.Max(10f, PlateHeight);
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

        if (_collisionShape == null)
        {
            return;
        }

        if (_collisionShape.Shape is not RectangleShape2D rectangleShape)
        {
            rectangleShape = new RectangleShape2D();
            _collisionShape.Shape = rectangleShape;
        }

        rectangleShape.Size = new Vector2(width, height + 14f);
    }

    private void OnWorldChanged(WorldType worldType)
    {
        ApplyVisualState(worldType);
    }

    private void ApplyVisualState(WorldType worldType)
    {
        if (_visual == null)
        {
            return;
        }

        var isLatched = _latchedStates.TryGetValue(worldType, out var latched) && latched;
        var isPressed = isLatched || (!LatchActivation && _pressingPlayerCount > 0 && CanActivateInWorld(worldType));
        _visual.Position = _visualBasePosition + (isPressed ? new Vector2(0f, PressDepth) : Vector2.Zero);

        _visual.Color = worldType == WorldType.Reality
            ? (isPressed ? ActiveRealityColor : IdleRealityColor)
            : (isPressed ? ActiveSoulColor : IdleSoulColor);
    }

    private bool CanActivateInWorld(WorldType worldType)
    {
        return worldType switch
        {
            WorldType.Reality => ActivatesInReality,
            WorldType.Soul => ActivatesInSoul,
            _ => false,
        };
    }

    private static bool IsPlayerBody(Node body)
    {
        return body.IsInGroup("player");
    }
}
