using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class LevelDistribution
{
    [Header("Level Distribution Ratios")]
    [Range(0f, 1f)]
    [Tooltip("Proportion of enemies that are 1 level below target level")]
    public float belowLevelRatio = 0.2f;
    
    [Range(0f, 1f)]
    [Tooltip("Proportion of enemies that are at target level")]
    public float targetLevelRatio = 0.6f;
    
    [Range(0f, 1f)]
    [Tooltip("Proportion of enemies that are 1 level above target level")]
    public float aboveLevelRatio = 0.2f;
    
    [Header("Auto-Normalise")]
    [Tooltip("Automatically normalise ratios to sum to 1.0")]
    public bool autoNormalise = true;
    
    public void NormaliseRatios()
    {
        float total = belowLevelRatio + targetLevelRatio + aboveLevelRatio;
        if (total > 0f)
        {
            belowLevelRatio /= total;
            targetLevelRatio /= total;
            aboveLevelRatio /= total;
        }
        else
        {
            // Default values if all are zero
            belowLevelRatio = 0.2f;
            targetLevelRatio = 0.6f;
            aboveLevelRatio = 0.2f;
        }
    }
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning and Prefabs")]
    public Transform enemiesParent; // Parent object to store all spawned enemies

    [SerializeField] private List<GameObject> meleeEnemyPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> projectileEnemyPrefabs = new List<GameObject>();

    [Header("Fallback Prefabs")]
    public GameObject meleePrefab;
    public GameObject projectilePrefab;

    [Range(0f, 1f)]
    public float meleeRatio = 0.6f;
    
    [Header("Spawn Rate Settings")]
    public float baseSpawnRate = 1f; // Base spawn rate (enemies per second) at level 1
    [Range(0f, 0.2f)]
    [Tooltip("Spawn rate reduction per player level (e.g., 0.05 = 5% faster per level)")]
    public float playerLevelSpawnReduction = 0.05f; // 5% faster per player level
    [Range(0f, 0.2f)]
    [Tooltip("Spawn rate reduction per game level (e.g., 0.10 = 10% faster per level)")]
    public float gameLevelSpawnReduction = 0.10f; // 10% faster per game level
    
    [Header("Enemy Level Settings")]
    [SerializeField] private int targetEnemyLevel = 1;
    [SerializeField] private LevelDistribution levelDistribution = new LevelDistribution();
    
    [Header("Spawn Distance")]
    public float minSpawnRadius = 12f; // Just to add some variation
    public float maxSpawnRadius = 18f;
    
    [Header("Enemy Count Scaling")]
    public int baseMaxEnemies = 15; // Base max enemies for wave 1
    public int maxEnemiesIncrement = 3; // How many more enemies per wave
    public int absoluteMaxEnemies = 50; // Hard cap to prevent performance issues
    
    public Transform player;

    [Header("Grid Detection")]
    public LayerMask gridLayerMask = -1; // Which layers to consider as grid objects
    
    private int currentEnemyCount = 0;
    private int currentMaxEnemies = 15;
    private bool spawningActive = false;
    private Coroutine spawnCoroutine;
    
    // Wave modifiers
    private float currentHealthMultiplier = 1f;
    private float currentXPMultiplier = 1f;
    
    // Fixed spawn wave tracking
    private int totalEnemiesSpawned = 0;
    private int maxEnemiesToSpawn = 0;
    private bool isFixedSpawnWave = false;
    
    // Camera bounds for off-screen detection
    private Camera mainCamera;
    private Bounds currentGridBounds;
    
    // Current calculated spawn rate
    private float currentSpawnRate = 1f;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>().transform;
            
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        // Ensure we have an enemies parent object
        SetupEnemiesParent();
        
        // Normalise level distribution ratios if auto-normalise is enabled
        if (levelDistribution.autoNormalise)
        {
            levelDistribution.NormaliseRatios();
        }
        
        // Set initial max enemies and spawn rate
        currentMaxEnemies = baseMaxEnemies;
        UpdateSpawnRate();
    }
    
    private void SetupEnemiesParent()
    {
        if (enemiesParent == null)
        {
            GameObject enemiesObject = GameObject.Find("Enemies");
            if (enemiesObject == null)
            {
                enemiesObject = new GameObject("Enemies");
                enemiesObject.transform.position = Vector3.zero;
            }
            enemiesParent = enemiesObject.transform;
        }
    }
    
    private void UpdateSpawnRate()
    {
        // Get player level from GameManager
        int playerLevel = 1;
        int gameLevel = 1;
        
        if (GameManager.Instance != null)
        {
            PlayerStats playerStats = GameManager.Instance.GetCurrentPlayerStats();
            if (playerStats != null)
            {
                playerLevel = playerStats.level;
            }
            
            // Get game level from WaveManager
            if (WaveManager.Instance != null)
            {
                gameLevel = WaveManager.Instance.GetCurrentWave();
            }
        }
        
        // Calculate spawn rate multiplier
        // Each player level reduces spawn time by playerLevelSpawnReduction (default 5%)
        // Each game level reduces spawn time by gameLevelSpawnReduction (default 10%)
        float playerLevelMultiplier = 1f + ((playerLevel - 1) * playerLevelSpawnReduction);
        float gameLevelMultiplier = 1f + ((gameLevel - 1) * gameLevelSpawnReduction);
        
        // Apply both multipliers to the base spawn rate
        currentSpawnRate = baseSpawnRate * playerLevelMultiplier * gameLevelMultiplier;
        
        Debug.Log($"Spawn rate updated: Player Level {playerLevel}, Game Level {gameLevel}, " +
                 $"Rate: {currentSpawnRate:F2} enemies/sec (interval: {1f/currentSpawnRate:F2}s)");
    }

    public void StartSpawning()
    {
        UpdateSpawnRate(); // Update spawn rate when starting
        spawningActive = true;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnEnemies());
    }
    
    public void StartFixedSpawning(int totalEnemies)
    {
        UpdateSpawnRate(); // Update spawn rate when starting
        isFixedSpawnWave = true;
        maxEnemiesToSpawn = totalEnemies;
        totalEnemiesSpawned = 0;
        
        spawningActive = true;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnEnemies());
        
        Debug.Log($"Starting fixed spawn wave: {totalEnemies} total enemies to spawn");
    }
    
    public void StopSpawning()
    {
        spawningActive = false;
        isFixedSpawnWave = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    
    public void SetWaveModifiers(float healthMultiplier, float xpMultiplier)
    {
        currentHealthMultiplier = healthMultiplier;
        currentXPMultiplier = xpMultiplier;
    }
    
    public void SetWaveNumber(int waveNumber)
    {
        // Calculate max enemies based on wave number
        currentMaxEnemies = Mathf.Min(
            baseMaxEnemies + ((waveNumber - 1) * maxEnemiesIncrement),
            absoluteMaxEnemies
        );
        
        // Update spawn rate when wave number changes
        UpdateSpawnRate();
        
        Debug.Log($"Wave {waveNumber}: Max simultaneous enemies set to {currentMaxEnemies}");
    }
    
    public void SetTargetEnemyLevel(int level)
    {
        targetEnemyLevel = Mathf.Max(1, level);
    }
    
    public int GetTargetEnemyLevel()
    {
        return targetEnemyLevel;
    }
    
    public int GetCurrentEnemyCount()
    {
        return currentEnemyCount;
    }
    
    public int GetCurrentMaxEnemies()
    {
        return currentMaxEnemies;
    }
    
    public float GetCurrentSpawnRate()
    {
        return currentSpawnRate;
    }
    
    public bool AreAllEnemiesSpawned()
    {
        return isFixedSpawnWave && totalEnemiesSpawned >= maxEnemiesToSpawn;
    }
    
    public bool AreAllEnemiesDefeated()
    {
        return isFixedSpawnWave && totalEnemiesSpawned >= maxEnemiesToSpawn && currentEnemyCount == 0;
    }

    private IEnumerator SpawnEnemies()
    {
        while (spawningActive)
        {
            // Use the dynamically calculated spawn rate
            yield return new WaitForSeconds(1f / currentSpawnRate);

            // Check if we should spawn more enemies
            bool shouldSpawn = false;
            
            if (isFixedSpawnWave)
            {
                // For fixed spawn waves, check if we haven't spawned all enemies yet
                // and we haven't reached the simultaneous enemy limit
                shouldSpawn = totalEnemiesSpawned < maxEnemiesToSpawn && 
                             currentEnemyCount < currentMaxEnemies;
            }
            else
            {
                // For continuous waves, just check the simultaneous enemy limit
                shouldSpawn = currentEnemyCount < currentMaxEnemies;
            }
            
            if (shouldSpawn)
            {
                SpawnEnemy();
            }
            else if (isFixedSpawnWave && totalEnemiesSpawned >= maxEnemiesToSpawn)
            {
                // Stop spawning if we've spawned all enemies for a fixed wave
                break;
            }
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        
        // If we can't find a valid spawn position, skip this spawn attempt
        if (spawnPos == Vector2.zero)
            return;

        GameObject prefabToSpawn = GetRandomEnemyPrefab();
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("No valid enemy prefab found!");
            return;
        }

        GameObject enemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, enemiesParent);

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.target = player;
            
            // Determine the level for this enemy
            int enemyLevel = DetermineEnemyLevel();
            
            // Set the enemy's level before applying wave scaling
            enemyScript.SetLevel(enemyLevel);
            
            // Apply wave scaling to the enemy's current stats
            float newMaxHealth = Mathf.Round(enemyScript.maxHealth * currentHealthMultiplier);
            float newXPValue = Mathf.Round(enemyScript.xpValue * currentXPMultiplier);
            
            enemyScript.maxHealth = newMaxHealth;
            enemyScript.currentHealth = newMaxHealth;
            enemyScript.xpValue = newXPValue;
            
            Debug.Log($"Spawned {enemyScript.enemyType} enemy at level {enemyLevel} " +
                     $"(Target: {targetEnemyLevel}) with {newMaxHealth} HP and {newXPValue} XP");
        }
        else
        {
            Debug.LogWarning("Spawned enemy is missing Enemy component!");
        }

        currentEnemyCount++;
        if (isFixedSpawnWave)
        {
            totalEnemiesSpawned++;
        }
        
        StartCoroutine(WaitForEnemyDestroy(enemy));
    }
    
    private int DetermineEnemyLevel()
    {
        // Normalise ratios if auto-normalise is enabled
        if (levelDistribution.autoNormalise)
        {
            levelDistribution.NormaliseRatios();
        }
        
        float randomValue = Random.value;
        float cumulativeRatio = 0f;
        
        // Check for below level (only if target level > 1)
        if (targetEnemyLevel > 1)
        {
            cumulativeRatio += levelDistribution.belowLevelRatio;
            if (randomValue <= cumulativeRatio)
            {
                return targetEnemyLevel - 1;
            }
        }
        
        // Check for target level
        // If target level is 1, we need to redistribute the below level ratio
        float targetRatio = levelDistribution.targetLevelRatio;
        if (targetEnemyLevel == 1)
        {
            // Add the below level ratio to target level ratio since we can't spawn level 0
            targetRatio += levelDistribution.belowLevelRatio;
        }
        
        cumulativeRatio += targetRatio;
        if (randomValue <= cumulativeRatio)
        {
            return targetEnemyLevel;
        }
        
        // Otherwise, spawn above level
        return targetEnemyLevel + 1;
    }
    
    private GameObject GetRandomEnemyPrefab()
    {
        float roundedMeleeRatio = Mathf.Round(meleeRatio * 100f) / 100f;
        bool spawnMelee = Random.value < roundedMeleeRatio;
        
        if (spawnMelee)
        {
            // Try to use list first, fallback to single prefab
            if (meleeEnemyPrefabs.Count > 0)
            {
                int randomIndex = Random.Range(0, meleeEnemyPrefabs.Count);
                return meleeEnemyPrefabs[randomIndex];
            }
            else if (meleePrefab != null)
            {
                return meleePrefab;
            }
        }
        else
        {
            // Try to use list first, fallback to single prefab
            if (projectileEnemyPrefabs.Count > 0)
            {
                int randomIndex = Random.Range(0, projectileEnemyPrefabs.Count);
                return projectileEnemyPrefabs[randomIndex];
            }
            else if (projectilePrefab != null)
            {
                return projectilePrefab;
            }
        }
        
        return null;
    }

    private IEnumerator WaitForEnemyDestroy(GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }
        currentEnemyCount--;
        
        // Notify wave manager that an enemy was killed
        WaveManager waveManager = WaveManager.Instance;
        if (waveManager != null)
        {
            waveManager.OnEnemyKilled();
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        // Get the current grid bounds
        if (!GetCurrentGridBounds())
            return Vector2.zero;
            
        // Get camera bounds in world space
        Bounds cameraBounds = GetCameraBounds();
        
        // Try multiple attempts to find a valid spawn position
        int maxAttempts = 50;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random distance between min and max radius
            float spawnDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 potentialSpawnPos = (Vector2)player.position + randomDirection * spawnDistance;
            
            // Check if position is within current grid bounds
            if (!currentGridBounds.Contains(potentialSpawnPos))
                continue;
                
            // Check if position is off-screen (outside camera bounds)
            if (IsOffScreen(potentialSpawnPos, cameraBounds))
            {
                return potentialSpawnPos;
            }
        }
        
        // If we couldn't find a valid position after all attempts, return zero
        Debug.LogWarning("Could not find valid off-screen spawn position within grid bounds");
        return Vector2.zero;
    }
    
    private bool GetCurrentGridBounds()
    {
        // Cast a ray downward from player position to find the current grid object
        RaycastHit2D hit = Physics2D.Raycast(player.position, Vector2.down, 0.1f, gridLayerMask);
        
        if (hit.collider != null)
        {
            // Get bounds from the collider
            currentGridBounds = hit.collider.bounds;
            return true;
        }
        
        // If no hit, try using OverlapPoint
        Collider2D gridCollider = Physics2D.OverlapPoint(player.position, gridLayerMask);
        if (gridCollider != null)
        {
            currentGridBounds = gridCollider.bounds;
            return true;
        }
        
        Debug.LogWarning("Could not find grid object under player");
        return false;
    }
    
    private Bounds GetCameraBounds()
    {
        if (mainCamera == null)
            return new Bounds();
            
        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;
        
        Vector3 center = mainCamera.transform.position;
        return new Bounds(center, new Vector3(width, height, 0));
    }
    
    private bool IsOffScreen(Vector2 position, Bounds cameraBounds)
    {
        return !cameraBounds.Contains(position);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;
            
        // Draw spawn radius range
        Gizmos.color = Color.red;
        
        // Draw minimum radius circle (thin)
        DrawCircle(player.position, minSpawnRadius, 64);
        
        // Draw maximum radius circle (thin)
        Gizmos.color = new Color(1f, 0f, 0f, 0.7f); // Slightly transparent red
        DrawCircle(player.position, maxSpawnRadius, 64);
        
        // Draw current grid bounds if available
        if (Application.isPlaying && GetCurrentGridBounds())
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Transparent green
            Gizmos.DrawCube(currentGridBounds.center, currentGridBounds.size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(currentGridBounds.center, currentGridBounds.size);
        }
        
        // Draw camera bounds if available
        if (Application.isPlaying && mainCamera != null)
        {
            Bounds camBounds = GetCameraBounds();
            Gizmos.color = new Color(0f, 0f, 1f, 0.2f); // Transparent blue
            Gizmos.DrawCube(camBounds.center, camBounds.size);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(camBounds.center, camBounds.size);
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    // Inspector validation
    private void OnValidate()
    {
        // Ensure target level is at least 1
        targetEnemyLevel = Mathf.Max(1, targetEnemyLevel);
        
        // Ensure base values are reasonable
        baseMaxEnemies = Mathf.Max(1, baseMaxEnemies);
        maxEnemiesIncrement = Mathf.Max(0, maxEnemiesIncrement);
        absoluteMaxEnemies = Mathf.Max(baseMaxEnemies, absoluteMaxEnemies);
        
        // Ensure spawn rate values are reasonable
        baseSpawnRate = Mathf.Max(0.1f, baseSpawnRate);
        playerLevelSpawnReduction = Mathf.Clamp(playerLevelSpawnReduction, 0f, 0.2f);
        gameLevelSpawnReduction = Mathf.Clamp(gameLevelSpawnReduction, 0f, 0.2f);
        
        // Auto-normalise ratios if enabled
        if (levelDistribution != null && levelDistribution.autoNormalise)
        {
            levelDistribution.NormaliseRatios();
        }
    }
}