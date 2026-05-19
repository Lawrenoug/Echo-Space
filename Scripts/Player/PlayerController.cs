using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EchoSpace.Core.Fsm;
using EchoSpace.Core.Input;
using EchoSpace.Core.Settings;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Combat;
using EchoSpace.Gameplay.Enemies;
using EchoSpace.Gameplay.Progression;
using EchoSpace.Player.States;
using Godot;

namespace EchoSpace.Player;

public partial class PlayerController : CharacterBody2D, IDamageable
{
	private sealed class PlayerAnimationDefinition
	{
		public PlayerAnimationDefinition(string actionName, int frameCount, float fps, bool loop)
		{
			ActionName = actionName;
			FrameCount = frameCount;
			Fps = fps;
			Loop = loop;
		}

		public string ActionName { get; }
		public int FrameCount { get; }
		public float Fps { get; }
		public bool Loop { get; }
	}

	private sealed class PlayerAnimationManifestEntry
	{
		[JsonPropertyName("source")]
		public string Source { get; set; } = string.Empty;

		[JsonPropertyName("scale")]
		public float Scale { get; set; } = 1f;
	}

	private static readonly PlayerAnimationDefinition[] AnimationDefinitions =
	[
		new("idle", 6, 6f, true),
		new("run", 8, 12f, true),
		new("jumpstart", 3, 10f, false),
		new("fall", 3, 8f, false),
		new("attack", 8, 8f, false),
		new("hurt", 3, 10f, false),
		new("dead", 6, 8f, false),
		new("execute", 6, 12f, false),
		new("guard", 1, 8f, false),
		new("parry", 4, 15f, false)
	];

	public event Action<int, int>? HealthChanged;
	public event Action<float, float>? StaminaChanged;

	[ExportGroup("Movement")]
	[Export] public float MoveSpeed { get; set; } = 220f;
	[Export] public float GroundAcceleration { get; set; } = 1700f;
	[Export] public float GroundDeceleration { get; set; } = 1800f;
	[Export] public float AirAcceleration { get; set; } = 1250f;
	[Export] public float AirDeceleration { get; set; } = 1100f;
	[Export] public float JumpSpeed { get; set; } = 380f;
	[Export] public float AttackDuration { get; set; } = 0.9f;
	[Export] public int MaxAirJumps { get; set; }

	[ExportGroup("Feel Tuning")]
	[Export] public float InputBufferTime { get; set; } = 0.12f;
	[Export] public float CoyoteTime { get; set; } = 0.10f;
	[Export] public float RiseGravityScale { get; set; } = 0.78f;
	[Export] public float JumpApexGravityScale { get; set; } = 0.5f;
	[Export] public float FallGravityScale { get; set; } = 1.8f;
	[Export] public float JumpApexVelocityThreshold { get; set; } = 42f;
	[Export] public float JumpHoldMaxTime { get; set; } = 0.18f;
	[Export] public float JumpHoldGravityScale { get; set; } = 0.28f;
	[Export] public float JumpCutVelocityMultiplier { get; set; } = 0.55f;

	[ExportGroup("Traversal")]
	[Export] public float DashSpeed { get; set; } = 460f;
	[Export] public float DashDuration { get; set; } = 0.16f;
	[Export] public float DashCooldown { get; set; } = 0.35f;
	[Export] public float DashStaminaCost { get; set; } = 12f;
	[Export] public int MaxAirDashes { get; set; } = 1;
	[Export] public float DashExitSpeedMultiplier { get; set; } = 0.62f;
	[Export] public float DashMomentumDuration { get; set; } = 0.14f;
	[Export] public float DashMomentumDecelerationMultiplier { get; set; } = 0.34f;

	[ExportGroup("Combat")]
	[Export] public int MaxHealth { get; set; } = 5;
	[Export] public int AttackDamage { get; set; } = 1;
	[Export] public float AttackPostureDamage { get; set; } = 18f;
	[Export] public float DamageInvulnerabilityTime { get; set; } = 0.45f;
	[Export] public float MaxStamina { get; set; } = 100f;
	[Export] public float StaminaRecoveryPerSecond { get; set; } = 24f;
	[Export] public float AttackStaminaCost { get; set; } = 14f;
	[Export] public float ExecutionStaminaCost { get; set; } = 10f;
	[Export] public float GuardStaminaDrainPerSecond { get; set; } = 10f;
	[Export] public float GuardHitStaminaCost { get; set; } = 22f;
	[Export] public float GuardDeflectWindow { get; set; } = 0.18f;
	[Export] public float DeflectPostureDamage { get; set; } = 36f;
	[Export] public float DeflectStaminaCost { get; set; } = 6f;
	[Export] public float GuardBreakDuration { get; set; } = 0.7f;
	[Export] public float ExecutionRange { get; set; } = 88f;
	[Export] public float ExecutionVerticalTolerance { get; set; } = 42f;
	[Export] public float ExecutionApproachSpeed { get; set; } = 720f;
	[Export] public float ExecutionStopDistance { get; set; } = 18f;
	[Export] public float ExecutionAttackDuration { get; set; } = 0.24f;
	[Export] public NodePath? AttackProbePath { get; set; } = new("AttackProbe");
	[Export] public NodePath? AttackProbeCollisionShapePath { get; set; } = new("AttackProbe/CollisionShape2D");
	[Export] public NodePath? HurtboxVisualPath { get; set; } = new("GuardEffect");
	[Export] public NodePath? BodyVisualPath { get; set; } = new("AnimatedSprite");
	[Export] public NodePath? AnimatedSpritePath { get; set; } = new("AnimatedSprite");
	[Export] public string AnimationFramesRoot { get; set; } = "res://Docs/Art/PlayerSpriteFrames";
	[Export] public NodePath? WeaponSpritePath { get; set; } = new("WeaponSprite");
	[Export] public string WeaponAnimationFramesRoot { get; set; } = "res://Docs/Art/PlayerWeaponFrames";

