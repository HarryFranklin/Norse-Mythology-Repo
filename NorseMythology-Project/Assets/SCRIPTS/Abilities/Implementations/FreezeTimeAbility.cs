using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "FreezeTimeAbility", menuName = "Abilities/FreezeTime")]
public class FreezeTimeAbility : Ability
{
    [Header("Time Settings")]
    [Tooltip("Target time scale (0.05 = 5% speed).")]
    [SerializeField, Range(0f, 1f)] private float timeScaleIntensity = 0.05f;
    [SerializeField] private float entryDuration = 0.1f; 
    [SerializeField] private float exitDuration = 0.5f;  

    [Header("Camera Settings")]
    [SerializeField] private bool modifyCamera = true;
    [SerializeField] private float maxCameraSizeCap = 6.75f; 

    [Header("Audio")]
    [SerializeField] private AudioClip freezeStartSound;
    [SerializeField] private AudioClip freezeEndSound;
    
    [Header("Visuals")]
    [SerializeField] private GameObject screenFilterPrefab;

    private static bool isTimeFrozen = false;
    
    private void Awake()
    {
        abilityName = "Verðandi Lock";
        description = "Drastically slows down time for all enemies and projectiles while you move freely.";
        activationMode = ActivationMode.Instant;
        useCodeDefinedMatrix = true;
        InitialiseFromCodeMatrix(); 
    }
    
    private void OnDisable()
    {
        isTimeFrozen = false;
    }

    protected override void InitialiseFromCodeMatrix()
    {
        SetLevelData(1, cooldown: 15f, duration: 3f, maxStacks: 1, stackRegenTime: 15f, specialValue1: 5.8f);
        SetLevelData(2, cooldown: 12f, duration: 4f, maxStacks: 1, stackRegenTime: 12f, specialValue1: 6.0f);
        SetLevelData(3, cooldown: 10f, duration: 5f, maxStacks: 1, stackRegenTime: 10f, specialValue1: 6.2f);
        SetLevelData(4, cooldown: 8f, duration: 6f, maxStacks: 2, stackRegenTime: 8f, specialValue1: 6.4f);
        SetLevelData(5, cooldown: 6f, duration: 7f, maxStacks: 3, stackRegenTime: 10f, specialValue1: 6.6f);
    }

    public override bool CanActivate(Player player)
    {
        if (isTimeFrozen) return false;
        return base.CanActivate(player);
    }

    public override void Activate(Player player, PlayerMovement playerMovement)
    {
        if (player == null) return;
        RemoveStack();
        player.StartCoroutine(ExecuteSmoothTimeFreeze(player));
    }

    private IEnumerator ExecuteSmoothTimeFreeze(Player player)
    {
        isTimeFrozen = true;
        float initialFixedDeltaTime = 0.02f; // Fallback default
        if (Time.fixedDeltaTime > 0) initialFixedDeltaTime = Time.fixedDeltaTime;

        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        AnimatorUpdateMode originalUpdateMode = AnimatorUpdateMode.Normal;

        if (playerAnimator != null)
        {
            originalUpdateMode = playerAnimator.updateMode;
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        if (freezeStartSound != null) 
            AudioSource.PlayClipAtPoint(freezeStartSound, player.transform.position);

        GameObject activeFilter = null;
        if (screenFilterPrefab != null)
            activeFilter = Instantiate(screenFilterPrefab, Vector3.zero, Quaternion.identity);

        Camera cam = Camera.main;
        float startCamSize = 5.4f;
        if (cam != null) startCamSize = cam.orthographicSize;

        float targetCamSize = GetCurrentLevelData().specialValue1;
        if (targetCamSize > maxCameraSizeCap) targetCamSize = maxCameraSizeCap;
        if (targetCamSize <= 0.1f) targetCamSize = startCamSize; 


        // --- PHASE 1: FAST ENTRY ---
        float timer = 0f;
        while (timer < entryDuration)
        {
            if (player == null) { Cleanup(initialFixedDeltaTime, activeFilter, null, originalUpdateMode); yield break; }

            // 1. PAUSE CHECK: If TimeScale is 0 (Game Paused), wait and do nothing.
            // We check for exactly 0, assuming your Pause Menu sets Scale to 0.
            if (Time.timeScale == 0f)
            {
                yield return null;
                continue; 
            }

            timer += Time.unscaledDeltaTime; 
            float progress = Mathf.Clamp01(timer / entryDuration);

            float currentScale = Mathf.Lerp(1f, timeScaleIntensity, progress);
            
            // Only apply scale if not paused (redundant check but safe)
            if (Time.timeScale > 0) SetTimeScale(currentScale, initialFixedDeltaTime);

            if (cam != null && modifyCamera)
                cam.orthographicSize = Mathf.Lerp(startCamSize, targetCamSize, progress);

            yield return null;
        }

        // Ensure we hit exact target (only if not paused)
        if (Time.timeScale > 0) SetTimeScale(timeScaleIntensity, initialFixedDeltaTime);
        if (cam != null && modifyCamera) cam.orthographicSize = targetCamSize;


        // --- PHASE 2: HOLD ---
        float holdDuration = StackedDuration - entryDuration - exitDuration;
        float holdTimer = 0f;
        
        while (holdTimer < holdDuration)
        {
            if (player == null) { Cleanup(initialFixedDeltaTime, activeFilter, null, originalUpdateMode); yield break; }

            // 2. PAUSE CHECK
            if (Time.timeScale == 0f)
            {
                yield return null;
                continue;
            }

            holdTimer += Time.unscaledDeltaTime;
            
            // Force TimeScale maintenance in case another script tries to drift it
            // But only if not paused
            if (Time.timeScale != 0f && Time.timeScale != timeScaleIntensity)
            {
                 SetTimeScale(timeScaleIntensity, initialFixedDeltaTime);
            }

            yield return null;
        }


        // --- PHASE 3: SLOW EXIT ---
        if (freezeEndSound != null && player != null) 
            AudioSource.PlayClipAtPoint(freezeEndSound, player.transform.position);

        timer = 0f;
        while (timer < exitDuration)
        {
            if (player == null) { Cleanup(initialFixedDeltaTime, activeFilter, null, originalUpdateMode); yield break; }

            // 3. PAUSE CHECK
            if (Time.timeScale == 0f)
            {
                yield return null;
                continue;
            }

            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / exitDuration);

            float currentScale = Mathf.Lerp(timeScaleIntensity, 1f, progress);
            if (Time.timeScale > 0) SetTimeScale(currentScale, initialFixedDeltaTime);

            if (cam != null && modifyCamera)
                cam.orthographicSize = Mathf.Lerp(targetCamSize, startCamSize, progress);

            yield return null;
        }

        // Final Cleanup
        Cleanup(initialFixedDeltaTime, activeFilter, playerAnimator, originalUpdateMode);
        if (cam != null && modifyCamera) cam.orthographicSize = startCamSize;

        Debug.Log("Time Freeze Ended.");
    }

    private void SetTimeScale(float scale, float baseFixedDelta)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixedDelta * scale;
    }

    private void Cleanup(float resetFixedDeltaTime, GameObject filter, Animator anim, AnimatorUpdateMode originalMode)
    {
        // Only reset time if the game isn't currently paused.
        // If the player somehow cancels the ability while paused, we don't want to accidentally unpause the game.
        if (Time.timeScale > 0)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = resetFixedDeltaTime;
        }

        if (filter != null) Destroy(filter);
        if (anim != null) anim.updateMode = originalMode;
        isTimeFrozen = false;
    }
}