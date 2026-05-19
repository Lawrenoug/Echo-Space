using System;
using System.Collections.Generic;
using Godot;

namespace EchoSpace.Gameplay.Progression;

public partial class TalentTreeManager : Node
{
    public static TalentTreeManager? Instance { get; private set; }

    public event Action? TreeChanged;

    [Export] public int StartingTalentPoints { get; set; } = 4;

    private readonly Dictionary<string, TalentNodeDefinition> _definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TalentNodeState> _states = new(StringComparer.Ordinal);
    private readonly List<TalentNodeDefinition> _orderedDefinitions = new();
    private bool _isInitialized;

    public int UnspentTalentPoints { get; private set; }
    public string StartNodeId { get; private set; } = "origin";
    public IReadOnlyList<TalentNodeDefinition> OrderedDefinitions => _orderedDefinitions;
    public IReadOnlyDictionary<string, TalentNodeState> States => _states;

    public override void _Ready()
    {
        Instance = this;
        EnsureInitialized();
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public TalentNodeDefinition? GetNodeDefinition(string? nodeId)
    {
        EnsureInitialized();
        return nodeId != null && _definitions.TryGetValue(nodeId, out var definition)
            ? definition
            : null;
    }

    public TalentNodeState? GetNodeState(string? nodeId)
    {
        EnsureInitialized();
        return nodeId != null && _states.TryGetValue(nodeId, out var state)
            ? state
            : null;
    }

    public bool IsNodeUnlocked(string? nodeId)
    {
        return GetNodeState(nodeId)?.IsUnlocked == true;
    }

    public bool CanUnlockNode(string? nodeId)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(nodeId)
            || !_definitions.TryGetValue(nodeId, out var definition)
            || !_states.TryGetValue(nodeId, out var state))
        {
            return false;
        }

        if (state.IsUnlocked || definition.IsStart || UnspentTalentPoints < definition.Cost)
        {
            return false;
        }

        foreach (var neighborId in definition.Neighbors)
        {
            if (IsNodeUnlocked(neighborId))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryUnlockNode(string? nodeId)
    {
        if (!CanUnlockNode(nodeId) || nodeId == null)
        {
            return false;
        }

        var definition = _definitions[nodeId];
        _states[nodeId].SetUnlocked(true);
        UnspentTalentPoints -= definition.Cost;
        EmitTreeChanged();
        return true;
    }

    public bool CanRefundNode(string? nodeId)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(nodeId)
            || !_definitions.TryGetValue(nodeId, out var definition)
            || !_states.TryGetValue(nodeId, out var state)
            || !state.IsUnlocked
            || definition.IsStart)
        {
            return false;
        }

