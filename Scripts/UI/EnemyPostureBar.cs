using Godot;
using EchoSpace.Gameplay.Enemies;

namespace EchoSpace.UI;

public partial class EnemyPostureBar : Node2D
{
	[Export] public NodePath? HealthBackgroundPath { get; set; } = new("HealthBackground");
	[Export] public NodePath? HealthFillPath { get; set; } = new("HealthFill");
	[Export] public NodePath? PostureBackgroundPath { get; set; } = new("PostureBackground");
	[Export] public NodePath? PostureFillPath { get; set; } = new("PostureFill");
	[Export] public NodePath? ExecutionMarkerPath { get; set; } = new("ExecutionMarker");

	private Polygon2D? _healthBackground;
	private Polygon2D? _healthFill;
	private Polygon2D? _postureBackground;
	private Polygon2D? _postureFill;
	private Node2D? _executionMarker;
	private EnemyCombatant? _enemy;
	private Vector2 _executionMarkerBasePosition = Vector2.Zero;
	private bool _isBroken;

	public override void _Ready()
	{
		_healthBackground = HealthBackgroundPath != null && !HealthBackgroundPath.IsEmpty ? GetNodeOrNull<Polygon2D>(HealthBackgroundPath) : null;
		_healthFill = HealthFillPath != null && !HealthFillPath.IsEmpty ? GetNodeOrNull<Polygon2D>(HealthFillPath) : null;
		_postureBackground = PostureBackgroundPath != null && !PostureBackgroundPath.IsEmpty ? GetNodeOrNull<Polygon2D>(PostureBackgroundPath) : null;
		_postureFill = PostureFillPath != null && !PostureFillPath.IsEmpty ? GetNodeOrNull<Polygon2D>(PostureFillPath) : null;
		_executionMarker = ExecutionMarkerPath != null && !ExecutionMarkerPath.IsEmpty ? GetNodeOrNull<Node2D>(ExecutionMarkerPath) : null;
		_enemy = GetParentOrNull<EnemyCombatant>();
		Visible = false;

		if (_executionMarker != null)
		{
			_executionMarkerBasePosition = _executionMarker.Position;
			_executionMarker.Visible = false;
		}

		if (_enemy != null)
		{
			_enemy.HealthChanged += UpdateHealthBar;
			_enemy.PostureChanged += UpdateBar;
			UpdateHealthBar(_enemy.CurrentHealth, _enemy.MaxHealth);
			UpdateBar(0f, _enemy.MaxPosture, false);
		}
	}

	public override void _Process(double delta)
	{
		if (_executionMarker == null)
		{
			return;
		}

		if (!_isBroken)
		{
			_executionMarker.Visible = false;
			_executionMarker.Position = _executionMarkerBasePosition;
			_executionMarker.Scale = Vector2.One;
			return;
		}

		var pulse = 0.5f + 0.5f * Mathf.Sin((float)(Time.GetTicksMsec() / 1000.0 * 8.0));
		_executionMarker.Visible = true;
		_executionMarker.Position = _executionMarkerBasePosition + new Vector2(0f, -3f * pulse);
		_executionMarker.Scale = Vector2.One * (1f + 0.12f * pulse);
		_executionMarker.Modulate = new Color(1f, 0.9f, 0.58f, 0.72f + 0.28f * pulse);
	}

	public override void _ExitTree()
	{
		if (_enemy != null)
		{
			_enemy.HealthChanged -= UpdateHealthBar;
			_enemy.PostureChanged -= UpdateBar;
		}
	}

	public void UpdateHealthBar(int currentValue, int maxValue)
	{
		if (_healthFill == null || _healthBackground == null || maxValue <= 0)
		{
			return;
		}

		var ratio = Mathf.Clamp((float)currentValue / maxValue, 0f, 1f);
		_healthFill.Scale = new Vector2(ratio, 1f);
		_healthFill.Modulate = new Color(0.48f, 0.92f, 0.52f, 1f);
		_healthBackground.Modulate = new Color(0.12f, 0.22f, 0.12f, 0.9f);
		UpdateVisibility(ratio, _postureFill?.Scale.X ?? 0f);
	}

	public void UpdateBar(float currentValue, float maxValue, bool isBroken)
	{
		if (_postureFill == null || _postureBackground == null || maxValue <= 0f)
		{
			return;
		}

		_isBroken = isBroken;
		var ratio = Mathf.Clamp(currentValue / maxValue, 0f, 1f);
		_postureFill.Scale = new Vector2(ratio, 1f);
		_postureFill.Modulate = isBroken
			? new Color(1f, 0.75f, 0.4f, 1f)
			: new Color(0.95f, 0.45f, 0.35f, 1f);
		_postureBackground.Modulate = isBroken
			? new Color(0.4f, 0.16f, 0.08f, 0.95f)
			: new Color(0.18f, 0.1f, 0.1f, 0.85f);
		UpdateVisibility(_healthFill?.Scale.X ?? 0f, ratio, isBroken);
	}

	private void UpdateVisibility(float healthRatio, float postureRatio, bool isBroken = false)
	{
		Visible = healthRatio > 0.01f || postureRatio > 0.01f || isBroken;
	}
}
