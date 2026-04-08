using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public abstract class PlayerState : State<PlayerController>
{
    protected PlayerState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public virtual float SpeedMultiplier => 1f;

    protected bool TryEnterAttack()
    {
        if (!Context.HasBufferedAttack())
        {
            return false;
        }

        Context.ConsumeAttackBuffer();
        StateMachine.ChangeState<PlayerAttackState>();
        return true;
    }

    protected bool TryEnterJump()
    {
        if (!Context.CanStartJump() || !Context.HasBufferedJump())
        {
            return false;
        }

        Context.ConsumeJumpBuffer();
        StateMachine.ChangeState<PlayerJumpState>();
        return true;
    }

    protected void ReturnToGroundState()
    {
        if (Context.GetMoveInput() == 0f)
        {
            StateMachine.ChangeState<PlayerIdleState>();
            return;
        }

        StateMachine.ChangeState<PlayerRunState>();
    }
}
