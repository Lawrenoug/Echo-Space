using System.Collections.Generic;
using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class WorldStateLift : AnimatableBody2D, IWorldActivationTarget
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public NodePath? CollisionShapePath { get; set; } = new("CollisionShape2D");
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public float PlatformWidth { get; set; } = 156f;
    [Export] public float PlatformHeight { get; set; } = 24f;
    [Export] public Color RealityColor { get; set; } = new(0.9f, 0.6f, 0.24f, 1f);
    [Export] public Color SoulColor { get; set; } = new(0.38f, 0.78f, 1f, 1f);
    [Export] public float MoveSpeed { get; set; } = 180f;

    [ExportGroup("World State")]
    [Export] public bool ExistsInReality { get; set; } = true;
    [Export] public bool ExistsInSoul { get; set; } = true;
    [Export] public bool StartsActiveInReality { get; set; }
    [Export] public bool StartsActiveInSoul { get; set; }

    [ExportGroup("Reality Offsets")]
    [Export] public Vector2 RealityInactiveOffset { get; set; } = Vector2.Zero;
    [Export] public Vector2 RealityActiveOffset { get; set; } = new(0f, -160f);

    [ExportGroup("Soul Offsets")]
    [Export] public Vector2 SoulInactiveOffset { get; set; } = new(0f, 20f);
    [Export] public Vector2 SoulActiveOffset { get; set; } = new(0f, -220f);

    private readonly Dictionary<WorldType, LiftMotionState> _motionStates = new();

    private Polygon2D? _visual;
    private CollisionShape2D? _collisionShape;
    private DualWorldObject? _dualWorldObject;
    private Vector2 _basePosition;

    public override void _Ready()
    {
        SyncToPhysics = true;
        _visual = VisualPath != null && !VisualPath.IsEmpty ? GetNodeOrNull<Polygon2D>(VisualPath) : null;
        _collisionShape = CollisionShapePath != null && !CollisionShapePath.IsEmpty ? GetNodeOrNull<CollisionShape2D>(CollisionShapePath) : null;
        _dualWorldObject = DualWorldObjectPath != null && !DualWorldObjectPath.IsEmpty ? GetNodeOrNull<DualWorldObject>(DualWorldObjectPath) : null;
        _basePosition = Position;

        ApplyWorldDefaults();
        ApplyGeometry();

        _motionStates[WorldType.Reality] = new LiftMotionState(RealityInactiveOffset, RealityActiveOffset, StartsActiveInReality);
        _motionStates[WorldType.Soul] = new LiftMotionState(SoulInactiveOffset, SoulActiveOffset, StartsActiveInSoul);

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            ApplyCurrentWorldVisual(WorldManager.Instance.CurrentWorld);
        }
        else
        {
            ApplyCurrentWorldVisual(WorldType.Reality);
        }
    }

    public override void _ExitTree()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= OnWorldChanged;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        if (!_motionStates.TryGetValue(currentWorld, out var state))
        {
            return;
        }

        state.Step((float)delta, MoveSpeed);
        Position = _basePosition + state.CurrentOffset;
    }

    public void SetActivatedForWorld(bool isActivated, WorldType worldType)
    {
        if (!_motionStates.TryGetValue(worldType, out var state))
        {
            return;
        }

        state.IsActive = isActivated;

        if ((WorldManager.Instance?.CurrentWorld ?? WorldType.Reality) == worldType)
        {
            ApplyCurrentWorldVisual(worldType);
        }
    }

    private void OnWorldChanged(WorldType worldType)
    {
        ApplyCurrentWorldVisual(worldType);
    }

    private void ApplyCurrentWorldVisual(WorldType worldType)
    {
        if (_motionStates.TryGetValue(worldType, out var state))
        {
            Position = _basePosition + state.CurrentOffset;
        }

        if (_visual != null)
        {
            _visual.Color = worldType == WorldType.Reality ? RealityColor : SoulColor;
        }
    }

    private void ApplyGeometry()
    {
        var width = Mathf.Max(48f, PlatformWidth);
        var height = Mathf.Max(12f, PlatformHeight);
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

        rectangleShape.Size = new Vector2(width, height);
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

    private sealed class LiftMotionState
    {
        private readonly Vector2 _inactiveOffset;
        private readonly Vector2 _activeOffset;

        public LiftMotionState(Vector2 inactiveOffset, Vector2 activeOffset, bool startsActive)
        {
            _inactiveOffset = inactiveOffset;
            _activeOffset = activeOffset;
            IsActive = startsActive;
            CurrentOffset = startsActive ? activeOffset : inactiveOffset;
        }

        public bool IsActive { get; set; }

        public Vector2 CurrentOffset { get; private set; }

        public void Step(float delta, float moveSpeed)
        {
            var target = IsActive ? _activeOffset : _inactiveOffset;
            var maxDistance = Mathf.Max(1f, moveSpeed) * delta;
            CurrentOffset = CurrentOffset.MoveToward(target, maxDistance);
        }
    }
}
