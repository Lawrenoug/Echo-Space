using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class SoulTetherAnchor : Node2D
{
    [Export] public NodePath? VisualPath { get; set; } = new("Visual");
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public float AnchorRadius { get; set; } = 16f;
    [Export] public Vector2 PullTargetOffset { get; set; } = new(0f, 28f);
    [Export] public Color AnchorColor { get; set; } = new(0.68f, 0.92f, 1f, 1f);

    [ExportGroup("World State")]
    [Export] public bool ExistsInReality { get; set; }
    [Export] public bool ExistsInSoul { get; set; } = true;

    private Polygon2D? _visual;
    private DualWorldObject? _dualWorldObject;

    public Vector2 PullTargetGlobalPosition => GlobalPosition + PullTargetOffset;

    public override void _Ready()
    {
        AddToGroup("soul_tether_anchor");

        _visual = VisualPath != null && !VisualPath.IsEmpty ? GetNodeOrNull<Polygon2D>(VisualPath) : null;
        _dualWorldObject = DualWorldObjectPath != null && !DualWorldObjectPath.IsEmpty ? GetNodeOrNull<DualWorldObject>(DualWorldObjectPath) : null;

        if (_dualWorldObject != null)
        {
            _dualWorldObject.ExistsInReality = ExistsInReality;
            _dualWorldObject.ExistsInSoul = ExistsInSoul;
        }

        ApplyGeometry();
    }

    public bool IsAvailableFor(WorldType worldType)
    {
        return worldType switch
        {
            WorldType.Reality => ExistsInReality,
            WorldType.Soul => ExistsInSoul,
            _ => false,
        };
    }

    private void ApplyGeometry()
    {
        if (_visual == null)
        {
            return;
        }

        var radius = Mathf.Max(8f, AnchorRadius);
        _visual.Color = AnchorColor;
        _visual.Polygon = new[]
        {
            new Vector2(0f, -radius),
            new Vector2(radius * 0.75f, 0f),
            new Vector2(0f, radius),
            new Vector2(-radius * 0.75f, 0f),
        };
    }
}
