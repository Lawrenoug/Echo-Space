using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerGuardState : PlayerState
{
    public PlayerGuardState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override float SpeedMultiplier => 0.35f;

    public override void Enter()
    {
        Context.BeginGuard();
    }

    public override void Exit()
    {
        Context.EndGuard();
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!Context.IsGrounded())
        {
            StateMachine.ChangeState<PlayerFallState>();
            return;
        }

        if (!Context.WantsToGuard())
        {
            ReturnToGroundState();
        }
    }
}
