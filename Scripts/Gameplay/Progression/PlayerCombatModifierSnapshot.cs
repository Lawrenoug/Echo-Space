namespace EchoSpace.Gameplay.Progression;

public readonly struct PlayerCombatModifierSnapshot
{
    public PlayerCombatModifierSnapshot(
        int bonusHealth,
        float bonusStamina,
        float attackPostureMultiplier,
        float guardStaminaMultiplier,
        float soulAttunementMultiplier)
    {
        BonusHealth = bonusHealth;
        BonusStamina = bonusStamina;
        AttackPostureMultiplier = attackPostureMultiplier;
        GuardStaminaMultiplier = guardStaminaMultiplier;
        SoulAttunementMultiplier = soulAttunementMultiplier;
    }

    public int BonusHealth { get; }
    public float BonusStamina { get; }
    public float AttackPostureMultiplier { get; }
    public float GuardStaminaMultiplier { get; }
    public float SoulAttunementMultiplier { get; }
}
