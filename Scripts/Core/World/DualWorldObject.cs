using System.Collections.Generic;
using Godot;

namespace EchoSpace.Core.World;

public partial class DualWorldObject : Node
{
    [Export] public NodePath? TargetRootPath { get; set; }
    [Export] public NodePath? VisualRootPath { get; set; }
    [Export] public NodePath? CollisionRootPath { get; set; }
    [Export] public bool ApplyPositionOffsetOnWorldChange { get; set; } = true;

    [ExportGroup("Reality")]
    [Export] public bool ExistsInReality { get; set; } = true;
    [Export] public bool RealityStartsBroken { get; set; }
    [Export] public bool RealityStartsActive { get; set; } = true;
    [Export] public Vector2 RealityPositionOffset { get; set; } = Vector2.Zero;

    [ExportGroup("Soul")]
    [Export] public bool ExistsInSoul { get; set; } = true;
    [Export] public bool SoulStartsBroken { get; set; }
    [Export] public bool SoulStartsActive { get; set; } = true;
    [Export] public Vector2 SoulPositionOffset { get; set; } = Vector2.Zero;

    private readonly Dictionary<WorldType, RuntimeState> _runtimeStates = new();

    private Node2D? _targetRoot;
    private CanvasItem? _visualRoot;
    private Node? _collisionRoot;
    private Vector2 _basePosition;

    public override void _Ready()
    {
        _targetRoot = ResolveTargetRoot();
        _visualRoot = ResolveVisualRoot();
        _collisionRoot = ResolveCollisionRoot();

        if (_targetRoot == null)
        {
            GD.PushWarning($"{GetPath()}: DualWorldObject could not find a Node2D target root.");
            return;
        }

        _basePosition = _targetRoot.Position;
        _runtimeStates[WorldType.Reality] = new RuntimeState(ExistsInReality, RealityStartsBroken, RealityStartsActive, RealityPositionOffset);
        _runtimeStates[WorldType.Soul] = new RuntimeState(ExistsInSoul, SoulStartsBroken, SoulStartsActive, SoulPositionOffset);

        WorldManager.Instance?.Register(this);
        ApplyWorld(WorldManager.Instance?.CurrentWorld ?? WorldType.Reality);
    }

    public override void _ExitTree()
    {
        WorldManager.Instance?.Unregister(this);
    }

    public void ApplyWorld(WorldType worldType)
    {
        if (_targetRoot == null || !_runtimeStates.TryGetValue(worldType, out var state))
        {
            return;
        }

        var isVisible = state.Exists && !state.IsBroken;
        var hasCollision = isVisible && state.IsActive;

        if (ApplyPositionOffsetOnWorldChange)
        {
            _targetRoot.Position = _basePosition + state.PositionOffset;
        }

        if (_visualRoot != null)
        {
            _visualRoot.Visible = isVisible;
        }

        if (_collisionRoot != null)
        {
            SetCollisionEnabled(_collisionRoot, hasCollision);
        }
    }

    public void SetBroken(WorldType worldType, bool isBroken)
    {
        if (_runtimeStates.TryGetValue(worldType, out var state))
        {
            state.IsBroken = isBroken;
            RefreshIfCurrentWorld(worldType);
        }
    }

    public void SetActive(WorldType worldType, bool isActive)
    {
        if (_runtimeStates.TryGetValue(worldType, out var state))
        {
            state.IsActive = isActive;
            RefreshIfCurrentWorld(worldType);
        }
    }

    public void SetExists(WorldType worldType, bool exists)
    {
        if (_runtimeStates.TryGetValue(worldType, out var state))
        {
            state.Exists = exists;
            RefreshIfCurrentWorld(worldType);
        }
    }

    public void BreakInCurrentWorld()
    {
        var currentWorld = WorldManager.Instance?.CurrentWorld ?? WorldType.Reality;
        SetBroken(currentWorld, true);
    }

    private void RefreshIfCurrentWorld(WorldType worldType)
    {
        if (WorldManager.Instance?.CurrentWorld == worldType)
        {
            ApplyWorld(worldType);
        }
    }

    private Node2D? ResolveTargetRoot()
    {
        if (TargetRootPath != null && !TargetRootPath.IsEmpty)
        {
            return GetNodeOrNull<Node2D>(TargetRootPath);
        }

        return GetParentOrNull<Node2D>();
    }

    private CanvasItem? ResolveVisualRoot()
    {
        if (VisualRootPath != null && !VisualRootPath.IsEmpty)
        {
            return GetNodeOrNull<CanvasItem>(VisualRootPath);
        }

        return _targetRoot;
    }

    private Node? ResolveCollisionRoot()
    {
        if (CollisionRootPath != null && !CollisionRootPath.IsEmpty)
        {
            return GetNodeOrNull(CollisionRootPath);
        }

        return _targetRoot;
    }

    private void SetCollisionEnabled(Node node, bool enabled)
    {
        switch (node)
        {
            case CollisionShape2D collisionShape:
                collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !enabled);
                break;
            case CollisionPolygon2D collisionPolygon:
                collisionPolygon.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, !enabled);
                break;
            case CanvasItem canvasItem when node != _visualRoot:
                canvasItem.Visible = enabled;
                break;
        }

        foreach (Node child in node.GetChildren())
        {
            SetCollisionEnabled(child, enabled);
        }
    }

    private sealed class RuntimeState
    {
        public RuntimeState(bool exists, bool isBroken, bool isActive, Vector2 positionOffset)
        {
            Exists = exists;
            IsBroken = isBroken;
            IsActive = isActive;
            PositionOffset = positionOffset;
        }

        public bool Exists { get; set; }

        public bool IsBroken { get; set; }

        public bool IsActive { get; set; }

        public Vector2 PositionOffset { get; set; }
    }
}
