using Godot;

namespace EchoSpace.Gameplay.Enemies;

public partial class EnemyController : EnemyCombatant
{
    [ExportGroup("Movement")]
    [Export] public float MoveSpeed { get; set; } = 65f;
    [Export] public float PatrolDistance { get; set; } = 96f;

    protected override void TickNeutral(double delta)
    {
        var leftLimit = SpawnPosition.X - PatrolDistance;
        var rightLimit = SpawnPosition.X + PatrolDistance;

        if (GlobalPosition.X <= leftLimit)
        {
            MoveDirection = 1;
        }
        else if (GlobalPosition.X >= rightLimit)
        {
            MoveDirection = -1;
        }

        SetHorizontalVelocity(MoveDirection * MoveSpeed);

        if (IsPlayerWithinAttackWindow())
        {
            StartWindup();
        }
    }

    protected override void AfterNeutralMove(double delta)
    {
        if (!IsOnWall())
        {
            return;
        }

        MoveDirection *= -1;
        SetHorizontalVelocity(MoveDirection * MoveSpeed);
    }
}
