using EchoSpace.Core.Fsm;

namespace EchoSpace.Player.States;

public sealed class PlayerDashState : PlayerState
{
    private double _remainingDuration;

    public PlayerDashState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override bool SuppressDefaultPhysics => true;
    public override float SpeedMultiplier => 0f;

    public override void Enter()
    {
        _remainingDuration = Context.DashDuration;
        Context.BeginDash();
    }

    public override void Exit()
    {
        Context.EndDash();
    }

    public override void PhysicsUpdate(double delta)
    {
        _remainingDuration -= delta;
        Context.UpdateDashTravel(delta);

        if (_remainingDuration > 0d)
        {
            return;
        }

        if (Context.IsGrounded())
        {
            ReturnToGroundState();
            return;
        }

        StateMachine.ChangeState<PlayerFallState>();
    }
}