	private readonly InputBuffer _inputBuffer = new();
	private readonly HashSet<ulong> _damagedTargetsThisAttack = new();
	private readonly Dictionary<string, float> _bodyAnimationScaleByName = new(StringComparer.Ordinal);
	private readonly Dictionary<string, float> _weaponAnimationScaleByName = new(StringComparer.Ordinal);

	private StateMachine<PlayerController>? _stateMachine;
	private double _lastGroundedAt = double.NegativeInfinity;
	private double _lastDamageTakenAt = double.NegativeInfinity;
	private double _lastGuardPressedAt = double.NegativeInfinity;
	private double _lastDashAt = double.NegativeInfinity;
	private double _dashMomentumRemaining;
	private double _guardBreakRemaining;
	private double _jumpHoldRemaining;
	private int _remainingAirJumps;
	private int _remainingAirDashes;
	private int _currentHealth;
	private float _currentStamina;
	private float _facingDirection = 1f;
	private Area2D? _attackProbe;
	private CollisionShape2D? _attackProbeCollisionShape;
	private CanvasItem? _bodyVisual;
	private CanvasItem? _guardEffectVisual;
	private CanvasItem? _fallbackBodyVisual;
	private CanvasItem? _fallbackFeetVisual;
	private AnimatedSprite2D? _animatedSprite;
	private AnimatedSprite2D? _weaponSprite;
	private Vector2 _attackProbeBasePosition;
	private Vector2 _bodySpriteBasePosition;
	private Vector2 _bodySpriteBaseScale = Vector2.One;
	private Vector2 _weaponSpriteBasePosition;
	private Vector2 _weaponSpriteBaseScale = Vector2.One;
	private int _baseMaxHealth;
	private int _baseAttackDamage;
	private float _baseMaxStamina;
	private float _baseAttackPostureDamage;
	private float _baseGuardStaminaDrainPerSecond;
	private float _baseGuardHitStaminaCost;
	private float _baseDeflectPostureDamage;
	private float _baseDeflectStaminaCost;
	private bool _isAttackActive;
	private bool _isGuarding;
	private bool _isDashing;
	private bool _isDead;
	private bool _jumpCutApplied;
	private string _currentAnimationAction = "idle";
	private double _animationOverrideRemaining;
	private double _deathAnimationRemaining;

	public int CurrentHealth => _currentHealth;
	public float CurrentStamina => _currentStamina;
	private PlayerState? CurrentPlayerState => _stateMachine?.CurrentState as PlayerState;

	public override void _Ready()
	{
		GameInputActions.EnsureDefaults();
		GameSettingsManager.Instance?.ApplyAll();
		GameSettingsManager.Instance?.ApplyGameplaySettings(this);
		CaptureBaseCombatValues();
		ApplyProgressionModifiers(false);
		AddToGroup("player");

		_currentHealth = MaxHealth;
		_currentStamina = MaxStamina;
		_remainingAirJumps = MaxAirJumps;
		_remainingAirDashes = MaxAirDashes;
		_stateMachine = new StateMachine<PlayerController>(this);
		_attackProbe = AttackProbePath != null && !AttackProbePath.IsEmpty ? GetNodeOrNull<Area2D>(AttackProbePath) : null;
		_attackProbeCollisionShape = AttackProbeCollisionShapePath != null && !AttackProbeCollisionShapePath.IsEmpty
			? GetNodeOrNull<CollisionShape2D>(AttackProbeCollisionShapePath)
			: null;
		_animatedSprite = AnimatedSpritePath != null && !AnimatedSpritePath.IsEmpty
			? GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath)
			: null;
		_weaponSprite = WeaponSpritePath != null && !WeaponSpritePath.IsEmpty
			? GetNodeOrNull<AnimatedSprite2D>(WeaponSpritePath)
			: null;
		_bodyVisual = BodyVisualPath != null && !BodyVisualPath.IsEmpty ? GetNodeOrNull<CanvasItem>(BodyVisualPath) : null;
		_guardEffectVisual = HurtboxVisualPath != null && !HurtboxVisualPath.IsEmpty ? GetNodeOrNull<CanvasItem>(HurtboxVisualPath) : null;
		_fallbackBodyVisual = GetNodeOrNull<CanvasItem>("Body");
		_fallbackFeetVisual = GetNodeOrNull<CanvasItem>("Feet");
		EnsureVisualLayerSeparation();
		ConfigureAnimatedSprite();

		_stateMachine.Register(new PlayerIdleState(this, _stateMachine));
		_stateMachine.Register(new PlayerRunState(this, _stateMachine));
		_stateMachine.Register(new PlayerJumpState(this, _stateMachine));
		_stateMachine.Register(new PlayerFallState(this, _stateMachine));
		_stateMachine.Register(new PlayerAttackState(this, _stateMachine));
		_stateMachine.Register(new PlayerGuardState(this, _stateMachine));
		_stateMachine.Register(new PlayerDashState(this, _stateMachine));
		_stateMachine.ChangeState<PlayerIdleState>();

