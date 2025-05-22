using UnityEngine;

public abstract class AttackAbility : Ability
{
    [Header("Attack Settings")]
    public float damage = 20f;
    public float range = 5f;
    
    protected AttackAbility()
    {
        abilityType = AbilityType.Attack;
    }
}