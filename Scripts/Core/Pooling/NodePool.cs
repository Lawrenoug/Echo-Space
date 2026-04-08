using System.Collections.Generic;
using Godot;

namespace EchoSpace.Core.Pooling;

public sealed class NodePool
{
    private readonly PackedScene _scene;
    private readonly Node _container;
    private readonly Queue<Node> _availableNodes = new();
    private readonly HashSet<Node> _activeNodes = new();

    public NodePool(string key, PackedScene scene, int initialSize, Node container)
    {
        Key = key;
        _scene = scene;
        _container = container;

        for (var i = 0; i < initialSize; i++)
        {
            _availableNodes.Enqueue(CreateInstance());
        }
    }

    public string Key { get; }

    public Node Spawn()
    {
        var node = _availableNodes.Count > 0 ? _availableNodes.Dequeue() : CreateInstance();
        _activeNodes.Add(node);
        SetNodeActive(node, true);

        if (node is IPoolable poolable)
        {
            poolable.OnSpawned();
        }

        return node;
    }

    public void Despawn(Node node)
    {
        if (!_activeNodes.Remove(node))
        {
            return;
        }

        if (node is IPoolable poolable)
        {
            poolable.OnDespawned();
        }

        SetNodeActive(node, false);
        _availableNodes.Enqueue(node);
    }

    private Node CreateInstance()
    {
        var instance = _scene.Instantiate<Node>();
        _container.AddChild(instance);
        SetNodeActive(instance, false);
        return instance;
    }

    private static void SetNodeActive(Node node, bool isActive)
    {
        node.ProcessMode = isActive ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled;

        if (node is CanvasItem canvasItem)
        {
            canvasItem.Visible = isActive;
        }
    }
}
