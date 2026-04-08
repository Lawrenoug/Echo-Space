using System;
using System.Collections.Generic;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Combat;
using EchoSpace.Player;
using Godot;

namespace EchoSpace.Gameplay.Enemies;

public partial class EnemyController : CharacterBody2D, IDamageable, IDeflectResponder
{
    private enum EnemyPhase
    {
        Patrol,
        Windup,
        Attack,
        Cooldown,
        Dying,
    }

    [Export] public int MaxHealth { get; set; } = 3;
    [Export] public float MaxPosture { get; set; } = 100f;
    [Export] public float PostureRecoveryPerSecond { get; set; } = 10f;
    [Export] public float MoveSpeed { get; set; } = 65f;
    [Export] public float PatrolDistance { get; set; } = 96f;
    [Export] public float DetectionRange { get; set; } = 140f;
    [Export] public float AttackRange { get; set; } = 52f;
    [Export] public float AttackWindupDuration { get; set; } = 0.42f;
    [Export] public float AttackActiveDuration { get; set; } = 0.14f;
    [Export] public float AttackCooldownDuration { get; set; } = 0.55f;
    [Export] public int AttackDamage { get; set; } = 1;
    [Export] public float AttackPostureDamage { get; set; } = 28f;
    [Export] public float DamageFlashDuration { get; set; } = 0.12f;
    [Export] public float DeathDuration { get; set; } = 0.32f;
    [Export] public float DeathRiseDistance { get; set; } = 22f;
    [Export] public WorldType AffiliatedWorld { get; set; } = WorldType.Reality;
    [Export] public NodePath? VisualRootPath { get; set; }
    [Export] public NodePath? AttackBoxPath { get; set; } = new("AttackBox");
    [Export] public NodePath? AttackCollisionShapePath { get; set; } = new("AttackBox/CollisionShape2D");

    private readonly HashSet<ulong> _attackVictims = new();

    private Vector2 _spawnPosition;
    private Node2D? _visualRoot;
    private Area2D? _attackBox;
    private CollisionShape2D? _attackCollisionShape;
    private PlayerController? _player;
    private int _currentHealth;
    private float _currentPosture;
    private int _moveDirection = -1;
    private double _damageFlashRemaining;
    private double _phaseRemaining;
    private EnemyPhase _phase = EnemyPhase.Patrol;
    private Vector2 _visualBaseScale = Vector2.One;
    private Vector2 _visualBasePosition = Vector2.Zero;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _spawnPosition = GlobalPosition;
        _visualRoot = VisualRootPath != null && !VisualRootPath.IsEmpty
            ? GetNodeOrNull<Node2D>(VisualRootPath)
            : null;
        _attackBox = AttackBoxPath != null && !AttackBoxPath.IsEmpty ? GetNodeOrNull<Area2D>(AttackBoxPath) : null;
        _attackCollisionShape = AttackCollisionShapePath != null && !AttackCollisionShapePath.IsEmpty
            ? GetNodeOrNull<CollisionShape2D>(AttackCollisionShapePath)
            : null;
        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;

        if (_visualRoot != null)
        {
            _visualBaseScale = _visualRoot.Scale;
            _visualBasePosition = _visualRoot.Position;
        }

        if (_attackCollisionShape != null)
        {
            _attackCollisionShape.Disabled = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateFacing();

        switch (_phase)
        {
            case EnemyPhase.Patrol:
                TickPatrol(delta);
                break;
            case EnemyPhase.Windup:
                TickWindup(delta);
                break;
            case EnemyPhase.Attack:
                TickAttack(delta);
                break;
            case EnemyPhase.Cooldown:
                TickCooldown(delta);
                break;
            case EnemyPhase.Dying:
                TickDeath(delta);
                return;
        }

        ApplyGravity(delta);
        MoveAndSlide();
        UpdatePatrolTurnaround();
        UpdateDamageFlash(delta);
        RecoverPosture(delta);
    }

