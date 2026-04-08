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
    [Export] public float DeathDuration { get; set; } = 0.32f;
    [Export] public float DeathRiseDistance { get; set; } = 22f;
    [Export] public WorldType AffiliatedWorld { get; set; } = WorldType.Reality;
    [Export] public NodePath? VisualRootPath { get; set; }

    private Vector2 _spawnPosition;
    private Node2D? _visualRoot;
    private int _currentHealth;
    private int _moveDirection = -1;
    private double _lastContactDamageAt = double.NegativeInfinity;
    private double _damageFlashRemaining;
    private bool _isDying;
    private double _deathRemaining;
    private Vector2 _visualBaseScale = Vector2.One;
    private Vector2 _visualBasePosition = Vector2.Zero;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _spawnPosition = GlobalPosition;
        _visualRoot = VisualRootPath != null && !VisualRootPath.IsEmpty
            ? GetNodeOrNull<Node2D>(VisualRootPath)
            : null;

        if (_visualRoot != null)
        {
            _visualBaseScale = _visualRoot.Scale;
            _visualBasePosition = _visualRoot.Position;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDying)
        {
            UpdateDeathSequence(delta);
            return;
        }

        ApplyPatrolMovement();
        ApplyGravity(delta);
        MoveAndSlide();
        ResolveContactDamage();
        UpdateDamageFlash(delta);
    }

    public void ApplyDamage(in DamageInfo damageInfo)
    {
        if (_isDying)
        {
            return;
        }

        if (damageInfo.SourceWorld != AffiliatedWorld)
        {
            return;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - damageInfo.Amount);
        Velocity += damageInfo.Knockback;
        _damageFlashRemaining = DamageFlashDuration;
        UpdateVisualTint();

        if (_currentHealth <= 0)
        {
            BeginDeathSequence();
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
        if (_isDying)
        {
            return;
        }

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

    private void BeginDeathSequence()
    {
        _isDying = true;
        _deathRemaining = DeathDuration;
        Velocity = Vector2.Zero;
        DisableCollisionRecursive(this);
        UpdateVisualTint();
    }

    private void UpdateDeathSequence(double delta)
    {
        _deathRemaining -= delta;
        var progress = 1f - Mathf.Clamp((float)(_deathRemaining / DeathDuration), 0f, 1f);

        if (_visualRoot != null)
        {
            _visualRoot.Modulate = new Color(1f, 0.78f, 0.78f, 1f - progress);
            _visualRoot.Position = _visualBasePosition + new Vector2(0f, -DeathRiseDistance * progress);
            _visualRoot.Scale = _visualBaseScale * (1f - progress * 0.25f);
        }

        if (_deathRemaining <= 0d)
        {
            QueueFree();
        }
    }

    private static void DisableCollisionRecursive(Node node)
    {
        switch (node)
        {
            case CollisionShape2D collisionShape:
                collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
                break;
            case CollisionPolygon2D collisionPolygon:
                collisionPolygon.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);
                break;
            case Area2D area2D:
                area2D.Monitoring = false;
                area2D.Monitorable = false;
                break;
        }

        foreach (Node child in node.GetChildren())
        {
            DisableCollisionRecursive(child);
        }
    }
}
