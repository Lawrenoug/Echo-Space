using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Combat;

public readonly struct DamageInfo
{
    public DamageInfo(int amount, WorldType sourceWorld, Node? source = null)
    {
        Amount = amount;
        SourceWorld = sourceWorld;
        Source = source;
    }

    public int Amount { get; }

    public WorldType SourceWorld { get; }

    public Node? Source { get; }
}
