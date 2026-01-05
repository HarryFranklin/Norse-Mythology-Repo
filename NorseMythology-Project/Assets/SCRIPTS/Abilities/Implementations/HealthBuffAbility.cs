using UnityEngine;

[CreateAssetMenu(fileName = "HealthBuffAbility", menuName = "Abilities/HealthBuff")]
public class HealthBuffAbility : Ability
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject buffEffectPrefab;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buffSound;
    [Range(0f, 1f)] 
    [SerializeField] private float buffVolume = 1f;
    [Tooltip("If true, randomises pitch slightly to avoid repetition.")]
    [SerializeField] private bool useRandomPitch = false; // Buffs often sound better consistent

    private void Awake()
    {
        abilityName = "Health Regeneration";
        description = "Instantly recover a portion of your health.";
        activationMode = ActivationMode.Instant;
    }

    public override void InitialiseFromCodeMatrix()
    {
        // Using 'damage' stat as the Heal Amount for simplicity
        SetLevelData(1, cooldown: 15f, damage: 20f, duration: 0f, radius: 0f, speed: 0f, distance: 0f, specialValue1: 0f, maxStacks: 1, stackRegenTime: 15f);
        SetLevelData(2, cooldown: 14f, damage: 30f, duration: 0f, radius: 0f, speed: 0f, distance: 0f, specialValue1: 0f, maxStacks: 1, stackRegenTime: 14f);
        SetLevelData(3, cooldown: 12f, damage: 45f, duration: 0f, radius: 0f, speed: 0f, distance: 0f, specialValue1: 0f, maxStacks: 1, stackRegenTime: 12f);
        SetLevelData(4, cooldown: 10f, damage: 65f, duration: 0f, radius: 0f, speed: 0f, distance: 0f, specialValue1: 0f, maxStacks: 2, stackRegenTime: 10f);
        SetLevelData(5, cooldown: 8f, damage: 100f, duration: 0f, radius: 0f, speed: 0f, distance: 0f, specialValue1: 0f, maxStacks: 2, stackRegenTime: 8f);
    }

    public override bool CanActivate(Player player)
    {
        // Optional: Prevent activation if already at full health?
        // return player != null && !player.isDead && CurrentStacks > 0 && player.currentHealth < player.maxHealth;
        
        return player != null && !player.isDead && CurrentStacks > 0;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;

        RemoveStack();

        // --- AUDIO ---
        if (AudioManager.Instance != null && buffSound != null)
        {
            AudioManager.Instance.PlaySFX(buffSound, buffVolume, useRandomPitch);
        }

        // --- VISUALS ---
        if (buffEffectPrefab != null)
        {
            // Spawn the effect attached to the player so it moves with them
            GameObject effect = Instantiate(buffEffectPrefab, player.transform.position, Quaternion.identity);
            effect.transform.SetParent(player.transform);
            
            // Auto-cleanup (optional, assuming the prefab has a self-destruct script or particle system)
            Destroy(effect, 2f);
        }

        // --- LOGIC ---
        // We use StackedDamage as the healing value based on the matrix above
        float healAmount = StackedDamage;
        player.Heal(healAmount);
        
        Debug.Log($"Health Buff Activated: Healed {healAmount} HP");
    }
}