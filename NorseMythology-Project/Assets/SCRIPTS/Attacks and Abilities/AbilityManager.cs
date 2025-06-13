using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerMovement playerMovement;
    public Camera playerCamera;
    
    [Header("Equipped Abilities")]
    public Ability[] equippedAbilities = new Ability[4];
    private float[] lastAbilityUse = new float[4];

    [Header("Targeting Settings")]
    private bool isInTargetingMode = false;
    private int targetingAbilityIndex = -1;
    private Ability currentTargetingAbility;
    
    // Store original cursor for restoration
    private Texture2D originalCursor;
    private Vector2 originalHotspot;

    private void Start()
    {
        // Store original cursor
        originalCursor = null;
        originalHotspot = Vector2.zero;
    }

    private void Update()
    {
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

    private void HandleTargetingMode()
    {
        UpdateTargetingVisuals();
        
        if (Input.GetKeyDown(KeyCode.Alpha1 + targetingAbilityIndex))
        {
            CancelTargeting();
            return;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            ExecuteTargetedAbility();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) // Right click or escape
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
        
        // Clamp to max range if specified
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

        // Check cooldown using stacked cooldown
        float cooldownReduction = 1f - (player.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = ability.StackedCooldown * cooldownReduction;

        if (Time.time - lastAbilityUse[index] < adjustedCooldown)
        {
            float timeLeft = adjustedCooldown - (Time.time - lastAbilityUse[index]);
            Debug.Log($"{ability.abilityName} on cooldown: {timeLeft:F1}s remaining (Stacked cooldown: {ability.StackedCooldown:F1}s)");
            return;
        }

        if (!ability.CanActivate(player))
        {
            Debug.Log($"Cannot activate {ability.abilityName}");
            return;
        }

        // Handle different activation modes
        if (ability.activationMode == ActivationMode.Instant)
        {
            ability.Activate(player, playerMovement);
            lastAbilityUse[index] = Time.time;
            Debug.Log($"{ability.abilityName} activated instantly! (Stack {ability.AbilityStacks})");
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
        lastAbilityUse[targetingAbilityIndex] = Time.time;
        
        Debug.Log($"{currentTargetingAbility.abilityName} (Stack {currentTargetingAbility.AbilityStacks}) executed with target direction: {targetDirection}");
        
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
        
        isInTargetingMode = false;
        targetingAbilityIndex = -1;
        currentTargetingAbility = null;
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

        float cooldownReduction = 1f - (player.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = equippedAbilities[slotIndex].StackedCooldown * cooldownReduction;

        return Mathf.Max(0f, adjustedCooldown - (Time.time - lastAbilityUse[slotIndex]));
    }
    
    public bool IsInTargetingMode()
    {
        return isInTargetingMode;
    }

    // Method to add ability stack to equipped ability
    public void AddAbilityStack(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < equippedAbilities.Length && equippedAbilities[slotIndex] != null)
        {
            equippedAbilities[slotIndex].AddAbilityStack();
        }
    }

    // Method to get ability info including stacking
    public string GetAbilityInfo(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedAbilities.Length || equippedAbilities[slotIndex] == null)
            return "Empty Slot";

        Ability ability = equippedAbilities[slotIndex];
        return $"{ability.abilityName} L{ability.CurrentLevel} (x{ability.AbilityStacks})\n" +
               $"Cooldown: {ability.StackedCooldown:F1}s\n" +
               $"Charges: {ability.CurrentStacks}/{ability.MaxStacksAtCurrentLevel}";
    }
}