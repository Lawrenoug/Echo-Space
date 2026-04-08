using System;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Combat;
using EchoSpace.Player;
using Godot;

namespace EchoSpace.Gameplay.Enemies;

public partial class EnemyController : CharacterBody2D, IDamageable
{
    [Export] public int MaxHealth { get; set; } = 3;
    [Export] public float MoveSpeed { get; set; } = 65f;
    [Export] public float PatrolDistance { get; set; } = 96f;
    [Export] public int ContactDamage { get; set; } = 1;
    [Export] public float ContactDamageCooldown { get; set; } = 0.6f;
    [Export] public float DamageFlashDuration { get; set; } = 0.12f;
    [Export] public WorldType AffiliatedWorld { get; set; } = WorldType.Reality;
    [Export] public NodePath? VisualRootPath { get; set; }

    private Vector2 _spawnPosition;
    private Node2D? _visualRoot;
    private int _currentHealth;
    private int _moveDirection = -1;
    private double _lastContactDamageAt = double.NegativeInfinity;
    private double _damageFlashRemaining;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _spawnPosition = GlobalPosition;
        _visualRoot = VisualRootPath != null && !VisualRootPath.IsEmpty
            ? GetNodeOrNull<Node2D>(VisualRootPath)
            : null;
    }

    public override void _PhysicsProcess(double delta)
    {
        ApplyPatrolMovement();
        ApplyGravity(delta);
        MoveAndSlide();
        ResolveContactDamage();
        UpdateDamageFlash(delta);
    }

    public void ApplyDamage(in DamageInfo damageInfo)
    {
        if (damageInfo.SourceWorld != AffiliatedWorld)
        {
            return;
        }

        _currentHealth -= damageInfo.Amount;
        Velocity += damageInfo.Knockback;
        _damageFlashRemaining = DamageFlashDuration;
        UpdateVisualTint();

        if (_currentHealth <= 0)
        {
            QueueFree();
        }
    }

    private void ApplyPatrolMovement()
    {
        var leftLimit = _spawnPosition.X - PatrolDistance;
        var rightLimit = _spawnPosition.X + PatrolDistance;

        if (GlobalPosition.X <= leftLimit)
        {
            _moveDirection = 1;
        }
        else if (GlobalPosition.X >= rightLimit)
        {
            _moveDirection = -1;
        }

        Velocity = new Vector2(_moveDirection * MoveSpeed, Velocity.Y);

        if (_visualRoot != null)
        {
            _visualRoot.Scale = new Vector2(Mathf.Abs(_visualRoot.Scale.X) * _moveDirection, _visualRoot.Scale.Y);
        }
    }

    private void ApplyGravity(double delta)
    {
        if (IsOnFloor() && Velocity.Y > 0f)
        {
            Velocity = new Vector2(Velocity.X, 0f);
            return;
        }

        if (IsOnFloor())
        {
            return;
        }

        var gravity = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
        Velocity += new Vector2(0f, gravity * (float)delta);
    }

    private void ResolveContactDamage()
    {
        if (Time.GetTicksMsec() / 1000.0 - _lastContactDamageAt < ContactDamageCooldown)
        {
            return;
        }

        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);

            if (collision.GetCollider() is not PlayerController player)
            {
                continue;
            }

            var knockbackDirection = MathF.Sign(player.GlobalPosition.X - GlobalPosition.X);
            if (Mathf.IsZeroApprox(knockbackDirection))
            {
                knockbackDirection = -_moveDirection;
            }

            player.ApplyDamage(new DamageInfo(
                ContactDamage,
                AffiliatedWorld,
                this,
                new Vector2(knockbackDirection * 180f, -120f)));

            _lastContactDamageAt = Time.GetTicksMsec() / 1000.0;
            break;
        }
    }

    private void UpdateDamageFlash(double delta)
    {
        if (_damageFlashRemaining <= 0d)
        {
            return;
        }

        _damageFlashRemaining -= delta;
        UpdateVisualTint();
    }

    private void UpdateVisualTint()
    {
        if (_visualRoot == null)
        {
            return;
        }

        _visualRoot.Modulate = _damageFlashRemaining > 0d
            ? new Color(1f, 0.55f, 0.55f, 1f)
            : Colors.White;
    }
}