        return WouldUnlockedNetworkRemainConnectedWithout(nodeId);
    }

    public bool TryRefundNode(string? nodeId)
    {
        if (!CanRefundNode(nodeId) || nodeId == null)
        {
            return false;
        }

        var definition = _definitions[nodeId];
        _states[nodeId].SetUnlocked(false);
        UnspentTalentPoints += definition.Cost;
        EmitTreeChanged();
        return true;
    }

    public void GrantTalentPoints(int amount)
    {
        EnsureInitialized();

        if (amount <= 0)
        {
            return;
        }

        UnspentTalentPoints += amount;
        EmitTreeChanged();
    }

    public void ResetUnlockedTalents()
    {
        EnsureInitialized();

        foreach (var definition in _orderedDefinitions)
        {
            _states[definition.Id].SetUnlocked(definition.IsStart);
        }

        UnspentTalentPoints = Mathf.Max(0, StartingTalentPoints);
        EmitTreeChanged();
    }

    public void ResetToDefaults()
    {
        EnsureInitialized();
        ResetUnlockedTalents();
    }

    public int GetUnlockedNodeCount()
    {
        EnsureInitialized();

        var count = 0;
        foreach (var state in _states.Values)
        {
            if (state.IsUnlocked)
            {
                count += 1;
            }
        }

        return count;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        BuildDefaultTree();
        ResetUnlockedTalents();
        _isInitialized = true;
    }

    private void BuildDefaultTree()
    {
        _definitions.Clear();
        _states.Clear();
        _orderedDefinitions.Clear();

        AddNode(new TalentNodeDefinition(
            "origin",
            "Origin Core",
            "The starting hub of the talent tree. Future classes and paths can branch from here.",
            new Vector2(0f, 0f),
            TalentNodeType.Start,
            0,
            true,
            "vitality_1",
            "strength_1",
            "deflection_1",
            "mobility_1"));

        AddNode(new TalentNodeDefinition(
            "vitality_1",
            "Hardened Blood",
            "Placeholder life branch node. Use this lane later for defensive sustain talents.",
            new Vector2(-180f, -60f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "vitality_2"));

        AddNode(new TalentNodeDefinition(
            "vitality_2",
            "Stone Heart",
            "Major life node placeholder. Good slot for max health, recovery, or mitigation talents.",
            new Vector2(-340f, -130f),
            TalentNodeType.Major,
            1,
            false,
            "vitality_1"));

        AddNode(new TalentNodeDefinition(
            "strength_1",
            "Edge Pressure",
            "Placeholder offense branch node. Use this lane for attack and posture pressure upgrades.",
            new Vector2(180f, -60f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "strength_2"));

        AddNode(new TalentNodeDefinition(
            "strength_2",
            "Execution Tempo",
            "Major offense node placeholder. Good fit for execution, combo, or damage conversion talents.",
            new Vector2(340f, -130f),
            TalentNodeType.Major,
            1,
            false,
            "strength_1"));

        AddNode(new TalentNodeDefinition(
            "deflection_1",
            "Guard Rhythm",
            "Placeholder guard branch node. Reserve this path for parry timing, guard stamina, and posture control.",
            new Vector2(-80f, 170f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "deflection_2"));

        AddNode(new TalentNodeDefinition(
            "deflection_2",
            "Mirror Counter",
            "Major guard node placeholder. Good slot for high-impact deflect or counterattack talents.",
            new Vector2(-160f, 320f),
            TalentNodeType.Major,
            1,
            false,
            "deflection_1"));

        AddNode(new TalentNodeDefinition(
            "mobility_1",
            "Stride Burst",
            "Placeholder mobility branch node. Reserve this path for dash, aerial control, or route-break talents.",
            new Vector2(90f, 170f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "mobility_2"));

        AddNode(new TalentNodeDefinition(
            "mobility_2",
            "Void Sprint",
            "Keystone mobility placeholder. Good slot for a future exploration-defining movement talent.",
            new Vector2(210f, 320f),
            TalentNodeType.Keystone,
            1,
            false,
            "mobility_1"));
    }

    private void AddNode(TalentNodeDefinition definition)
    {
        _definitions[definition.Id] = definition;
        _states[definition.Id] = new TalentNodeState(definition.Id);
        _orderedDefinitions.Add(definition);

        if (definition.IsStart)
        {
            StartNodeId = definition.Id;
        }
    }

    private bool WouldUnlockedNetworkRemainConnectedWithout(string removedNodeId)
    {
        var remainingUnlocked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in _states)
        {
            if (pair.Key != removedNodeId && pair.Value.IsUnlocked)
            {
                remainingUnlocked.Add(pair.Key);
            }
        }

        if (remainingUnlocked.Count == 0)
        {
            return true;
        }

        var queue = new Queue<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in _orderedDefinitions)
        {
            if (definition.IsStart && remainingUnlocked.Contains(definition.Id))
            {
                queue.Enqueue(definition.Id);
                visited.Add(definition.Id);
            }
        }

        if (queue.Count == 0)
        {
            return false;
        }

        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            if (!_definitions.TryGetValue(nodeId, out var definition))
            {
                continue;
            }

            foreach (var neighborId in definition.Neighbors)
            {
                if (!remainingUnlocked.Contains(neighborId) || !visited.Add(neighborId))
                {
                    continue;
                }

                queue.Enqueue(neighborId);
            }
        }

        return visited.Count == remainingUnlocked.Count;
    }

    private void EmitTreeChanged()
    {
        TreeChanged?.Invoke();
    }
}
