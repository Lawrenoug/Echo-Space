using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!Context.IsGrounded())
        {
            StateMachine.ChangeState<PlayerFallState>();
            return;
        }

        if (TryEnterAttack() || TryEnterJump())
        {
            return;
        }

        if (Context.GetMoveInput() != 0f)
        {
            StateMachine.ChangeState<PlayerRunState>();
        }
    }
}
