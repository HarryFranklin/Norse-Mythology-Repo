using UnityEngine;

public abstract class DefendAbility : Ability
{
    [Header("Defend Settings")]
    public float duration = 3f;
    public float effectStrength = 1f;

    protected DefendAbility()
    {
        abilityType = AbilityType.Defend;
    }
}