using System;
using System.Collections.Generic;
using EchoSpace.Gameplay.Progression;
using Godot;

namespace EchoSpace.UI;

public partial class TalentTreeView : Control
{
    public event Action<string>? NodeSelected;
    public event Action<string>? NodeUnlockRequested;
    public event Action<string>? NodeRefundRequested;

    [ExportGroup("View")]
    [Export] public float MinZoom { get; set; } = 0.7f;
    [Export] public float MaxZoom { get; set; } = 1.45f;
    [Export] public float ZoomStep { get; set; } = 0.08f;
    [Export] public float GridStep { get; set; } = 64f;

    [ExportGroup("Colors")]
    [Export] public Color BackgroundColor { get; set; } = new(0.09f, 0.10f, 0.13f, 1f);
    [Export] public Color GridColor { get; set; } = new(0.20f, 0.23f, 0.30f, 0.55f);
    [Export] public Color LockedNodeColor { get; set; } = new(0.28f, 0.31f, 0.37f, 1f);
    [Export] public Color AvailableNodeColor { get; set; } = new(0.35f, 0.65f, 0.95f, 1f);
    [Export] public Color UnlockedNodeColor { get; set; } = new(0.92f, 0.76f, 0.37f, 1f);
    [Export] public Color KeystoneNodeColor { get; set; } = new(0.95f, 0.49f, 0.36f, 1f);
    [Export] public Color ConnectionLockedColor { get; set; } = new(0.34f, 0.37f, 0.44f, 0.72f);
    [Export] public Color ConnectionActiveColor { get; set; } = new(0.92f, 0.76f, 0.37f, 0.95f);
    [Export] public Color SelectionColor { get; set; } = new(1f, 1f, 1f, 0.95f);

    private TalentTreeManager? _manager;
    private string? _selectedNodeId;
    private float _zoom = 1f;
    private Vector2 _panOffset;
    private bool _isPanning;
    private Vector2 _lastPanMousePosition;

    public string? SelectedNodeId => _selectedNodeId;

    public override void _Ready()
    {
        ClipContents = true;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        _manager = TalentTreeManager.Instance;
        if (_manager != null)
        {
            _manager.TreeChanged += OnTreeChanged;
            if (string.IsNullOrWhiteSpace(_selectedNodeId))
            {
                SetSelectedNode(_manager.StartNodeId, false);
            }
        }
    }

    public override void _ExitTree()
    {
        if (_manager != null)
        {
            _manager.TreeChanged -= OnTreeChanged;
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor, true);
        DrawGrid();

        if (_manager == null)
        {
            return;
        }

        var drawnConnections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in _manager.OrderedDefinitions)
        {
            foreach (var neighborId in definition.Neighbors)
            {
                var neighbor = _manager.GetNodeDefinition(neighborId);
                if (neighbor == null)
                {
                    continue;
                }

                var connectionKey = string.CompareOrdinal(definition.Id, neighborId) < 0
                    ? $"{definition.Id}|{neighborId}"
                    : $"{neighborId}|{definition.Id}";
                if (!drawnConnections.Add(connectionKey))
                {
                    continue;
                }

                var isActive = _manager.IsNodeUnlocked(definition.Id) && _manager.IsNodeUnlocked(neighborId);
                DrawLine(
                    ToCanvasPosition(definition.Position),
                    ToCanvasPosition(neighbor.Position),
                    isActive ? ConnectionActiveColor : ConnectionLockedColor,
                    isActive ? 4f : 2f,
                    true);
            }
        }

        foreach (var definition in _manager.OrderedDefinitions)
        {
            DrawNode(definition);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_manager == null)
        {
            return;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
            {
                AdjustZoom(mouseButton.Position, ZoomStep);
                AcceptEvent();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
            {
                AdjustZoom(mouseButton.Position, -ZoomStep);
                AcceptEvent();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                _isPanning = mouseButton.Pressed;
                _lastPanMousePosition = mouseButton.Position;
                AcceptEvent();
                return;
            }

            if (!mouseButton.Pressed)
            {
                return;
            }

            var clickedNode = FindNodeAt(mouseButton.Position);
            if (clickedNode == null)
            {
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                var wasSelected = string.Equals(_selectedNodeId, clickedNode.Id, StringComparison.Ordinal);
                SetSelectedNode(clickedNode.Id, true);

                if (wasSelected
                    && !_manager.IsNodeUnlocked(clickedNode.Id)
                    && _manager.CanUnlockNode(clickedNode.Id))
                {
                    NodeUnlockRequested?.Invoke(clickedNode.Id);
                }

                AcceptEvent();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                SetSelectedNode(clickedNode.Id, true);
                if (_manager.CanRefundNode(clickedNode.Id))
                {
                    NodeRefundRequested?.Invoke(clickedNode.Id);
                }

                AcceptEvent();
            }

            return;
        }

        if (@event is InputEventMouseMotion mouseMotion && _isPanning)
        {
            var delta = mouseMotion.Position - _lastPanMousePosition;
            _panOffset += delta;
            _lastPanMousePosition = mouseMotion.Position;
            QueueRedraw();
            AcceptEvent();
        }
    }

