using System.Collections.Generic;
using Godot;

namespace EchoSpace.Gameplay.Progression;

public sealed class TalentNodeDefinition
{
    public TalentNodeDefinition(
        string id,
        string displayName,
        string description,
        Vector2 position,
        TalentNodeType nodeType,
        int cost,
        bool isStart,
        params string[] neighbors)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Position = position;
        NodeType = nodeType;
        Cost = Mathf.Max(0, cost);
        IsStart = isStart;
        Neighbors = neighbors;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public Vector2 Position { get; }
    public TalentNodeType NodeType { get; }
    public int Cost { get; }
    public bool IsStart { get; }
    public IReadOnlyList<string> Neighbors { get; }
}
