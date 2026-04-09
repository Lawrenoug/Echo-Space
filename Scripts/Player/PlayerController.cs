using System;
using System.Collections.Generic;
using EchoSpace.Core.Fsm;
using EchoSpace.Core.Input;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Combat;
using EchoSpace.Player.States;
using Godot;

namespace EchoSpace.Player;

public partial class PlayerController : CharacterBody2D, IDamageable
{
    public event Action<int, int>? HealthChanged;
    public event Action<float, float>? StaminaChanged;

    [ExportGroup("Movement")]
    [Export] public float MoveSpeed { get; set; } = 220f;
    [Export] public float GroundAcceleration { get; set; } = 1700f;
    [Export] public float GroundDeceleration { get; set; } = 1800f;
    [Export] public float AirAcceleration { get; set; } = 1250f;
    [Export] public float AirDeceleration { get; set; } = 1100f;
    [Export] public float JumpSpeed { get; set; } = 380f;
    [Export] public float AttackDuration { get; set; } = 0.18f;
    [Export] public int MaxAirJumps { get; set; }

    [ExportGroup("Feel Tuning")]
    [Export] public float InputBufferTime { get; set; } = 0.12f;
    [Export] public float CoyoteTime { get; set; } = 0.10f;
    [Export] public float JumpApexGravityScale { get; set; } = 0.5f;
    [Export] public float FallGravityScale { get; set; } = 1.8f;

    [ExportGroup("Combat")]
    [Export] public int MaxHealth { get; set; } = 5;
    [Export] public int AttackDamage { get; set; } = 1;
    [Export] public float AttackPostureDamage { get; set; } = 18f;
    [Export] public float DamageInvulnerabilityTime { get; set; } = 0.45f;
    [Export] public float MaxStamina { get; set; } = 100f;
    [Export] public float StaminaRecoveryPerSecond { get; set; } = 28f;
    [Export] public float AttackStaminaCost { get; set; } = 14f;
    [Export] public float GuardStaminaDrainPerSecond { get; set; } = 10f;
    [Export] public float GuardHitStaminaCost { get; set; } = 22f;
    [Export] public float GuardDeflectWindow { get; set; } = 0.18f;
    [Export] public float DeflectPostureDamage { get; set; } = 36f;
    [Export] public float DeflectStaminaCost { get; set; } = 6f;
    [Export] public float GuardBreakDuration { get; set; } = 0.7f;
    [Export] public NodePath? AttackProbePath { get; set; } = new("AttackProbe");
    [Export] public NodePath? AttackProbeCollisionShapePath { get; set; } = new("AttackProbe/CollisionShape2D");
    [Export] public NodePath? HurtboxVisualPath { get; set; } = new("GuardEffect");
    [Export] public NodePath? BodyVisualPath { get; set; } = new("Body");

    private readonly InputBuffer _inputBuffer = new();
    private readonly HashSet<ulong> _damagedTargetsThisAttack = new();

    private StateMachine<PlayerController>? _stateMachine;
    private double _lastGroundedAt = double.NegativeInfinity;
    private double _lastDamageTakenAt = double.NegativeInfinity;
    private double _lastGuardPressedAt = double.NegativeInfinity;
    private double _guardBreakRemaining;
    private int _remainingAirJumps;
    private int _currentHealth;
    private float _currentStamina;
    private float _facingDirection = 1f;
    private Area2D? _attackProbe;
    private CollisionShape2D? _attackProbeCollisionShape;
    private CanvasItem? _bodyVisual;
    private CanvasItem? _guardEffectVisual;
    private Vector2 _attackProbeBasePosition;
    private bool _isAttackActive;
    private bool _isGuarding;

    public int CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;

