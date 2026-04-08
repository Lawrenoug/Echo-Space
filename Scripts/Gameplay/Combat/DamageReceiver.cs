using Godot;

namespace EchoSpace.Gameplay.Combat;

public partial class DamageReceiver : Area2D
{
    [Export] public NodePath? TargetPath { get; set; }

    private IDamageable? _target;

    public override void _Ready()
    {
        var targetNode = TargetPath != null && !TargetPath.IsEmpty
            ? GetNodeOrNull(TargetPath)
            : GetParent();

        _target = targetNode as IDamageable;

        if (_target == null)
        {
            GD.PushWarning($"{GetPath()}: DamageReceiver target does not implement IDamageable.");
        }
    }

    public void ReceiveDamage(in DamageInfo damageInfo)
    {
        _target?.ApplyDamage(damageInfo);
    }
}