    public void ApplyDamage(in DamageInfo damageInfo)
    {
        if (_phase == EnemyPhase.Dying || damageInfo.SourceWorld != AffiliatedWorld)
        {
            return;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - damageInfo.Amount);
        _currentPosture = Mathf.Clamp(_currentPosture + damageInfo.PostureDamage, 0f, MaxPosture);
        Velocity += damageInfo.Knockback;
        _damageFlashRemaining = DamageFlashDuration;
        UpdateVisualTint();

        if (_currentHealth <= 0 || _currentPosture >= MaxPosture)
        {
            BeginDeathSequence();
        }
    }

    public void OnDeflected(float postureDamage, Node deflector)
    {
        if (_phase is EnemyPhase.Dying or EnemyPhase.Patrol)
        {
            return;
        }

        _currentPosture = Mathf.Clamp(_currentPosture + postureDamage, 0f, MaxPosture);
        Velocity = new Vector2(-_moveDirection * 80f, Velocity.Y);

        if (_currentPosture >= MaxPosture)
        {
            BeginDeathSequence();
            return;
        }

        SetPhase(EnemyPhase.Cooldown, AttackCooldownDuration);
        DisableAttackHitbox();

        if (_visualRoot != null)
        {
            _visualRoot.Modulate = new Color(0.86f, 0.96f, 1f, 1f);
        }
    }

    private void TickPatrol(double delta)
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