    public void SetSelectedNode(string? nodeId, bool emitSelection)
    {
        if (_manager == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nodeId) || _manager.GetNodeDefinition(nodeId) == null)
        {
            nodeId = _manager.StartNodeId;
        }

        _selectedNodeId = nodeId;
        QueueRedraw();

        if (emitSelection && nodeId != null)
        {
            NodeSelected?.Invoke(nodeId);
        }
    }

    public bool ActivateSelectedNode()
    {
        if (_manager == null || string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            return false;
        }

        if (!_manager.CanUnlockNode(_selectedNodeId))
        {
            return false;
        }

        NodeUnlockRequested?.Invoke(_selectedNodeId);
        return true;
    }

    public bool RefundSelectedNode()
    {
        if (_manager == null || string.IsNullOrWhiteSpace(_selectedNodeId))
        {
            return false;
        }

        if (!_manager.CanRefundNode(_selectedNodeId))
        {
            return false;
        }

        NodeRefundRequested?.Invoke(_selectedNodeId);
        return true;
    }

    private void DrawGrid()
    {
        var step = Mathf.Max(32f, GridStep * _zoom);
        for (float x = Mathf.PosMod(_panOffset.X, step); x < Size.X; x += step)
        {
            DrawLine(new Vector2(x, 0f), new Vector2(x, Size.Y), GridColor, 1f, true);
        }

        for (float y = Mathf.PosMod(_panOffset.Y, step); y < Size.Y; y += step)
        {
            DrawLine(new Vector2(0f, y), new Vector2(Size.X, y), GridColor, 1f, true);
        }
    }

    private void DrawNode(TalentNodeDefinition definition)
    {
        if (_manager == null)
        {
            return;
        }

        var position = ToCanvasPosition(definition.Position);
        var radius = GetNodeRadius(definition.NodeType) * _zoom;
        var color = GetNodeColor(definition);

        DrawCircle(position, radius, color);

        var outlineWidth = string.Equals(_selectedNodeId, definition.Id, StringComparison.Ordinal) ? 4f : 2f;
        var outlineColor = string.Equals(_selectedNodeId, definition.Id, StringComparison.Ordinal)
            ? SelectionColor
            : new Color(0.04f, 0.05f, 0.08f, 0.95f);
        DrawArc(position, radius + 1f, 0f, Mathf.Tau, 28, outlineColor, outlineWidth, true);

        if (definition.NodeType == TalentNodeType.Keystone)
        {
            DrawCircle(position, radius * 0.38f, new Color(0.15f, 0.08f, 0.08f, 0.9f));
        }
        else if (definition.NodeType == TalentNodeType.Start)
        {
            DrawCircle(position, radius * 0.28f, new Color(0.2f, 0.16f, 0.05f, 0.85f));
        }
    }

    private Color GetNodeColor(TalentNodeDefinition definition)
    {
        if (_manager == null)
        {
            return LockedNodeColor;
        }

        if (_manager.IsNodeUnlocked(definition.Id))
        {
            return definition.NodeType == TalentNodeType.Keystone
                ? KeystoneNodeColor
                : UnlockedNodeColor;
        }

        return _manager.CanUnlockNode(definition.Id)
            ? AvailableNodeColor
            : LockedNodeColor;
    }

    private TalentNodeDefinition? FindNodeAt(Vector2 localPosition)
    {
        if (_manager == null)
        {
            return null;
        }

        for (var index = _manager.OrderedDefinitions.Count - 1; index >= 0; index--)
        {
            var definition = _manager.OrderedDefinitions[index];
            var radius = GetNodeRadius(definition.NodeType) * _zoom;
            if (localPosition.DistanceTo(ToCanvasPosition(definition.Position)) <= radius + 6f)
            {
                return definition;
            }
        }

        return null;
    }

    private Vector2 ToCanvasPosition(Vector2 logicalPosition)
    {
        return Size * 0.5f + _panOffset + logicalPosition * _zoom;
    }

    private void AdjustZoom(Vector2 focusPosition, float delta)
    {
        var previousZoom = _zoom;
        _zoom = Mathf.Clamp(_zoom + delta, MinZoom, MaxZoom);
        if (Mathf.IsEqualApprox(previousZoom, _zoom))
        {
            return;
        }

        var focusFromCenter = focusPosition - Size * 0.5f - _panOffset;
        _panOffset -= focusFromCenter * ((_zoom / previousZoom) - 1f);
        QueueRedraw();
    }

    private static float GetNodeRadius(TalentNodeType nodeType)
    {
        return nodeType switch
        {
            TalentNodeType.Start => 22f,
            TalentNodeType.Minor => 14f,
            TalentNodeType.Major => 18f,
            TalentNodeType.Keystone => 24f,
            _ => 14f,
        };
    }

    private void OnTreeChanged()
    {
        if (_manager == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedNodeId) || _manager.GetNodeDefinition(_selectedNodeId) == null)
        {
            _selectedNodeId = _manager.StartNodeId;
        }

        QueueRedraw();
    }
}
