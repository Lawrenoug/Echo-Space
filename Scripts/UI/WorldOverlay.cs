using EchoSpace.Player;
using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.UI;

public partial class WorldOverlay : CanvasLayer
{
    [Export] public NodePath? LabelPath { get; set; }
    [Export] public NodePath? TintPath { get; set; }
    [Export] public NodePath? PlayerPath { get; set; }
    [Export] public NodePath? StaminaLabelPath { get; set; }
    [Export] public NodePath? StaminaBarPath { get; set; }

    private Label? _label;
    private Label? _staminaLabel;
    private ProgressBar? _staminaBar;
    private CanvasModulate? _tint;
    private PlayerController? _player;

    public override void _Ready()
    {
        _label = LabelPath != null && !LabelPath.IsEmpty ? GetNodeOrNull<Label>(LabelPath) : null;
        _staminaLabel = StaminaLabelPath != null && !StaminaLabelPath.IsEmpty ? GetNodeOrNull<Label>(StaminaLabelPath) : null;
        _staminaBar = StaminaBarPath != null && !StaminaBarPath.IsEmpty ? GetNodeOrNull<ProgressBar>(StaminaBarPath) : null;
        _tint = TintPath != null && !TintPath.IsEmpty ? GetNodeOrNull<CanvasModulate>(TintPath) : null;
        _player = PlayerPath != null && !PlayerPath.IsEmpty ? GetNodeOrNull<PlayerController>(PlayerPath) : null;

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            OnWorldChanged(WorldManager.Instance.CurrentWorld);
        }

        if (_player != null)
        {
            _player.StaminaChanged += OnPlayerStaminaChanged;
            OnPlayerStaminaChanged(_player.CurrentStamina, _player.MaxStamina);
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
            _player.StaminaChanged -= OnPlayerStaminaChanged;
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

    private void OnPlayerStaminaChanged(float currentStamina, float maxStamina)
    {
        if (_staminaLabel != null)
        {
            _staminaLabel.Text = $"Stamina {Mathf.RoundToInt(currentStamina)}/{Mathf.RoundToInt(maxStamina)}";
        }

        if (_staminaBar != null)
        {
            _staminaBar.MaxValue = maxStamina;
            _staminaBar.Value = currentStamina;
        }
    }
}
