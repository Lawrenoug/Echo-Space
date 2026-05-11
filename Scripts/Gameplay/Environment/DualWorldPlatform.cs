using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class DualWorldPlatform : Node2D
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public NodePath? CollisionShapePath { get; set; } = new("StaticBody2D/CollisionShape2D");
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public float PlatformWidth { get; set; } = 180f;
    [Export] public float PlatformHeight { get; set; } = 24f;
    [Export] public Color PlatformColor { get; set; } = new(0.45f, 0.64f, 0.92f, 1f);
    [ExportGroup("World State")]
    [Export] public bool ExistsInReality { get; set; } = true;
    [Export] public bool ExistsInSoul { get; set; } = true;
    [Export] public Vector2 RealityPositionOffset { get; set; } = Vector2.Zero;
    [Export] public Vector2 SoulPositionOffset { get; set; } = Vector2.Zero;

    private Polygon2D? _visual;
    private CollisionShape2D? _collisionShape;
    private DualWorldObject? _dualWorldObject;

    public override void _Ready()
    {
        _visual = VisualPath != null && !VisualPath.IsEmpty ? GetNodeOrNull<Polygon2D>(VisualPath) : null;
        _collisionShape = CollisionShapePath != null && !CollisionShapePath.IsEmpty ? GetNodeOrNull<CollisionShape2D>(CollisionShapePath) : null;
        _dualWorldObject = DualWorldObjectPath != null && !DualWorldObjectPath.IsEmpty ? GetNodeOrNull<DualWorldObject>(DualWorldObjectPath) : null;
        ApplyWorldDefaults();
        ApplyGeometry();
    }

    private void ApplyWorldDefaults()
    {
        if (_dualWorldObject == null)
        {
            return;
        }

        _dualWorldObject.ExistsInReality = ExistsInReality;
        _dualWorldObject.ExistsInSoul = ExistsInSoul;
        _dualWorldObject.RealityPositionOffset = RealityPositionOffset;
        _dualWorldObject.SoulPositionOffset = SoulPositionOffset;
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
            _visual.Color = PlatformColor;
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
}
