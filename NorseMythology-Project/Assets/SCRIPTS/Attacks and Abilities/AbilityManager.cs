using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public PlayerMovement playerMovement;
    
    [Header("Equipped Abilities")]
    public Ability[] equippedAbilities = new Ability[4];
    
    private float[] lastAbilityUse = new float[4];
    
    private void Update()
    {
        if (playerController != null && !playerController.isDead)
        {
            HandleAbilityInput();
        }
    }
    
    private void HandleAbilityInput()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                TryActivateAbility(i);
            }
        }
    }
    
    private void TryActivateAbility(int index)
    {
        if (equippedAbilities[index] == null)
        {
            Debug.Log($"No ability equipped in slot {index + 1}");
            return;
        }
        
        // Add null check for playerController
        if (playerController == null || playerController.currentStats == null)
        {
            Debug.LogWarning("AbilityManager: PlayerController or currentStats is null");
            return;
        }
        
        Ability ability = equippedAbilities[index];
        
        // Calculate cooldown with player's cooldown reduction
        float cooldownReduction = 1f - (playerController.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = ability.cooldown * cooldownReduction;
        
        if (Time.time - lastAbilityUse[index] >= adjustedCooldown)
        {
            if (ability.CanActivate(playerController))
            {
                ability.Activate(playerController, playerMovement);
                lastAbilityUse[index] = Time.time;
                Debug.Log($"{ability.abilityName} activated!");
            }
            else
            {
                Debug.Log($"Cannot activate {ability.abilityName}");
            }
        }
        else
        {
            float timeLeft = adjustedCooldown - (Time.time - lastAbilityUse[index]);
            Debug.Log($"{ability.abilityName} on cooldown: {timeLeft:F1}s remaining");
        }
    }
    
    public void EquipAbility(Ability ability, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < equippedAbilities.Length)
        {
            equippedAbilities[slotIndex] = ability;
            Debug.Log($"{ability.abilityName} equipped to slot {slotIndex + 1}");
        }
    }
    
    public float GetAbilityCooldownRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedAbilities.Length || equippedAbilities[slotIndex] == null)
            return 0f;

        // Add null check for playerController and currentStats
        if (playerController == null || playerController.currentStats == null)
            return 0f;

        float cooldownReduction = 1f - (playerController.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = equippedAbilities[slotIndex].cooldown * cooldownReduction;

        return Mathf.Max(0f, adjustedCooldown - (Time.time - lastAbilityUse[slotIndex]));
    }
}