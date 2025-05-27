using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public Transform enemiesParent; // Parent object to store all spawned enemies
    public GameObject meleePrefab; // Replace later with prefabs for various enemies
    public GameObject projectilePrefab; // Replace later with prefabs for various enemies

    [Range(0f, 1f)]
    public float meleeRatio = 0.6f;
    public float spawnRate = 2f; // Per second
    
    [Header("Spawn Distance")]
    public float minSpawnRadius = 12f; // Just to add some variation
    public float maxSpawnRadius = 18f;
    
    public int maxEnemies = 20; // Add functionality to increase this later with wave count
    public Transform player;

    [Header("Grid Detection")]
    public LayerMask gridLayerMask = -1; // Which layers to consider as grid objects
    
    private int currentEnemyCount = 0;
    private bool spawningActive = false;
    private Coroutine spawnCoroutine;
    
    // Wave modifiers
    private float currentHealthMultiplier = 1f;
    private float currentXPMultiplier = 1f;
    
    // Camera bounds for off-screen detection
    private Camera mainCamera;
    private Bounds currentGridBounds;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>().transform;
            
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        // Ensure we have an enemies parent object
        SetupEnemiesParent();
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

    public void StartSpawning()
    {
        spawningActive = true;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnEnemies());
    }
    
    public void StopSpawning()
    {
        spawningActive = false;
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

    private IEnumerator SpawnEnemies()
    {
        while (spawningActive)
        {
            yield return new WaitForSeconds(1f / spawnRate);

            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        
        // If we can't find a valid spawn position, skip this spawn attempt
        if (spawnPos == Vector2.zero)
            return;

        float roundedMeleeRatio = Mathf.Round(meleeRatio * 100f) / 100f;
        GameObject prefabToSpawn = Random.value < roundedMeleeRatio ? meleePrefab : projectilePrefab;
        GameObject enemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, enemiesParent);

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.target = player;
            
            // Apply wave scaling
            float newMaxHealth = Mathf.Round(enemyScript.maxHealth * currentHealthMultiplier);
            float newXPValue = Mathf.Round(enemyScript.xpValue * currentXPMultiplier);
            
            enemyScript.maxHealth = newMaxHealth;
            enemyScript.currentHealth = newMaxHealth;
            enemyScript.xpValue = newXPValue;
        }
        else
        {
            Debug.LogWarning("Spawned enemy is missing Enemy component!");
        }

        currentEnemyCount++;
        StartCoroutine(WaitForEnemyDestroy(enemy));
    }

    private IEnumerator WaitForEnemyDestroy(GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }
        currentEnemyCount--;
        
        // Notify wave manager that an enemy was killed
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
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
}