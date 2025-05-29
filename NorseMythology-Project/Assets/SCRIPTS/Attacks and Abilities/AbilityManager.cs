using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerMovement playerMovement;

    [Header("Equipped Abilities")]
    public Ability[] equippedAbilities = new Ability[4];

    private float[] lastAbilityUse = new float[4];

    private void Update()
    {
        if (player != null && !player.isDead)
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

        // Add null check for player
        if (player == null || player.currentStats == null)
        {
            Debug.LogWarning("AbilityManager: Player or currentStats is null");
            return;
        }

        Ability ability = equippedAbilities[index];

        // Calculate cooldown with player's cooldown reduction
        float cooldownReduction = 1f - (player.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = ability.cooldown * cooldownReduction;

        if (Time.time - lastAbilityUse[index] >= adjustedCooldown)
        {
            if (ability.CanActivate(player))
            {
                ability.Activate(player, playerMovement);
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

        // Add null check for player and currentStats
        if (player == null || player.currentStats == null)
            return 0f;

        float cooldownReduction = 1f - (player.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = equippedAbilities[slotIndex].cooldown * cooldownReduction;

        return Mathf.Max(0f, adjustedCooldown - (Time.time - lastAbilityUse[slotIndex]));
    }
}