using EchoSpace.Core.World;
using Godot;

namespace EchoSpace.Gameplay.Combat;

public readonly struct DamageInfo
{
    public DamageInfo(int amount, WorldType sourceWorld, Node? source = null, Vector2? knockback = null)
    {
        Amount = amount;
        SourceWorld = sourceWorld;
        Source = source;
        Knockback = knockback ?? Vector2.Zero;
    }

    public int Amount { get; }

    public WorldType SourceWorld { get; }

    public Node? Source { get; }

    public Vector2 Knockback { get; }
}
