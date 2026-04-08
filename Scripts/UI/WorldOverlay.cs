using EchoSpace.Player;
using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.UI;

public partial class WorldOverlay : CanvasLayer
{
    [Export] public NodePath? LabelPath { get; set; }
    [Export] public NodePath? TintPath { get; set; }
    [Export] public NodePath? PlayerPath { get; set; }
    [Export] public NodePath? HealthLabelPath { get; set; }
    [Export] public NodePath? HealthBarPath { get; set; }

    private Label? _label;
    private Label? _healthLabel;
    private ProgressBar? _healthBar;
    private CanvasModulate? _tint;
    private PlayerController? _player;

    public override void _Ready()
    {
        _label = LabelPath != null && !LabelPath.IsEmpty ? GetNodeOrNull<Label>(LabelPath) : null;
        _healthLabel = HealthLabelPath != null && !HealthLabelPath.IsEmpty ? GetNodeOrNull<Label>(HealthLabelPath) : null;
        _healthBar = HealthBarPath != null && !HealthBarPath.IsEmpty ? GetNodeOrNull<ProgressBar>(HealthBarPath) : null;
        _tint = TintPath != null && !TintPath.IsEmpty ? GetNodeOrNull<CanvasModulate>(TintPath) : null;
        _player = PlayerPath != null && !PlayerPath.IsEmpty ? GetNodeOrNull<PlayerController>(PlayerPath) : null;

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            OnWorldChanged(WorldManager.Instance.CurrentWorld);
        }

        if (_player != null)
        {
            _player.HealthChanged += OnPlayerHealthChanged;
            OnPlayerHealthChanged(_player.CurrentHealth, _player.MaxHealth);
        }
    }

    public override void _ExitTree()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= OnWorldChanged;
        }

        if (_player != null)
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
        }
    }

    private void OnWorldChanged(WorldType worldType)
    {
        if (_label != null)
        {
            _label.Text = worldType == WorldType.Reality
                ? "Reality World  [Tab]"
                : "Soul World  [Tab]";
        }

        if (_tint != null)
        {
            _tint.Color = worldType == WorldType.Reality
                ? new Color(1f, 0.97f, 0.92f, 1f)
                : new Color(0.72f, 0.82f, 1f, 1f);
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (_healthLabel != null)
        {
            _healthLabel.Text = $"HP {currentHealth}/{maxHealth}";
        }

        if (_healthBar != null)
        {
            _healthBar.MaxValue = maxHealth;
            _healthBar.Value = currentHealth;
        }
    }
}
