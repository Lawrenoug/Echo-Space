using System;
using System.Collections.Generic;
using EchoSpace.Core.World;
using EchoSpace.Gameplay.Combat;
using EchoSpace.Player;
using Godot;

namespace EchoSpace.Gameplay.Enemies;

public abstract partial class EnemyCombatant : CharacterBody2D, IDamageable, IDeflectResponder
{
    public event Action<int, int>? HealthChanged;
    public event Action<float, float, bool>? PostureChanged;

    protected enum CombatPhase
    {
        Neutral,
        Windup,
        Attack,
        Cooldown,
        Broken,
        Dying,
    }

    [ExportGroup("Combat")]
    [Export] public int MaxHealth { get; set; } = 3;
    [Export] public float MaxPosture { get; set; } = 100f;
    [Export] public float PostureRecoveryPerSecond { get; set; } = 10f;
    [Export] public float DetectionRange { get; set; } = 140f;
    [Export] public float AttackRange { get; set; } = 52f;
    [Export] public float AttackVerticalTolerance { get; set; } = 36f;
    [Export] public float AttackHitVerticalTolerance { get; set; } = 40f;
    [Export] public float AttackWindupDuration { get; set; } = 0.42f;
    [Export] public float AttackActiveDuration { get; set; } = 0.14f;
    [Export] public float AttackCooldownDuration { get; set; } = 0.55f;
    [Export] public float BrokenDuration { get; set; } = 2.2f;
    [Export] public int AttackDamage { get; set; } = 1;
    [Export] public float AttackPostureDamage { get; set; } = 28f;
    [Export] public float DamageFlashDuration { get; set; } = 0.12f;
    [Export] public float DeathDuration { get; set; } = 0.32f;
    [Export] public float DeathRiseDistance { get; set; } = 22f;
    [Export] public WorldType AffiliatedWorld { get; set; } = WorldType.Reality;

    [ExportGroup("Nodes")]
    [Export] public NodePath? VisualRootPath { get; set; }
    [Export] public NodePath? AttackBoxPath { get; set; } = new("AttackBox");
    [Export] public NodePath? AttackCollisionShapePath { get; set; } = new("AttackBox/CollisionShape2D");

    private readonly HashSet<ulong> _attackVictims = new();

    private Node2D? _visualRoot;
    private Area2D? _attackBox;
    private CollisionShape2D? _attackCollisionShape;
    private PlayerController? _player;
    private int _currentHealth;
    private float _currentPosture;
    private int _moveDirection = -1;
    private double _damageFlashRemaining;
    private double _phaseRemaining;
    private CombatPhase _phase = CombatPhase.Neutral;
    private Vector2 _spawnPosition;
    private Vector2 _visualBaseScale = Vector2.One;
    private Vector2 _visualBasePosition = Vector2.Zero;
    private Vector2 _attackBoxBasePosition = Vector2.Zero;

    public int CurrentHealth => _currentHealth;
    public float CurrentPosture => _currentPosture;
    public bool IsBroken => _phase == CombatPhase.Broken;
    protected CombatPhase CurrentPhase => _phase;
    protected Vector2 SpawnPosition => _spawnPosition;
    protected PlayerController? Player => _player;
    protected Node2D? VisualRoot => _visualRoot;
    protected Vector2 VisualBaseScale => _visualBaseScale;
    protected Vector2 VisualBasePosition => _visualBasePosition;

    protected int MoveDirection
    {
        get => _moveDirection;
        set
        {
            if (value == 0)
            {
                return;
            }

            _moveDirection = Math.Sign(value);
        }
    }

