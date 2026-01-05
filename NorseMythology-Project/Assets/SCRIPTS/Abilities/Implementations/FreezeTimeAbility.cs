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
    
    [Range(0f, 1f)] 
    [SerializeField] private float startVolume = 1f;

    [Tooltip("The looping tick sound.")]
    [SerializeField] private AudioClip freezeLoopSound; 
    
    [Tooltip("Controls how loud the tick loop is (0.5 = 50% volume).")]
    [Range(0f, 1f)] 
    [SerializeField] private float loopVolumeScale = 0.5f; 
    
    [SerializeField] private AudioClip freezeEndSound;
    // NEW: Volume control for end
    [Range(0f, 1f)] 
    [SerializeField] private float endVolume = 1f;

    [Header("Audio Dynamics")]
    [Tooltip("How high the pitch goes right before the ability ends (1.5 = +50% speed).")]
    [SerializeField] private float exitPitchTarget = 1.5f;

    [Header("Visuals")]
    [SerializeField] private GameObject screenFilterPrefab;

    // --- GLOBAL ACCESSORS ---
    public static bool IsTimeFrozen { get; private set; } = false;
    public static float GlobalRechargeMultiplier { get; private set; } = 1f;
    
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
        CleanupGlobalState();
    }

    public override void InitialiseFromCodeMatrix()
    {
        SetLevelData(1, cooldown: 15f, duration: 3f, maxStacks: 1, stackRegenTime: 15f, specialValue1: 5.8f, specialValue2: 0.1f); 
        SetLevelData(2, cooldown: 12f, duration: 4f, maxStacks: 1, stackRegenTime: 12f, specialValue1: 6.0f, specialValue2: 0.3f);
        SetLevelData(3, cooldown: 10f, duration: 5f, maxStacks: 1, stackRegenTime: 10f, specialValue1: 6.2f, specialValue2: 0.5f);
        SetLevelData(4, cooldown: 8f, duration: 6f, maxStacks: 2, stackRegenTime: 8f,  specialValue1: 6.4f, specialValue2: 0.7f);
        SetLevelData(5, cooldown: 6f, duration: 7f, maxStacks: 3, stackRegenTime: 10f, specialValue1: 6.6f, specialValue2: 0.8f); 
    }

    public override bool CanActivate(Player player)
    {
        if (IsTimeFrozen) return false;
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
        IsTimeFrozen = true;
        
        float recoveryFactor = GetCurrentLevelData().specialValue2; 
        GlobalRechargeMultiplier = Mathf.Lerp(timeScaleIntensity, 1f, recoveryFactor);

        float initialFixedDeltaTime = 0.02f;
        if (Time.fixedDeltaTime > 0) initialFixedDeltaTime = Time.fixedDeltaTime;

        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        AnimatorUpdateMode originalUpdateMode = AnimatorUpdateMode.Normal;

        if (playerAnimator != null)
        {
            originalUpdateMode = playerAnimator.updateMode;
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        // --- AUDIO START ---
        if (AudioManager.Instance != null)
        {
            // Passing startVolume. Ensure your AudioManager has an overload for this!
            if (freezeStartSound != null) AudioManager.Instance.PlaySFX(freezeStartSound, startVolume);
            
            if (freezeLoopSound != null) 
            {
                AudioManager.Instance.PlayLoop(freezeLoopSound, 0f);
                AudioManager.Instance.SetLoopPitch(1f);
            }
        }

        GameObject activeFilter = null;
        if (screenFilterPrefab != null)
            activeFilter = Instantiate(screenFilterPrefab, Vector3.zero, Quaternion.identity);

        Camera cam = Camera.main;
        float startCamSize = 5.4f;
        if (cam != null) startCamSize = cam.orthographicSize;

        float targetCamSize = GetCurrentLevelData().specialValue1;
        if (targetCamSize > maxCameraSizeCap) targetCamSize = maxCameraSizeCap;
        if (targetCamSize <= 0.1f) targetCamSize = startCamSize; 

        // --- PHASE 1: ENTRY ---
        float timer = 0f;
        while (timer < entryDuration)
        {
            if (player == null) { Cleanup(initialFixedDeltaTime, activeFilter, null, originalUpdateMode); yield break; }
            if (Time.timeScale == 0f) { yield return null; continue; }

            timer += Time.unscaledDeltaTime; 
            float progress = Mathf.Clamp01(timer / entryDuration);

            float currentScale = Mathf.Lerp(1f, timeScaleIntensity, progress);
            if (Time.timeScale > 0) SetTimeScale(currentScale, initialFixedDeltaTime);

            if (cam != null && modifyCamera)
                cam.orthographicSize = Mathf.Lerp(startCamSize, targetCamSize, progress);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetLoopVolume(progress * loopVolumeScale); 
            }

            yield return null;
        }

        if (Time.timeScale > 0) SetTimeScale(timeScaleIntensity, initialFixedDeltaTime);
        if (cam != null && modifyCamera) cam.orthographicSize = targetCamSize;
        
        // --- PHASE 2: HOLD ---
        float holdDuration = StackedDuration - entryDuration - exitDuration;
        float holdTimer = 0f;
        
        while (holdTimer < holdDuration)
        {
            if (player == null) { Cleanup(initialFixedDeltaTime, activeFilter, null, originalUpdateMode); yield break; }
            if (Time.timeScale == 0f) { yield return null; continue; }

            holdTimer += Time.unscaledDeltaTime;
            
            if (Time.timeScale != 0f && Time.timeScale != timeScaleIntensity)
                SetTimeScale(timeScaleIntensity, initialFixedDeltaTime);

            if (AudioManager.Instance != null) 
            {
                AudioManager.Instance.SetLoopVolume(loopVolumeScale);
            }

            yield return null;
        }

        // --- PHASE 3: EXIT ---
        if (AudioManager.Instance != null)
        {
            // Passing endVolume
            if (freezeEndSound != null) AudioManager.Instance.PlaySFX(freezeEndSound, endVolume);
        }
        else if (freezeEndSound != null && player != null) 
        {
            // PlayClipAtPoint natively supports volume as the 3rd argument
            AudioSource.PlayClipAtPoint(freezeEndSound, player.transform.position, endVolume);
        }

        timer = 0f;
        while (timer < exitDuration)
        {
            if (player == null) { Cleanup(initialFixedDeltaTime, activeFilter, null, originalUpdateMode); yield break; }
            if (Time.timeScale == 0f) { yield return null; continue; }

            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / exitDuration);

            float currentScale = Mathf.Lerp(timeScaleIntensity, 1f, progress);
            if (Time.timeScale > 0) SetTimeScale(currentScale, initialFixedDeltaTime);

            if (cam != null && modifyCamera)
                cam.orthographicSize = Mathf.Lerp(targetCamSize, startCamSize, progress);

            if (AudioManager.Instance != null)
            {
                float currentVol = (1f - progress) * loopVolumeScale;
                AudioManager.Instance.SetLoopVolume(currentVol);
                
                float currentPitch = Mathf.Lerp(1f, exitPitchTarget, progress);
                AudioManager.Instance.SetLoopPitch(currentPitch);
            }

            yield return null;
        }

        Cleanup(initialFixedDeltaTime, activeFilter, playerAnimator, originalUpdateMode);
        if (cam != null && modifyCamera) cam.orthographicSize = startCamSize;
    }

    private void SetTimeScale(float scale, float baseFixedDelta)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixedDelta * scale;
    }

    private void Cleanup(float resetFixedDeltaTime, GameObject filter, Animator anim, AnimatorUpdateMode originalMode)
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = resetFixedDeltaTime;
        }

        if (filter != null) Destroy(filter);
        if (anim != null) anim.updateMode = originalMode;
        
        if (AudioManager.Instance != null) 
        {
            AudioManager.Instance.StopLoop(fadeOut: false);
            AudioManager.Instance.SetLoopPitch(1f);
        }
        
        CleanupGlobalState();
    }
    
    private void CleanupGlobalState()
    {
        IsTimeFrozen = false;
        GlobalRechargeMultiplier = 1f;
    }
}