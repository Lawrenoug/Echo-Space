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
        ResetUnlockedTalentsInternal(emitChanged: true);
    }

    public void ResetToDefaults()
    {
        EnsureInitialized();
        ResetUnlockedTalentsInternal(emitChanged: true);
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

        // Mark initialized before resetting defaults so startup cannot recurse
        // back into EnsureInitialized through public reset helpers.
        _isInitialized = true;
        BuildDefaultTree();
        ResetUnlockedTalentsInternal(emitChanged: false);
    }

    private void ResetUnlockedTalentsInternal(bool emitChanged)
    {
        foreach (var definition in _orderedDefinitions)
        {
            _states[definition.Id].SetUnlocked(definition.IsStart);
        }

        UnspentTalentPoints = Mathf.Max(0, StartingTalentPoints);

        if (emitChanged)
        {
            EmitTreeChanged();
        }
    }

    private void BuildDefaultTree()
    {
        _definitions.Clear();
        _states.Clear();
        _orderedDefinitions.Clear();

        AddNode(new TalentNodeDefinition(
            "origin",
            "原初核心",
            "天赋树的起始节点。后续职业分支、战斗流派和探索能力都可以从这里继续展开。",
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
            "坚韧血脉",
            "生命分支占位节点。后续可替换为最大生命、减伤、恢复效率等防御向天赋。",
            new Vector2(-180f, -60f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "vitality_2"));

        AddNode(new TalentNodeDefinition(
            "vitality_2",
            "磐石之心",
            "生命分支核心节点。适合放置高价值的生存强化、恢复机制或受击收益类天赋。",
            new Vector2(-340f, -130f),
            TalentNodeType.Major,
            1,
            false,
            "vitality_1"));

        AddNode(new TalentNodeDefinition(
            "strength_1",
            "锋刃压迫",
            "攻击分支占位节点。后续可用于普通攻击、架势伤害和处决节奏强化。",
            new Vector2(180f, -60f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "strength_2"));

        AddNode(new TalentNodeDefinition(
            "strength_2",
            "处决节奏",
            "攻击分支核心节点。适合放置处决收益、连段增幅或伤害转化类天赋。",
            new Vector2(340f, -130f),
            TalentNodeType.Major,
            1,
            false,
            "strength_1"));

        AddNode(new TalentNodeDefinition(
            "deflection_1",
            "格挡律动",
            "防御分支占位节点。后续可替换为弹反窗口、格挡耗耐和架势控制相关天赋。",
            new Vector2(-80f, 170f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "deflection_2"));

        AddNode(new TalentNodeDefinition(
            "deflection_2",
            "镜返反击",
            "防御分支核心节点。适合放置高收益弹反、反击追击或破绽惩罚类天赋。",
            new Vector2(-160f, 320f),
            TalentNodeType.Major,
            1,
            false,
            "deflection_1"));

        AddNode(new TalentNodeDefinition(
            "mobility_1",
            "步伐爆发",
            "机动分支占位节点。后续可接入冲刺、空中控制和回收路线所需的探索能力。",
            new Vector2(90f, 170f),
            TalentNodeType.Minor,
            1,
            false,
            "origin",
            "mobility_2"));

        AddNode(new TalentNodeDefinition(
            "mobility_2",
            "虚界疾驰",
            "机动分支关键石节点。适合放置改变探索路线的大型位移或双世界机动能力。",
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
