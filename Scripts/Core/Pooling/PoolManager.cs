using System;
using System.Collections.Generic;
using Godot;

namespace EchoSpace.Core.Pooling;

public partial class PoolManager : Node
{
    private readonly Dictionary<string, NodePool> _pools = new();

    public static PoolManager? Instance { get; private set; }

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

    public bool HasPool(string key)
    {
        return _pools.ContainsKey(key);
    }

    public void RegisterPool(string key, PackedScene scene, int initialSize, Node? container = null)
    {
        if (_pools.ContainsKey(key))
        {
            return;
        }

        _pools[key] = new NodePool(key, scene, initialSize, container ?? this);
    }

    public T Spawn<T>(string key) where T : Node
    {
        return (T)_pools[key].Spawn();
    }

    public void Despawn(string key, Node node)
    {
        if (_pools.TryGetValue(key, out var pool))
        {
            pool.Despawn(node);
        }
    }
}
