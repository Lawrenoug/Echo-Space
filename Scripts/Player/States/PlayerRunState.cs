using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerRunState : PlayerState
{
    public PlayerRunState(PlayerController context, StateMachine<PlayerController> stateMachine)
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

        if (Context.GetMoveInput() == 0f)
        {
            StateMachine.ChangeState<PlayerIdleState>();
        }
    }
}
