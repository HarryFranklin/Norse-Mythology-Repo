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
            belowLevelRatio = 0.2f;
            targetLevelRatio = 0.6f;
            aboveLevelRatio = 0.2f;
        }
    }
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning and Prefabs")]
    public Transform enemiesParent;
    public WaveManager waveManager;

    [Tooltip("The pool tags for your different melee enemy prefabs, as defined in the ObjectPooler.")]
    [SerializeField] private List<string> meleeEnemyTags = new List<string>();

    [Tooltip("The pool tags for your different projectile enemy prefabs, as defined in the ObjectPooler.")]
    [SerializeField] private List<string> projectileEnemyTags = new List<string>();

    [Range(0f, 1f)]
    public float meleeRatio = 0.6f;
    
    [Header("Spawn Rate Settings")]
    public float baseSpawnRate = 1f;
    [Range(0f, 0.2f)]
    [Tooltip("Spawn rate reduction per player level (e.g., 0.05 = 5% faster per level)")]
    public float playerLevelSpawnReduction = 0.05f;
    [Range(0f, 0.2f)]
    [Tooltip("Spawn rate reduction per game level (e.g., 0.10 = 10% faster per level)")]
    public float gameLevelSpawnReduction = 0.10f;
    
    [Header("Enemy Level Settings")]
    [SerializeField] private int targetEnemyLevel = 1;
    [SerializeField] private LevelDistribution levelDistribution = new LevelDistribution();
    
    [Header("Spawn Distance")]
    public float minSpawnRadius = 12f;
    public float maxSpawnRadius = 18f;
    
    [Header("Enemy Count Scaling")]
    public int baseMaxEnemies = 15;
    public int maxEnemiesIncrement = 3;
    public int absoluteMaxEnemies = 50;
    
    public Transform player;

    [Header("Grid Detection")]
    public LayerMask gridLayerMask = -1;
    
    private int currentEnemyCount = 0;
    private int currentMaxEnemies = 15;
    private bool spawningActive = false;
    private Coroutine spawnCoroutine;
    
    private float currentHealthMultiplier = 1f;
    private float currentXPMultiplier = 1f;
    
    private int totalEnemiesSpawned = 0;
    private int maxEnemiesToSpawn = 0;
    private bool isFixedSpawnWave = false;
    
    private Camera mainCamera;
    private Bounds currentGridBounds;
    
    private float currentSpawnRate = 1f;

    public static List<Enemy> activeEnemies = new List<Enemy>();

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>().transform;
            
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        SetupEnemiesParent();
        
        if (levelDistribution.autoNormalise)
        {
            levelDistribution.NormaliseRatios();
        }
        
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
        int playerLevel = 1;
        int gameLevel = 1;
        
        if (GameManager.Instance != null)
        {
            PlayerStats playerStats = GameManager.Instance.GetCurrentPlayerStats();
            if (playerStats != null)
            {
                playerLevel = playerStats.level;
            }

            if (waveManager != null)
            {
                gameLevel = waveManager.GetCurrentWave();
            }
        }
        
        float playerLevelMultiplier = 1f + ((playerLevel - 1) * playerLevelSpawnReduction);
        float gameLevelMultiplier = 1f + ((gameLevel - 1) * gameLevelSpawnReduction);
        
        currentSpawnRate = baseSpawnRate * playerLevelMultiplier * gameLevelMultiplier;
    }

    public void StartSpawning()
    {
        UpdateSpawnRate();
        spawningActive = true;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnEnemies());
    }
    
    public void StartFixedSpawning(int totalEnemies)
    {
        UpdateSpawnRate();
        isFixedSpawnWave = true;
        maxEnemiesToSpawn = totalEnemies;
        totalEnemiesSpawned = 0;
        
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
        currentMaxEnemies = Mathf.Min(
            baseMaxEnemies + ((waveNumber - 1) * maxEnemiesIncrement),
            absoluteMaxEnemies
        );
        UpdateSpawnRate();
    }
    
    public void SetTargetEnemyLevel(int level)
    {
        targetEnemyLevel = Mathf.Max(1, level);
    }
    
    public int GetCurrentEnemyCount() => currentEnemyCount;
    
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
            yield return new WaitForSeconds(1f / currentSpawnRate);

            bool shouldSpawn = isFixedSpawnWave ? 
                totalEnemiesSpawned < maxEnemiesToSpawn && currentEnemyCount < currentMaxEnemies : 
                currentEnemyCount < currentMaxEnemies;
            
            if (shouldSpawn)
            {
                SpawnEnemy();
            }
            else if (isFixedSpawnWave && totalEnemiesSpawned >= maxEnemiesToSpawn)
            {
                break;
            }
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPos = GetRandomSpawnPosition();
        if (spawnPos == Vector2.zero) return;

        string enemyTagToSpawn;
        bool spawnMelee = Random.value < meleeRatio;

        if (spawnMelee)
        {
            if (meleeEnemyTags.Count == 0) { Debug.LogWarning("Melee enemy tags list is empty!"); return; }
            enemyTagToSpawn = meleeEnemyTags[Random.Range(0, meleeEnemyTags.Count)];
        }
        else
        {
            if (projectileEnemyTags.Count == 0) { Debug.LogWarning("Projectile enemy tags list is empty!"); return; }
            enemyTagToSpawn = projectileEnemyTags[Random.Range(0, projectileEnemyTags.Count)];
        }
        
        GameObject enemyGO = ObjectPooler.Instance.SpawnFromPool(enemyTagToSpawn, spawnPos, Quaternion.identity);
        if (enemyGO == null) return;
        
        enemyGO.transform.SetParent(enemiesParent);
        
        Enemy enemyScript = enemyGO.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            // Manually link the spawner so the enemy knows who to notify on death
            enemyScript.spawner = this; 
            
            enemyScript.target = player;
            if (!activeEnemies.Contains(enemyScript)) activeEnemies.Add(enemyScript);

            int enemyLevel = DetermineEnemyLevel();
            enemyScript.SetLevel(enemyLevel);

            float newMaxHealth = Mathf.Round(enemyScript.maxHealth * currentHealthMultiplier);
            float newXPValue = Mathf.Round(enemyScript.xpValue * currentXPMultiplier);

            enemyScript.maxHealth = newMaxHealth;
            enemyScript.currentHealth = newMaxHealth;
            enemyScript.xpValue = newXPValue;
        }

        currentEnemyCount++;
        if (isFixedSpawnWave)
        {
            totalEnemiesSpawned++;
        }
    }

    public void EnemyDied(Enemy enemyScript)
    {
        currentEnemyCount--;
        if (enemyScript != null)
        {
            activeEnemies.Remove(enemyScript);
        }

        waveManager.OnEnemyKilled();
    }
    
    private int DetermineEnemyLevel()
    {
        if (levelDistribution.autoNormalise) levelDistribution.NormaliseRatios();
        
        float randomValue = Random.value;
        if (targetEnemyLevel > 1 && randomValue < levelDistribution.belowLevelRatio)
        {
            return targetEnemyLevel - 1;
        }
        
        float targetRatio = targetEnemyLevel == 1 ? 
            levelDistribution.targetLevelRatio + levelDistribution.belowLevelRatio : 
            levelDistribution.targetLevelRatio;
            
        if (randomValue < levelDistribution.belowLevelRatio + targetRatio)
        {
            return targetEnemyLevel;
        }
        
        return targetEnemyLevel + 1;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        if (!GetCurrentGridBounds()) return Vector2.zero;
        Bounds cameraBounds = GetCameraBounds();
        
        for (int i = 0; i < 50; i++)
        {
            float spawnDistance = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector2 potentialSpawnPos = (Vector2)player.position + (Random.insideUnitCircle.normalized * spawnDistance);
            
            if (currentGridBounds.Contains(potentialSpawnPos) && !cameraBounds.Contains(potentialSpawnPos))
            {
                return potentialSpawnPos;
            }
        }
        
        Debug.LogWarning("Could not find valid off-screen spawn position within grid bounds");
        return Vector2.zero;
    }
    
    private bool GetCurrentGridBounds()
    {
        Collider2D gridCollider = Physics2D.OverlapPoint(player.position, gridLayerMask);
        if (gridCollider != null)
        {
            currentGridBounds = gridCollider.bounds;
            return true;
        }
        return false;
    }
    
    private Bounds GetCameraBounds()
    {
        if (mainCamera == null) return new Bounds();
        float height = mainCamera.orthographicSize * 2f;
        float width = height * mainCamera.aspect;
        return new Bounds(mainCamera.transform.position, new Vector3(width, height, 0));
    }
}