    public override void _Ready()
    {
        AddToGroup("enemy");

        _currentHealth = MaxHealth;
        _spawnPosition = GlobalPosition;
        _visualRoot = VisualRootPath != null && !VisualRootPath.IsEmpty
            ? GetNodeOrNull<Node2D>(VisualRootPath)
            : null;
        _attackBox = AttackBoxPath != null && !AttackBoxPath.IsEmpty
            ? GetNodeOrNull<Area2D>(AttackBoxPath)
            : null;
        _attackCollisionShape = AttackCollisionShapePath != null && !AttackCollisionShapePath.IsEmpty
            ? GetNodeOrNull<CollisionShape2D>(AttackCollisionShapePath)
            : null;
        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;

        if (_visualRoot != null)
        {
            _visualBaseScale = _visualRoot.Scale;
            _visualBasePosition = _visualRoot.Position;
        }

        if (_attackBox != null)
        {
            _attackBoxBasePosition = _attackBox.Position;
        }

        if (_attackCollisionShape != null)
        {
            _attackCollisionShape.Disabled = true;
        }

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged += HandleWorldChanged;
            HandleWorldChanged(WorldManager.Instance.CurrentWorld);
        }

        OnCombatantReady();
        EmitCombatStateChanged();
    }

    public override void _ExitTree()
    {
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.WorldChanged -= HandleWorldChanged;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateFacing();

        switch (_phase)
        {
            case CombatPhase.Neutral:
                TickNeutral(delta);
                break;
            case CombatPhase.Windup:
                TickWindup(delta);
                break;
            case CombatPhase.Attack:
                TickAttack(delta);
                break;
            case CombatPhase.Cooldown:
                TickCooldown(delta);
                break;
            case CombatPhase.Broken:
                TickBroken(delta);
                break;
            case CombatPhase.Dying:
                TickDeath(delta);
                return;
        }

        ApplyGravity(delta);
        MoveAndSlide();

        if (_phase == CombatPhase.Neutral)
        {
            AfterNeutralMove(delta);
        }

        UpdateDamageFlash(delta);
        RecoverPosture(delta);
    }

    public virtual bool CanBeExecutedBy(PlayerController player, float horizontalRange, float verticalTolerance)
    {
        if (_phase != CombatPhase.Broken)
        {
            return false;
        }

        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        if (currentWorld != AffiliatedWorld)
        {
            return false;
        }

        var delta = player.GlobalPosition - GlobalPosition;
        return MathF.Abs(delta.X) <= horizontalRange && MathF.Abs(delta.Y) <= verticalTolerance;
    }

    public virtual bool TryExecute(PlayerController executor)
    {
        if (!CanBeExecutedBy(executor, executor.ExecutionRange + 12f, executor.ExecutionVerticalTolerance + 12f))
        {
            return false;
        }

        BeginDeathSequence();
        return true;
    }

    public virtual void ApplyDamage(in DamageInfo damageInfo)
    {
        if (_phase == CombatPhase.Dying || damageInfo.SourceWorld != AffiliatedWorld)
        {
            return;
        }

        if (_phase == CombatPhase.Broken)
        {
            BeginDeathSequence();
            return;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - damageInfo.Amount);
        HealthChanged?.Invoke(_currentHealth, MaxHealth);
        _currentPosture = Mathf.Clamp(_currentPosture + damageInfo.PostureDamage, 0f, MaxPosture);
        PostureChanged?.Invoke(_currentPosture, MaxPosture, false);
        Velocity += damageInfo.Knockback;
        _damageFlashRemaining = DamageFlashDuration;
        UpdateVisualTint();

        if (_currentHealth <= 0)
        {
            BeginDeathSequence();
            return;
        }

        if (_currentPosture >= MaxPosture)
        {
            EnterBrokenState();
        }
    }

    public virtual void OnDeflected(float postureDamage, Node deflector)
    {
        if (_phase is CombatPhase.Dying or CombatPhase.Neutral)
        {
            return;
        }

        _currentPosture = Mathf.Clamp(_currentPosture + postureDamage, 0f, MaxPosture);
        PostureChanged?.Invoke(_currentPosture, MaxPosture, false);
        Velocity = new Vector2(-_moveDirection * 80f, Velocity.Y);

        if (_currentPosture >= MaxPosture)
        {
            EnterBrokenState();
            return;
        }

        SetPhase(CombatPhase.Cooldown, AttackCooldownDuration);
        DisableAttackHitbox();
        OnDeflectedVisual();
    }

    protected abstract void TickNeutral(double delta);

    protected virtual void AfterNeutralMove(double delta)
    {
    }

    protected virtual void OnCombatantReady()
    {
    }

    protected virtual void OnDeflectedVisual()
    {
        if (_visualRoot != null)
        {
            _visualRoot.Modulate = new Color(0.86f, 0.96f, 1f, 1f);
        }
    }

    protected virtual void OnBrokenVisualUpdate(double delta)
    {
        if (_visualRoot != null)
        {
            var pulse = 1f + 0.08f * Mathf.Sin((float)(Time.GetTicksMsec() / 1000.0 * 14.0));
            _visualRoot.Modulate = new Color(1f, 0.82f, 0.48f, 1f);
            _visualRoot.Scale = new Vector2(Mathf.Abs(_visualBaseScale.X) * _moveDirection * pulse, _visualBaseScale.Y * pulse);
        }
    }

    protected bool IsPlayerRelevant()
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

    protected bool IsPlayerWithinAttackWindow()
    {
        EnsurePlayerReference();

        if (_player == null || !IsPlayerRelevant())
        {
            return false;
        }

        var delta = _player.GlobalPosition - GlobalPosition;
        return MathF.Abs(delta.X) <= AttackRange && MathF.Abs(delta.Y) <= AttackVerticalTolerance;
    }

    protected void StartWindup()
    {
        SetPhase(CombatPhase.Windup, AttackWindupDuration);
        Velocity = new Vector2(0f, Velocity.Y);
    }

    protected void SetHorizontalVelocity(float horizontalVelocity)
    {
        Velocity = new Vector2(horizontalVelocity, Velocity.Y);
    }

    protected void FacePlayer()
    {
        EnsurePlayerReference();

        if (_player == null || !IsInstanceValid(_player))
        {
            return;
        }

        var towardPlayer = MathF.Sign(_player.GlobalPosition.X - GlobalPosition.X);
        if (!Mathf.IsZeroApprox(towardPlayer))
        {
            MoveDirection = (int)towardPlayer;
        }
    }

    private void TickWindup(double delta)
    {
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0f, 260f * (float)delta), Velocity.Y);
        _phaseRemaining -= delta;

        if (_visualRoot != null)
        {
            _visualRoot.Modulate = new Color(1f, 0.85f, 0.55f, 1f);
        }

        if (_phaseRemaining <= 0d)
        {
            SetPhase(CombatPhase.Attack, AttackActiveDuration);
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
            SetPhase(CombatPhase.Cooldown, AttackCooldownDuration);
        }
    }

    private void TickCooldown(double delta)
    {
        Velocity = new Vector2(0f, Velocity.Y);
        _phaseRemaining -= delta;

        if (_phaseRemaining <= 0d)
        {
            SetPhase(CombatPhase.Neutral, 0d);
        }
    }

    private void TickBroken(double delta)
    {
        Velocity = new Vector2(0f, Velocity.Y);
        _phaseRemaining -= delta;
        OnBrokenVisualUpdate(delta);

        if (_phaseRemaining <= 0d)
        {
            _currentPosture = MaxPosture * 0.35f;
            PostureChanged?.Invoke(_currentPosture, MaxPosture, false);
            SetPhase(CombatPhase.Cooldown, AttackCooldownDuration);
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
        if (_phase == CombatPhase.Dying)
        {
            return;
        }

        if (IsPlayerRelevant())
        {
            FacePlayer();
        }

        if (_visualRoot != null)
        {
            _visualRoot.Scale = new Vector2(Mathf.Abs(_visualBaseScale.X) * _moveDirection, _visualBaseScale.Y);
        }

        if (_attackBox != null)
        {
            _attackBox.Position = new Vector2(Mathf.Abs(_attackBoxBasePosition.X) * _moveDirection, _attackBoxBasePosition.Y);
        }
    }

    private void EnsurePlayerReference()
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        }
    }

    private void HandleWorldChanged(WorldType currentWorld)
    {
        if (currentWorld == AffiliatedWorld || _phase == CombatPhase.Dying)
        {
            return;
        }

        DisableAttackHitbox();
        _attackVictims.Clear();

        if (_phase is CombatPhase.Windup or CombatPhase.Attack or CombatPhase.Cooldown)
        {
            SetPhase(CombatPhase.Neutral, 0d);
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

    private void UpdateDamageFlash(double delta)
    {
        if (_phase == CombatPhase.Dying)
        {
            return;
        }

        if (_damageFlashRemaining > 0d)
        {
            _damageFlashRemaining -= delta;
            UpdateVisualTint();
            return;
        }

        if (_phase == CombatPhase.Neutral && _visualRoot != null)
        {
            _visualRoot.Modulate = Colors.White;
        }
    }

    private void RecoverPosture(double delta)
    {
        if (_phase is CombatPhase.Windup or CombatPhase.Attack || _currentPosture <= 0f)
        {
            return;
        }

        _currentPosture = Mathf.Max(0f, _currentPosture - PostureRecoveryPerSecond * (float)delta);
        PostureChanged?.Invoke(_currentPosture, MaxPosture, _phase == CombatPhase.Broken);
    }

    private void ProcessAttackHits()
    {
        if (_player == null || !IsPlayerRelevant())
        {
            return;
        }

        if (MathF.Abs(_player.GlobalPosition.X - GlobalPosition.X) > AttackRange + 12f
            || MathF.Abs(_player.GlobalPosition.Y - GlobalPosition.Y) > AttackHitVerticalTolerance)
        {
            return;
        }

        var instanceId = _player.GetInstanceId();
        if (!_attackVictims.Add(instanceId))
        {
            return;
        }

        _player.ApplyDamage(new DamageInfo(
            AttackDamage,
            AffiliatedWorld,
            this,
            new Vector2(_moveDirection * 140f, -60f),
            AttackPostureDamage,
            true));
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

    private void SetPhase(CombatPhase nextPhase, double duration)
    {
        _phase = nextPhase;
        _phaseRemaining = duration;
    }

    private void BeginDeathSequence()
    {
        SetPhase(CombatPhase.Dying, DeathDuration);
        DisableAttackHitbox();
        Velocity = Vector2.Zero;
        DisableCollisionRecursive(this);
        PostureChanged?.Invoke(MaxPosture, MaxPosture, true);
        UpdateVisualTint();
    }

    private void EnterBrokenState()
    {
        _currentPosture = MaxPosture;
        PostureChanged?.Invoke(_currentPosture, MaxPosture, true);
        SetPhase(CombatPhase.Broken, BrokenDuration);
        DisableAttackHitbox();
        Velocity = Vector2.Zero;
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

        if (_phase == CombatPhase.Cooldown)
        {
            _visualRoot.Modulate = new Color(0.95f, 0.95f, 1f, 1f);
            return;
        }

        if (_phase == CombatPhase.Broken)
        {
            _visualRoot.Modulate = new Color(1f, 0.82f, 0.48f, 1f);
            return;
        }

        if (_phase == CombatPhase.Neutral)
        {
            _visualRoot.Modulate = Colors.White;
        }
    }

    private void EmitCombatStateChanged()
    {
        HealthChanged?.Invoke(_currentHealth, MaxHealth);
        PostureChanged?.Invoke(_currentPosture, MaxPosture, _phase == CombatPhase.Broken);
        UpdateVisualTint();
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
