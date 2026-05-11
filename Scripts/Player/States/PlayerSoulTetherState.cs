using EchoSpace.Core.Fsm;
using EchoSpace.Gameplay.Environment;
using Godot;

namespace EchoSpace.Player.States;

public sealed class PlayerSoulTetherState : PlayerState
{
    private SoulTetherAnchor? _anchor;

    public PlayerSoulTetherState(PlayerController context, StateMachine<PlayerController> stateMachine)
        : base(context, stateMachine)
    {
    }

    public override bool SuppressDefaultPhysics => true;
    public override float SpeedMultiplier => 0f;

    public override void Enter()
    {
        _anchor = Context.FindSoulTetherTarget();
        if (_anchor == null)
        {
            ReturnToMovementState();
            return;
        }

        Context.BeginSoulTether(_anchor);
    }

    public override void Exit()
    {
        Context.EndSoulTether();
        _anchor = null;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (_anchor == null || !GodotObject.IsInstanceValid(_anchor))
        {
            ReturnToMovementState();
            return;
        }

        if (!Context.UpdateSoulTetherTravel(_anchor, delta))
        {
            return;
        }

        ReturnToMovementState();
    }

    private void ReturnToMovementState()
    {
        if (Context.IsGrounded())
        {
            ReturnToGroundState();
            return;
        }

        StateMachine.ChangeState<PlayerFallState>();
    }
}
