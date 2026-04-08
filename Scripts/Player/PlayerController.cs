using EchoSpace.Core.Fsm;
using EchoSpace.Core.Input;
using EchoSpace.Core.World;
using EchoSpace.Player.States;
using Godot;

namespace EchoSpace.Player;

public partial class PlayerController : CharacterBody2D
{
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

    private readonly InputBuffer _inputBuffer = new();

    private StateMachine<PlayerController>? _stateMachine;
    private double _lastGroundedAt = double.NegativeInfinity;
    private int _remainingAirJumps;

    public override void _Ready()
    {
        GameInputActions.EnsureDefaults();

        _remainingAirJumps = MaxAirJumps;
        _stateMachine = new StateMachine<PlayerController>(this);

        _stateMachine.Register(new PlayerIdleState(this, _stateMachine));
        _stateMachine.Register(new PlayerRunState(this, _stateMachine));
        _stateMachine.Register(new PlayerJumpState(this, _stateMachine));
        _stateMachine.Register(new PlayerFallState(this, _stateMachine));
        _stateMachine.Register(new PlayerAttackState(this, _stateMachine));
        _stateMachine.ChangeState<PlayerIdleState>();
    }

    public override void _UnhandledInput(InputEvent @event)
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

        if (@event.IsActionPressed(GameInputActions.SwitchWorld))
        {
            _inputBuffer.Buffer(GameInputActions.SwitchWorld, now);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var now = GetGameTime();
        _inputBuffer.ExpireOlderThan(now, InputBufferTime);

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

    private static double GetGameTime()
    {
        return Time.GetTicksMsec() / 1000.0;
    }
}
