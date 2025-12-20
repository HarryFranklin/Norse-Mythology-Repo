using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "FreezeTimeAbility", menuName = "Abilities/FreezeTime")]
public class FreezeTimeAbility : Ability
{
    [Header("Time Settings")]
    [Tooltip("Target time scale (0.05 = 5% speed).")]
    [SerializeField, Range(0f, 1f)] private float timeScaleIntensity = 0.05f;
    [SerializeField] private float entryDuration = 0.1f; // Fast entry
    [SerializeField] private float exitDuration = 0.5f;  // Slower exit

    [Header("Camera Settings")]
    [Tooltip("If true, the camera size will change to the value defined in SpecialValue1 per level.")]
    [SerializeField] private bool modifyCamera = true;
    [SerializeField] private float maxCameraSizeCap = 6.75f; 

    [Header("Audio")]
    [SerializeField] private AudioClip freezeStartSound;
    [SerializeField] private AudioClip freezeEndSound;
    
    [Header("Visuals")]
    [SerializeField] private GameObject screenFilterPrefab;

    private static bool isTimeFrozen = false;
    private float defaultFixedDeltaTime;

    private void Awake()
    {
        abilityName = "Verðandi Lock";
        description = "Drastically slows down time for all enemies and projectiles while you move freely.";
        activationMode = ActivationMode.Instant;
        useCodeDefinedMatrix = true;
        
        InitialiseFromCodeMatrix(); 
    }
    
    private void OnEnable()
    {
        isTimeFrozen = false;
    }

    protected override void InitialiseFromCodeMatrix()
    {
        // Level 1: Size 5.8
        SetLevelData(1, cooldown: 15f, duration: 3f, maxStacks: 1, stackRegenTime: 15f, specialValue1: 5.8f);
        SetLevelData(2, cooldown: 12f, duration: 4f, maxStacks: 1, stackRegenTime: 12f, specialValue1: 6.0f);
        SetLevelData(3, cooldown: 10f, duration: 5f, maxStacks: 1, stackRegenTime: 10f, specialValue1: 6.2f);
        SetLevelData(4, cooldown: 8f, duration: 6f, maxStacks: 2, stackRegenTime: 8f, specialValue1: 6.4f);
        SetLevelData(5, cooldown: 6f, duration: 7f, maxStacks: 3, stackRegenTime: 10f, specialValue1: 6.6f);

        Debug.Log($"FreezeTimeAbility initialised. Level 1 Target Size: {GetStatsForLevel(1).specialValue1}");
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
        Debug.Log($"Time Freeze Activated! Duration: {StackedDuration}s");
    }

    private IEnumerator ExecuteSmoothTimeFreeze(Player player)
    {
        isTimeFrozen = true;
        defaultFixedDeltaTime = 0.02f;

        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        AnimatorUpdateMode originalUpdateMode = AnimatorUpdateMode.Normal;

        if (playerAnimator != null)
        {
            originalUpdateMode = playerAnimator.updateMode;
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        // Setup Audio/Visuals
        if (freezeStartSound != null) 
            AudioSource.PlayClipAtPoint(freezeStartSound, player.transform.position);

        GameObject activeFilter = null;
        if (screenFilterPrefab != null)
            activeFilter = Instantiate(screenFilterPrefab, Vector3.zero, Quaternion.identity);

        // --- CAMERA SETUP ---
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
            timer += Time.unscaledDeltaTime; 
            float progress = Mathf.Clamp01(timer / entryDuration);

            Time.timeScale = Mathf.Lerp(1f, timeScaleIntensity, progress);
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

            if (cam != null && modifyCamera)
                cam.orthographicSize = Mathf.Lerp(startCamSize, targetCamSize, progress);

            yield return null;
        }

        Time.timeScale = timeScaleIntensity;
        Time.fixedDeltaTime = defaultFixedDeltaTime * timeScaleIntensity;
        if (cam != null && modifyCamera) cam.orthographicSize = targetCamSize;


        // --- PHASE 2: HOLD ---
        float holdDuration = StackedDuration - entryDuration - exitDuration;
        if (holdDuration > 0)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }


        // --- PHASE 3: SLOW EXIT ---
        if (freezeEndSound != null) 
            AudioSource.PlayClipAtPoint(freezeEndSound, player.transform.position);

        timer = 0f;
        while (timer < exitDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / exitDuration);

            Time.timeScale = Mathf.Lerp(timeScaleIntensity, 1f, progress);
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

            if (cam != null && modifyCamera)
                cam.orthographicSize = Mathf.Lerp(targetCamSize, startCamSize, progress);

            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        if (cam != null && modifyCamera) cam.orthographicSize = startCamSize;
        
        // Restore Animator to normal
        if (playerAnimator != null)
        {
            playerAnimator.updateMode = originalUpdateMode;
        }

        if (activeFilter != null) 
            Destroy(activeFilter);

        isTimeFrozen = false;
        Debug.Log("Time Freeze Ended.");
    }
}