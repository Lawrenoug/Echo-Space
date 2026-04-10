using EchoSpace.Gameplay.Progression;

namespace EchoSpace.Save;

public sealed class ProgressionAttributeSaveData
{
    public PlayerAttributeType AttributeType { get; set; }
    public int CurrentLevel { get; set; }
}
