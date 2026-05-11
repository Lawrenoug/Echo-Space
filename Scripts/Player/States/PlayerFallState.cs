using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override void Enter()
    {
        Context.PlayStateAnimation("fall", true);
    }

    public override void PhysicsUpdate(double delta)
    {
        if (TryEnterDash() || TryEnterSoulTether() || TryEnterAttack() || TryEnterJump())
        {
            return;
        }

        if (Context.IsGrounded())
        {
            ReturnToGroundState();
        }
    }
}
