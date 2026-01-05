using UnityEngine;

public class HammerAudioController : MonoBehaviour
{
    [Header("Pitch Settings")]
    [Tooltip("Semistones to add per hit on the way OUT.")]
    [SerializeField] private float semitoneStep = 1f; 
    
    [Tooltip("Maximum pitch offset (12 = 1 octave).")]
    [SerializeField] private float maxSemitoneCap = 12f;

    private float currentSemitone = 0f;
    private bool isReturning = false;
    private Projectile projectile;

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
    }

    private void OnEnable()
    {
        if (projectile != null)
        {
            // Subscribe to the hit event we made in Step 3
            projectile.OnEnemyHit += HandleEnemyHit;
            
            // Tell the projectile to SHUT UP so we can talk
            projectile.muteImpactSound = true;
        }
    }

    private void OnDisable()
    {
        if (projectile != null)
        {
            projectile.OnEnemyHit -= HandleEnemyHit;
        }
    }
    
    // Call this from HammerThrowAbility or Projectile when it turns around
    public void SetReturningState(bool returning)
    {
        isReturning = returning;
    }

    private void HandleEnemyHit(Enemy enemy)
    {
        // 1. Calculate Pitch
        if (!isReturning)
            currentSemitone += semitoneStep;
        else
            currentSemitone -= semitoneStep;

        // Clamp logic
        currentSemitone = Mathf.Clamp(currentSemitone, -5f, maxSemitoneCap);

        // 2. Play the sound using the ENEMY'S clip
        if (enemy.damageSound != null)
        {
            // Calculate pitch multiplier: 1.05946**semitones
            float pitchMult = Mathf.Pow(1.05946f, currentSemitone);
            
            AudioManager.Instance.PlaySFX(enemy.damageSound, 1f, pitchMult);
        }
    }
}