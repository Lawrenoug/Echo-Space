using EchoSpace.Core.Fsm;
using EchoSpace.Gameplay.Combat;
using Godot;

namespace EchoSpace.Gameplay.Enemies;

public partial class EnemyController : CharacterBody2D, IDamageable
{
    [Export] public int MaxHealth { get; set; } = 3;

    private StateMachine<EnemyController>? _stateMachine;
    private int _currentHealth;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _stateMachine = new StateMachine<EnemyController>(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        _stateMachine?.PhysicsUpdate(delta);
        MoveAndSlide();
    }

    public void ApplyDamage(in DamageInfo damageInfo)
    {
        _currentHealth -= damageInfo.Amount;

        if (_currentHealth <= 0)
        {
            QueueFree();
        }
    }
}
