using Godot;

namespace EchoSpace.Gameplay.Combat;

public interface IDeflectResponder
{
    void OnDeflected(float postureDamage, Node deflector);
}