        if (CanStartAttack())
        {
            SetPhase(EnemyPhase.Windup, AttackWindupDuration);
            Velocity = new Vector2(0f, Velocity.Y);
        }
    }

    private void TickWindup(double delta)
    {
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, MoveSpeed * 4f * (float)delta), Velocity.Y);
        _phaseRemaining -= delta;

        if (_visualRoot != null)
        {
            _visualRoot.Modulate = new Color(1f, 0.85f, 0.55f, 1f);
        }

        if (_phaseRemaining <= 0d)
        {
            SetPhase(EnemyPhase.Attack, AttackActiveDuration);
            EnableAttackHitbox();
        }
    }

    private void TickAttack(double delta)
    {
        Velocity = new Vector2(0f, Velocity.Y);
        _phaseRemaining -= delta;
        ProcessAttackHits();

        if (_phaseRemaining <= 0d)
        {
            DisableAttackHitbox();
            SetPhase(EnemyPhase.Cooldown, AttackCooldownDuration);
        }
    }

    private void TickCooldown(double delta)
    {
        Velocity = new Vector2(0f, Velocity.Y);
        _phaseRemaining -= delta;

        if (_phaseRemaining <= 0d)
        {
            SetPhase(EnemyPhase.Patrol, 0d);
        }
    }

    private void TickDeath(double delta)
    {
        _phaseRemaining -= delta;
        var progress = 1f - Mathf.Clamp((float)(_phaseRemaining / DeathDuration), 0f, 1f);

        if (_visualRoot != null)
        {
            _visualRoot.Modulate = new Color(1f, 0.78f, 0.78f, 1f - progress);
            _visualRoot.Position = _visualBasePosition + new Vector2(0f, -DeathRiseDistance * progress);
            _visualRoot.Scale = _visualBaseScale * (1f - progress * 0.25f);
        }

        if (_phaseRemaining <= 0d)
        {
            QueueFree();
        }
    }

    private void UpdateFacing()
    {
        if (_player == null || !IsPlayerRelevant())
        {
            if (_visualRoot != null)
            {
                _visualRoot.Scale = new Vector2(Mathf.Abs(_visualBaseScale.X) * _moveDirection, _visualBaseScale.Y);
            }

            return;
        }

        var towardPlayer = MathF.Sign(_player.GlobalPosition.X - GlobalPosition.X);
        if (!Mathf.IsZeroApprox(towardPlayer))
        {
            _moveDirection = (int)towardPlayer;
        }

        if (_visualRoot != null)
        {
            _visualRoot.Scale = new Vector2(Mathf.Abs(_visualBaseScale.X) * _moveDirection, _visualBaseScale.Y);
        }

        if (_attackBox != null)
        {
            _attackBox.Position = new Vector2(Mathf.Abs(_attackBox.Position.X) * _moveDirection, _attackBox.Position.Y);
        }
    }

    private bool CanStartAttack()
    {
        EnsurePlayerReference();

        if (_player == null || !IsPlayerRelevant())
        {
            return false;
        }

        var distance = _player.GlobalPosition - GlobalPosition;
        return MathF.Abs(distance.X) <= AttackRange && MathF.Abs(distance.Y) <= 36f;
    }

    private bool IsPlayerRelevant()
    {
        EnsurePlayerReference();

        if (_player == null || !IsInstanceValid(_player))
        {
            return false;
        }

        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        if (currentWorld != AffiliatedWorld)
        {
            return false;
        }

        return _player.GlobalPosition.DistanceTo(GlobalPosition) <= DetectionRange;
    }

    private void EnsurePlayerReference()
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
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

    private void UpdatePatrolTurnaround()
    {
        if (_phase != EnemyPhase.Patrol)
        {
            return;
        }

        if (IsOnWall())
        {
            _moveDirection *= -1;
            Velocity = new Vector2(_moveDirection * MoveSpeed, Velocity.Y);
        }
    }

    private void UpdateDamageFlash(double delta)
    {
        if (_phase == EnemyPhase.Dying)
        {
            return;
        }

        if (_damageFlashRemaining > 0d)
        {
            _damageFlashRemaining -= delta;
            UpdateVisualTint();
            return;
        }

        if (_phase == EnemyPhase.Patrol && _visualRoot != null)
        {
            _visualRoot.Modulate = Colors.White;
        }
    }

    private void RecoverPosture(double delta)
    {
        if (_phase is EnemyPhase.Windup or EnemyPhase.Attack || _currentPosture <= 0f)
        {
            return;
        }

        _currentPosture = Mathf.Max(0f, _currentPosture - PostureRecoveryPerSecond * (float)delta);
    }

    private void ProcessAttackHits()
    {
        if (_attackBox == null)
        {
            return;
        }

        foreach (var area in _attackBox.GetOverlappingAreas())
        {
            if (area is not DamageReceiver damageReceiver)
            {
                continue;
            }

            var instanceId = damageReceiver.GetInstanceId();
            if (!_attackVictims.Add(instanceId))
            {
                continue;
            }

            damageReceiver.ReceiveDamage(new DamageInfo(
                AttackDamage,
                AffiliatedWorld,
                this,
                new Vector2(_moveDirection * 140f, -60f),
                AttackPostureDamage,
                true));
        }
    }

    private void EnableAttackHitbox()
    {
        _attackVictims.Clear();

        if (_attackCollisionShape != null)
        {
            _attackCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        }
    }

    private void DisableAttackHitbox()
    {
        if (_attackCollisionShape != null)
        {
            _attackCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }
    }

    private void SetPhase(EnemyPhase nextPhase, double duration)
    {
        _phase = nextPhase;
        _phaseRemaining = duration;
    }

    private void BeginDeathSequence()
    {
        SetPhase(EnemyPhase.Dying, DeathDuration);
        DisableAttackHitbox();
        Velocity = Vector2.Zero;
        DisableCollisionRecursive(this);
        UpdateVisualTint();
    }

    private void UpdateVisualTint()
    {
        if (_visualRoot == null)
        {
            return;
        }

        if (_damageFlashRemaining > 0d)
        {
            _visualRoot.Modulate = new Color(1f, 0.55f, 0.55f, 1f);
            return;
        }

        if (_phase == EnemyPhase.Cooldown)
        {
            _visualRoot.Modulate = new Color(0.95f, 0.95f, 1f, 1f);
            return;
        }

        if (_phase == EnemyPhase.Patrol)
        {
            _visualRoot.Modulate = Colors.White;
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
