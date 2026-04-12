using System;
using Godot;

namespace EchoSpace.Gameplay.Enemies;

public partial class ChaserEnemyController : EnemyCombatant
{
    [ExportGroup("Movement")]
    [Export] public float ApproachSpeed { get; set; } = 96f;
    [Export] public float ReturnSpeed { get; set; } = 72f;
    [Export] public float LeashDistance { get; set; } = 220f;
    [Export] public float ReturnTolerance { get; set; } = 6f;

    protected override void TickNeutral(double delta)
    {
        var distanceFromSpawn = GlobalPosition.X - SpawnPosition.X;

        if (MathF.Abs(distanceFromSpawn) > LeashDistance)
        {
            ReturnToSpawn(distanceFromSpawn);
            return;
        }

        if (IsPlayerRelevant() && Player != null)
        {
            FacePlayer();

            if (IsPlayerWithinAttackWindow())
            {
                StartWindup();
                return;
            }

            var playerDeltaX = Player.GlobalPosition.X - GlobalPosition.X;
            var directionToPlayer = MathF.Sign(playerDeltaX);
            if (!Mathf.IsZeroApprox(directionToPlayer))
            {
                MoveDirection = (int)directionToPlayer;
                SetHorizontalVelocity(directionToPlayer * ApproachSpeed);
                return;
            }
        }

        ReturnToSpawn(distanceFromSpawn);
    }

    protected override void AfterNeutralMove(double delta)
    {
        if (!IsOnWall())
        {
            return;
        }

        SetHorizontalVelocity(0f);
        MoveDirection *= -1;
    }

    private void ReturnToSpawn(float distanceFromSpawn)
    {
        if (MathF.Abs(distanceFromSpawn) <= ReturnTolerance)
        {
            SetHorizontalVelocity(0f);
            return;
        }

        var directionToSpawn = -MathF.Sign(distanceFromSpawn);
        MoveDirection = (int)directionToSpawn;
        SetHorizontalVelocity(directionToSpawn * ReturnSpeed);
    }
}
