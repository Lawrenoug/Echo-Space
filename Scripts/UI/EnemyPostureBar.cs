using Godot;
using EchoSpace.Gameplay.Enemies;

namespace EchoSpace.UI;

public partial class EnemyPostureBar : Node2D
{
    [Export] public NodePath? BackgroundPath { get; set; } = new("Background");
    [Export] public NodePath? FillPath { get; set; } = new("Fill");

    private Polygon2D? _background;
    private Polygon2D? _fill;
    private EnemyController? _enemy;

    public override void _Ready()
    {
        _background = BackgroundPath != null && !BackgroundPath.IsEmpty ? GetNodeOrNull<Polygon2D>(BackgroundPath) : null;
        _fill = FillPath != null && !FillPath.IsEmpty ? GetNodeOrNull<Polygon2D>(FillPath) : null;
        _enemy = GetParentOrNull<EnemyController>();
        Visible = false;

        if (_enemy != null)
        {
            _enemy.PostureChanged += UpdateBar;
            UpdateBar(0f, _enemy.MaxPosture, false);
        }
    }

    public override void _ExitTree()
    {
        if (_enemy != null)
        {
            _enemy.PostureChanged -= UpdateBar;
        }
    }

    public void UpdateBar(float currentValue, float maxValue, bool isBroken)
    {
        if (_fill == null || _background == null || maxValue <= 0f)
        {
            return;
        }

        var ratio = Mathf.Clamp(currentValue / maxValue, 0f, 1f);
        Visible = ratio > 0.01f || isBroken;

        _fill.Scale = new Vector2(ratio, 1f);
        _fill.Modulate = isBroken
            ? new Color(1f, 0.75f, 0.4f, 1f)
            : new Color(0.95f, 0.45f, 0.35f, 1f);
        _background.Modulate = isBroken
            ? new Color(0.4f, 0.16f, 0.08f, 0.95f)
            : new Color(0.18f, 0.1f, 0.1f, 0.85f);
    }
}
