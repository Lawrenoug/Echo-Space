using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerAttackState : PlayerState
{
    private double _remainingDuration;

    public PlayerAttackState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override float SpeedMultiplier => 0.45f;

    public override void Enter()
    {
        _remainingDuration = Context.AttackDuration;
        Context.BeginAttack();
    }

    public override void Exit()
    {
        Context.EndAttack();
    }

    public override void PhysicsUpdate(double delta)
    {
        _remainingDuration -= delta;

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
