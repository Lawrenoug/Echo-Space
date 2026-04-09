using System;
using System.Collections.Generic;
using Godot;

namespace EchoSpace.Gameplay.Progression;

public partial class ProgressionManager : Node
{
    public static ProgressionManager? Instance { get; private set; }

    public event Action<int>? LevelChanged;
    public event Action<int>? UnspentPointsChanged;
    public event Action<PlayerAttributeType, int>? AttributeChanged;
    public event Action? AttributesReset;

    [Export] public int StartingLevel { get; set; } = 1;
    [Export] public int StartingUnspentPoints { get; set; } = 5;
    [Export] public int BaseAttributeLevel { get; set; } = 1;
    [Export] public int AttributeHardCap { get; set; } = 20;
    [Export] public int PointsPerLevel { get; set; } = 1;

    private readonly Dictionary<PlayerAttributeType, PlayerAttributeState> _attributes = new();
    private bool _isInitialized;

    public int CurrentLevel { get; private set; }
    public int UnspentPoints { get; private set; }
    public IReadOnlyDictionary<PlayerAttributeType, PlayerAttributeState> Attributes => _attributes;

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

    public int GetAttributeLevel(PlayerAttributeType attributeType)
    {
        EnsureInitialized();
        return _attributes[attributeType].CurrentLevel;
    }

    public bool TryAllocatePoint(PlayerAttributeType attributeType)
    {
        EnsureInitialized();

        if (UnspentPoints <= 0 || !_attributes.TryGetValue(attributeType, out var attributeState))
        {
            return false;
        }

        if (!attributeState.TryIncrease())
        {
            return false;
        }

        UnspentPoints -= 1;
        AttributeChanged?.Invoke(attributeType, attributeState.CurrentLevel);
        UnspentPointsChanged?.Invoke(UnspentPoints);
        return true;
    }

    public bool TryRefundPoint(PlayerAttributeType attributeType)
    {
        EnsureInitialized();

        if (!_attributes.TryGetValue(attributeType, out var attributeState) || !attributeState.TryDecrease())
        {
            return false;
        }

        UnspentPoints += 1;
        AttributeChanged?.Invoke(attributeType, attributeState.CurrentLevel);
        UnspentPointsChanged?.Invoke(UnspentPoints);
        return true;
    }

    public void GrantLevel(int levels = 1)
    {
        EnsureInitialized();

        if (levels <= 0)
        {
            return;
        }

        CurrentLevel += levels;
        UnspentPoints += levels * Mathf.Max(1, PointsPerLevel);
        LevelChanged?.Invoke(CurrentLevel);
        UnspentPointsChanged?.Invoke(UnspentPoints);
    }

    public void GrantPoints(int points)
    {
        EnsureInitialized();

        if (points <= 0)
        {
            return;
        }

        UnspentPoints += points;
        UnspentPointsChanged?.Invoke(UnspentPoints);
    }

    public void ResetAllocatedPoints()
    {
        EnsureInitialized();

        var refunded = 0;
        foreach (var pair in _attributes)
        {
            refunded += pair.Value.ResetToBase();
            AttributeChanged?.Invoke(pair.Key, pair.Value.CurrentLevel);
        }

        if (refunded <= 0)
        {
            return;
        }

        UnspentPoints += refunded;
        UnspentPointsChanged?.Invoke(UnspentPoints);
        AttributesReset?.Invoke();
    }

    public PlayerCombatModifierSnapshot BuildCombatModifiers()
    {
        EnsureInitialized();

        var vitalityBonus = GetAllocatedPoints(PlayerAttributeType.Vitality);
        var enduranceBonus = GetAllocatedPoints(PlayerAttributeType.Endurance);
        var strengthBonus = GetAllocatedPoints(PlayerAttributeType.Strength);
        var deflectionBonus = GetAllocatedPoints(PlayerAttributeType.Deflection);
        var soulBonus = GetAllocatedPoints(PlayerAttributeType.SoulAttunement);

        return new PlayerCombatModifierSnapshot(
            bonusHealth: vitalityBonus * 2,
            bonusStamina: enduranceBonus * 6f,
            attackPostureMultiplier: 1f + strengthBonus * 0.06f,
            guardStaminaMultiplier: Mathf.Max(0.65f, 1f - deflectionBonus * 0.04f),
            soulAttunementMultiplier: 1f + soulBonus * 0.08f);
    }

    private int GetAllocatedPoints(PlayerAttributeType attributeType)
    {
        return _attributes.TryGetValue(attributeType, out var attributeState)
            ? attributeState.AllocatedPoints
            : 0;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        CurrentLevel = Mathf.Max(1, StartingLevel);
        UnspentPoints = Mathf.Max(0, StartingUnspentPoints);
        _attributes.Clear();

        foreach (PlayerAttributeType attributeType in Enum.GetValues(typeof(PlayerAttributeType)))
        {
            _attributes[attributeType] = new PlayerAttributeState(
                attributeType,
                Mathf.Max(1, BaseAttributeLevel),
                Mathf.Max(BaseAttributeLevel, AttributeHardCap));
        }

        _isInitialized = true;
    }
}
