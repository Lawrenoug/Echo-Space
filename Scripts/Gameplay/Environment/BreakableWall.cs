using EchoSpace.Core.World;
using EchoSpace.Gameplay.Combat;
using Godot;

namespace EchoSpace.Gameplay.Environment;

public partial class BreakableWall : Node2D, IDamageable
{
    [Export] public NodePath? DualWorldObjectPath { get; set; } = new("DualWorldObject");
    [Export] public NodePath? VisualRootPath { get; set; } = new("Visual");
    [Export] public float BreakFlashDuration { get; set; } = 0.06f;

    private DualWorldObject? _dualWorldObject;
    private CanvasItem? _visualRoot;
    private double _breakFlashRemaining;

    public override void _Ready()
    {
        _dualWorldObject = DualWorldObjectPath != null && !DualWorldObjectPath.IsEmpty
            ? GetNodeOrNull<DualWorldObject>(DualWorldObjectPath)
            : null;
        _visualRoot = VisualRootPath != null && !VisualRootPath.IsEmpty
            ? GetNodeOrNull<CanvasItem>(VisualRootPath)
            : null;
    }

    public override void _Process(double delta)
    {
        if (_breakFlashRemaining <= 0d)
        {
            return;
        }

        _breakFlashRemaining -= delta;

        if (_visualRoot != null)
        {
            _visualRoot.Modulate = _breakFlashRemaining > 0d
                ? new Color(1f, 0.92f, 0.72f, 1f)
                : Colors.White;
        }
    }

    public void ApplyDamage(in DamageInfo damageInfo)
    {
        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        if (damageInfo.SourceWorld != currentWorld)
        {
            return;
        }

        _breakFlashRemaining = BreakFlashDuration;
        _dualWorldObject?.BreakInCurrentWorld();
    }
}
