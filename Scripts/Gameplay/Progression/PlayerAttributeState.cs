using System;

namespace EchoSpace.Gameplay.Progression;

public sealed class PlayerAttributeState
{
    public PlayerAttributeState(PlayerAttributeType attributeType, int baseLevel, int hardCap)
    {
        AttributeType = attributeType;
        BaseLevel = baseLevel;
        CurrentLevel = baseLevel;
        HardCap = hardCap;
    }

    public PlayerAttributeType AttributeType { get; }
    public int BaseLevel { get; }
    public int CurrentLevel { get; private set; }
    public int HardCap { get; }
    public int AllocatedPoints => CurrentLevel - BaseLevel;

    public bool TryIncrease()
    {
        if (CurrentLevel >= HardCap)
        {
            return false;
        }

        CurrentLevel += 1;
        return true;
    }

    public bool TryDecrease()
    {
        if (CurrentLevel <= BaseLevel)
        {
            return false;
        }

        CurrentLevel -= 1;
        return true;
    }

    public int ResetToBase()
    {
        var refunded = AllocatedPoints;
        CurrentLevel = BaseLevel;
        return refunded;
    }

    public void SetLevel(int level)
    {
        CurrentLevel = Math.Clamp(level, BaseLevel, HardCap);
    }
}
