using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerJumpState : PlayerState
{
    public PlayerJumpState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override void Enter()
    {
        Context.CommitJump();
    }

    public override void PhysicsUpdate(double delta)
    {
        if (TryEnterAttack())
        {
            return;
        }

        if (Context.Velocity.Y >= 0f)
        {
            StateMachine.ChangeState<PlayerFallState>();
        }
    }
}
