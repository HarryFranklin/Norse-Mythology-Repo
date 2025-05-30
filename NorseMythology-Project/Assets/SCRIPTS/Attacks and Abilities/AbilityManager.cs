using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public Player player;
    public PlayerMovement playerMovement;
    public Camera playerCamera;
    
    [Header("Equipped Abilities")]
    public Ability[] equippedAbilities = new Ability[4];
    
    [Header("Targeting UI")]
    public LineRenderer targetingLine;
    
    [Header("Dashed Line Settings")]
    public Material solidLineMaterial;   // Use a solid white material instead of textured
    public float lineWidth = 0.05f;
    public float dashLength = 0.1f;      // Length of each dash segment
    public float gapLength = 0.1f;       // Length of each gap between dashes
    
    private float[] lastAbilityUse = new float[4];
    private bool isInTargetingMode = false;
    private int targetingAbilityIndex = -1;
    private Ability currentTargetingAbility;
    
    // Store original cursor for restoration
    private Texture2D originalCursor;
    private Vector2 originalHotspot;
    
    // Cache for efficiency
    private MaterialPropertyBlock materialPropertyBlock;
    private static readonly int MainTexScaleID = Shader.PropertyToID("_MainTex_ST");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Start()
    {
        // Store original cursor
        originalCursor = null;
        originalHotspot = Vector2.zero;
        
        // Initialize material property block for efficient material updates
        materialPropertyBlock = new MaterialPropertyBlock();
        
        // Set up targeting line
        if (targetingLine != null)
        {
            SetupTargetingLine();
        }
        else
        {
            CreateTargetingLineRenderer();
        }
    }
    
    private void SetupTargetingLine()
    {
        targetingLine.enabled = false;
        targetingLine.useWorldSpace = true;
        targetingLine.startWidth = lineWidth;
        targetingLine.endWidth = lineWidth;
        
        // Use solid material - we'll create dashes by controlling segments
        if (solidLineMaterial != null)
        {
            targetingLine.material = solidLineMaterial;
        }
        else
        {
            // Create a simple unlit material
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.white;
            targetingLine.material = mat;
        }
    }
    
    private void CreateTargetingLineRenderer()
    {
        GameObject lineObject = new GameObject("TargetingLine");
        lineObject.transform.SetParent(transform);
        
        targetingLine = lineObject.AddComponent<LineRenderer>();
        SetupTargetingLine();
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
        
        // Update dashed line
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
        if (targetingLine == null) return;
        
        float totalDistance = Vector3.Distance(startPos, endPos);
        
        if (totalDistance < 0.01f)
        {
            HideDashedLine();
            return;
        }
        
        // Calculate direction
        Vector3 direction = (endPos - startPos).normalized;
        
        // Calculate dash pattern
        float cycleLength = dashLength + gapLength;
        int totalCycles = Mathf.FloorToInt(totalDistance / cycleLength);
        
        // Create list of dash segments
        var dashPositions = new System.Collections.Generic.List<Vector3>();
        
        float currentDistance = 0f;
        
        // Add dash segments
        for (int i = 0; i <= totalCycles; i++)
        {
            float dashStart = i * cycleLength;
            float dashEnd = dashStart + dashLength;
            
            // Clamp to total distance
            if (dashStart >= totalDistance) break;
            if (dashEnd > totalDistance) dashEnd = totalDistance;
            
            // Add start and end points for this dash
            Vector3 segmentStart = startPos + direction * dashStart;
            Vector3 segmentEnd = startPos + direction * dashEnd;
            
            dashPositions.Add(segmentStart);
            dashPositions.Add(segmentEnd);
        }
        
        // If we have no dashes, hide the line
        if (dashPositions.Count == 0)
        {
            HideDashedLine();
            return;
        }
        
        // Set up LineRenderer with all dash segments
        targetingLine.enabled = true;
        targetingLine.positionCount = dashPositions.Count;
        
        for (int i = 0; i < dashPositions.Count; i++)
        {
            targetingLine.SetPosition(i, dashPositions[i]);
        }
        
        // Set color
        targetingLine.material.color = lineColor;
    }
    
    private void HideDashedLine()
    {
        if (targetingLine != null)
        {
            targetingLine.enabled = false;
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
            ability.Activate(player, playerMovement);
            lastAbilityUse[index] = Time.time;
            Debug.Log($"{ability.abilityName} activated instantly!");
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
        
        Cursor.SetCursor(originalCursor, originalHotspot, CursorMode.Auto);
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