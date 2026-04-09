using EchoSpace.Core.Fsm;
using EchoSpace.Gameplay.Enemies;
using Godot;

namespace EchoSpace.Player.States;

public sealed class PlayerAttackState : PlayerState
{
    private double _remainingDuration;
    private EnemyCombatant? _executionTarget;
    private bool _isExecutionAttack;
    private bool _executionResolved;

    public PlayerAttackState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override float SpeedMultiplier => _isExecutionAttack ? 0f : 0.45f;

    public override void Enter()
    {
        _executionTarget = Context.FindExecutionTarget();
        _isExecutionAttack = _executionTarget != null;
        _executionResolved = false;

        if (_isExecutionAttack && _executionTarget != null)
        {
            _remainingDuration = Context.ExecutionAttackDuration;
            Context.BeginExecutionAttack(_executionTarget);
            return;
        }

        _remainingDuration = Context.AttackDuration;
        Context.BeginAttack();
    }

    public override void Exit()
    {
        if (_isExecutionAttack)
        {
            Context.EndExecutionAttack();
        }
        else
        {
            Context.EndAttack();
        }

        _executionTarget = null;
        _isExecutionAttack = false;
        _executionResolved = false;
    }

    public override void PhysicsUpdate(double delta)
    {
        _remainingDuration -= delta;

        if (_isExecutionAttack)
        {
            if (!_executionResolved && _executionTarget != null && GodotObject.IsInstanceValid(_executionTarget))
            {
                if (Context.UpdateExecutionApproach(_executionTarget, delta))
                {
                    _executionResolved = Context.TryExecuteTarget(_executionTarget);
                }
            }

            if (_remainingDuration > 0d)
            {
                return;
            }

            if (!Context.IsGrounded())
            {
                StateMachine.ChangeState<PlayerFallState>();
                return;
            }

            ReturnToGroundState();
            return;
        }

        if (_remainingDuration > 0d)
        {
            return;
        }

        if (!Context.IsGrounded())
        {
            StateMachine.ChangeState<PlayerFallState>();
            return;
        }

        ReturnToGroundState();
    }
}
