using UnityEngine;
using System;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerMovement playerMovement;
    public Camera playerCamera;
    
    [Header("Equipped Abilities")]
    public Ability[] equippedAbilities = new Ability[4];

    [Header("UI Visuals")]
    [SerializeField] private Color normalCooldownColour = new Color(0, 0, 0, 1f);
    [SerializeField] private Color timeFrozenCooldownColour = new Color(1, 0.8f, 0, 1f);

    [Header("Targeting Settings")]
    private bool isInTargetingMode = false;
    private int targetingAbilityIndex = -1;
    private Ability currentTargetingAbility;

    [Header("Input Settings")]
    [Tooltip("If true, pressing another ability key while targeting will switch to that ability. If false, it will just cancel the current targeting.")]
    public bool enableAbilitySwapping = true;
    
    // Store original cursor for restoration
    private Texture2D originalCursor;
    private Vector2 originalHotspot;

    // --- Events ---
    public event Action<int> OnAbilityUsed;
    public event Action<int> OnAbilityTargetingStarted;
    public event Action<int> OnAbilityTargetingEnded;

    private void Awake()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] != null)
            {
                equippedAbilities[i] = Instantiate(equippedAbilities[i]);
            }
        }
    }

    private void Start()
    {
        // Store original cursor
        originalCursor = null;
        originalHotspot = Vector2.zero;
    }

    private void Update()
    {
        UpdateAllAbilityStacks();
        
        if (player != null && !player.isDead)
        {
            if (isInTargetingMode)
            {
                HandleTargetingMode();
            }
            else
            {
                HandleAbilityInput();
            }
        }
    }

    // Update stack regeneration for all equipped abilities
    private void UpdateAllAbilityStacks()
    {
        // 1. Calculate the Delta Time we should use for recharging
        float rechargeDelta = Time.unscaledDeltaTime; 

        // 2. If Time is Frozen, apply the multiplier we calculated in FreezeTimeAbility
        if (FreezeTimeAbility.IsTimeFrozen)
        {
            rechargeDelta *= FreezeTimeAbility.GlobalRechargeMultiplier;
        }
        else
        {
            // If not frozen, match Standard Game Time or Unscaled Time 
            if (Time.timeScale == 0) rechargeDelta = 0; // Don't recharge if paused
            else rechargeDelta = Time.deltaTime; 
        }

        // 3. Update all abilities with this modified time
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] != null)
            {
                equippedAbilities[i].UpdateCooldownLogic(rechargeDelta);
            }
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

    // --- UPDATED LOGIC HERE ---
    private void HandleTargetingMode()
    {
        UpdateTargetingVisuals();

        // 1. Check for Ability Keys (1-4)
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (i == targetingAbilityIndex)
                {
                    // Scenario: Player pressed the SAME key again (e.g. 1 while holding 1)
                    // Result: Toggle Off (Cancel)
                    CancelTargeting();
                }
                else
                {
                    // Scenario: Player pressed a DIFFERENT key (e.g. 2 while holding 1)
                    if (enableAbilitySwapping)
                    {
                        // Option A: SWAP
                        // Cancel current, then immediately pick up the new one
                        CancelTargeting();
                        TryActivateAbility(i);
                    }
                    else
                    {
                        // Option B: CANCEL ONLY
                        // Just drop the current ability.
                        CancelTargeting();
                    }
                }
                return; // Input handled, stop processing this frame
            }
        }

        // 2. Execute Command (Left Click)
        if (Input.GetMouseButtonDown(0))
        {
            ExecuteTargetedAbility();
        }

        // 3. Cancel Command (Right Click ONLY - Removed Escape)
        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
        }
    }

    private void UpdateTargetingVisuals()
    {
        if (currentTargetingAbility == null) return;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 playerPos = player.transform.position;
        Vector3 direction = (mouseWorldPos - playerPos);
        if (currentTargetingAbility.maxTargetingRange > 0)
        {
            if (direction.magnitude > currentTargetingAbility.maxTargetingRange)
            {
                direction = direction.normalized * currentTargetingAbility.maxTargetingRange;
                mouseWorldPos = playerPos + direction;
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

        if (player == null || player.currentStats == null)
        {
            Debug.LogWarning("AbilityManager: Player or currentStats is null");
            return;
        }

        Ability ability = equippedAbilities[index];

        // Check if ability can activate first inc. stack checking
        if (!ability.CanActivate(player))
        {
            Debug.Log($"Cannot activate {ability.abilityName} - Stacks: {ability.CurrentStacks}/{ability.MaxStacksAtCurrentLevel}");
            return;
        }
        
        // Handle different activation modes
        if (ability.activationMode == ActivationMode.Instant)
        {
            ability.Activate(player, playerMovement);
            Debug.Log($"{ability.abilityName} activated instantly! (Stack {ability.AbilityStacks}) - Remaining charges: {ability.CurrentStacks}/{ability.MaxStacksAtCurrentLevel}");
            
            // Notify listeners (UI) that ability was used
            OnAbilityUsed?.Invoke(index);
        }
        else if (ability.activationMode == ActivationMode.ClickToTarget)
        {
            EnterTargetingMode(ability, index);
        }
    }

    private void EnterTargetingMode(Ability ability, int abilityIndex)
    {
        isInTargetingMode = true;
        targetingAbilityIndex = abilityIndex;
        currentTargetingAbility = ability;
        
        // Change cursor if custom cursor is specified
        if (ability.targetingCursor != null)
        {
            Texture2D cursorTexture = ability.targetingCursor.texture;
            Vector2 hotspot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        }
        
        ability.EnterTargetingMode(player);
        
        // Notify listeners (UI) that targeting started
        OnAbilityTargetingStarted?.Invoke(abilityIndex);
        
        Debug.Log($"Entered targeting mode for {ability.abilityName} (Stack {ability.AbilityStacks}). Click to target or press {abilityIndex + 1} again to cancel!");
    }

    private void ExecuteTargetedAbility()
    {
        if (currentTargetingAbility == null) return;
        
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 playerPos = player.transform.position;
        Vector2 targetDirection = ((Vector2)(mouseWorldPos - playerPos)).normalized;
        
        // Clamp to max range if specified
        Vector2 clampedWorldPos = mouseWorldPos;
        if (currentTargetingAbility.maxTargetingRange > 0)
        {
            Vector2 direction = mouseWorldPos - playerPos;
            if (direction.magnitude > currentTargetingAbility.maxTargetingRange)
            {
                clampedWorldPos = playerPos + (Vector3)(direction.normalized * currentTargetingAbility.maxTargetingRange);
            }
        }
        
        currentTargetingAbility.ActivateWithTarget(player, playerMovement, targetDirection, clampedWorldPos);
        
        Debug.Log($"{currentTargetingAbility.abilityName} (Stack {currentTargetingAbility.AbilityStacks}) executed with target direction: {targetDirection}");
        
        // Notify listeners (UI) that ability was successfully used
        OnAbilityUsed?.Invoke(targetingAbilityIndex);
        
        ExitTargetingMode();
    }

    private void CancelTargeting()
    {
        Debug.Log($"Targeting cancelled for {currentTargetingAbility?.abilityName}");
        ExitTargetingMode();
    }

    private void ExitTargetingMode()
    {
        if (currentTargetingAbility != null)
        {
            currentTargetingAbility.ExitTargetingMode(player);
        }
        
        Cursor.SetCursor(originalCursor, originalHotspot, CursorMode.Auto);
        
        int endedIndex = targetingAbilityIndex;

        isInTargetingMode = false;
        targetingAbilityIndex = -1;
        currentTargetingAbility = null;
        
        // Notify listeners (UI) that targeting ended
        if (endedIndex != -1)
        {
            OnAbilityTargetingEnded?.Invoke(endedIndex);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = playerCamera.nearClipPlane;
        return playerCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    public void EquipAbility(Ability ability, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < equippedAbilities.Length)
        {
            equippedAbilities[slotIndex] = ability;
            Debug.Log($"{ability.abilityName} (Stack {ability.AbilityStacks}) equipped to slot {slotIndex + 1}");
        }
    }

    public float GetAbilityCooldownRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedAbilities.Length || equippedAbilities[slotIndex] == null)
            return 0f;

        if (player == null || player.currentStats == null)
            return 0f;
            
        return equippedAbilities[slotIndex].GetStackCooldownRemaining();
    }
    
    public bool IsInTargetingMode()
    {
        return isInTargetingMode;
    }

    public void AddAbilityStack(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < equippedAbilities.Length && equippedAbilities[slotIndex] != null)
        {
            equippedAbilities[slotIndex].AddAbilityStack();
        }
    }

    public string GetAbilityInfo(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedAbilities.Length || equippedAbilities[slotIndex] == null)
            return "Empty Slot";

        Ability ability = equippedAbilities[slotIndex];
        return $"{ability.abilityName} L{ability.CurrentLevel} (x{ability.AbilityStacks})\n" +
               $"Cooldown: {ability.StackedMaxCooldown:F1}s\n" +
               $"Charges: {ability.CurrentStacks}/{ability.MaxStacksAtCurrentLevel}";
    }

    public Color GetCurrentCooldownColor()
    {
        if (FreezeTimeAbility.IsTimeFrozen)
        {
            return timeFrozenCooldownColour;
        }
        return normalCooldownColour;
    }
}