using System.Collections.Generic;
using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class DifferentialMovingPlatform : AnimatableBody2D
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public NodePath? CollisionShapePath { get; set; } = new("CollisionShape2D");
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public float PlatformWidth { get; set; } = 180f;
    [Export] public float PlatformHeight { get; set; } = 24f;
    [Export] public Color RealityColor { get; set; } = new(0.9f, 0.6f, 0.24f, 1f);
    [Export] public Color SoulColor { get; set; } = new(0.38f, 0.78f, 1f, 1f);
    [ExportGroup("World State")]
    [Export] public bool ExistsInReality { get; set; } = true;
    [Export] public bool ExistsInSoul { get; set; } = true;

    [ExportGroup("Reality Motion")]
    [Export] public Vector2 RealityStartOffset { get; set; } = Vector2.Zero;
    [Export] public Vector2 RealityEndOffset { get; set; } = Vector2.Zero;
    [Export] public float RealityMoveSpeed { get; set; }
    [Export] public bool RealityPingPong { get; set; } = true;

    [ExportGroup("Soul Motion")]
    [Export] public Vector2 SoulStartOffset { get; set; } = Vector2.Zero;
    [Export] public Vector2 SoulEndOffset { get; set; } = new(280f, 0f);
    [Export] public float SoulMoveSpeed { get; set; } = 96f;
    [Export] public bool SoulPingPong { get; set; } = true;

    private readonly Dictionary<WorldType, MotionState> _motionStates = new();

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

        _motionStates[WorldType.Reality] = new MotionState(RealityStartOffset, RealityEndOffset, RealityMoveSpeed, RealityPingPong);
        _motionStates[WorldType.Soul] = new MotionState(SoulStartOffset, SoulEndOffset, SoulMoveSpeed, SoulPingPong);

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            ApplyCurrentWorldState(WorldManager.Instance.CurrentWorld);
        }
        else
        {
            ApplyCurrentWorldState(WorldType.Reality);
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
        var worldType = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        if (!_motionStates.TryGetValue(worldType, out var motionState))
        {
            return;
        }

        motionState.Advance((float)delta);
        Position = _basePosition + motionState.CurrentOffset;
    }

    private void OnWorldChanged(WorldType worldType)
    {
        ApplyCurrentWorldState(worldType);
    }

    private void ApplyCurrentWorldState(WorldType worldType)
    {
        if (_motionStates.TryGetValue(worldType, out var motionState))
        {
            Position = _basePosition + motionState.CurrentOffset;
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

    private sealed class MotionState
    {
        private readonly Vector2 _startOffset;
        private readonly Vector2 _endOffset;
        private readonly float _speed;
        private readonly bool _pingPong;
        private readonly float _travelDistance;
        private int _direction = 1;
        private float _progress;

        public MotionState(Vector2 startOffset, Vector2 endOffset, float speed, bool pingPong)
        {
            _startOffset = startOffset;
            _endOffset = endOffset;
            _speed = Mathf.Max(0f, speed);
            _pingPong = pingPong;
            _travelDistance = startOffset.DistanceTo(endOffset);
            CurrentOffset = startOffset;
        }

        public Vector2 CurrentOffset { get; private set; }

        public void Advance(float delta)
        {
            if (_speed <= 0f || _travelDistance <= 0.01f)
            {
                CurrentOffset = _startOffset;
                return;
            }

            _progress += (_speed / _travelDistance) * delta * _direction;

            if (_pingPong)
            {
                if (_progress >= 1f)
                {
                    _progress = 1f;
                    _direction = -1;
                }
                else if (_progress <= 0f)
                {
                    _progress = 0f;
                    _direction = 1;
                }
            }
            else
            {
                if (_progress > 1f)
                {
                    _progress -= 1f;
                }
                else if (_progress < 0f)
                {
                    _progress += 1f;
                }
            }

            CurrentOffset = _startOffset.Lerp(_endOffset, _progress);
        }
    }
}