		if (_attackProbe != null)
		{
			_attackProbeBasePosition = _attackProbe.Position;
		}

		if (_attackProbeCollisionShape != null)
		{
			_attackProbeCollisionShape.Disabled = true;
		}

		HealthChanged?.Invoke(_currentHealth, MaxHealth);
		StaminaChanged?.Invoke(_currentStamina, MaxStamina);

		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.AttributeChanged += OnProgressionAttributeChanged;
			ProgressionManager.Instance.AttributesReset += OnProgressionReset;
		}

		if (WorldManager.Instance != null)
		{
			WorldManager.Instance.WorldChanged += OnWorldChanged;
		}
	}

	public override void _ExitTree()
	{
		if (WorldManager.Instance != null)
		{
			WorldManager.Instance.WorldChanged -= OnWorldChanged;
		}

		if (ProgressionManager.Instance != null)
		{
			ProgressionManager.Instance.AttributeChanged -= OnProgressionAttributeChanged;
			ProgressionManager.Instance.AttributesReset -= OnProgressionReset;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_isDead)
		{
			return;
		}

		var now = GetGameTime();

		if (@event.IsActionPressed(GameInputActions.Jump))
		{
			_inputBuffer.Buffer(GameInputActions.Jump, now);
		}

		if (@event.IsActionPressed(GameInputActions.Attack))
		{
			_inputBuffer.Buffer(GameInputActions.Attack, now);
		}

		if (@event.IsActionPressed(GameInputActions.Guard))
		{
			_lastGuardPressedAt = now;
		}

		if (@event.IsActionPressed(GameInputActions.Dash))
		{
			_inputBuffer.Buffer(GameInputActions.Dash, now);
		}

		if (@event.IsActionPressed(GameInputActions.SwitchWorld))
		{
			_inputBuffer.Buffer(GameInputActions.SwitchWorld, now);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			UpdateTransientAnimation(delta);
			_deathAnimationRemaining = Math.Max(0d, _deathAnimationRemaining - delta);
			if (_deathAnimationRemaining <= 0d)
			{
				QueueFree();
			}

			return;
		}

		var now = GetGameTime();
		_inputBuffer.ExpireOlderThan(now, InputBufferTime);
		UpdateFacingDirection();
		UpdateSpriteFacing();
		UpdateAttackProbeTransform();
		UpdateStamina(delta);
		UpdateGuardBreak(delta);
		UpdateTransientAnimation(delta);
		_dashMomentumRemaining = Math.Max(0d, _dashMomentumRemaining - delta);

		if (IsOnFloor())
		{
			_lastGroundedAt = now;
			_remainingAirJumps = MaxAirJumps;
			_remainingAirDashes = MaxAirDashes;
		}

		if (_inputBuffer.Consume(GameInputActions.SwitchWorld, now, InputBufferTime))
		{
			WorldManager.Instance?.ToggleWorld();
		}

		_stateMachine?.PhysicsUpdate(delta);

		if (CurrentPlayerState?.SuppressDefaultPhysics != true)
		{
			ApplyHorizontalMovement(delta);
			ApplyGravity(delta);
			MoveAndSlide();
		}

		ProcessAttackHits();

		if (IsOnFloor())
		{
			_lastGroundedAt = now;
			_remainingAirJumps = MaxAirJumps;
			_remainingAirDashes = MaxAirDashes;
		}
	}

	public bool HasBufferedJump()
	{
		return _inputBuffer.HasBuffered(GameInputActions.Jump, GetGameTime(), InputBufferTime);
	}

	public bool HasBufferedAttack()
	{
		return _inputBuffer.HasBuffered(GameInputActions.Attack, GetGameTime(), InputBufferTime);
	}

	public bool HasBufferedDash()
	{
		return _inputBuffer.HasBuffered(GameInputActions.Dash, GetGameTime(), InputBufferTime);
	}

	public void ConsumeJumpBuffer()
	{
		_inputBuffer.Consume(GameInputActions.Jump, GetGameTime(), InputBufferTime);
	}

	public void ConsumeAttackBuffer()
	{
		_inputBuffer.Consume(GameInputActions.Attack, GetGameTime(), InputBufferTime);
	}

	public void ConsumeDashBuffer()
	{
		_inputBuffer.Consume(GameInputActions.Dash, GetGameTime(), InputBufferTime);
	}

	public float GetMoveInput()
	{
		return Input.GetAxis(GameInputActions.MoveLeft, GameInputActions.MoveRight);
	}

	public bool IsGrounded()
	{
		return IsOnFloor();
	}

	public bool CanStartJump()
	{
		if (IsOnFloor())
		{
			return true;
		}

		if (GetGameTime() - _lastGroundedAt <= CoyoteTime)
		{
			return true;
		}

		return _remainingAirJumps > 0;
	}

	public bool WantsToGuard()
	{
		return Input.IsActionPressed(GameInputActions.Guard) && _guardBreakRemaining <= 0d && _currentStamina > 0f;
	}

	public bool CanGuard()
	{
		return IsGrounded() && _guardBreakRemaining <= 0d && _currentStamina > 0f;
	}

	public bool CanDash()
	{
		if (GetGameTime() - _lastDashAt < DashCooldown || _currentStamina < DashStaminaCost)
		{
			return false;
		}

		return IsOnFloor() || _remainingAirDashes > 0;
	}

	public EnemyCombatant? FindExecutionTarget()
	{
		EnemyCombatant? closestTarget = null;
		var closestDistance = float.MaxValue;

		foreach (Node node in GetTree().GetNodesInGroup("enemy"))
		{
			if (node is not EnemyCombatant enemy || !IsInstanceValid(enemy))
			{
				continue;
			}

			if (!enemy.CanBeExecutedBy(this, ExecutionRange, ExecutionVerticalTolerance))
			{
				continue;
			}

			var distance = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
			if (distance >= closestDistance)
			{
				continue;
			}

			closestDistance = distance;
			closestTarget = enemy;
		}

		return closestTarget;
	}

	public void BeginGuard()
	{
		_isGuarding = true;

		if (_guardEffectVisual != null)
		{
			_guardEffectVisual.Visible = true;
			_guardEffectVisual.Modulate = new Color(0.7f, 0.9f, 1f, 0.85f);
		}
	}

	public void EndGuard()
	{
		_isGuarding = false;

		if (_guardEffectVisual != null)
		{
			_guardEffectVisual.Visible = false;
		}
	}

	public void BeginExecutionAttack(EnemyCombatant target)
	{
		FaceTowards(target.GlobalPosition.X);
		ConsumeStamina(ExecutionStaminaCost);
	}

	public void EndExecutionAttack()
	{
		if (_bodyVisual != null && _lastDamageTakenAt + DamageInvulnerabilityTime < GetGameTime())
		{
			_bodyVisual.Modulate = Colors.White;
		}
	}

	public bool UpdateExecutionApproach(EnemyCombatant target, double delta)
	{
		if (!IsInstanceValid(target))
		{
			return true;
		}

		FaceTowards(target.GlobalPosition.X);

		float direction = Mathf.Sign(target.GlobalPosition.X - GlobalPosition.X);
		if (Mathf.IsZeroApprox(direction))
		{
			direction = _facingDirection;
		}

		var destinationX = target.GlobalPosition.X - direction * ExecutionStopDistance;
		var currentPosition = GlobalPosition;
		var nextX = Mathf.MoveToward(currentPosition.X, destinationX, ExecutionApproachSpeed * (float)delta);
		GlobalPosition = new Vector2(nextX, currentPosition.Y);
		Velocity = new Vector2(0f, Velocity.Y);

		return Mathf.Abs(destinationX - nextX) <= 2f;
	}

	public bool TryExecuteTarget(EnemyCombatant target)
	{
		if (!IsInstanceValid(target))
		{
			return false;
		}

		FaceTowards(target.GlobalPosition.X);
		return target.TryExecute(this);
	}

	public void BeginAttack()
	{
		ConsumeStamina(AttackStaminaCost);
		_isAttackActive = true;
		_damagedTargetsThisAttack.Clear();

		if (_attackProbeCollisionShape != null)
		{
			_attackProbeCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		}
	}

	public void BeginDash()
	{
		_lastDashAt = GetGameTime();
		if (!IsOnFloor() && _remainingAirDashes > 0)
		{
			_remainingAirDashes -= 1;
		}

		ConsumeStamina(DashStaminaCost);
		_isDashing = true;
		_dashMomentumRemaining = 0d;
		Velocity = new Vector2(0f, IsOnFloor() ? 0f : Velocity.Y * 0.2f);
		PlayStateAnimation("run", true);

		if (_guardEffectVisual != null)
		{
			_guardEffectVisual.Visible = true;
			_guardEffectVisual.Modulate = new Color(1f, 1f, 1f, 0.4f);
		}
	}

	public void UpdateDashTravel(double delta)
	{
		var direction = _facingDirection;
		if (Mathf.IsZeroApprox(direction))
		{
			direction = 1f;
		}

		Velocity = new Vector2(direction * DashSpeed, IsOnFloor() ? 0f : Velocity.Y);
		MoveAndSlide();
	}

	public void EndDash()
	{
		_isDashing = false;
		var carryVelocity = DashSpeed * DashExitSpeedMultiplier * _facingDirection;
		Velocity = new Vector2(carryVelocity, Velocity.Y);
		_dashMomentumRemaining = DashMomentumDuration;

		if (_guardEffectVisual != null && !_isGuarding)
		{
			_guardEffectVisual.Visible = false;
		}
	}

	public void EndAttack()
	{
		_isAttackActive = false;
		_damagedTargetsThisAttack.Clear();

		if (_attackProbeCollisionShape != null)
		{
			_attackProbeCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}
	}

	public void CommitJump()
	{
		var usedCoyote = !IsOnFloor() && GetGameTime() - _lastGroundedAt <= CoyoteTime;

		if (!IsOnFloor() && !usedCoyote && _remainingAirJumps > 0)
		{
			_remainingAirJumps -= 1;
		}

		Velocity = new Vector2(Velocity.X, -JumpSpeed);
		_jumpHoldRemaining = JumpHoldMaxTime;
		_jumpCutApplied = false;
		_lastGroundedAt = double.NegativeInfinity;
	}

	public void PlayStateAnimation(string action, bool restart = false)
	{
		_currentAnimationAction = action;
		_animationOverrideRemaining = 0d;
		PlayResolvedAnimation(action, restart);
	}

	public void PlayParryAnimation()
	{
		_animationOverrideRemaining = GetAnimationDuration("parry");
		PlayResolvedAnimation("parry", true);
	}

	public double GetAnimationDurationForAction(string action)
	{
		return GetAnimationDuration(action);
	}

	public void PlayTransientAnimation(string action)
	{
		_animationOverrideRemaining = GetAnimationDuration(action);
		PlayResolvedAnimation(action, true);
	}

	public void ApplyDamage(in DamageInfo damageInfo)
	{
		if (_isDead)
		{
			return;
		}

		var now = GetGameTime();
		if (!_isGuarding && now - _lastDamageTakenAt < DamageInvulnerabilityTime)
		{
			return;
		}

		var isDeflect = _isGuarding
			&& damageInfo.CanBeGuarded
			&& now - _lastGuardPressedAt <= GuardDeflectWindow;

		if (_isGuarding && damageInfo.CanBeGuarded)
		{
			if (isDeflect)
			{
				ConsumeStamina(DeflectStaminaCost);

				if (damageInfo.Source is IDeflectResponder deflectResponder)
				{
					deflectResponder.OnDeflected(DeflectPostureDamage, this);
				}

				if (_guardEffectVisual != null)
				{
					_guardEffectVisual.Visible = true;
					_guardEffectVisual.Modulate = new Color(0.82f, 1f, 1f, 1f);
				}

				PlayParryAnimation();

				return;
			}

			ConsumeStamina(GuardHitStaminaCost);
			Velocity += damageInfo.Knockback * 0.3f;

			if (_currentStamina <= 0f)
			{
				TriggerGuardBreak();
			}

			return;
		}

		_lastDamageTakenAt = now;
		_currentHealth = Mathf.Max(0, _currentHealth - damageInfo.Amount);
		Velocity += damageInfo.Knockback;
		HealthChanged?.Invoke(_currentHealth, MaxHealth);
		ConsumeStamina(Mathf.Max(GuardHitStaminaCost * 0.65f, damageInfo.PostureDamage * 0.5f));

		if (_bodyVisual != null)
		{
			_bodyVisual.Modulate = new Color(1f, 0.55f, 0.55f, 1f);
		}

		PlayTransientAnimation(_currentHealth <= 0 ? "dead" : "hurt");

		if (_currentHealth <= 0)
		{
			TriggerDeath();
		}
	}

	public bool RestoreHealth(int amount)
	{
		if (amount <= 0 || _currentHealth >= MaxHealth)
		{
			return false;
		}

		_currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
		HealthChanged?.Invoke(_currentHealth, MaxHealth);
		return true;
	}

	public bool RestoreStamina(float amount)
	{
		if (amount <= 0f || _currentStamina >= MaxStamina)
		{
			return false;
		}

		_currentStamina = Mathf.Min(MaxStamina, _currentStamina + amount);
		StaminaChanged?.Invoke(_currentStamina, MaxStamina);
		return true;
	}

	private void ApplyHorizontalMovement(double delta)
	{
		var moveInput = GetMoveInput();
		var speedMultiplier = 1f;

		if (_stateMachine?.CurrentState is PlayerState playerState)
		{
			speedMultiplier = playerState.SpeedMultiplier;
		}

		var targetSpeed = moveInput * MoveSpeed * speedMultiplier;
		var acceleration = IsOnFloor() ? GroundAcceleration : AirAcceleration;
		var deceleration = IsOnFloor() ? GroundDeceleration : AirDeceleration;
		if (_dashMomentumRemaining > 0d && (Mathf.IsZeroApprox(moveInput) || Mathf.Sign(moveInput) == Mathf.Sign(Velocity.X)))
		{
			deceleration *= DashMomentumDecelerationMultiplier;
		}

		var weight = moveInput == 0f ? deceleration : acceleration;

		Velocity = new Vector2(
			Mathf.MoveToward(Velocity.X, targetSpeed, weight * (float)delta),
			Velocity.Y);
	}

	private void ApplyGravity(double delta)
	{
		if (IsOnFloor() && Velocity.Y > 0f)
		{
			Velocity = new Vector2(Velocity.X, 0f);
			_jumpHoldRemaining = 0d;
			_jumpCutApplied = true;
			return;
		}

		if (IsOnFloor())
		{
			return;
		}

		var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
		var gravityScale = GetCurrentGravityScale(delta);
		Velocity += new Vector2(0f, gravity * gravityScale * (float)delta);
	}

	private float GetCurrentGravityScale(double delta)
	{
		if (Velocity.Y >= 0f)
		{
			_jumpHoldRemaining = 0d;
			return FallGravityScale;
		}

		if (Input.IsActionPressed(GameInputActions.Jump) && _jumpHoldRemaining > 0d)
		{
			_jumpHoldRemaining = Math.Max(0d, _jumpHoldRemaining - delta);
			return JumpHoldGravityScale;
		}

		if (!Input.IsActionPressed(GameInputActions.Jump) && !_jumpCutApplied)
		{
			Velocity = new Vector2(Velocity.X, Velocity.Y * JumpCutVelocityMultiplier);
			_jumpCutApplied = true;
		}

		if (Mathf.Abs(Velocity.Y) <= JumpApexVelocityThreshold)
		{
			return JumpApexGravityScale;
		}

		return RiseGravityScale;
	}

	private void ProcessAttackHits()
	{
		if (!_isAttackActive || _attackProbe == null)
		{
			if (_bodyVisual != null && _lastDamageTakenAt + DamageInvulnerabilityTime < GetGameTime())
			{
				_bodyVisual.Modulate = Colors.White;
			}

			return;
		}

		foreach (var area in _attackProbe.GetOverlappingAreas())
		{
			if (area is not DamageReceiver damageReceiver)
			{
				continue;
			}

			var instanceId = damageReceiver.GetInstanceId();
			if (!_damagedTargetsThisAttack.Add(instanceId))
			{
				continue;
			}

			var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
			var knockback = new Vector2(_facingDirection * 200f, -80f);
			damageReceiver.ReceiveDamage(new DamageInfo(
				AttackDamage,
				currentWorld,
				this,
				knockback,
				AttackPostureDamage));
		}

		if (_bodyVisual != null && _lastDamageTakenAt + DamageInvulnerabilityTime < GetGameTime())
		{
			_bodyVisual.Modulate = Colors.White;
		}
	}

	private void UpdateFacingDirection()
	{
		var moveInput = GetMoveInput();
		if (!Mathf.IsZeroApprox(moveInput))
		{
			_facingDirection = Mathf.Sign(moveInput);
		}
	}

	public void FaceTowards(float worldX)
	{
		var direction = Mathf.Sign(worldX - GlobalPosition.X);
		if (!Mathf.IsZeroApprox(direction))
		{
			_facingDirection = direction;
		}
	}

	private void UpdateSpriteFacing()
	{
		if (_animatedSprite == null)
		{
			if (_weaponSprite != null)
			{
				_weaponSprite.FlipH = _facingDirection < 0f;
			}

			return;
		}

		_animatedSprite.FlipH = _facingDirection < 0f;
		if (_weaponSprite != null)
		{
			_weaponSprite.FlipH = _animatedSprite.FlipH;
		}
	}

	private void UpdateAttackProbeTransform()
	{
		if (_attackProbe == null)
		{
			return;
		}

		_attackProbe.Position = new Vector2(Mathf.Abs(_attackProbeBasePosition.X) * _facingDirection, _attackProbeBasePosition.Y);
	}

	private void UpdateStamina(double delta)
	{
		if (_isGuarding)
		{
			ConsumeStamina(GuardStaminaDrainPerSecond * (float)delta);

			if (_currentStamina <= 0f)
			{
				TriggerGuardBreak();
			}

			return;
		}

		if (_isDashing)
		{
			return;
		}

		if (_currentStamina >= MaxStamina)
		{
			return;
		}

		_currentStamina = Mathf.Min(MaxStamina, _currentStamina + StaminaRecoveryPerSecond * (float)delta);
		StaminaChanged?.Invoke(_currentStamina, MaxStamina);
	}

	private void UpdateGuardBreak(double delta)
	{
		if (_guardBreakRemaining <= 0d)
		{
			return;
		}

		_guardBreakRemaining -= delta;

		if (_guardBreakRemaining <= 0d && _guardEffectVisual != null && !_isGuarding)
		{
			_guardEffectVisual.Visible = false;
		}
	}

	private void ConsumeStamina(float amount)
	{
		if (amount <= 0f)
		{
			return;
		}

		_currentStamina = Mathf.Clamp(_currentStamina - amount, 0f, MaxStamina);
		StaminaChanged?.Invoke(_currentStamina, MaxStamina);
	}

	private void TriggerGuardBreak()
	{
		_guardBreakRemaining = GuardBreakDuration;
		_isGuarding = false;
		_currentStamina = 0f;
		StaminaChanged?.Invoke(_currentStamina, MaxStamina);

		if (_guardEffectVisual != null)
		{
			_guardEffectVisual.Visible = true;
			_guardEffectVisual.Modulate = new Color(1f, 0.72f, 0.62f, 0.95f);
		}
	}

	private void TriggerDeath()
	{
		_isDead = true;
		_isGuarding = false;
		_isAttackActive = false;
		Velocity = Vector2.Zero;
		_deathAnimationRemaining = Math.Max(0.5d, GetAnimationDuration("dead"));

		if (_guardEffectVisual != null)
		{
			_guardEffectVisual.Visible = false;
		}

		if (_attackProbeCollisionShape != null)
		{
			_attackProbeCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}
	}

	private void CaptureBaseCombatValues()
	{
		_baseMaxHealth = MaxHealth;
		_baseAttackDamage = AttackDamage;
		_baseMaxStamina = MaxStamina;
		_baseAttackPostureDamage = AttackPostureDamage;
		_baseGuardStaminaDrainPerSecond = GuardStaminaDrainPerSecond;
		_baseGuardHitStaminaCost = GuardHitStaminaCost;
		_baseDeflectPostureDamage = DeflectPostureDamage;
		_baseDeflectStaminaCost = DeflectStaminaCost;
	}

	private void ApplyProgressionModifiers(bool preserveCurrentRatios)
	{
		if (ProgressionManager.Instance == null)
		{
			return;
		}

		var healthRatio = MaxHealth > 0 ? Mathf.Clamp((float)_currentHealth / MaxHealth, 0f, 1f) : 1f;
		var staminaRatio = MaxStamina > 0f ? Mathf.Clamp(_currentStamina / MaxStamina, 0f, 1f) : 1f;
		var modifiers = ProgressionManager.Instance.BuildCombatModifiers();

		MaxHealth = Mathf.Max(1, _baseMaxHealth + modifiers.BonusHealth);
		AttackDamage = Mathf.Max(1, _baseAttackDamage + modifiers.BonusAttackDamage);
		MaxStamina = Mathf.Max(1f, _baseMaxStamina + modifiers.BonusStamina);
		AttackPostureDamage = _baseAttackPostureDamage * modifiers.AttackPostureMultiplier * modifiers.SoulAttunementMultiplier;
		DeflectPostureDamage = _baseDeflectPostureDamage * modifiers.DeflectPostureMultiplier * modifiers.SoulAttunementMultiplier;
		GuardStaminaDrainPerSecond = _baseGuardStaminaDrainPerSecond * modifiers.GuardStaminaMultiplier;
		GuardHitStaminaCost = _baseGuardHitStaminaCost * modifiers.GuardStaminaMultiplier;
		DeflectStaminaCost = _baseDeflectStaminaCost * modifiers.GuardStaminaMultiplier;

		if (preserveCurrentRatios)
		{
			_currentHealth = Mathf.Clamp(Mathf.RoundToInt(MaxHealth * healthRatio), 1, MaxHealth);
			_currentStamina = Mathf.Clamp(MaxStamina * staminaRatio, 0f, MaxStamina);
			HealthChanged?.Invoke(_currentHealth, MaxHealth);
			StaminaChanged?.Invoke(_currentStamina, MaxStamina);
		}
	}

	private void OnProgressionAttributeChanged(PlayerAttributeType _, int __)
	{
		ApplyProgressionModifiers(true);
	}

	private void OnProgressionReset()
	{
		ApplyProgressionModifiers(true);
	}

	private void ConfigureAnimatedSprite()
	{
		_bodyAnimationScaleByName.Clear();
		_weaponAnimationScaleByName.Clear();

		if (_animatedSprite == null)
		{
			_bodyVisual = _fallbackBodyVisual;
			return;
		}

		_bodySpriteBasePosition = _animatedSprite.Position;
		_bodySpriteBaseScale = _animatedSprite.Scale;

		var spriteFrames = BuildSpriteFrames(AnimationFramesRoot, _bodyAnimationScaleByName);
		if (spriteFrames == null)
		{
			_bodyVisual = _fallbackBodyVisual;
			return;
		}

		_animatedSprite.SpriteFrames = spriteFrames;
		_animatedSprite.Visible = true;
		_bodyVisual = _animatedSprite;

		if (_fallbackBodyVisual != null)
		{
			_fallbackBodyVisual.Visible = false;
		}

		if (_fallbackFeetVisual != null)
		{
			_fallbackFeetVisual.Visible = false;
		}

		if (_weaponSprite != null)
		{
			_weaponSpriteBasePosition = _weaponSprite.Position;
			_weaponSpriteBaseScale = _weaponSprite.Scale;
			var weaponFrames = BuildSpriteFrames(WeaponAnimationFramesRoot, _weaponAnimationScaleByName);
			if (weaponFrames != null)
			{
				_weaponSprite.SpriteFrames = weaponFrames;
				_weaponSprite.Visible = true;
			}
			else
			{
				_weaponSprite.Visible = false;
			}
		}

		ApplyAnimationPresentation(ResolveAnimationName(_currentAnimationAction));
	}

	private SpriteFrames? BuildSpriteFrames(string framesRoot, Dictionary<string, float> scaleByAnimation)
	{
		var spriteFrames = new SpriteFrames();
		var loadedAnyAnimation = false;
		foreach (var pair in LoadAnimationManifest(framesRoot))
		{
			scaleByAnimation[pair.Key] = pair.Value;
		}

		foreach (var worldName in new[] { "reality", "soul" })
		{
			foreach (var definition in AnimationDefinitions)
			{
				var animationName = $"{worldName}_{definition.ActionName}";
				var framePrefix = $"player_{worldName}_{definition.ActionName}";
				var frameDirectory = $"{framesRoot}/{framePrefix}_frames_v1";
				var loadedFrameCount = 0;

				for (var frameIndex = 0; frameIndex < definition.FrameCount; frameIndex++)
				{
					var framePath = $"{frameDirectory}/{framePrefix}_{frameIndex:00}.png";
					var texture = ResourceLoader.Load<Texture2D>(framePath);
					if (texture == null)
					{
						continue;
					}

					if (loadedFrameCount == 0)
					{
						spriteFrames.AddAnimation(animationName);
						spriteFrames.SetAnimationSpeed(animationName, definition.Fps);
						spriteFrames.SetAnimationLoop(animationName, definition.Loop);
					}

					spriteFrames.AddFrame(animationName, texture);
					loadedFrameCount += 1;
				}

				loadedAnyAnimation |= loadedFrameCount > 0;
			}
		}

		return loadedAnyAnimation ? spriteFrames : null;
	}

	private void PlayResolvedAnimation(string action, bool restart)
	{
		if (_animatedSprite?.SpriteFrames == null)
		{
			return;
		}

		var animationName = ResolveAnimationName(action);
		if (animationName == null)
		{
			return;
		}

		var shouldRestart = restart || _animatedSprite.Animation != animationName || !_animatedSprite.IsPlaying();
		if (!shouldRestart)
		{
			ApplyAnimationPresentation(animationName);
			return;
		}

		_animatedSprite.Play(animationName);
		ApplyAnimationPresentation(animationName);
	}

	private string? ResolveAnimationName(string action)
	{
		if (_animatedSprite?.SpriteFrames == null)
		{
			return null;
		}

		var preferred = $"{GetCurrentWorldAnimationPrefix()}_{action}";
		if (_animatedSprite.SpriteFrames.HasAnimation(preferred))
		{
			return preferred;
		}

		var realityFallback = $"reality_{action}";
		if (_animatedSprite.SpriteFrames.HasAnimation(realityFallback))
		{
			return realityFallback;
		}

		var idleFallback = $"{GetCurrentWorldAnimationPrefix()}_idle";
		return _animatedSprite.SpriteFrames.HasAnimation(idleFallback) ? idleFallback : null;
	}

	private string GetCurrentWorldAnimationPrefix()
	{
		return (WorldManager.Instance?.CurrentWorld ?? WorldType.Reality) == WorldType.Soul ? "soul" : "reality";
	}

	private double GetAnimationDuration(string action)
	{
		if (_animatedSprite?.SpriteFrames == null)
		{
			return 0d;
		}

		var animationName = ResolveAnimationName(action);
		if (animationName == null)
		{
			return 0d;
		}

		var frameCount = _animatedSprite.SpriteFrames.GetFrameCount(animationName);
		var speed = _animatedSprite.SpriteFrames.GetAnimationSpeed(animationName);
		if (frameCount <= 0 || speed <= 0f)
		{
			return 0d;
		}

		return frameCount / speed;
	}

	private void UpdateTransientAnimation(double delta)
	{
		if (_animationOverrideRemaining <= 0d)
		{
			return;
		}

		_animationOverrideRemaining = Math.Max(0d, _animationOverrideRemaining - delta);
		if (_animationOverrideRemaining <= 0d)
		{
			PlayResolvedAnimation(_currentAnimationAction, true);
		}
	}

	private void OnWorldChanged(WorldType _)
	{
		PlayResolvedAnimation(_animationOverrideRemaining > 0d ? "parry" : _currentAnimationAction, true);
	}

	private void EnsureVisualLayerSeparation()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		if (_weaponSprite != null)
		{
			return;
		}

		_weaponSprite = new AnimatedSprite2D
		{
			Name = "WeaponSprite",
			Visible = false,
			Position = _animatedSprite.Position,
			Scale = _animatedSprite.Scale,
			ZIndex = _animatedSprite.ZIndex + 1,
		};
		AddChild(_weaponSprite);
	}

	private Dictionary<string, float> LoadAnimationManifest(string framesRoot)
	{
		var scaleMap = new Dictionary<string, float>(StringComparer.Ordinal);
		if (string.IsNullOrWhiteSpace(framesRoot))
		{
			return scaleMap;
		}

		var manifestPath = ProjectSettings.GlobalizePath($"{framesRoot.TrimEnd('/')}/manifest.json");
		if (!File.Exists(manifestPath))
		{
			return scaleMap;
		}

		try
		{
			var manifestJson = File.ReadAllText(manifestPath);
			var entries = JsonSerializer.Deserialize<List<PlayerAnimationManifestEntry>>(manifestJson);
			if (entries == null)
			{
				return scaleMap;
			}

			foreach (var entry in entries)
			{
				var animationName = TryParseAnimationNameFromSource(entry.Source);
				if (animationName == null)
				{
					continue;
				}

				scaleMap[animationName] = Mathf.Max(0.01f, entry.Scale);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Failed to load animation manifest '{framesRoot}': {exception.Message}");
		}

		return scaleMap;
	}

	private void ApplyAnimationPresentation(string? animationName)
	{
		if (animationName == null)
		{
			return;
		}

		if (_animatedSprite != null)
		{
			var bodyScale = _bodyAnimationScaleByName.TryGetValue(animationName, out var scaleMultiplier)
				? scaleMultiplier
				: 1f;
			_animatedSprite.Scale = _bodySpriteBaseScale * bodyScale;
			_animatedSprite.Position = _bodySpriteBasePosition;
		}

		if (_weaponSprite?.SpriteFrames == null)
		{
			return;
		}

		var weaponAnimation = ResolveWeaponAnimationName(animationName);
		if (weaponAnimation == null)
		{
			_weaponSprite.Visible = false;
			return;
		}

		var shouldRestart = _weaponSprite.Animation != weaponAnimation || !_weaponSprite.IsPlaying();
		if (shouldRestart)
		{
			_weaponSprite.Play(weaponAnimation);
		}

		var weaponScale = _weaponAnimationScaleByName.TryGetValue(weaponAnimation, out var weaponScaleMultiplier)
			? weaponScaleMultiplier
			: 1f;
		_weaponSprite.Scale = _weaponSpriteBaseScale * weaponScale;
		_weaponSprite.Position = _weaponSpriteBasePosition;
		_weaponSprite.FlipH = _facingDirection < 0f;
		_weaponSprite.Visible = true;
	}

	private string? ResolveWeaponAnimationName(string fallbackAnimation)
	{
		if (_weaponSprite?.SpriteFrames == null)
		{
			return null;
		}

		if (_weaponSprite.SpriteFrames.HasAnimation(fallbackAnimation))
		{
			return fallbackAnimation;
		}

		var idleFallback = $"{GetCurrentWorldAnimationPrefix()}_idle";
		return _weaponSprite.SpriteFrames.HasAnimation(idleFallback) ? idleFallback : null;
	}

	private static string? TryParseAnimationNameFromSource(string? source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return null;
		}

		var fileName = Path.GetFileNameWithoutExtension(source);
		const string prefix = "player_";
		const string suffix = "_sheet_v1";
		if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			|| !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return fileName.Substring(prefix.Length, fileName.Length - prefix.Length - suffix.Length);
	}

	private static double GetGameTime()
	{
		return Time.GetTicksMsec() / 1000.0;
	}
}
