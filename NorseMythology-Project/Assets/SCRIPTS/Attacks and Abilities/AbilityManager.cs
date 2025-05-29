using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerMovement playerMovement;
    public Camera playerCamera;  // Reference to the camera for world position calculations
    
    [Header("Equipped Abilities")]
    public Ability[] equippedAbilities = new Ability[4];
    
    [Header("Targeting UI")]
    public LineRenderer targetingLine;  // Optional line renderer for showing targeting line
    
    [Header("Dashed Line Settings")]
    public float dashLength = 0.2f;      // Length of each dash
    public float gapLength = 0.2f;       // Length of each gap
    public float lineWidth = 0.05f;      // Width of the line
    
    private float[] lastAbilityUse = new float[4];
    private bool isInTargetingMode = false;
    private int targetingAbilityIndex = -1;
    private Ability currentTargetingAbility;
    
    // Store original cursor for restoration
    private Texture2D originalCursor;
    private Vector2 originalHotspot;
    
    // For dashed line rendering
    private LineRenderer[] dashLineRenderers;
    private int maxDashCount = 50; // Maximum number of dash segments we might need

    private void Start()
    {
        // Store original cursor
        // Note: Unity doesn't provide a way to get current cursor, so we assume default
        originalCursor = null;  // null means default system cursor
        originalHotspot = Vector2.zero;
        
        // Set up targeting line if available
        if (targetingLine != null)
        {
            targetingLine.enabled = false;
        }
        
        // Initialise dash line renderers
        InitialiseDashLineRenderers();
    }
    
    private void InitialiseDashLineRenderers()
    {
        // Create a parent object to hold all dash line renderers
        GameObject dashParent = new GameObject("DashLineRenderers");
        dashParent.transform.SetParent(transform);
        
        dashLineRenderers = new LineRenderer[maxDashCount];
        
        for (int i = 0; i < maxDashCount; i++)
        {
            GameObject dashObject = new GameObject($"Dash_{i}");
            dashObject.transform.SetParent(dashParent.transform);
            
            LineRenderer lr = dashObject.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.enabled = false;
            
            // Use a simple material - you can assign a material in the inspector if needed
            // or create a default material
            if (lr.material == null)
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            dashLineRenderers[i] = lr;
        }
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
        // Update targeting visuals
        UpdateTargetingVisuals();
        
        // Check if the same ability key is pressed again to cancel targeting
        if (Input.GetKeyDown(KeyCode.Alpha1 + targetingAbilityIndex))
        {
            CancelTargeting();
            return;
        }
        
        // Check for mouse click to execute ability
        if (Input.GetMouseButtonDown(0))  // Left click
        {
            ExecuteTargetedAbility();
        }
        
        // Check for escape or right click to cancel
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
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
        
        // Update dashed targeting line
        if (currentTargetingAbility.showTargetingLine)
        {
            UpdateDashedLine(playerPos, mouseWorldPos, currentTargetingAbility.targetingLineColor);
        }
        else
        {
            HideDashedLine();
        }
    }
    
    private void UpdateDashedLine(Vector3 startPos, Vector3 endPos, Color lineColor)
    {
        Vector3 direction = endPos - startPos;
        float totalDistance = direction.magnitude;
        Vector3 normalizedDirection = direction.normalized;
        
        // Calculate how many dashes we can fit
        float dashAndGapLength = dashLength + gapLength;
        int dashCount = Mathf.FloorToInt(totalDistance / dashAndGapLength);
        
        // Hide all dash renderers first
        HideDashedLine();
        
        // Create dashes
        for (int i = 0; i < dashCount && i < maxDashCount; i++)
        {
            float dashStartDistance = i * dashAndGapLength;
            float dashEndDistance = dashStartDistance + dashLength;
            
            // Make sure we don't exceed the total distance
            if (dashEndDistance > totalDistance)
            {
                dashEndDistance = totalDistance;
            }
            
            Vector3 dashStart = startPos + normalizedDirection * dashStartDistance;
            Vector3 dashEnd = startPos + normalizedDirection * dashEndDistance;
            
            LineRenderer lr = dashLineRenderers[i];
            lr.enabled = true;
            lr.material.color = lineColor;
            lr.SetPosition(0, dashStart);
            lr.SetPosition(1, dashEnd);
        }
    }
    
    private void HideDashedLine()
    {
        if (dashLineRenderers != null)
        {
            for (int i = 0; i < dashLineRenderers.Length; i++)
            {
                dashLineRenderers[i].enabled = false;
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

        // Check cooldown
        float cooldownReduction = 1f - (player.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = ability.cooldown * cooldownReduction;

        if (Time.time - lastAbilityUse[index] < adjustedCooldown)
        {
            float timeLeft = adjustedCooldown - (Time.time - lastAbilityUse[index]);
            Debug.Log($"{ability.abilityName} on cooldown: {timeLeft:F1}s remaining");
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
            // Instant activation
            ability.Activate(player, playerMovement);
            lastAbilityUse[index] = Time.time;
            Debug.Log($"{ability.abilityName} activated instantly!");
        }
        else if (ability.activationMode == ActivationMode.ClickToTarget)
        {
            // Enter targeting mode
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
        
        // Call ability's enter targeting mode method
        ability.EnterTargetingMode(player);
        
        Debug.Log($"Entered targeting mode for {ability.abilityName}. Click to target or press {abilityIndex + 1} again to cancel!");
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
        
        // Execute the ability
        currentTargetingAbility.ActivateWithTarget(player, playerMovement, targetDirection, clampedWorldPos);
        lastAbilityUse[targetingAbilityIndex] = Time.time;
        
        Debug.Log($"{currentTargetingAbility.abilityName} executed with target direction: {targetDirection}");
        
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
        
        // Restore original cursor
        Cursor.SetCursor(originalCursor, originalHotspot, CursorMode.Auto);
        
        // Hide dashed targeting line
        HideDashedLine();
        
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
            Debug.Log($"{ability.abilityName} equipped to slot {slotIndex + 1}");
        }
    }

    public float GetAbilityCooldownRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedAbilities.Length || equippedAbilities[slotIndex] == null)
            return 0f;

        if (player == null || player.currentStats == null)
            return 0f;

        float cooldownReduction = 1f - (player.currentStats.abilityCooldownReduction / 100f);
        float adjustedCooldown = equippedAbilities[slotIndex].cooldown * cooldownReduction;

        return Mathf.Max(0f, adjustedCooldown - (Time.time - lastAbilityUse[slotIndex]));
    }
    
    public bool IsInTargetingMode()
    {
        return isInTargetingMode;
    }
}