using EchoSpace.Core.World;

namespace EchoSpace.Gameplay.Environment;

public interface IWorldActivationTarget
{
    void SetActivatedForWorld(bool isActivated, WorldType worldType);
}
