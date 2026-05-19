namespace EchoSpace.Gameplay.Progression;

public sealed class TalentNodeState
{
    public TalentNodeState(string nodeId)
    {
        NodeId = nodeId;
    }

    public string NodeId { get; }
    public bool IsUnlocked { get; private set; }

    public void SetUnlocked(bool value)
    {
        IsUnlocked = value;
    }
}
