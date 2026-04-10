using System.Collections.Generic;

namespace EchoSpace.Save;

public sealed class ProgressionSaveData
{
    public int CurrentLevel { get; set; } = 1;
    public int UnspentPoints { get; set; }
    public List<ProgressionAttributeSaveData> Attributes { get; set; } = new();
}
