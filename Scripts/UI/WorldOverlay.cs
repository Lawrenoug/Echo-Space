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
    [Export] public NodePath? StaminaLabelPath { get; set; }
    [Export] public NodePath? StaminaBarPath { get; set; }

    private Label? _label;
    private Label? _healthLabel;
    private Label? _staminaLabel;
    private ProgressBar? _healthBar;
    private ProgressBar? _staminaBar;
    private CanvasModulate? _tint;
    private PlayerController? _player;
    private bool _isSubscribed;
    private int _lastDisplayedHealth = int.MinValue;
    private int _lastDisplayedMaxHealth = int.MinValue;
    private int _lastDisplayedStamina = int.MinValue;
    private int _lastDisplayedMaxStamina = int.MinValue;

    public override void _Ready()
    {
        ResolveBindings();

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += OnWorldChanged;
            OnWorldChanged(WorldManager.Instance.CurrentWorld);
        }

        if (!TrySubscribeToPlayer())
        {
            GD.PrintErr("Player not found! Check PlayerPath.");
        }
    }

    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = null;
            _isSubscribed = false;
            ResolveBindings();
            TrySubscribeToPlayer();
            return;
        }

        RefreshPlayerVitals(false);
    }

    public override void _ExitTree()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= OnWorldChanged;
        }

        if (_isSubscribed && _player != null)
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
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
        GD.Print($"Stamina changed: {currentStamina}/{maxStamina}");

        _lastDisplayedStamina = Mathf.RoundToInt(currentStamina);
        _lastDisplayedMaxStamina = Mathf.RoundToInt(maxStamina);

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

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        GD.Print($"Health changed: {currentHealth}/{maxHealth}");

        _lastDisplayedHealth = currentHealth;
        _lastDisplayedMaxHealth = maxHealth;

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

    private void ResolveBindings()
    {
        _label ??= ResolveNode<Label>(LabelPath, "Panel/Label");
        _healthLabel ??= ResolveNode<Label>(HealthLabelPath, "Panel/HealthLabel");
        _staminaLabel ??= ResolveNode<Label>(StaminaLabelPath, "Panel/StaminaLabel");
        _healthBar ??= ResolveNode<ProgressBar>(HealthBarPath, "Panel/HealthBar");
        _staminaBar ??= ResolveNode<ProgressBar>(StaminaBarPath, "Panel/StaminaBar");
        _tint ??= ResolveNode<CanvasModulate>(TintPath, "../WorldTint");
        _player ??= ResolvePlayer();

        GD.Print($"WorldOverlay bindings -> Player: {_player != null}, HealthBar: {_healthBar != null}, StaminaBar: {_staminaBar != null}, HealthLabel: {_healthLabel != null}, StaminaLabel: {_staminaLabel != null}");
    }

    private bool TrySubscribeToPlayer()
    {
        if (_isSubscribed || _player == null)
        {
            return _isSubscribed;
        }

        _player.HealthChanged += OnPlayerHealthChanged;
        _player.StaminaChanged += OnPlayerStaminaChanged;
        _isSubscribed = true;
        RefreshPlayerVitals(true);
        return true;
    }

    private void RefreshPlayerVitals(bool logChanges)
    {
        if (_player == null)
        {
            return;
        }

        if (_player.CurrentHealth != _lastDisplayedHealth || _player.MaxHealth != _lastDisplayedMaxHealth)
        {
            if (logChanges)
            {
                OnPlayerHealthChanged(_player.CurrentHealth, _player.MaxHealth);
            }
            else
            {
                UpdateHealthVisuals(_player.CurrentHealth, _player.MaxHealth);
            }
        }

        var currentStamina = Mathf.RoundToInt(_player.CurrentStamina);
        var maxStamina = Mathf.RoundToInt(_player.MaxStamina);
        if (currentStamina != _lastDisplayedStamina || maxStamina != _lastDisplayedMaxStamina)
        {
            if (logChanges)
            {
                OnPlayerStaminaChanged(_player.CurrentStamina, _player.MaxStamina);
            }
            else
            {
                UpdateStaminaVisuals(_player.CurrentStamina, _player.MaxStamina);
            }
        }
    }

    private void UpdateHealthVisuals(int currentHealth, int maxHealth)
    {
        _lastDisplayedHealth = currentHealth;
        _lastDisplayedMaxHealth = maxHealth;

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

    private void UpdateStaminaVisuals(float currentStamina, float maxStamina)
    {
        var roundedCurrent = Mathf.RoundToInt(currentStamina);
        var roundedMax = Mathf.RoundToInt(maxStamina);
        _lastDisplayedStamina = roundedCurrent;
        _lastDisplayedMaxStamina = roundedMax;

        if (_staminaLabel != null)
        {
            _staminaLabel.Text = $"Stamina {roundedCurrent}/{roundedMax}";
        }

        if (_staminaBar != null)
        {
            _staminaBar.MaxValue = maxStamina;
            _staminaBar.Value = currentStamina;
        }
    }

    private T? ResolveNode<T>(NodePath? primaryPath, string fallbackPath) where T : class
    {
        if (primaryPath != null && !primaryPath.IsEmpty)
        {
            var fromPrimary = GetNodeOrNull<T>(primaryPath);
            if (fromPrimary != null)
            {
                return fromPrimary;
            }
        }

        return GetNodeOrNull<T>(fallbackPath);
    }

    private PlayerController? ResolvePlayer()
    {
        if (PlayerPath != null && !PlayerPath.IsEmpty)
        {
            var fromPath = GetNodeOrNull<PlayerController>(PlayerPath);
            if (fromPath != null)
            {
                return fromPath;
            }
        }

        return GetNodeOrNull<PlayerController>("../Player")
            ?? GetTree().GetFirstNodeInGroup("player") as PlayerController;
    }
}
