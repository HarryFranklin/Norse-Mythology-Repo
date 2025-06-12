using UnityEngine;

[CreateAssetMenu(fileName = "HammerSlamAbility", menuName = "Abilities/Defend/HammerSlam")]
public class HammerSlamAbilitySimple : DefendAbility
{
    [Header("Hammer Slam Damage")]
    [SerializeField] private float maxRadius = 4f;
    [SerializeField] private float maxDamage = 8f;
    [SerializeField] private float minDamage = 2f;
    [SerializeField] private float maxKnockbackDistance = 3f;
    [SerializeField] private float minKnockbackDistance = 1f;
    [SerializeField] private float knockbackSpeed = 18f;
    [SerializeField] private float knockbackDuration = 0.4f;
    
    [Header("Stun Effects")]
    [SerializeField] private float maxStunDuration = 2.5f;
    [SerializeField] private float minStunDuration = 0.8f;
    
    [Header("Knockback Variation")]
    [Range(0f, 0.3f)]
    [SerializeField] private float damageVariation = 0.15f;
    [Range(0f, 0.3f)]
    [SerializeField] private float knockbackVariation = 0.2f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hammerPrefab;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private GameObject dustCloudPrefab;
    
    [Header("Audio")]
    [SerializeField] private AudioClip slamSound;
    [SerializeField] private AudioClip shockwaveSound;
    
    [Header("Falloff Curves")]
    [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
    [SerializeField] private AnimationCurve knockbackFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.4f);
    [SerializeField] private AnimationCurve stunFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

    private Knockback.KnockbackSettings knockbackSettings;

    private void Awake()
    {
        abilityName = "Hammer Slam";
        description = "Slam a massive hammer into the ground, dealing heavy damage to nearby enemies and knocking them back with devastating force.";
        cooldown = 8f;
        duration = 0.5f; // Instant effect
        effectStrength = maxDamage;
        
        InitialiseKnockbackSettings();
    }

    private void InitialiseKnockbackSettings()
    {
        knockbackSettings = new Knockback.KnockbackSettings();
        knockbackSettings.maxRadius = maxRadius;
        knockbackSettings.maxDamage = maxDamage;
        knockbackSettings.minDamage = minDamage;
        knockbackSettings.maxKnockbackDistance = maxKnockbackDistance;
        knockbackSettings.minKnockbackDistance = minKnockbackDistance;
        knockbackSettings.knockbackSpeed = knockbackSpeed;
        knockbackSettings.knockbackDuration = knockbackDuration;
        knockbackSettings.maxStunDuration = maxStunDuration;
        knockbackSettings.minStunDuration = minStunDuration;
        knockbackSettings.damageVariation = damageVariation;
        knockbackSettings.knockbackVariation = knockbackVariation;
        knockbackSettings.damageFalloff = damageFalloff;
        knockbackSettings.knockbackFalloff = knockbackFalloff;
        knockbackSettings.stunFalloff = stunFalloff;
    }

    public override bool CanActivate(Player player)
    {
        return player != null && !player.isDead;
    }

    public override void Activate(Player player, PlayerMovement playerMovement = null)
    {
        if (player == null) return;

        Vector3 slamPosition = player.transform.position;
        
        // Play sound effects
        if (slamSound != null)
        {
            AudioSource.PlayClipAtPoint(slamSound, slamPosition);
        }
        
        // Spawn visual effects
        SpawnImpactEffects(slamPosition);
        
        // Apply knockback damage
        LayerMask enemyLayerMask = 1 << 8; // Enemy layer
        InitialiseKnockbackSettings();
        Knockback.ApplyRadialKnockback(slamPosition, knockbackSettings, enemyLayerMask, "Enemy");
        
        // Play delayed shockwave sound
        if (shockwaveSound != null)
        {
            // Use Invoke to delay the sound
            player.GetComponent<MonoBehaviour>()?.Invoke(nameof(PlayShockwaveSound), 0.2f);
        }
    }

    private void SpawnImpactEffects(Vector3 position)
    {
        // Hammer effect
        if (hammerPrefab != null)
        {
            GameObject hammer = Instantiate(hammerPrefab, position, Quaternion.identity);
            Destroy(hammer, 2f);
        }

        // Shockwave effect
        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, position, Quaternion.identity);
            Destroy(shockwave, 1f);
        }

        // Dust cloud effect
        if (dustCloudPrefab != null)
        {
            GameObject dust = Instantiate(dustCloudPrefab, position, Quaternion.identity);
            Destroy(dust, 3f);
        }
    }

    private void PlayShockwaveSound()
    {
        if (shockwaveSound != null)
        {
            AudioSource.PlayClipAtPoint(shockwaveSound, Vector3.zero);
        }
    }

    [ContextMenu("Test Hammer Slam")]
    private void TestHammerSlam()
    {
        if (Application.isPlaying)
        {
            InitialiseKnockbackSettings();
            LayerMask enemyLayerMask = 1 << 8; // Enemy layer
            Knockback.ApplyRadialKnockback(Vector3.zero, knockbackSettings, enemyLayerMask, "Enemy");
        }
    }

    private void OnValidate()
    {
        minDamage = Mathf.Min(minDamage, maxDamage);
        minKnockbackDistance = Mathf.Min(minKnockbackDistance, maxKnockbackDistance);
        minStunDuration = Mathf.Min(minStunDuration, maxStunDuration);
        
        if (Application.isPlaying)
        {
            InitialiseKnockbackSettings();
        }
    }
}