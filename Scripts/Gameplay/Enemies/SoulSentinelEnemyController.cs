using System;
using Godot;

namespace EchoSpace.Gameplay.Enemies;

public partial class SoulSentinelEnemyController : EnemyCombatant
{
	[ExportGroup("Movement")]
	[Export] public float GuardRadius { get; set; } = 170f;
	[Export] public float DriftSpeed { get; set; } = 42f;
	[Export] public float ReturnSpeed { get; set; } = 54f;

	protected override void OnCombatantReady()
	{
		MoveDirection = -1;
	}

	protected override void TickNeutral(double delta)
	{
		var distanceFromSpawn = GlobalPosition.X - SpawnPosition.X;

		if (IsPlayerRelevant() && Player != null)
		{
			FacePlayer();

			if (IsPlayerWithinAttackWindow())
			{
				StartWindup();
				return;
			}

			var playerDeltaX = Player.GlobalPosition.X - GlobalPosition.X;
			if (MathF.Abs(playerDeltaX) <= GuardRadius)
			{
				MoveDirection = (int)MathF.Sign(playerDeltaX);
				SetHorizontalVelocity(MoveDirection * DriftSpeed);
				return;
			}
		}

		if (MathF.Abs(distanceFromSpawn) > GuardRadius)
		{
			var directionToSpawn = -MathF.Sign(distanceFromSpawn);
			MoveDirection = (int)directionToSpawn;
			SetHorizontalVelocity(directionToSpawn * ReturnSpeed);
			return;
		}

		SetHorizontalVelocity(MoveDirection * DriftSpeed * 0.55f);
	}

	protected override void AfterNeutralMove(double delta)
	{
		if (!IsOnWall())
		{
			return;
		}

		MoveDirection *= -1;
		SetHorizontalVelocity(MoveDirection * DriftSpeed);
	}

	protected override void OnDeflectedVisual()
	{
		if (VisualRoot != null)
		{
			VisualRoot.Modulate = new Color(0.62f, 0.95f, 1f, 1f);
		}
	}

	protected override void OnBrokenVisualUpdate(double delta)
	{
		if (VisualRoot == null)
		{
			return;
		}

		var pulse = 1f + 0.12f * Mathf.Sin((float)(Time.GetTicksMsec() / 1000.0 * 16.0));
		VisualRoot.Modulate = new Color(0.92f, 0.96f, 1f, 1f);
		VisualRoot.Scale = new Vector2(Mathf.Abs(VisualBaseScale.X) * MoveDirection * pulse, VisualBaseScale.Y * pulse);
		VisualRoot.Position = VisualBasePosition + new Vector2(0f, -2f * pulse);
	}
}