    public override void _Ready()
    {
        GameInputActions.EnsureDefaults();
        AddToGroup("player");

        _currentHealth = MaxHealth;
        _currentStamina = MaxStamina;
        _remainingAirJumps = MaxAirJumps;
        _stateMachine = new StateMachine<PlayerController>(this);
        _attackProbe = AttackProbePath != null && !AttackProbePath.IsEmpty ? GetNodeOrNull<Area2D>(AttackProbePath) : null;
        _attackProbeCollisionShape = AttackProbeCollisionShapePath != null && !AttackProbeCollisionShapePath.IsEmpty
            ? GetNodeOrNull<CollisionShape2D>(AttackProbeCollisionShapePath)
            : null;
        _bodyVisual = BodyVisualPath != null && !BodyVisualPath.IsEmpty ? GetNodeOrNull<CanvasItem>(BodyVisualPath) : null;
        _guardEffectVisual = HurtboxVisualPath != null && !HurtboxVisualPath.IsEmpty ? GetNodeOrNull<CanvasItem>(HurtboxVisualPath) : null;

        _stateMachine.Register(new PlayerIdleState(this, _stateMachine));
        _stateMachine.Register(new PlayerRunState(this, _stateMachine));
        _stateMachine.Register(new PlayerJumpState(this, _stateMachine));
        _stateMachine.Register(new PlayerFallState(this, _stateMachine));
        _stateMachine.Register(new PlayerAttackState(this, _stateMachine));
        _stateMachine.Register(new PlayerGuardState(this, _stateMachine));
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
    }

    public override void _Input(InputEvent @event)
    {
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

        if (@event.IsActionPressed(GameInputActions.SwitchWorld))
        {
            _inputBuffer.Buffer(GameInputActions.SwitchWorld, now);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var now = GetGameTime();
        _inputBuffer.ExpireOlderThan(now, InputBufferTime);
        UpdateFacingDirection();
        UpdateAttackProbeTransform();
        UpdateStamina(delta);
        UpdateGuardBreak(delta);

        if (IsOnFloor())
        {
            _lastGroundedAt = now;
            _remainingAirJumps = MaxAirJumps;
        }

        if (_inputBuffer.Consume(GameInputActions.SwitchWorld, now, InputBufferTime))
        {
            WorldManager.Instance?.ToggleWorld();
        }

        _stateMachine?.PhysicsUpdate(delta);
        ApplyHorizontalMovement(delta);
        ApplyGravity(delta);
        MoveAndSlide();
        ProcessAttackHits();

        if (IsOnFloor())
        {
            _lastGroundedAt = now;
            _remainingAirJumps = MaxAirJumps;
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

    public void ConsumeJumpBuffer()
    {
        _inputBuffer.Consume(GameInputActions.Jump, GetGameTime(), InputBufferTime);
    }

    public void ConsumeAttackBuffer()
    {
        _inputBuffer.Consume(GameInputActions.Attack, GetGameTime(), InputBufferTime);
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
        _lastGroundedAt = double.NegativeInfinity;
    }

    public void ApplyDamage(in DamageInfo damageInfo)
    {
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
        GD.Print($"Player ApplyDamage -> HP: {_currentHealth}/{MaxHealth}, Damage: {damageInfo.Amount}, PostureDamage: {damageInfo.PostureDamage}");
        Velocity += damageInfo.Knockback;
        HealthChanged?.Invoke(_currentHealth, MaxHealth);
        ConsumeStamina(Mathf.Max(GuardHitStaminaCost * 0.65f, damageInfo.PostureDamage * 0.5f));

        if (_bodyVisual != null)
        {
            _bodyVisual.Modulate = new Color(1f, 0.55f, 0.55f, 1f);
        }

        if (_currentHealth <= 0)
        {
            QueueFree();
        }
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
            return;
        }

        if (IsOnFloor())
        {
            return;
        }

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        var gravityScale = Velocity.Y < 0f ? JumpApexGravityScale : FallGravityScale;
        Velocity += new Vector2(0f, gravity * gravityScale * (float)delta);
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
        GD.Print($"Player ConsumeStamina -> Stamina: {_currentStamina}/{MaxStamina}, Cost: {amount}");
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

    private static double GetGameTime()
    {
        return Time.GetTicksMsec() / 1000.0;
    }
}
