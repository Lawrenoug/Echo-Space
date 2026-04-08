namespace EchoSpace.Core.Pooling;

public interface IPoolable
{
    void OnSpawned();

    void OnDespawned();
}
