namespace EchoSpace.Gameplay.Progression;

public readonly struct PlayerCombatModifierSnapshot
{
    public PlayerCombatModifierSnapshot(
        int bonusHealth,
        float bonusStamina,
        int bonusAttackDamage,
        float attackPostureMultiplier,
        float deflectPostureMultiplier,
        float guardStaminaMultiplier,
        float soulAttunementMultiplier)
    {
        BonusHealth = bonusHealth;
        BonusStamina = bonusStamina;
        BonusAttackDamage = bonusAttackDamage;
        AttackPostureMultiplier = attackPostureMultiplier;
        DeflectPostureMultiplier = deflectPostureMultiplier;
        GuardStaminaMultiplier = guardStaminaMultiplier;
        SoulAttunementMultiplier = soulAttunementMultiplier;
    }

    public int BonusHealth { get; }
    public float BonusStamina { get; }
    public int BonusAttackDamage { get; }
    public float AttackPostureMultiplier { get; }
    public float DeflectPostureMultiplier { get; }
    public float GuardStaminaMultiplier { get; }
    public float SoulAttunementMultiplier { get; }
}
