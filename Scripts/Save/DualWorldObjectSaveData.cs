using EchoSpace.Core.World;

namespace EchoSpace.Save;

public sealed class DualWorldObjectSaveData
{
    public string SaveId { get; set; } = string.Empty;
    public DualWorldStateData Reality { get; set; } = new();
    public DualWorldStateData Soul { get; set; } = new();
}

public sealed class DualWorldStateData
{
    public bool Exists { get; set; } = true;
    public bool IsBroken { get; set; }
    public bool IsActive { get; set; } = true;
}
