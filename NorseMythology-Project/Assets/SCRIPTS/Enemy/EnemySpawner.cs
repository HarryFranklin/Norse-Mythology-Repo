using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    // Put enemies in their own parent object in the hierarchy
    // Put enemy projectie under its parent object

    // Put prefabs for melee and projectiles in their own list to have various sprites for each type of enemy.

    public GameObject meleePrefab;
    public GameObject projectilePrefab;

    [Range(0f, 1f)]
    public float meleeRatio = 0.6f;
    public float spawnRate = 2f;
    public float spawnRadius = 15f;
    public int maxEnemies = 20;
    public Transform player;

    private int currentEnemyCount = 0;
    private bool spawningActive = false;
    private Coroutine spawnCoroutine;
    
    // Wave modifiers
    private float currentHealthMultiplier = 1f;
    private float currentXPMultiplier = 1f;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>().transform;
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

        float roundedMeleeRatio = Mathf.Round(meleeRatio * 100f) / 100f;
        GameObject prefabToSpawn = Random.value < roundedMeleeRatio ? meleePrefab : projectilePrefab;
        GameObject enemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

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
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        return (Vector2)player.position + randomDirection * spawnRadius;
    }
}