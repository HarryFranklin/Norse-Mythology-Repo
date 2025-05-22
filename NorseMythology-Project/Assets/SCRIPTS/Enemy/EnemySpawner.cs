using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject meleePrefab;
    public GameObject projectilePrefab;

    [Range(0f, 1f)]
    public float meleeRatio = 0.6f; // 60% chance to spawn melee enemies from 0 to 1
    public float spawnRate = 2f;
    public float spawnRadius = 15f;
    public int maxEnemies = 20;
    public Transform player;

    private int currentEnemyCount = 0;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>().transform;

        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
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

        // Decide type based on ratio
        float roundedMeleeRatio = Mathf.Round(meleeRatio * 100f) / 100f;
        GameObject prefabToSpawn = Random.value < roundedMeleeRatio ? meleePrefab : projectilePrefab;
        GameObject enemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        // Assign target if needed
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.target = player;
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
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        return (Vector2)player.position + randomDirection * spawnRadius;
    }
}
