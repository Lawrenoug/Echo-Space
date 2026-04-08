using System;
using System.Collections.Generic;
using Godot;

namespace EchoSpace.Core.World;

public partial class WorldManager : Node
{
    private readonly HashSet<DualWorldObject> _registeredObjects = new();

    public static WorldManager? Instance { get; private set; }

    public event Action<WorldType>? WorldChanged;

    public WorldType CurrentWorld { get; private set; } = WorldType.Reality;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public void Register(DualWorldObject dualWorldObject)
    {
        if (_registeredObjects.Add(dualWorldObject))
        {
            dualWorldObject.ApplyWorld(CurrentWorld);
        }
    }

    public void Unregister(DualWorldObject dualWorldObject)
    {
        _registeredObjects.Remove(dualWorldObject);
    }

    public void ToggleWorld()
    {
        SetWorld(CurrentWorld == WorldType.Reality ? WorldType.Soul : WorldType.Reality);
    }

    public void SetWorld(WorldType nextWorld)
    {
        if (CurrentWorld == nextWorld)
        {
            return;
        }

        CurrentWorld = nextWorld;

        var snapshot = new DualWorldObject[_registeredObjects.Count];
        _registeredObjects.CopyTo(snapshot);

        foreach (var dualWorldObject in snapshot)
        {
            if (IsInstanceValid(dualWorldObject))
            {
                dualWorldObject.ApplyWorld(CurrentWorld);
            }
        }

        WorldChanged?.Invoke(CurrentWorld);
    }
}
